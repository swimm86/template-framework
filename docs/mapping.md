# Mapping

**Assembly:**
- `Shared.Domain.Core.dll` — провайдеро-независимые абстракции (интерфейсы, базовый профиль, конвертеры типов).
- `Shared.Infrastructure.Mapper.Core.dll` — общая инфраструктура слоя маппинга (DI-база, scope, profile builder).
- `Shared.Infrastructure.Mapper.AutoMapper.dll` — реализация поверх **AutoMapper 14.0.0**.
- `Shared.Infrastructure.Mapper.Mapster.dll` — реализация поверх **Mapster**.

**Namespace:**
- `Shared.Domain.Core.Mapping`, `Shared.Domain.Core.Mapping.Interfaces`.
- `Shared.Domain.Core.Mapping.Expression`, `Shared.Domain.Core.Mapping.Expression.Interfaces`.
- `Shared.Domain.Core.Mapping.Description`, `Shared.Domain.Core.Mapping.Description.Interfaces` *(internal)*.
- `Shared.Domain.Core.Mapping.Extensions`.
- `Shared.Infrastructure.Mapper.Core`, `.Scope`, `.ProfileBuilder`, `.ProfileBuilder.Interfaces`, `.DependencyInjection`.
- `Shared.Infrastructure.Mapper.AutoMapper`, `.DependencyInjection`, `.Adapters.*`, `.Adapters.Applicators.*`, `.Adapters.ExpressionTransformers.*`, `.Adapters.Helpers`, `.Adapters.Wrappers`.
- `Shared.Infrastructure.Mapper.Mapster`, `.DependencyInjection`, `.Adapters.*`, `.Adapters.Applicators.*`, `.Adapters.ExpressionTransformers.*`, `.Adapters.Helpers`, `.Adapters.Wrappers`, `.Utils`.

**Исходники:**
- `src/Shared/Core/Shared.Domain.Core/Mapping/`.
- `src/Shared/Mapper/Shared.Infrastructure.Mapper.Core/`.
- `src/Shared/Mapper/Shared.Infrastructure.Mapper.AutoMapper/`.
- `src/Shared/Mapper/Shared.Infrastructure.Mapper.Mapster/`.

**Lifetime:** `IMapper` регистрируется в DI как **Singleton** (потокобезопасная обёртка над `global::AutoMapper.IMapper` либо `MapsterMapper.IMapper`).

---

## 1. Обзор и философия

Слой маппинга в Shared Framework предоставляет **провайдеро-независимый** API над популярными библиотеками преобразования объектов. В качестве конкретного провайдера на сегодня поддерживаются AutoMapper и Mapster; адаптер любой другой библиотеки может быть добавлен без правки доменного и прикладного кода.

Цели, ради которых введён этот слой:

- **Заменяемость реализации.** Доменный и прикладной слои зависят только от `IMapper` (из `Shared.Domain.Core`). Выбор между AutoMapper и Mapster — решение инфраструктурного слоя конкретного сервиса.
- **Один контракт, несколько реализаций.** Один и тот же набор `MappingProfileBase`-наследников сначала описывает преобразования декларативно, а затем применяется выбранным адаптером (см. § 3).
- **Оптимизированная `ProjectTo`-проекция.** Поддерживается проекция `IQueryable` в DTO, выполняемая на стороне СУБД (см. § 13).
- **Diff-merge коллекций.** `ConfigureCollection` синхронизирует дочерние коллекции сущностей по ключу без N+1 (см. § 8).
- **Изоляция адаптеров.** Конкретные библиотеки (AutoMapper, Mapster) упоминаются только в проектах `Shared.Infrastructure.Mapper.AutoMapper` и `Shared.Infrastructure.Mapper.Mapster`. `Shared.Domain.Core` от них свободен (Dependency Rule).
- **Программный fluent API** с поддержкой `ForMember`, `ForCtorParam`, `ConstructUsing`, `ConvertUsing`, `ReverseMap`, `IncludeBase`, `BeforeMap`, `AfterMap`.

> **Принцип.** Сценарий «домен говорит `IMapper`, инфраструктура подсовывает реализацию» — это Clean Architecture в действии: зависимости направлены внутрь, к Domain.

---

## 2. Архитектура

```
                    ┌─────────────────────────────────────────────┐
                    │ Application / Domain                        │
                    │                                             │
                    │   IMapper   ITypeConverter<,>               │
                    │   MappingProfileBase                        │
                    │   ResolutionContext                         │
                    └───────────────┬─────────────────────────────┘
                                    │ depends on
                                    ▼
                    ┌─────────────────────────────────────────────┐
                    │ Shared.Infrastructure.Mapper.Core           │
                    │                                             │
                    │   DependencyInjectorBase<TConfig, TMapper>  │
                    │   MapperProfileBuilderBase                  │
                    │   MapperContextScope / Accessor / NullMapper│
                    └───────────────┬─────────────────────────────┘
                                    │  ← only this layer knows about specific libs
                ┌───────────────────┴───────────────────┐
                ▼                                       ▼
┌───────────────────────────────┐       ┌───────────────────────────────┐
│ Shared.Infrastructure.        │       │ Shared.Infrastructure.        │
│   Mapper.AutoMapper           │       │   Mapper.Mapster              │
│                               │       │                               │
│  AdapterProfile               │       │  TypeAdapterConfig            │
│  AutoMapperProfileBuilder     │       │  MapsterProfileBuilder        │
│  Mapper (wraps AM IMapper)    │       │  Mapper (wraps M IMapper)     │
│  Applicators × 6              │       │  Applicators × 6              │
└───────────────────────────────┘       └───────────────────────────────┘
                ▲                                       ▲
                │               выбирается ОДИН          │
                └───────────────┬────────────────────────┘
                                │
                ┌───────────────┴───────────────────┐
                │ AddReferencedDependencyInjectors  |
                │ (Shared.Infrastructure.Core)      │
                │                                   │
                │ Сканирует все сборки → находит    │
                │ наследников DependencyInjectorBase|
                │ → Execute() → IMapper Singleton   |
                └───────────────────────────────────┘
```

**Ключевые правила:**

- `Shared.Domain.Core` не имеет зависимостей на AutoMapper и Mapster — только BCL.
- `Shared.Infrastructure.Mapper.Core` не имеет зависимостей на AutoMapper и Mapster — это общий каркас.
- Конкретные провайдеры знают про свой пакет (`AutoMapper` 14.0.0 либо `Mapster`) и реализуют абстракции `MapperProfileBuilderBase` и `IDescriptionApplicator`.
- В рантайме зарегистрирован **ровно один** `IMapper`. Подключение обоих провайдеров одновременно приведёт к конфликту DI (см. § 11).

### Контракт `Domain ↔ Adapter` через `InternalsVisibleTo`

`Shared.Domain.Core` экспортирует **internal**-API для адаптеров — это часть архитектурного контракта, а не утечка инкапсуляции:

- `IMapDescription`, `IMemberMapDescription`, `ICtorParamMapDescription`, `MapDescription<T1,T2>`, `MemberMapDescription`, `CtorParamMapDescription`, `MappingProfileBase.GetMapDescriptions`, `MappingProfileBase.RegisterDescription`, `IMappingProfile`.
- Доступ открыт через `InternalsVisibleTo` к `Shared.Infrastructure.Mapper.AutoMapper`, `Shared.Infrastructure.Mapster.Mapster`, `Shared.Infrastructure.Mapper.Core` (см. `Shared.Domain.Core.csproj`).

Изменение любого из этих членов — это **breaking change** для адаптеров и требует синхронного обновления всех провайдеров. Внешний (public) контракт Domain — `IMapper`, `MappingProfileBase`, `MappingProfileExtensions`, `ResolutionContext` — от провайдера не зависит.

