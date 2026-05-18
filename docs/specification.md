# Specification Pattern

## Обзор

Specification Pattern в фреймворке Shared предоставляет способ инкапсуляции бизнес-критериев выборки данных в переиспользуемые объекты. Этот паттерн отделяет логику фильтрации, сортировки и включения связанных сущностей от кода доступа к данным.

Спецификация наследуется от `SpecificationBase<TEntity>` и через защищённые методы `AddFilter()`, `AddOrderBy()`, `AddInclude()` формирует `QueryOptions<TEntity>`, которые затем используются репозиторием.

##Assembly / Namespace

| Компонент | Namespace |
|-----------|-----------|
| `SpecificationBase<TEntity>` | `Shared.Domain.Core.Dal.Specification.Models` |
| `ISpecification<TEntity>` | `Shared.Domain.Core.Dal.Specification.Interfaces` |
| `QueryOptions<TEntity>` | `Shared.Domain.Core.Dal.Repository.Models` |
| `SortOption` | `Shared.Domain.Core.Dal.Models` |
| `OrderDirectionType` | `Shared.Domain.Core.Dal` |

## Интерфейс ISpecification<TEntity>

```csharp
public interface ISpecification<TEntity>
    where TEntity : IEntity
{
    /// <summary>
    /// Собирает настройки для спецификации.
    /// </summary>
    QueryOptions<TEntity> BuildOptions();
}
```

Спецификация преобразуется в `QueryOptions<TEntity>`, которые используются репозиторием для построения запроса.

## SpecificationBase<TEntity>

`SpecificationBase<TEntity>` — абстрактная record, реализующая `ISpecification<TEntity>`. Предоставляет защищённые методы для построения критериев выборки.

### Сигнатура

```csharp
public abstract record SpecificationBase<TEntity>(
    ICollection<SortOption>? SortOptions = default)
    : ISpecification<TEntity>
    where TEntity : class, IEntity
```

### Защищённые члены

| Член | Тип | Описание |
|------|-----|----------|
| `Options` | `QueryOptions<TEntity>` | Защищённое поле для накопления критериев |
| `BuildOptions()` | `virtual QueryOptions<TEntity>` | Применяет `SortOptions` и возвращает `Options` |
| `AddFilter()` | `void` | Добавляет выражение фильтрации |
| `AddOrderBy()` | `void` | Добавляет выражение сортировки с направлением |
| `AddInclude<TProperty>()` | `Includable<TEntity, TProperty>` | Добавляет навигационное свойство (одиночное) |
| `AddInclude<TProperty>()` | `Includable<TEntity, TProperty>` | Добавляет навигационное свойство (коллекция) |

### Методы подробно

```csharp
// Добавляет фильтр к запросу
protected void AddFilter(Expression<Func<TEntity, bool>> expression)

// Добавляет сортировку
protected void AddOrderBy(
    Expression<Func<TEntity, object>> expression,
    OrderDirectionType orderDirectionType)

// Добавляет Include для одиночного свойства навигации
protected Includable<TEntity, TProperty> AddInclude<TProperty>(
    Expression<Func<TEntity, TProperty>> expression)

// Добавляет Include для свойства-коллекции навигации
protected Includable<TEntity, TProperty> AddInclude<TProperty>(
    Expression<Func<TEntity, IEnumerable<TProperty>>> expression)
```

### BuildOptions()

Метод `BuildOptions()` по умолчанию применяет конструкторские `SortOptions` к `Options.OrderBy` и возвращает накопленный `QueryOptions<TEntity>`. Переопределяйте при необходимости кастомной логики сборки.

```csharp
public virtual QueryOptions<TEntity> BuildOptions()
{
    SortOptions?.ForEach(Options.AddOrderBy);
    return Options;
}
```

## Quick Start

