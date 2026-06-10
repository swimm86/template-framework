# Mapping

**Assembly:** `Shared.Domain.Core.dll` (интерфейс), `Shared.Infrastructure.Mapper.AutoMapper.dll` (реализация, AutoMapper **14.0.0**)  
**Namespace:** `Shared.Domain.Core.Mapping.Interfaces`, `Shared.Infrastructure.Mapper.AutoMapper`  
**Исходники:** `src/Shared/Core/Shared.Domain.Core/Mapping/`, `src/Shared/Mapper/Shared.Infrastructure.Mapper.AutoMapper/`  
**Lifetime:** `IMapper` регистрируется в DI как **Singleton** (потокобезопасная обёртка над `global::AutoMapper.IMapper`).

---

## Обзор

Mapping-абстракция в фреймворке Shared предоставляет слой индирекции над AutoMapper. Это позволяет:

- **Заменять реализацию** — доменный слой зависит от `IMapper`, а не от конкретной библиотеки
- **ProjectTo оптимизация** — автоматический skip маппинга если source и destination типы совпадают
- **Diff-merge коллекций** — `ConfigureCollection` для умного сравнения и синхронизации дочерних коллекций
- **Extension methods** — удобные методы для fluent-стиля

---

## IMapper Interface

Базовый интерфейс маппера в доменном слое.

```csharp
public interface IMapper
{
    TResult Map<TSource, TResult>(TSource source);
    void Map<TSource, TResult>(TSource source, TResult result);
    IQueryable<TResult> ProjectTo<TResult>(
        IQueryable source,
        object? parameters = null,
        params Expression<Func<TResult, object>>[] membersToExpand);
}
```

### Методы

| Метод | Описание | Возвращает |
|-------|----------|------------|
| `Map<TSource, TResult>(source)` | Маппинг одного объекта в другой | `TResult` |
| `Map<TSource, TResult>(source, result)` | Маппинг в существующий экземпляр | `void` |
| `ProjectTo<TResult>(source, ...)` | Проекция IQueryable в целевой тип | `IQueryable<TResult>` |

### Пример использования

```csharp
public class PersonCreateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Person>> validators,
    IUserProvider userProvider)
    : CreateCommandHandler<PersonCreateCommand, PersonCreateRequest, Person, PersonDto, PersonCreateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    protected override Task ProcessEntityAsync(Person entity, PersonCreateCommand command)
    {
        // Маппинг Request → Entity
        mapper.Map(command.Request, entity);
        return Task.CompletedTask;
    }
}
```

---

## MapperExtensions

Extension methods для удобного fluent-синтаксиса.

```csharp
public static class MapperExtensions
{
    public static TResult Map<TSource, TResult>(this TSource source, IMapper mapper);
    public static IQueryable<TResult> ProjectTo<TResult>(
        this IQueryable source,
        IMapper mapper,
        object? parameters = null,
        params Expression<Func<TResult, object>>[] membersToExpand);
    public static void Map<TSource, TResult>(this TSource source, TResult result, IMapper mapper);
}
```

### Примеры

```csharp
// Extension-стиль
var dto = entity.Map<Person, PersonDto>(mapper);

// Проекция с membersToExpand
var dtos = query
    .ProjectTo<PersonDto>(mapper, membersToExpand: x => new { x.RelatedEntity })
    .ToList();

// Маппинг в существующий объект
source.Map(target, mapper);
```

---

## AutoMapper Implementation

Реализация `IMapper` поверх AutoMapper с оптимизациями.

```csharp
public class Mapper(global::AutoMapper.IMapper mapper) : IMapper
{
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return mapper.Map<TSource, TDestination>(source);
    }

    public IQueryable<TDestination> ProjectTo<TDestination>(
        IQueryable source,
        object? parameters = null,
        params Expression<Func<TDestination, object>>[] membersToExpand)
    {
        var sourceType = source.GetType();
        return sourceType is { IsGenericType: true, GenericTypeArguments.Length: 1 } &&
               typeof(TDestination) == sourceType.GenericTypeArguments[0]
            ? (source as IQueryable<TDestination>)!
            : source.ProjectTo(mapper.ConfigurationProvider, parameters, membersToExpand);
    }

    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        mapper.Map(source, result);
    }
}
```

### Оптимизация ProjectTo

Метод `ProjectTo` проверяет, совпадает ли тип источника с типом назначения. Если типы идентичны, маппинг пропускается — возвращается исходная коллекция без накладных расходов.