---

## 3. IMapper Interface

`IMapper` — единственный контракт, через который прикладной код общается с системой маппинга. Определён в `Shared.Domain.Core.Mapping.Interfaces`.

```csharp
namespace Shared.Domain.Core.Mapping.Interfaces;

public interface IMapper
{
    TResult Map<TSource, TResult>(TSource source);

    IQueryable<TResult> ProjectTo<TResult>(
        IQueryable source,
        object? parameters = null);

    void Map<TSource, TResult>(TSource source, TResult result);
}
```

### Методы

| Метод | Описание | Возвращает |
|-------|----------|------------|
| `Map<TSource, TResult>(source)` | Преобразование в новый экземпляр | `TResult` |
| `Map<TSource, TResult>(source, result)` | Преобразование в существующий экземпляр (in-place) | `void` |
| `ProjectTo<TResult>(source, parameters?)` | Проекция `IQueryable` в `IQueryable<TResult>`. Параметры доступны в `MapFrom((src, p) => ...)` | `IQueryable<TResult>` |

> Сигнатура `ProjectTo<TResult>(IQueryable source, object? parameters = null)` принимает необязательный словарь/объект runtime-параметров. Эти параметры доступны в параметризованных выражениях `ForMember.MapFrom((src, p) => ...)` (см. § 7 и § 13).

### Пример из handler'а

`PersonCreateCommandHandler` не переопределяет `ProcessEntityAsync` явно — базовый `CreateCommandHandler<TCommand, TRequest, TEntity, TResponsePayload, TResponse>` использует `IMapper` сам:

```csharp
public class PersonCreateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Domain.Entities.Person>> validators,
    IUserProvider userProvider)
    : CreateCommandHandler<
        PersonCreateCommand,
        PersonCreateRequest,
        Domain.Entities.Person,
        PersonDto,
        PersonCreateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider);
```

Внутри `CreateCommandHandler.CreateAsync` (`Shared.Application.Cqrs.Core`) ровно две точки вызова `IMapper`:

```csharp
var entity = mapper.Map<TRequest, TEntity>(command.Request);  // 1) DTO → сущность
...
return CreateResponseDto(newEntity);  // внутри: Payload = mapper.Map<TEntity, TResponsePayload>(entity)
```

Handler ничего не знает о провайдере. Этот же handler работает и в AutoMapper-конфигурации, и в Mapster-конфигурации без изменений.

### Пример из Update-handler'а (in-place)

```csharp
public class PersonUpdateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Person>> validators,
    IUserProvider userProvider)
    : UpdateCommandHandler<PersonUpdateCommand, PersonUpdateRequest, Person, PersonDto, PersonUpdateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    protected override bool WithTracking => true;

    protected override Task ProcessEntityAsync(Person entity, PersonUpdateCommand command)
    {
        // Копирование полей из DTO в уже отслеживаемую сущность
        mapper.Map(command.Request, entity);
        return Task.CompletedTask;
    }
}
```

`mapper.Map(src, dest)` важен, когда EF Core уже загрузил сущность в `ChangeTracker` — замена ссылки на новый объект вырвала бы её из графа, а in-place-обновление сохраняет трекинг.

---

## 4. MapperExtensions

Удобные методы-расширения над `IMapper` объявлены в `Shared.Domain.Core.Mapping.Extensions`. Позволяют писать в fluent-стиле без явного `IMapper`-аргумента в каждом вызове.

```csharp
public static class MapperExtensions
{
    public static TResult Map<TSource, TResult>(this TSource source, IMapper mapper);
    public static IQueryable<TResult> ProjectTo<TResult>(
        this IQueryable source,
        IMapper mapper,
        object? parameters = null);
    public static void Map<TSource, TResult>(this TSource source, TResult result, IMapper mapper);
}
```

### Примеры

```csharp
// Преобразование в новый объект
var dto = entity.Map<Person, PersonDto>(mapper);

// Проекция IQueryable (parameters — необязательный)
var dtos = query
    .ProjectTo<PersonDto>(mapper, parameters: new { Culture = "ru-RU" })
    .ToList();

// In-place обновление
source.Map(target, mapper);
```

> Расширения колонок описываются декларативно в `MappingProfileBase` через `ForMember(opt => opt.MapFrom(...))` (см. § 7), а runtime-параметры передаются через необязательный аргумент `parameters` (см. § 13).

---

## 5. ITypeConverter и ResolutionContext

Эта связка — главный механизм для **полностью кастомного** маппинга, когда декларативного `ForMember` уже недостаточно: нужны lookup-сервисы, фабрики, перекрёстные проверки, нестандартные сценарии жизненного цикла целевого объекта.

### 5.1. Контракт `ITypeConverter<TSource, TDestination>`

`ITypeConverter<,>` — это провайдеро-независимый аналог `AutoMapper.ITypeConverter<,>`. Описывает один метод `Convert`:

```csharp
public interface ITypeConverter<in TSource, TDestination>
{
    TDestination Convert(TSource source, TDestination destination, ResolutionContext context);
}
```

- `source` — входной объект.
- `destination` — существующий целевой объект, если маппинг выполняется in-place (для обновления сущности), либо `default`/пустой при первичном создании. Конвертер сам решает, как его обрабатывать.
- `context` — провайдеро-независимый `ResolutionContext` (см. § 5.2): позволяет делать вложенные `Map<>`-вызовы, не зная о провайдере.

Регистрация конвертера — через `IMappingExpression.ConvertUsing(converter)`:

```csharp
public interface IMappingExpression<TSource, TDestination>
{
    ...
    IMappingExpression<TSource, TDestination> ConvertUsing(
        ITypeConverter<TSource, TDestination> converter);
    ...
}
```

Конвертер типов и DI-контейнер: `ITypeConverter<,>` — обычный POCO с конструктором. Если конвертеру нужны зависимости (репозиторий, фабрика, текущий пользователь), их следует получать через DI — конвертер регистрируется в `IServiceCollection` как `Transient`/`Scoped` (в зависимости от требований) и адаптер достаёт его из контейнера при вызове `ConvertUsing(...)`.

> **Важно.** `ITypeConverter<,>` объявлен в `Shared.Domain.Core` и не имеет зависимости ни на AutoMapper, ни на Mapster. Адаптер каждого провайдера самостоятельно оборачивает его в нативный объект (`TypeConverterAdapter<,>`) перед передачей провайдеру.

### 5.2. Контракт `ResolutionContext`

`ResolutionContext` — обёртка над `IMapper`, передаваемая в `ITypeConverter.Convert`. Скрывает тот факт, что под капотом — AutoMapper либо Mapster.

```csharp
public class ResolutionContext(IMapper mapper)
{
    public TDestination Map<TDestination>(object source);
    public TDestination Map<TSource, TDestination>(TSource source);
    public void Map<TSource, TDestination>(TSource source, TDestination destination);
}
```

Смысл методов:

| Метод | Назначение |
|-------|-----------|
| `Map<TDest>(object source)` | Удобная перегрузка, когда тип источника известен только как `object`. |
| `Map<TSrc, TDest>(TSrc source)` | Создание нового целевого экземпляра через вложенный `IMapper`. |
| `Map<TSrc, TDest>(TSrc source, TDest destination)` | In-place обновление существующего целевого объекта через вложенный `IMapper`. |

Как это работает «под капотом»:

- Обёртка `Mapper` (и AutoMapper-, и Mapster-реализация) перед каждым `Map`/`ProjectTo`/`Map(in-place)` создаёт `MapperContextScope` и кладёт `this` в `MapperContextAccessor.Current` (статический `AsyncLocal<IMapper?>`).
- Внутри `ITypeConverter` адаптер (`TypeConverterAdapter<,>`) создаёт `new ResolutionContext(MapperContextAccessor.Current)`.
- Все три метода `ResolutionContext` делегируют работу этому сохранённому `IMapper`.