```csharp
using Shared.Domain.Core.Dal.Specification.Models;
using Shared.Domain.Core.Dal;

// Простая спецификация — активные пользователи
public class ActiveUsersSpecification : SpecificationBase<User>
{
    protected override void OnInit()
    {
        AddFilter(u => u.IsActive && !u.IsDeleted);
    }
}
```

> **Важно:** Спецификации наследуются от `SpecificationBase<TEntity>` и используют защищённые методы `AddFilter()`, `AddOrderBy()`, `AddInclude()` в конструкторе или переопределённом `OnInit()`. Не присваивайте свойства `QueryOptions` напрямую.

## Создание спецификаций

### Базовая спецификация (фильтрация)

```csharp
public class ActiveUsersSpecification : SpecificationBase<User>
{
    public ActiveUsersSpecification()
    {
        AddFilter(u => u.IsActive && !u.IsDeleted);
    }
}
```

> **Примечание:** В текущей версии `SpecificationBase<TEntity>` не имеет виртуального метода `OnInit()`. Критерии следует добавлять в конструкторе спецификации.

### Спецификация с сортировкой

```csharp
public class UsersByRegistrationDateSpecification : SpecificationBase<User>
{
    public UsersByRegistrationDateSpecification()
    {
        AddFilter(u => u.IsActive);
        AddOrderBy(u => u.CreatedAt, OrderDirectionType.Descending);
    }
}
```

### Спецификация с includes

```csharp
public class UsersWithProfilesSpecification : SpecificationBase<User>
{
    public UsersWithProfilesSpecification()
    {
        AddFilter(u => u.IsActive);
        AddInclude(u => u.Profile);
        AddInclude(u => u.Roles);
    }
}
```

Для `AsSplitQuery` и `WithTracking` переопределяйте `BuildOptions()`:

```csharp
public class UsersWithProfilesSpecification : SpecificationBase<User>
{
    public UsersWithProfilesSpecification()
    {
        AddFilter(u => u.IsActive);
        AddInclude(u => u.Profile);
        AddInclude(u => u.Roles);
    }

    public override QueryOptions<User> BuildOptions()
    {
        var options = base.BuildOptions();
        options.AsSplitQuery = true;
        return options;
    }
}
```

### Параметризированная спецификация

```csharp
public class UsersByRoleSpecification : SpecificationBase<User>
{
    private readonly Guid _roleId;

    public UsersByRoleSpecification(Guid roleId)
    {
        _roleId = roleId;
        AddFilter(u => u.Roles.Any(r => r.Id == _roleId));
        AddFilter(u => u.IsActive);
        AddOrderBy(u => u.Name, OrderDirectionType.Ascending);
    }
}
```

> Обратите внимание: `AddFilter()` вызывается многократно — фильтры накапливаются в список `Filters` и объединяются через AND при выполнении запроса.

### Спецификация с динамической сортировкой через SortOptions

Конструктор `SpecificationBase<TEntity>` принимает `ICollection<SortOption>?`, что позволяет передавать сортировку из внешних параметров (например, из query string API):

```csharp
public class SortedUsersSpecification : SpecificationBase<User>
{
    public SortedUsersSpecification(ICollection<SortOption>? sortOptions)
        : base(sortOptions)
    {
        AddFilter(u => u.IsActive);
    }
}

// Использование
var sortOptions = new List<SortOption>
{
    new("Name", OrderDirectionType.Ascending),
    new("CreatedAt", OrderDirectionType.Descending)
};
var spec = new SortedUsersSpecification(sortOptions);
```

`SortOptions` автоматически применяются в `BuildOptions()` через `Options.AddOrderBy(SortOption)`.

### Спецификация с ThenInclude

```csharp
using Shared.Domain.Core.Dal.Repository.Extensions;

public class UsersWithRolesAndPermissionsSpecification : SpecificationBase<User>
{
    public UsersWithRolesAndPermissionsSpecification()
    {
        AddFilter(u => u.IsActive);
        AddInclude(u => u.Roles)
            .ThenInclude(r => r.Permissions);
    }
}
```