```csharp
// Без оптимизации — лишний маппинг
var items = repository.GetRangeAsync(options)  // IQueryable<Person>
    .ProjectTo<Person>(mapper)                  // Маппинг Person → Person (бесполезно!)
    .ToList();

// С оптимизацией — skip маппинга
// typeof(TDestination) == source.GenericTypeArguments[0] → true
// Возвращается исходный IQueryable<Person> без преобразования
```

---

## ConfigureCollection — Diff-Merge Pattern

`ProfileExtensions.ConfigureCollection` — ключевой механизм для синхронизации дочерних коллекций без N+1 проблем.

### Базовая версия

```csharp
public static void ConfigureCollection<TSrc, TDest>(this Profile profile)
    where TSrc : IEntity
    where TDest : IEntity
```

Автоматически определяет элементы для добавления, удаления и обновления через `GetDifferenceForMerge`.

### Версия с кастомными селекторами

```csharp
public static void ConfigureCollection<TSrc, TDest>(
    this Profile profile,
    Func<TSrc, object> subSelector,
    Func<TDest, object> entitySelector,
    Func<TSrc, TDest, bool> compareFunc)
```

Позволяет настроить:
- `subSelector` — как извлечь ключ из DTO
- `entitySelector` — как извлечь ключ из Entity
- `compareFunc` — функция сравнения для определения изменений

### Алгоритм работы

```
1. Получить разницу между source и destination коллекциями
   → toAdd: элементы есть в source, но нет в destination
   → toDelete: элементы есть в destination, но нет в source
   → toUpdate: элементы есть в обоих, но могут быть изменены

2. Удалить элементы из toDelete
3. Обновить элементы из toUpdate (маппинг source → destination)
4. Добавить новые элементы из toAdd
```

### Пример: Order → OrderItems

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderItemDto, OrderItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // Базовая версия — сравнение по Id
        this.ConfigureCollection<OrderItemDto, OrderItem>();
    }
}
```

### Пример с кастомными селекторами

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderItemDto, OrderItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // Кастомная версия — сравнение по ProductId
        this.ConfigureCollection<OrderItemDto, OrderItem>(
            subSelector: dto => dto.ProductId,
            entitySelector: entity => entity.ProductId,
            compareFunc: (dto, entity) => dto.Quantity == entity.Quantity &&
                                          dto.Price == entity.Price);
    }
}
```

### Пример в CQRS Handler

```csharp
public class OrderUpdateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Order>> validators,
    IUserProvider userProvider)
    : UpdateCommandHandler<OrderUpdateCommand, OrderUpdateRequest, Order, OrderResponse, OrderUpdateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    protected override bool WithTracking => true;

    protected override Task ProcessEntityAsync(Order entity, OrderUpdateCommand command)
    {
        // Маппинг основных полей
        mapper.Map(command.Request, entity);

        // Маппинг коллекции OrderItems — ConfigureCollection автоматически:
        // 1. Удалит OrderItems которых нет в request
        // 2. Обновит существующие OrderItems
        // 3. Добавит новые OrderItems
        mapper.Map(command.Request.Items, entity.Items);

        return Task.CompletedTask;
    }
}
```

### Преимущества ConfigureCollection

| Проблема | Решение |
|----------|---------|
| Ручное сравнение коллекций | Автоматический diff через `GetDifferenceForMerge` |
| N+1 при обновлении дочерних элементов | Один вызов маппинга — все изменения применены |
| Ошибки при удалении/добавлении | Алгоритм гарантирует корректность |
| Дублирование кода в handlers | Единая конфигурация в Profile |

---

## Регистрация в DI

Mapper регистрируется автоматически через `AddReferencedDependencyInjectors()`:

```csharp
// Program.cs
builder.ImplementDependencies();
// Внутри: Shared.Infrastructure.Mapper.AutoMapper.DependencyInjection.Extensions
// регистрирует IMapper → Mapper (AutoMapper) как Singleton
```

### Настройка Profile

```csharp
public class PersonProfile : Profile
{
    public PersonProfile()
    {
        CreateMap<Person, PersonDto>();
        CreateMap<PersonDto, Person>();

        // Diff-merge для дочерних коллекций
        this.ConfigureCollection<AddressDto, Address>();
    }
}
```

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Использование IMapper в Command/Query handlers |
| [EF Core Internals](efcore-internals.md) | Как ConfigureCollection взаимодействует с EF Core tracking |
| [API Client](api-client.md) | Маппинг DTO в API responses |