Это даёт два важных свойства:

1. **Конвертер остаётся провайдеро-независимым** — он работает с абстрактным `IMapper` и `ResolutionContext`, а не с `AutoMapper.IMapper` / `global::AutoMapper.ResolutionContext` / `MapContext`.
2. **Вложенные `Map`-вызовы корректно профилируются и логируются** — все они идут через тот же `IMapper`, что и внешний вызов.

### 5.3. Пример A — converter с фабрикой и демаппингом

Задача: при обновлении заказа нужно создать или переиспользовать позиции заказа по `ProductId`, и заполнить их поля из DTO. Декларативно через `ForMember` это неудобно, потому что для коллекций сущностей нужно уметь различать «новая позиция» и «существующая».

```csharp
// --- DTO и сущности ---

public sealed record OrderItemDto(Guid ProductId, int Quantity, decimal Price);

public sealed class OrderItem : IEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public sealed class Order : IEntity<Guid>
{
    public Guid Id { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

// --- Конвертер ---

public sealed class OrderItemConverter
    : ITypeConverter<OrderItemDto, OrderItem>
{
    public OrderItem Convert(OrderItemDto source, OrderItem destination, ResolutionContext context)
    {
        // dest == default → создаём новую позицию через фабрику домена
        // dest != default → демаппим поля DTO в существующий экземпляр (трекаемый EF Core)
        if (destination is null)
        {
            destination = OrderItem.CreateNew(source.ProductId, source.Quantity, source.Price);
        }
        else
        {
            // через контекст маппинга — копируем скалярные поля
            // (OrderItemDto → OrderItem, простой случай)
            context.Map(source, destination);
        }

        return destination;
    }
}
```

Регистрация — в `MappingProfileBase`:

```csharp
public class OrderProfile : MappingProfileBase
{
    public OrderProfile()
    {
        // Простой скалярный маппинг внутри конвертера
        CreateMap<OrderItemDto, OrderItem>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.OrderId, opt => opt.Ignore());

        // Конвертер-фабрика для тех случаев, когда нужно выбирать между new / reuse
        CreateMap<OrderItemDto, OrderItem>()
            .ConvertUsing<OrderItemConverter>();
    }
}
```

> **Примечание.** Два `CreateMap<OrderItemDto, OrderItem>` в одном профиле — корректный приём: первое описание используется адаптером при `context.Map(source, destination)` (in-place), второе — при `ConvertUsing(...)` (полное создание или полный пересчёт). Альтернативно, оба поведения можно уместить в одном `CreateMap`, если конвертер сам решает, что делать с `destination`.

> **Почему так, а не `ConstructUsing`.** `ConstructUsing` применяется **до** сопоставления полей, и AutoMapper/Mapster всё равно вызовут свои сопоставления после. `ConvertUsing` же **полностью подменяет** сопоставление: провайдер ничего не делает помимо делегирования в `ITypeConverter`. Это единственный способ надёжно реализовать сценарий «либо создать новый, либо обновить существующий».

### 5.4. Пример B — converter с DI-зависимостями

Задача: при маппинге `AddressDto → Address` подтянуть `Country` из справочника `ICountryRepository`, и если такой страны нет — вернуть ошибку валидации.

```csharp
public sealed class AddressConverter(
    ICountryRepository countryRepository,
    ILogger<AddressConverter> logger)
    : ITypeConverter<AddressDto, Address>
{
    public Address Convert(AddressDto source, Address destination, ResolutionContext context)
    {
        var country = countryRepository.GetByCodeAsync(source.CountryCode)
            .GetAwaiter().GetResult();

        if (country is null)
        {
            logger.LogWarning("Unknown country code: {Code}", source.CountryCode);
            throw new ValidationException($"Unknown country code: {source.CountryCode}");
        }

        destination ??= new Address { Id = Guid.NewGuid() };

        // Простые поля — копируем через контекст маппинга
        context.Map(source, destination);
        destination.Country = country;

        return destination;
    }
}
```

Регистрация в `MappingProfileBase`:

```csharp
public class AddressProfile : MappingProfileBase
{
    public AddressProfile()
    {
        CreateMap<AddressDto, Address>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Country, opt => opt.Ignore());

        CreateMap<AddressDto, Address>()
            .ConvertUsing<AddressConverter>();
    }
}
```

Регистрация в `Program.cs` (обязательно, иначе DI не разрешит `AddressConverter`):

```csharp
builder.Services.AddTransient<AddressConverter>();
```

### 5.5. Когда НЕ использовать `ITypeConverter`

| Сценарий | Что использовать |
|----------|------------------|
| Простое плоское копирование полей | `CreateMap` без дополнительной конфигурации |
| Переименование или вычисляемое поле | `ForMember(d => d.X, opt => opt.MapFrom(s => ...))` |
| Поле берётся из конструктора, а не из сеттера | `ForCtorParam("name", opt => opt.MapFrom(s => ...))` |
| Создание сущности через фабричный метод безусловно | `ConstructUsing(s => MyEntity.Create(...))` |
| Декларативное правило на коллекцию | `ConfigureCollection()` или `ForMember` на коллекцию |
| Требуется lookup в БД, валидация, выбор new/reuse | `ITypeConverter<,>` |

**Принцип:** сначала декларативно (`ForMember`, `ConstructUsing`, `ConfigureCollection`), и только когда декларативность упёрлась — `ITypeConverter`.

---

## 6. MappingProfileBase

`MappingProfileBase` — абстрактный базовый класс, от которого наследуются все профили маппинга в прикладном коде. Заменяет ранее использовавшийся `AutoMapper.Profile` в качестве базы.

```csharp
namespace Shared.Domain.Core.Mapping;

public abstract class MappingProfileBase
    : IMappingProfile
{
    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
}
```

- Наследник объявляет преобразования в конструкторе через `CreateMap<,>()`.
- `CreateMap<,>()` возвращает `IMappingExpression<TSource, TDestination>` — fluent-цепочку для конфигурации.
- Внутри базовый класс накапливает описания (`IMapDescription`) в собственной коллекции; при инициализации DI-контейнера `DependencyInjectorBase` собирает все `MappingProfileBase`-наследники из загруженных сборок и передаёт их адаптеру выбранного провайдера.

### 6.1. Fluent API `IMappingExpression<TSource, TDestination>`

| Метод | Назначение |
|-------|-----------|
| `ForMember<TMember>(dest, opt => opt.MapFrom(src) | opt.MapFrom((src, p) => ...) | opt.Ignore())` | Настройка отдельного свойства |
| `ForCtorParam(name, opt => opt.MapFrom(src))` | Настройка параметра конструктора целевого типа |
| `BeforeMap(action)`, `AfterMap(action)` | Действия до/после маппинга |
| `ConstructUsing(Func<TSource, TDestination>)` | Фабрика целевого объекта |
| `ConstructUsing(Func<TSource, ResolutionContext, TDestination>)` | Фабрика с доступом к контексту |
| `ConvertUsing(ITypeConverter<,>)` | Полная подмена маппинга конвертером типов |
| `ConvertUsing(Func<TSource, TDestination>)` | Лямбда-конвертер (без `dest`) |
| `ConvertUsing(Func<TSource, TDestination, ResolutionContext, TDestination>)` | Лямбда-конвертер (с `dest` и контекстом) |
| `ReverseMap()` | Регистрация обратного преобразования |
| `IncludeBase<TSourceBase, TDestBase>()` | Наследование правил базового маппинга |