## Использование спецификаций

### Базовое использование с репозиторием

```csharp
public class UserService
{
    private readonly IRepository<User> _repository;

    public UserService(IRepository<User> repository)
    {
        _repository = repository;
    }

    public async Task<List<User>> GetActiveUsers()
    {
        var spec = new ActiveUsersSpecification();
        return await _repository.GetRangeAsync(spec);
    }

    public async Task<User?> GetUserWithProfile(Guid userId)
    {
        var spec = new UsersWithProfilesSpecification();
        return await _repository.GetAsync(userId, spec);
    }
}
```

### Использование с CQRS Query Handlers

```csharp
public class GetActiveUsersQueryHandler : IQueryHandler<GetActiveUsersQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<UserDto>> Handle(GetActiveUsersQuery query, CancellationToken ct)
    {
        var userRepo = _unitOfWork.GetRepository<User>();
        var spec = new ActiveUsersSpecification();

        var users = await userRepo.GetRangeAsync(
            spec,
            skip: (query.Page - 1) * query.PageSize,
            take: query.PageSize,
            cancellationToken: ct);

        return _mapper.Map<List<UserDto>>(users);
    }
}
```

### Спецификация с проекцией

```csharp
public class UserSummarySpecification : SpecificationBase<User>
{
    public UserSummarySpecification()
    {
        AddFilter(u => u.IsActive);
        AddOrderBy(u => u.Name, OrderDirectionType.Ascending);
    }
}

// Использование с проекцией
var spec = new UserSummarySpecification();
var summaries = await repository.GetRangeAsync<UserSummaryDto>(
    spec,
    skip: 0,
    take: 100,
    selector: u => new UserSummaryDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        CreatedAt = u.CreatedAt
    });
```

## QueryOptions<TEntity>

`QueryOptions<TEntity>` — mutable-объект, описывающий параметры запроса. Создаётся автоматически в `SpecificationBase` или используется напрямую через `IRepository`.

### Конструктор

```csharp
public QueryOptions<TEntity>(
    bool withTracking = false,
    bool asSplitQuery = false,
    bool distinct = false)
```

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Filters` | `List<Expression<Func<TEntity, bool>>>` | Список фильтров (объединяются через AND) |
| `OrderBy` | `List<QueryOrderByOption<TEntity>>` | Список критериев сортировки |
| `Includes` | `List<IIncludable<TEntity>>` | Список навигационных свойств |
| `WithTracking` | `bool` | Отслеживание изменений сущностей (default: `false`) |
| `AsSplitQuery` | `bool` | Разделение запроса на несколько SQL-запросов (default: `false`) |
| `Distinct` | `bool` | Исключение дубликатов (default: `false`) |
| `DistinctBy` | `Expression<Func<TEntity, bool>>?` | Условие для исключения дубликатов |
| `CustomQueryBeforeProcesses` | `List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>` | Кастомные пре-преобразования IQueryable |
| `CustomQueryPostProcesses` | `List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>` | Кастомные пост-преобразования IQueryable |

### Методы

| Метод | Возвращает | Описание |
|-------|-----------|----------|
| `AddFilter(Expression)` | `QueryOptions<TEntity>` | Добавляет фильтр |
| `AddFilterIf(bool, Expression)` | `QueryOptions<TEntity>` | Добавляет фильтр при условии |
| `AddOrderBy(Expression, OrderDirectionType, int?)` | `QueryOptions<TEntity>` | Добавляет сортировку |
| `AddOrderBy(SortOption)` | `void` | Добавляет сортировку из SortOption |
| `AddOrderByIf(bool, Expression, OrderDirectionType, int?)` | `QueryOptions<TEntity>` | Добавляет сортировку при условии |
| `AddInclude<TProperty>(Expression)` | `Includable<TEntity, TProperty>` | Добавляет Include одиночного свойства |
| `AddInclude<TProperty>(Expression<IEnumerable<TProperty>>)` | `Includable<TEntity, TProperty>` | Добавляет Include коллекции |

### Прямое использование (без Specification)

```csharp
var options = new QueryOptions<User>()
    .AddFilter(u => u.IsActive)
    .AddOrderBy(u => u.Name, OrderDirectionType.Ascending);