### 6.2. Полный пример

```csharp
using Shared.Domain.Core.Mapping;
using Template.Application.Dto.Person;
using Template.Domain.Entities;

namespace Template.Application.Mapping;

public class MapperProfile : MappingProfileBase
{
    public MapperProfile()
    {
        // DTO → Entity
        CreateMap<PersonDto, Person>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email));

        // Entity → DTO
        CreateMap<Person, PersonDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email));
    }
}
```

### 6.3. Пример с `ConstructUsing` (фабрика домена)

```csharp
public class MapperProfile : MappingProfileBase
{
    public MapperProfile()
    {
        CreateMap<PersonCreateRequest, Person>()
            .ConstructUsing(src => Person.Create(src.Name, src.Email));
    }
}
```

`Person.Create(name, email)` — статический фабричный метод из доменной сущности. `ConstructUsing` вызывается **до** заполнения остальных полей, поэтому AutoMapper/Mapster не будут пытаться создать `Person` через конструктор по умолчанию.

### 6.4. Пример с `IncludeBase` (наследование DTO)

В `Template.Getter.Application`:

```csharp
public class MapperProfile : MappingProfileBase
{
    public MapperProfile()
    {
        // PersonListPayload наследует PersonDto
        CreateMap<Person, PersonListPayload>()
            .IncludeBase<Person, PersonDto>();
    }
}
```

Без `IncludeBase` маппер не знал бы, что поля `PersonListPayload` нужно брать так же, как у `PersonDto`. `IncludeBase` ссылается на ранее зарегистрированное преобразование `Person → PersonDto`.

### 6.5. Сравнение со старым `AutoMapper.Profile`

| Раньше | Сейчас |
|--------|--------|
| Базовый класс — `AutoMapper.Profile` | Базовый класс — `MappingProfileBase` (наш, провайдеро-независимый) |
| `CreateMap` возвращал `IMappingExpression<,>` AutoMapper | `CreateMap` возвращает `IMappingExpression<,>` из `Shared.Domain.Core` |
| `ConvertUsing` принимал `ITypeConverter<,>` AutoMapper | `ConvertUsing` принимает `ITypeConverter<,>` из `Shared.Domain.Core` |
| `ForMember(opt => opt.MapFrom(...))` | То же, но через `IMemberConfigurationExpression` из `Shared.Domain.Core` |
| `ConfigureCollection` расширял `Profile` и применялся на `this` | `ConfigureCollection` расширяет `IMappingExpression<ICollection<...>, ICollection<...>>` и применяется на `CreateMap` |
| Один профиль мог подключаться только в AutoMapper-конфиг | Один и тот же `MappingProfileBase` собирается и AutoMapper-, и Mapster-адаптером |

> **Что осталось без изменений.** Сигнатуры `CreateMap`, `ForMember`, `ConstructUsing`, `ConvertUsing`, `ReverseMap`, `IncludeBase` максимально близки к AutoMapper — это упрощает миграцию существующих профилей.

### 6.6. `IMemberConfigurationExpression`

```csharp
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
    void MapFrom<TSourceMember>(Expression<Func<TSource, IDictionary<string, object?>, TSourceMember>> sourceMember);
    void Ignore();
}
```

- **Обычная перегрузка** `MapFrom(src => ...)` — самое частое использование: `opt.MapFrom(s => s.Name)`.
- **Параметризованная перегрузка** `MapFrom((src, p) => ...)` — используется при `ProjectTo(..., parameters: ...)`. В теле лямбды доступен словарь `p` с runtime-параметрами, провайдер корректно превращает такой expression в SQL-проекцию.
- **`Ignore()`** — исключает свойство из маппинга; значение в целевом объекте остаётся без изменений (важно для in-place обновлений трекаемых сущностей).

### 6.7. `ICtorParamConfigurationExpression`

```csharp
public interface ICtorParamConfigurationExpression<TSource>
{
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
}
```

Используется, когда целевой тип имеет неизменяемые свойства, задаваемые только через конструктор.

```csharp
public class Person : IEntity<Guid>
{
    public Person(string name, string email)
    {
        Name = name;
        Email = email;
    }
    public string Name { get; }
    public string Email { get; }
    public Guid Id { get; set; }
}

public class MapperProfile : MappingProfileBase
{
    public MapperProfile()
    {
        CreateMap<PersonCreateRequest, Person>()
            .ForCtorParam("name", opt => opt.MapFrom(s => s.Name))
            .ForCtorParam("email", opt => opt.MapFrom(s => s.Email));
    }
}
```

---

## 7. ConfigureCollection — diff-merge коллекций

`ConfigureCollection` — это метод-расширение над `IMappingExpression<ICollection<TSource>, ICollection<TDestination>>`, реализующий паттерн **diff-merge**: при маппинге коллекции вычисляется дельта между источником и существующим целевым набором, и к целевому набору применяются только изменения (Add / Update / Remove). Объявлен в `Shared.Domain.Core.Mapping.Extensions`.

Сигнатуры:

```csharp
// Сравнение по IEntity.Id
public static IMappingExpression<ICollection<TSource>, ICollection<TDestination>> ConfigureCollection<TSource, TDestination>(
    this IMappingExpression<ICollection<TSource>, ICollection<TDestination>> mapping)
    where TSource : IEntity
    where TDestination : IEntity;

// Сравнение по произвольным селекторам ключа
public static IMappingExpression<ICollection<TSource>, ICollection<TDestination>> ConfigureCollection<TSource, TDestination>(
    this IMappingExpression<ICollection<TSource>, ICollection<TDestination>> mapping,
    Func<TSource, object> sourceSelector,
    Func<TDestination, object> destinationSelector)
    where TSource : IEntity
    where TDestination : IEntity;
```

Обе перегрузки требуют, чтобы `TSource` и `TDestination` реализовывали `IEntity` (для случая «без селекторов» — потому что используется `IEntity.Id`).

> **Важно.** `ConfigureCollection` — это **расширение** над `MappingExpression` (а не над `Profile`, как было в старой версии). Поэтому оно применяется на `CreateMap<ICollection<...>, ICollection<...>>()`, а не на `this` внутри конструктора профиля.

### 7.1. Алгоритм

Под капотом `ConfigureCollection` делегирует работу `EntityExtensions.GetDifferenceForMerge`:

```
1. Сравнить src и dest по ключу (Id или заданным селекторам):
     toAdd    — есть в src, нет в dest  → создать
     toDelete — есть в dest, нет в src → удалить
     toUpdate — есть в обоих          → обновить поля через Map(srcItem, destItem)

2. Удалить элементы toDelete из dest
3. Для каждой пары (srcItem, destItem) из toUpdate:
     ctx.Map(srcItem, destItem)  // in-place обновление
4. Для каждого newItem из toAdd:
     dest.Add(ctx.Map<TSource, TDestination>(newItem))  // создание нового
```

Схема потока:

```
src (DTO)                     dest (Entity, отслеживаемая EF Core)
   │                                  │
   ├── toAdd (нет в dest) ──► ctx.Map(src) ──► dest.Add(new)
   ├── toUpdate (есть)    ──► ctx.Map(src, dest)  (in-place)
   └── toDelete (нет в src)──► dest.Remove(item)
```

Преимущества:

- **Нет N+1.** Один `Map(srcCollection, destCollection)` обновляет все позиции.
- **Сохраняется Change Tracker EF Core.** Удалённые и обновлённые сущности — те же самые объекты, что и в `DbContext`; их состояние корректно отслеживается, и `SaveChangesAsync` сгенерирует правильные `DELETE`/`UPDATE` SQL.
- **Нет необходимости вручную писать diff-логику** в каждом handler'е.

### 7.2. Пример: `Order → OrderItems` по `ProductId`

```csharp
public class OrderProfile : MappingProfileBase
{
    public OrderProfile()
    {
        // Скалярный маппинг одной позиции (используется внутри ConfigureCollection)
        CreateMap<OrderItemDto, OrderItem>()
            .ForMember(d => d.Id, opt => opt.Ignore())        // не перетираем Id
            .ForMember(d => d.OrderId, opt => opt.Ignore());   // не перетираем внешний ключ

        // Diff-merge по ProductId
        CreateMap<ICollection<OrderItemDto>, ICollection<OrderItem>>()
            .ConfigureCollection(
                sourceSelector: dto => dto.ProductId,
                destinationSelector: entity => entity.ProductId);
    }
}
```

В handler'е обновления заказа:

```csharp
protected override Task ProcessEntityAsync(Order entity, OrderUpdateCommand command)
{
    // Простые поля заказа
    mapper.Map(command.Request, entity);

    // Коллекция: один вызов — diff-merge
    mapper.Map(command.Request.Items, entity.Items);

    return Task.CompletedTask;
}
```

### 7.3. Поведенческие инварианты (из тестов)

Тест `ProfileFeaturesTestBase.ConfigureCollection_AddsUpdatesAndRemovesItems` закрепляет следующее поведение:

| Сценарий | Поведение |
|----------|-----------|
| Элемент есть в src и в dest с тем же `Id` | Поля перезаписаны, **тот же экземпляр** в dest (`BeSameAs`) |
| Элемент есть только в src | Создан новый через `ctx.Map<TSource, TDestination>(newItem)` |
| Элемент есть только в dest | Удалён из dest (`dest.Remove(item)`) |
| Кол-во элементов после `Map` | `|toAdd| + |toUpdate|` (равно `|src|`) |

---

## 8. AutoMapper-реализация

`Shared.Infrastructure.Mapper.AutoMapper` — адаптер, превращающий декларативные описания из `MappingProfileBase` в конфигурацию AutoMapper 14.0.0.

### 8.1. Состав слоя

| Файл | Назначение |
|------|-----------|
| `Mapper.cs` | `internal sealed class Mapper : IMapper` — обёртка над `global::AutoMapper.IMapper` |
| `DependencyInjection/DependencyInjector.cs` | Регистрация AutoMapper-конфигурации в DI |
| `Adapters/AutoMapperProfileBuilder.cs` | Преобразует `IMappingProfile` в `global::AutoMapper.Profile` |
| `Adapters/AdapterProfile.cs` | Минимальный наследник `global::AutoMapper.Profile` для адаптера |
| `Adapters/Applicators/*` (6 файлов) | Применяют секции `IMapDescription` к AutoMapper-выражению |
| `Adapters/ExpressionTransformers/*` | Переписывают параметризованные `MapFrom` под AutoMapper |
| `Adapters/Wrappers/*` | Обёртки для функций и конвертеров |
| `Adapters/TypeConverterAdapter.cs` | Обёртка `ITypeConverter<,>` под `AutoMapper.ITypeConverter<,>` |

### 8.2. `Mapper`

```csharp
internal sealed class Mapper
    : IMapper
{
    private readonly global::AutoMapper.IMapper _inner;

    public Mapper(global::AutoMapper.IMapper inner)
    {
        _inner = inner;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
    {
        using var scope = new MapperContextScope(this);
        return _inner.Map<TSource, TDestination>(source);
    }

    public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, object? parameters = null)
    {
        using var scope = new MapperContextScope(this);
        return _inner.ProjectTo<TDestination>(source, parameters);
    }

    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        using var scope = new MapperContextScope(this);
        _inner.Map(source, result);
    }
}
```

**Важно:** каждый публичный метод оборачивается в `MapperContextScope(this)`. Это кладёт текущий `IMapper` в `MapperContextAccessor.Current`, чтобы вложенные вызовы из `ITypeConverter` (через `ResolutionContext.Map(...)`) нашли его и не получили `NullMapper`.

### 8.3. `AutoMapperProfileBuilder`

```csharp
internal sealed class AutoMapperProfileBuilder
    : MapperProfileBuilderBase<Profile>
{
    protected override ICollection<IDescriptionApplicator> Applicators =>
    [
        new MemberApplicator(),
        new CtorParamApplicator(),
        new ConstructUsingApplicator(),
        new ConverterApplicator(),
        new BeforeAfterMapApplicator(),
        new IncludedBasesApplicator(),
    ];

    public Profile Build(IMappingProfile profile)
    {
        var autoMapperProfile = new AdapterProfile();
        Apply(autoMapperProfile, profile);
        return autoMapperProfile;
    }
}
```

`Build` создаёт пустой `AdapterProfile` (наш наследник `global::AutoMapper.Profile`), затем `Apply` проходит по всем `IMapDescription` из профиля и по очереди применяет applicator'ы.

> **Рефлексия.** `IMapDescription` хранит типы как `Type` (без generic-параметров), поэтому `Profile.CreateMap<TSource, TDestination>()` вызывается через `MethodInfo.MakeGenericMethod` — applicator'ы оперируют через reflection.

### 8.4. Applicator'ы

Каждый applicator реализует `IDescriptionApplicator` и отвечает за одну секцию `IMapDescription`:

| Applicator | Что применяет | Куда в AutoMapper |
|------------|---------------|-------------------|
| `MemberApplicator` | `ForMember` (`MapFrom`, `MapFrom(src, p)`, `Ignore`) | `IMappingExpression.ForMember(...)` |
| `CtorParamApplicator` | `ForCtorParam` | `IProjectionExpression.ForCtorParam(name, ...)` |
| `ConstructUsingApplicator` | `ConstructUsing(Func<,>)` и `ConstructUsing(Func<,,,>)` | `IMappingExpression.ConstructUsing(...)` через `ConstructUsingWrapper<,>` |
| `ConverterApplicator` | `ConvertUsing(ITypeConverter<,>)`, `ConvertUsing(Func<,>)`, `ConvertUsing(Func<,,,>)` | `IMappingExpression.ConvertUsing(...)`; `ITypeConverter<,>` оборачивается в `TypeConverterAdapter<,>` |
| `BeforeAfterMapApplicator` | `BeforeMap`, `AfterMap` | `IMappingExpression.BeforeMap/AfterMap` |
| `IncludedBasesApplicator` | `IncludeBase`, `ReverseMap` | `IMappingExpression.IncludeBase`/`ReverseMap` |

**Особый случай — параметризованный `MapFrom`.** AutoMapper нативно не понимает выражения `(src, p) => ...` с параметрами. `MemberApplicator` при обнаружении `ParameterizedSourceExpression` собирает ключи через `ParamKeyCollector`, динамически строит «holder»-тип через `ParamHolderTypeCache` и подменяет обращения `params["key"]` на `holder.key` через `ParamAccessReplacer`. В итоге AutoMapper получает обычное `Expression<Func<TSource, TMember>>` и корректно транслирует его в SQL при `ProjectTo`.

### 8.5. `DependencyInjector`

```csharp
internal sealed class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase<Profile[], Mapper>(loggerFactory)
{
    protected override Profile[] BuildConfig(IReadOnlyCollection<IMappingProfile> profiles)
    {
        var builder = new AutoMapperProfileBuilder();
        return profiles.Select(builder.Build).ToArray();
    }

    protected override IServiceCollection RegisterConfig(
        IServiceCollection serviceCollection,
        Profile[] config)
    {
        return serviceCollection.AddAutoMapper(cfg => cfg.AddProfiles(config));
    }
}
```