var users = await repository.GetRangeAsync(options, skip: 0, take: 20);
```

## SortOption

`SortOption` — модель для передачи сортировки из внешних источников (query string и т.д.).

```csharp
public class SortOption(string key, OrderDirectionType directionType)
{
    public string Key { get; } = key;
    public OrderDirectionType DirectionType { get; } = directionType;
}
```

`Key` — имя свойства (регистронезависимое). `SortOption` автоматически маппится на выражение сортировки через reflection внутри `QueryOptions.AddOrderBy(SortOption)`.

## OrderDirectionType

```csharp
public enum OrderDirectionType
{
    [Description("asc")]
    Ascending,
    [Description("desc")]
    Descending
}
```

## QueryOrderByOption<TEntity>

```csharp
public record QueryOrderByOption<TEntity>(
    Expression<Func<TEntity, object>> Expression,
    OrderDirectionType Direction);
```

Record-тип, содержащий выражение сортировки и направление. Создаётся внутри через `AddOrderBy()`.

## Includable и ThenInclude

`Includable<TEntity, TProperty>` — класс для цепочечного построения Include/ThenInclude.

```csharp
public class Includable<TSrcEntity, TDstEntity>(LambdaExpression expression)
    : IIncludable<TSrcEntity>
{
    public LambdaExpression Expression { get; }
    public IIncludable<TSrcEntity>? Child { get; }
    public void SetChild(IIncludable<TSrcEntity> includable);
}
```

### ThenInclude

```csharp
using Shared.Domain.Core.Dal.Repository.Extensions;

// Цепочечное включение
AddInclude(u => u.Roles)
    .ThenInclude(r => r.Permissions);
```

## Расширенные возможности

### Динамическая сортировка из API

```csharp
public class SortableUserSpecification : SpecificationBase<User>
{
    public SortableUserSpecification(ICollection<SortOption>? sortOptions)
        : base(sortOptions)
    {
        AddFilter(u => u.IsActive);
    }
}

// В API handler
var sortOptions = request.SortItems
    .Select(s => new SortOption(s.Field, s.Descending
        ? OrderDirectionType.Descending
        : OrderDirectionType.Ascending))
    .ToList();

var spec = new SortableUserSpecification(sortOptions);
```

### Условная фильтрация

Используйте `AddFilter` с условными проверками в конструкторе:

```csharp
public class UserSearchSpecification : SpecificationBase<User>
{
    public UserSearchSpecification(string? name, int? minAge, int? maxAge)
    {
        AddFilter(u => u.IsActive);

        if (!string.IsNullOrEmpty(name))
            AddFilter(u => u.Name.Contains(name));

        if (minAge.HasValue)
            AddFilter(u => u.Age >= minAge.Value);

        if (maxAge.HasValue)
            AddFilter(u => u.Age <= maxAge.Value);

        AddOrderBy(u => u.Name, OrderDirectionType.Ascending);
    }
}
```

### Кастомные преобразования IQueryable

```csharp
public class CustomQuerySpecification : SpecificationBase<User>
{
    public CustomQuerySpecification()
    {
        AddFilter(u => u.IsActive);
    }

    public override QueryOptions<User> BuildOptions()
    {
        var options = base.BuildOptions();
        options.CustomQueryPostProcesses.Add(q => q.Where(u => !u.IsBlocked));
        return options;
    }
}
```

### Distinct и DistinctBy

```csharp
public class UniqueEmailsSpecification : SpecificationBase<User>
{
    public UniqueEmailsSpecification()
    {
        AddFilter(u => u.IsActive);
    }