- `BuildConfig` — собирает массив AutoMapper-профилей из декларативных `IMappingProfile`.
- `RegisterConfig` — вызывает стандартный `AddAutoMapper(...)` с этим массивом.
- Базовый класс `DependencyInjectorBase<Profile[], Mapper>` автоматически регистрирует `IMapper → Mapper` как **Singleton** после `RegisterConfig`.

---

## 9. Mapster-реализация

`Shared.Infrastructure.Mapper.Mapster` — адаптер, превращающий декларативные описания из `MappingProfileBase` в конфигурацию Mapster (`TypeAdapterConfig`).

### 9.1. Состав слоя

| Файл | Назначение |
|------|-----------|
| `Mapper.cs` | `public class Mapper : IMapper` — обёртка над `MapsterMapper.IMapper` |
| `DependencyInjection/DependencyInjector.cs` | Регистрация Mapster-конфигурации в DI |
| `Adapters/MapsterProfileBuilder.cs` | Преобразует `IMappingProfile` в настройки `TypeAdapterConfig` |
| `Adapters/Applicators/*` (6 файлов) | Применяют секции `IMapDescription` к Mapster-сеттеру |
| `Adapters/ExpressionTransformers/ParameterizedExpressionTransformer.cs` | Переписывает параметризованные `MapFrom` под Mapster |
| `Adapters/TypeConverterAdapter.cs` | Обёртка `ITypeConverter<,>` под `MapWith` |
| `Utils/MapperContextScope.cs` | Свой scope с поддержкой `CurrentMappingTarget` для in-place |
| `Utils/MapContextScope.cs` | Disposable-обёртка над `MapContext.Current` для параметров |
| `Utils/MapperContextAccessor.cs` | Хранит `CurrentMappingTarget` (целевой объект для in-place) |
| `Utils/ParameterKeyRegistry.cs` | Реестр ключей параметров по паре типов |

### 9.2. `Mapper`

В отличие от AutoMapper-версии, `Mapper` здесь объявлен как `public` (не `internal`, не `sealed`) и принимает зависимость через primary constructor:

```csharp
public class Mapper(MapsterMapper.IMapper mapper) : IMapper
{
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        using var scope = new MapperContextScope(this);
        return mapper.Map<TSource, TDestination>(source);
    }

    public IQueryable<TDestination> ProjectTo<TDestination>(
        IQueryable source,
        object? parameters = null)
    {
        // Быстрый путь: тип-идентичный запрос — без проекции
        var sourceType = source.GetType();
        if (sourceType is { IsGenericType: true, GenericTypeArguments.Length: 1 } &&
            typeof(TDestination) == sourceType.GenericTypeArguments[0])
        {
            return (source as IQueryable<TDestination>)!;
        }

        using var mapperScope = new MapperContextScope(this);
        using var mapContextScope = MapContextScope.Create();

        var builder = source.BuildAdapter(mapper.Config);

        var parameterValues = GetParameterValues(source.ElementType, typeof(TDestination), parameters);

        foreach (var (key, value) in parameterValues)
        {
            builder = builder.AddParameters(key, value!);
            MapContext.Current!.Parameters[key] = value!;
        }

        return builder.ProjectToType<TDestination>();
    }

    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        using var scope = new MapperContextScope(this, result!);
        mapper.Map(source, result);
    }
}
```

**Ключевые отличия от AutoMapper-версии:**

1. **Быстрый путь в `ProjectTo`.** Если `IQueryable<T>` уже совпадает с целевым типом `T`, проекция пропускается — возвращается исходный `IQueryable<T>`. Это та же оптимизация, что была в предыдущей версии AutoMapper-обёртки.
2. **Поддержка `parameters`.** Параметры извлекаются из объекта через `ParameterKeyRegistry` (для `IDictionary<string, object?>`) или через reflection по публичным свойствам, и пробрасываются в `BuildAdapter().AddParameters(...)` и `MapContext.Current.Parameters`.
3. **In-place `Map`.** Сохраняет целевой объект в `MapperContextAccessor.CurrentMappingTarget`, чтобы `ITypeConverter` через `TypeConverterAdapter<,>` мог его получить.

### 9.3. `MapsterProfileBuilder`

```csharp
internal sealed class MapsterProfileBuilder
    : MapperProfileBuilderBase<TypeAdapterConfig>
{
    protected override ICollection<IDescriptionApplicator> Applicators =>
    [
        new MemberApplicator(),
        new CtorParamApplicator(),
        new ConstructUsingApplicator(),
        new ConverterApplicator(),
        new BeforeAfterMapApplicator(),
        new IncludedBasesApplicator(),
    ];

    protected override object CreateMappingContext(
        TypeAdapterConfig config,
        Type sourceType,
        Type destinationType)
    {
        var newConfigMethod = NewConfigGenericMethod.MakeGenericMethod(sourceType, destinationType);
        return newConfigMethod.Invoke(config, null)!;
    }
}
```

`CreateMappingContext` вызывает `TypeAdapterConfig.NewConfig<TSource, TDestination>()` через reflection, получая fluent-сеттер `TypeAdapterSetter<TSource, TDestination>`, к которому затем применяются applicator'ы.

### 9.4. Applicator'ы

| Applicator | Что применяет | Куда в Mapster |
|------------|---------------|----------------|
| `MemberApplicator` | `ForMember` (`MapFrom`, `MapFrom(src, p)`, `Ignore`) | `TypeAdapterSetter.Map(destName, src => ...)` / `Ignore(destName)` |
| `CtorParamApplicator` | `ForCtorParam` | `MapToConstructor(true).Map(paramName, src => ...)` |
| `ConstructUsingApplicator` | `ConstructUsing(Func<,>)`, `ConstructUsing(Func<,,,>)` | `TypeAdapterSetter.ConstructUsing(Expression<Func<TSource, TDestination>>)` |
| `ConverterApplicator` | `ConvertUsing(ITypeConverter<,>)`, `ConvertUsing(Func<,>)`, `ConvertUsing(Func<,,,>)` | `TypeAdapterSetter.MapWith(...)` / `MapToTargetWith(...)` через `TypeConverterAdapter<,>` |
| `BeforeAfterMapApplicator` | `BeforeMap`, `AfterMap` | `TypeAdapterSetter.BeforeMapping/AfterMapping` |
| `IncludedBasesApplicator` | `IncludeBase`, `ReverseMap` | `TypeAdapterSetter.TwoWays()` / extension `Inherits(...)` |

**Особенность — `ConstructUsing` с `ResolutionContext`.** Mapster не имеет нативной перегрузки `ConstructUsing(Func<TSource, ResolutionContext, TDestination>)`. Applicator строит expression, который внутри вызывает наш `func(src, null)` (контекст игнорируется — Mapster всё равно не передаст его в SQL-проекцию).

**Особенность — параметризованный `MapFrom`.** В Mapster нет стандартного пути для `(src, p) => ...`. `MemberApplicator` использует `ParameterizedExpressionTransformer` для переписывания выражения и `ParameterKeyRegistry` для регистрации ожидаемых ключей, чтобы в `ProjectTo` они были добавлены через `AddParameters`.

### 9.5. `DependencyInjector`

```csharp
internal class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase<TypeAdapterConfig, Mapper>(loggerFactory)
{
    protected override TypeAdapterConfig BuildConfig(IReadOnlyCollection<IMappingProfile> profiles)
    {
        var config = new TypeAdapterConfig();
        var builder = new MapsterProfileBuilder();
        foreach (var profile in profiles)
        {
            builder.Apply(config, profile);
        }
        return config;
    }

    protected override IServiceCollection RegisterConfig(
        IServiceCollection serviceCollection,
        TypeAdapterConfig config)
    {
        return serviceCollection
            .AddSingleton(config)
            .AddSingleton<MapsterMapper.IMapper, ServiceMapper>();
    }
}
```