    public override QueryOptions<User> BuildOptions()
    {
        var options = base.BuildOptions();
        options.Distinct = true;
        return options;
    }
}
```

## Best Practices

### 1. Одна ответственность — одна спецификация

```csharp
// Правильно — каждая спецификация решает одну задачу
public class ActiveUsersSpecification : SpecificationBase<User>
{
    public ActiveUsersSpecification()
    {
        AddFilter(u => u.IsActive);
    }
}

public class PremiumUsersSpecification : SpecificationBase<User>
{
    public PremiumUsersSpecification()
    {
        AddFilter(u => u.IsPremium && u.IsActive);
    }
}

// Неправильно — слишком много логики в одной спецификации
public class ComplexUserSpecification : SpecificationBase<User>
{
    // Слишком много условий и параметров
}
```

### 2. Используйте параметризацию для гибкости

```csharp
// Правильно — параметризированная спецификация
public class UsersByAgeRangeSpecification : SpecificationBase<User>
{
    public UsersByAgeRangeSpecification(int minAge, int maxAge)
    {
        AddFilter(u => u.Age >= minAge && u.Age <= maxAge);
    }
}

// Неправильно — хардкод значений
public class AdultUsersSpecification : SpecificationBase<User>
{
    public AdultUsersSpecification()
    {
        AddFilter(u => u.Age >= 18 && u.Age <= 65); // Хардкод!
    }
}
```

### 3. Композиция фильтров через AddFilter

```csharp
// Каждый вызов AddFilter добавляет фильтр в список.
// При выполнении запроса все фильтры объединяются через AND.
public class ActivePremiumUsersSpecification : SpecificationBase<User>
{
    public ActivePremiumUsersSpecification()
    {
        AddFilter(u => u.IsActive);
        AddFilter(u => u.IsPremium);
    }
}
```

### 4. Переопределяйте BuildOptions для настройки QueryOptions

```csharp
// Доступ к WithTracking, AsSplitQuery, Distinct и другим свойствам
public class DetailedUserSpecification : SpecificationBase<User>
{
    public DetailedUserSpecification()
    {
        AddInclude(u => u.Profile);
        AddInclude(u => u.Roles);
        AddFilter(u => u.IsActive);
    }

    public override QueryOptions<User> BuildOptions()
    {
        var options = base.BuildOptions();
        options.AsSplitQuery = true;
        options.WithTracking = false;
        return options;
    }
}
```

### 5. Тестируемость спецификаций

```csharp
[Fact]
public void ActiveUsersSpecification_BuildOptions_ReturnsFilter()
{
    // Arrange
    var spec = new ActiveUsersSpecification();

    // Act
    var options = spec.BuildOptions();

    // Assert
    options.Filters.Should().HaveCount(1);
    options.OrderBy.Should().BeEmpty();
    options.Includes.Should().BeEmpty();
}
```

## Отличия от прямого использования QueryOptions

| Аспект | SpecificationBase | QueryOptions напрямую |
|--------|-------------------|----------------------|
| **Назначение** | Инкапсуляция бизнес-критериев | Настройки запроса |
| **Переиспользование** | Высокое (отдельный класс) | Низкое (inline создание) |
| **Тестируемость** | Легко тестировать отдельно | Сложнее тестировать |
| **Параметризация** | Через конструктор | Через fluent-методы |
| **DI-friendly** | Да, регистрируется как сервис | Нет, создаётся inline |

## См. также

| Документ | Описание |
|----------|----------|
| [Repository Pattern](repository.md) | Использование спецификаций с репозиторием |
| [Unit of Work](unit-of-work.md) | Координация транзакций и репозиториев |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Подробное руководство по фильтрации и сортировке |
| [CQRS](cqrs.md) | Разделение команд и запросов |