- `BuildConfig` — собирает одну общую `TypeAdapterConfig` из всех `IMappingProfile`.
- `RegisterConfig` — регистрирует `TypeAdapterConfig` как Singleton и `MapsterMapper.IMapper → ServiceMapper` как Singleton.
- Базовый класс затем дополнительно регистрирует `IMapper → Mapper` (наш) как Singleton.

---

## 10. Регистрация в DI

Регистрация полностью автоматическая и идёт через `Shared.Presentation.Core.ImplementDependencies`. Прямого вызова `AddAutoMapper(...)` или `AddMapster(...)` из `Program.cs` не требуется.

### 10.1. Что происходит при `builder.ImplementDependencies()`

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();
```

`ImplementDependencies` (из `Shared.Presentation.Core.Extensions.WebApplicationBuilderExtensions`):

1. Загружает `.env`-конфигурацию.
2. Регистрирует контроллеры с конвенциями и фильтром логов.
3. Вызывает `services.AddReferencedDependencyInjectors()`.

`AddReferencedDependencyInjectors` (из `Shared.Infrastructure.Core.DependencyInjection.Extensions.ServiceCollectionExtensions`):

1. Загружает project-сборки решения через `DependencyContext`.
2. Находит все наследники `DependencyInjectorBase` через `AssemblyHelper.GetDerivedTypesFromAssemblies<DependencyInjectorBase>()`.
3. Сортирует их по слоям (Shared → Application → Infrastructure) и вызывает `Inject(serviceCollection)` на каждом.

Каждый `DependencyInjector` отвечает за свой фрагмент регистрации. Для AutoMapper или Mapster это `DependencyInjectorBase<Profile[], Mapper>` либо `DependencyInjectorBase<TypeAdapterConfig, Mapper>` соответственно.

### 10.2. `IsMappingProfileCandidate` — фильтр профилей

`DependencyInjectorBase` находит профили в загруженных сборках по правилу:

```csharp
private static bool IsMappingProfileCandidate(Type type) =>
    typeof(IMappingProfile).IsAssignableFrom(type)
    && type is { IsAbstract: false, IsInterface: false }
    && type.GetConstructor(Type.EmptyTypes) is not null;
```

То есть:

- Класс реализует `IMappingProfile` (то есть наследует `MappingProfileBase`).
- Класс **конкретный** (не `abstract`, не `interface`).
- У класса есть **публичный конструктор без параметров** (для `Activator.CreateInstance`).

Это автоматически отсеивает generic-определения, абстрактные базы, тестовые двойники и прочие неподходящие типы.

### 10.3. Выбор провайдера

Подключение в `.csproj` сервиса:

| Хотим AutoMapper | Хотим Mapster |
|------------------|---------------|
| `ProjectReference` на `Shared.Infrastructure.Mapper.AutoMapper` | `ProjectReference` на `Shared.Infrastructure.Mapper.Mapster` |

```xml
<!-- Setter (AutoMapper) -->
<ItemGroup>
  <ProjectReference Include="..\..\Shared\Mapper\Shared.Infrastructure.Mapper.AutoMapper\Shared.Infrastructure.Mapper.AutoMapper.csproj" />
</ItemGroup>
```

> **⚠️ Подключение обоих провайдеров одновременно приведёт к конфликту DI.** Каждый `DependencyInjector` базы регистрирует `IMapper → Mapper` как Singleton; в коллекции сервисов окажется **две** регистрации `IMapper`, и `BuildServiceProvider` выбросит исключение. Не подключайте оба проекта в одном сервисе.

### 10.4. Полный пример `Program.cs` (AutoMapper)

```csharp
using Template.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.InitializeConfiguration(builder.Environment);
builder.ImplementDependencies();

var app = builder.Build();
app.UseSharedPipeline();   // инфраструктура Shared (маршрутизация, обработчики ошибок и т.д.)
app.Run();
```

Для Mapster — внешне `Program.cs` идентичен; разница только в `.csproj` (ссылка на Mapster-адаптер вместо AutoMapper-адаптера).

### 10.5. Регистрация пользовательских `ITypeConverter<,>`

Если конвертер типов использует DI-зависимости (см. § 5.4), его нужно зарегистрировать в `IServiceCollection` явно:

```csharp
builder.Services.AddTransient<AddressConverter>();
```

Регистрация в `MappingProfileBase.ConfigureCollection`/`ConvertUsing` остаётся прежней — адаптер сам извлечёт экземпляр из DI при вызове.

---

## 11. Написание своего `MappingProfile`

Пошаговый туториал.

### Шаг 1. Выбор расположения

`MapperProfile` живёт в проекте `*.Application` (НЕ в `*.Infrastructure` — это требование Clean Architecture). Точный путь:

```
src/Services/{Service}/Template.{Service}.Application/Mapping/MapperProfile.cs
```

Примеры:
- `src/Services/Setter/Template.Setter.Application/Mapping/MapperProfile.cs`
- `src/Services/Getter/Template.Getter.Application/Mapping/MapperProfile.cs`
- `src/Services/Common/Template.Application/Mapping/MapperProfile.cs`
- `src/Services/Bff/Template.Bff.Application/Mapping/MapperProfile.cs`

> Все профили располагаются в `*.Application/Mapping/` — это требование Clean Architecture, по которому слой Infrastructure не должен зависеть от прикладного слоя обратно.

### Шаг 2. Создание класса

```csharp
using Shared.Domain.Core.Mapping;
using Template.Application.Dto.Order;
using Template.Domain.Entities;

namespace Template.Application.Mapping;

/// <summary>
/// Профиль маппинга для Order/OrderItem.
/// </summary>
public class MapperProfile : MappingProfileBase
{
    public MapperProfile()
    {
        // 1. Простой плоский маппинг
        CreateMap<OrderDto, Order>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));

        // 2. Обратное направление
        CreateMap<Order, OrderDto>();

        // 3. Фабрика для создания
        CreateMap<OrderCreateRequest, Order>()
            .ConstructUsing(src => Order.Create(src.Number, src.CustomerId));

        // 4. Полная подмена через конвертер типов
        CreateMap<OrderItemDto, OrderItem>()
            .ConvertUsing<OrderItemConverter>();

        // 5. Diff-merge коллекции
        CreateMap<ICollection<OrderItemDto>, ICollection<OrderItem>>()
            .ConfigureCollection(
                sourceSelector: dto => dto.ProductId,
                destinationSelector: entity => entity.ProductId);
    }
}
```

### Шаг 3. Использование в handler'е

```csharp
public class OrderUpdateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Order>> validators,
    IUserProvider userProvider)
    : UpdateCommandHandler<OrderUpdateCommand, OrderUpdateRequest, Order, OrderDto, OrderUpdateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    protected override bool WithTracking => true;

    protected override Task ProcessEntityAsync(Order entity, OrderUpdateCommand command)
    {
        mapper.Map(command.Request, entity);
        mapper.Map(command.Request.Items, entity.Items);
        return Task.CompletedTask;
    }
}
```

### Шаг 4. (Опционально) Регистрация пользовательских конвертеров в DI

```csharp
builder.Services.AddTransient<OrderItemConverter>();
```

---

## 12. `ProjectTo` и оптимизации

### 12.1. Быстрый путь в Mapster-реализации

`Mapper.ProjectTo<TDest>` (Mapster) проверяет, совпадает ли тип источника с целевым. Если да — маппинг пропускается:

```csharp
// Без оптимизации — лишний маппинг
var items = repository.GetRangeAsync(options)  // IQueryable<Person>
    .ProjectTo<Person>(mapper)                  // лишняя проекция
    .ToList();

// С оптимизацией — skip маппинга
// typeof(TDestination) == source.GenericTypeArguments[0] → true
// Возвращается исходный IQueryable<Person>
```

В AutoMapper-реализации такой быстрый путь отсутствует: `global::AutoMapper.IMapper.ProjectTo<T>(...)` всегда создаёт `IQueryable<T>`. Если вы заранее знаете, что типы совпадают, предпочтительнее работать с `IQueryable<T>` напрямую.

### 12.2. Параметризованные проекции

`parameters` в `ProjectTo` — это runtime-параметры, которые нужно учесть в SQL-проекции. Используются, когда итоговый SQL зависит от значений, недоступных на этапе компиляции (например, текущая культура, часовой пояс, флаги).

```csharp
// DTO с параметризованным полем
public sealed record LocalizedPersonDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

// Профиль
public class LocalizedPersonProfile : MappingProfileBase
{
    public LocalizedPersonProfile()
    {
        CreateMap<Person, LocalizedPersonDto>()
            .ForMember(
                d => d.DisplayName,
                opt => opt.MapFrom((src, p) =>
                    $"{p["culture"]}: {src.Name}"));
    }
}

// Использование
var dtos = query.ProjectTo<LocalizedPersonDto>(
    mapper,
    parameters: new { culture = "ru-RU" })
    .ToList();
```

В обоих провайдерах параметризованные `MapFrom(src, p)` корректно транслируются в SQL через подстановку `holder.<key>` (AutoMapper) либо через регистрацию ключей в `ParameterKeyRegistry` (Mapster).

### 12.3. EF Core интеграция

`ProjectTo` возвращает `IQueryable<TDest>`, поэтому результат можно композить с другими LINQ-операторами до материализации:

```csharp
var dtos = await query
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .ProjectTo<PersonDto>(mapper)
    .Skip(skip)
    .Take(take)
    .ToListAsync(cancellationToken);
```

AutoMapper-проекция транслируется в `SELECT ... FROM Persons ...` сразу с нужными колонками; Mapster — аналогично. N+1 не возникает.

> Если в проекции нужны навигационные свойства, их явно подгружают в запросе через `Include` и описывают правила в `MappingProfileBase.ForMember(d => d.RelatedName, opt => opt.MapFrom(s => s.Related.Name))` — это транслируется в SQL провайдером проекции.

---

## 13. Выбор провайдера: AutoMapper vs Mapster

| Аспект | AutoMapper 14.0.0 | Mapster |
|--------|-------------------|---------|
| Зрелость | Высокая, проверен годами в enterprise | Активно развивается |
| Производительность runtime | Хорошая | Выше на горячих путях за счёт кодогенерации и `MapToTarget` |
| `ProjectTo` в EF Core | Зрелая, широкая поддержка выражений | Поддерживается, в т.ч. параметризованные выражения через `MapContext` |
| `ITypeConverter` | Нативный | Обёрнут через `MapWith` + `TypeConverterAdapter<,>` |
| `IncludeBase` | Нативный | Через extension `Inherits(...)` |
| `ConstructUsing(Func<TSource, ResolutionContext, TDestination>)` | Нативно | Аппроксимируется через expression с null-контекстом |
| In-place `Map(src, dest)` | `_inner.Map(source, dest)` | Через `MapperContextAccessor.CurrentMappingTarget` для `TypeConverterAdapter` |
| API модели | `Profile` + `CreateMap` + applicators | `TypeAdapterConfig.NewConfig` + `TypeAdapterSetter` |
| Стоимость миграции | — | Без правки доменного/прикладного кода — только `.csproj` |

**Когда выбрать AutoMapper:**

- Большой существующий профиль, активно использующий нативные AutoMapper-фичи (`IncludeBase`, `ConstructProjectionUsing`, etc.).
- Команда уже знакома с AutoMapper API.

**Когда выбрать Mapster:**

- Важна скорость проекций и materialisation.
- Хочется более простой/компактный API сеттеров (`Map(name, src => ...)`).
- Готовы чуть глубже разбираться в нюансах обработки `TypeAdapterSetter`.

**Миграция между провайдерами:**

1. В `*.Application/Mapping/MapperProfile.cs` **ничего не меняется** — описания идут через `IMappingExpression<,>`.
2. В `*.csproj` сервиса меняется `ProjectReference` с `Shared.Infrastructure.Mapper.AutoMapper` на `Shared.Infrastructure.Mapper.Mapster` или наоборот.
3. Если в `MappingProfile` использовались специфичные AutoMapper-фичи, которых нет в Mapster (например, `ConstructProjectionUsing` для одного из типов), их нужно заменить на эквивалент (`Map(...).ProjectToType(...)` или параметризованный `MapFrom`).

---

## 14. Изоляция адаптеров и Dependency Rule

`Shared.Domain.Core` (где живут `IMapper`, `ITypeConverter<,>`, `MappingProfileBase`, `ResolutionContext`) **не имеет** зависимостей на:

- `AutoMapper`, `AutoMapper.ITypeConverter`, `AutoMapper.ResolutionContext`, `IMemberConfigurationExpression`, `ICtorParamConfigurationExpression`, `IProjectionExpression`, `Profile`, `IConfigurationProvider`.
- `Mapster`, `TypeAdapterConfig`, `TypeAdapterSetter`, `MapContext`, `ServiceMapper`, `MapsterMapper.IMapper`.

Эти упоминания появляются **только** в:

- `Shared.Infrastructure.Mapper.AutoMapper` — адаптер.
- `Shared.Infrastructure.Mapper.Mapster` — адаптер.

Тесты `Shared.Domain.Core.Tests.Mapping.*` используют `MappingProfileBase` напрямую и не подтягивают ни AutoMapper, ни Mapster. В тестах вышестоящих слоёв (`Shared.Infrastructure.Mapper.Tests`) — общий базовый класс `ProfileFeaturesTestBase` параметризуется конкретным провайдером и проверяет, что оба адаптера ведут себя идентично.

> **Если вы вносите изменения в `Shared.Domain.Core` и замечаете, что вам приходится `using AutoMapper;` или `using Mapster;` — это архитектурная ошибка.** Введите недостающую абстракцию в `IMapDescription`/`IMappingExpression` и обработайте её в applicator'е соответствующего адаптера.

---

## 15. См. также

| Документ | Связь со слоем маппинга |
|----------|-------------------------|
| [CQRS](cqrs.md) | Использование `IMapper` в `CreateCommandHandler`/`UpdateCommandHandler` |
| [EF Core Internals](efcore-internals.md) | Как `ConfigureCollection` взаимодействует с `ChangeTracker` |
| [Auto-Registration](auto-registration.md) | `AddReferencedDependencyInjectors` и порядок загрузки слоёв |
| [Services](services.md) | Обзор микросервисной архитектуры (Setter, Getter, Bff, Common) |
| [Service Creation Guide](service-creation-guide.md) | Создание нового сервиса с собственным набором `MappingProfile`-наследников |
| [Service Startup](service-startup.md) | `Program.cs` и подключение `ImplementDependencies()` |
| [Domain Modeling](domain-modeling.md) | Фабричные методы, используемые в `ConstructUsing` |
| [Entity Interfaces](entity-interfaces.md) | `IEntity`/`IEntity<T>` как контракт для `ConfigureCollection` |
| [Repository](repository.md) | `GetRangeAsync<TDest>` — проекция через `IMapper` под капотом |
| [Unit of Work](unit-of-work.md) | Управление транзакциями при обновлении коллекций через `ConfigureCollection` |
| [Testing](testing.md) | `FakeMapper` из `Shared.Testing` для тестов handler'ов |
| [API Client](api-client.md) | Маппинг DTO из внешних API-ответов |
