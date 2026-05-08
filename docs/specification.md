# Specification Pattern

## Обзор

Specification Pattern в фреймворке Shared предоставляет способ инкапсуляции бизнес-критериев выборки данных в переиспользуемые объекты. Этот паттерн отделяет логику фильтрации, сортировки и включения связанных сущностей от кода доступа к данным.

## Интерфейс ISpecification<TEntity>

Базовый интерфейс определен в `Shared.Domain.Core.Dal.Specification.Interfaces.ISpecification`:

```csharp
public interface ISpecification<TEntity> where TEntity : IEntity
{
    QueryOptions<TEntity> BuildOptions();
}
```

Specification преобразуется в `QueryOptions<TEntity>`, которые используются репозиторием для построения запроса.

## Создание спецификаций

### Базовая спецификация

```csharp
public class ActiveUsersSpecification : ISpecification<User>
{
    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.IsActive && !u.IsDeleted
        };
    }
}
```

### Спецификация с сортировкой

```csharp
public class UsersByRegistrationDateSpecification : ISpecification<User>
{
    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.IsActive,
            OrderBy = new[] { QueryOrderByOption.Desc(u => u.CreatedAt) }
        };
    }
}
```

### Спецификация с includes

```csharp
public class UsersWithProfilesSpecification : ISpecification<User>
{
    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.IsActive,
            Includes = new Expression<Func<User, object>>[]
            {
                u => u.Profile,
                u => u.Roles
            },
            AsSplitQuery = true
        };
    }
}
```

### Параметризированная спецификация

```csharp
public class UsersByRoleSpecification : ISpecification<User>
{
    private readonly Guid _roleId;
    private readonly int _page;
    private readonly int _pageSize;

    public UsersByRoleSpecification(Guid roleId, int page = 1, int pageSize = 20)
    {
        _roleId = roleId;
        _page = page;
        _pageSize = pageSize;
    }

    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.Roles.Any(r => r.Id == _roleId) && u.IsActive,
            OrderBy = new[] { QueryOrderByOption.Asc(u => u.Name) }
        };
    }

    public int Page => _page;
    public int PageSize => _pageSize;
}
```

### Композитная спецификация (AND/OR)

```csharp
public class AdvancedUserSearchSpecification : ISpecification<User>
{
    private readonly SearchCriteria _criteria;

    public AdvancedUserSearchSpecification(SearchCriteria criteria)
    {
        _criteria = criteria;
    }

    public QueryOptions<User> BuildOptions()
    {
        var options = new QueryOptions<User>();

        var predicates = new List<Expression<Func<User, bool>>>();

        if (!string.IsNullOrEmpty(_criteria.Name))
        {
            predicates.Add(u => u.Name.Contains(_criteria.Name));
        }

        if (_criteria.MinAge.HasValue)
        {
            predicates.Add(u => u.Age >= _criteria.MinAge.Value);
        }

        if (_criteria.MaxAge.HasValue)
        {
            predicates.Add(u => u.Age <= _criteria.MaxAge.Value);
        }

        if (_criteria.IsActive.HasValue)
        {
            predicates.Add(u => u.IsActive == _criteria.IsActive.Value);
        }

        // Комбинирование предикатов через AND
        if (predicates.Any())
        {
            options.Filter = predicates.Aggregate(
                PredicateBuilder.And<User>()
            );
        }

        options.OrderBy = new[] { QueryOrderByOption.Asc(u => u.Name) };

        return options;
    }
}

// Вспомогательный класс для комбинации предикатов
public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> And<T>(this IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        return predicates.Aggregate(
            PredicateBuilder.True<T>(),
            (current, predicate) => current.And(predicate)
        );
    }

    public static Expression<Func<T, bool>> Or<T>(this IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        return predicates.Aggregate(
            PredicateBuilder.False<T>(),
            (current, predicate) => current.Or(predicate)
        );
    }

    public static Expression<Func<T, bool>> True<T>() => f => true;
    public static Expression<Func<T, bool>> False<T>() => f => false;

    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter)
        );
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter)
        );
        return Expression.Lambda<Func<T, bool>>(body, parameter);
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

    public async Task<List<User>> GetUsersByRole(Guid roleId, int page, int pageSize)
    {
        var spec = new UsersByRoleSpecification(roleId, page, pageSize);
        return await _repository.GetRangeAsync(
            spec,
            skip: (spec.Page - 1) * spec.PageSize,
            take: spec.PageSize
        );
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
public class GetActiveUsersQuery : IQuery<List<UserDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetActiveUsersQueryHandler : IQueryHandler<GetActiveUsersQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<UserDto>> Handle(GetActiveUsersQuery query, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.GetRepository<User>();
        var spec = new ActiveUsersSpecification();

        var users = await userRepo.GetRangeAsync(
            spec,
            skip: (query.Page - 1) * query.PageSize,
            take: query.PageSize,
            cancellationToken: cancellationToken
        );

        return _mapper.Map<List<UserDto>>(users);
    }
}
```

### Спецификация с проекцией

```csharp
public class UserSummarySpecification : ISpecification<User>
{
    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.IsActive,
            OrderBy = new[] { QueryOrderByOption.Asc(u => u.Name) }
        };
    }
}

// Использование с проекцией
var spec = new UserSummarySpecification();
var summaries = await repository.GetRangeAsync(
    spec,
    skip: 0,
    take: 100,
    selector: u => new UserSummaryDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        CreatedAt = u.CreatedAt
    }
);
```

## Расширенные возможности

### Динамическая сортировка

```csharp
public class SortableUserSpecification : ISpecification<User>
{
    private readonly string? _sortBy;
    private readonly bool _descending;

    public SortableUserSpecification(string? sortBy = "Name", bool descending = false)
    {
        _sortBy = sortBy;
        _descending = descending;
    }

    public QueryOptions<User> BuildOptions()
    {
        var options = new QueryOptions<User>
        {
            Filter = u => u.IsActive
        };

        var orderByOptions = new List<QueryOrderByOption<User>>();

        switch (_sortBy?.ToLower())
        {
            case "name":
                orderByOptions.Add(
                    _descending
                        ? QueryOrderByOption.Desc(u => u.Name)
                        : QueryOrderByOption.Asc(u => u.Name)
                );
                break;
            case "email":
                orderByOptions.Add(
                    _descending
                        ? QueryOrderByOption.Desc(u => u.Email)
                        : QueryOrderByOption.Asc(u => u.Email)
                );
                break;
            case "createdat":
                orderByOptions.Add(
                    _descending
                        ? QueryOrderByOption.Desc(u => u.CreatedAt)
                        : QueryOrderByOption.Asc(u => u.CreatedAt)
                );
                break;
            default:
                orderByOptions.Add(QueryOrderByOption.Asc(u => u.Name));
                break;
        }

        options.OrderBy = orderByOptions.ToArray();
        return options;
    }
}
```

### Спецификация с пагинацией

```csharp
public class PagedUserSpecification : ISpecification<User>
{
    private readonly int _page;
    private readonly int _pageSize;

    public PagedUserSpecification(int page = 1, int pageSize = 20)
    {
        _page = page;
        _pageSize = pageSize;
    }

    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.IsActive,
            OrderBy = new[] { QueryOrderByOption.Asc(u => u.Name) }
        };
    }

    public int Skip => (_page - 1) * _pageSize;
    public int Take => _pageSize;
}

// Использование
var spec = new PagedUserSpecification(page: 2, pageSize: 50);
var users = await repository.GetRangeAsync(
    spec,
    skip: spec.Skip,
    take: spec.Take
);
```

### Кэширование спецификаций

```csharp
public class CachedUserSpecification : ISpecification<User>
{
    private readonly string _cacheKey;

    public CachedUserSpecification(string cacheKey)
    {
        _cacheKey = cacheKey;
    }

    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.IsActive,
            CacheKey = _cacheKey // Предполагаемое свойство для кэширования
        };
    }
}
```

## Best Practices

### 1. Одна ответственность — одна спецификация

```csharp
// Правильно - каждая спецификация решает одну задачу
public class ActiveUsersSpecification : ISpecification<User> { ... }
public class InactiveUsersSpecification : ISpecification<User> { ... }
public class PremiumUsersSpecification : ISpecification<User> { ... }

// Неправильно - слишком много логики в одной спецификации
public class ComplexUserSpecification : ISpecification<User>
{
    // Слишком много условий и параметров
}
```

### 2. Используйте параметризацию для гибкости

```csharp
// Правильно - параметризированная спецификация
public class UsersByAgeRangeSpecification : ISpecification<User>
{
    private readonly int _minAge;
    private readonly int _maxAge;

    public UsersByAgeRangeSpecification(int minAge, int maxAge)
    {
        _minAge = minAge;
        _maxAge = maxAge;
    }

    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.Age >= _minAge && u.Age <= _maxAge
        };
    }
}

// Неправильно - хардкод значений
public class AdultUsersSpecification : ISpecification<User>
{
    public QueryOptions<User> BuildOptions()
    {
        return new QueryOptions<User>
        {
            Filter = u => u.Age >= 18 && u.Age <= 65 // Хардкод!
        };
    }
}
```

### 3. Композиция вместо наследования

```csharp
// Правильно - композиция спецификаций
var baseSpec = new ActiveUsersSpecification();
var roleSpec = new UsersByRoleSpecification(roleId);
// Комбинируйте логику в сервисе

// Неправильно - глубокое наследование
public class ActivePremiumUsersByRoleSpecification : ActiveUsersSpecification
{
    // ...
}
```

### 4. Тестируемость спецификаций

```csharp
[TestClass]
public class ActiveUsersSpecificationTests
{
    [TestMethod]
    public void BuildOptions_ReturnsCorrectFilter()
    {
        // Arrange
        var spec = new ActiveUsersSpecification();

        // Act
        var options = spec.BuildOptions();

        // Assert
        Assert.IsNotNull(options.Filter);
        // Дальнейшая проверка выражения фильтра
    }
}
```

### 5. Избегайте бизнес-логики в спецификациях

```csharp
// Правильно - только критерии выборки
public class RecentOrdersSpecification : ISpecification<Order>
{
    public QueryOptions<Order> BuildOptions()
    {
        return new QueryOptions<Order>
        {
            Filter = o => o.CreatedAt > DateTime.UtcNow.AddDays(-30),
            OrderBy = new[] { QueryOrderByOption.Desc(o => o.CreatedAt) }
        };
    }
}

// Неправильно - бизнес-логика в спецификации
public class ProcessableOrdersSpecification : ISpecification<Order>
{
    public QueryOptions<Order> BuildOptions()
    {
        return new QueryOptions<Order>
        {
            // Смешивание критериев выборки и бизнес-правил
            Filter = o => o.Status == OrderStatus.Pending
                       && o.TotalAmount > 1000
                       && !o.HasFraudRisk() // Бизнес-логика!
        };
    }
}
```

## Отличия от QueryOptions

| Аспект | Specification | QueryOptions |
|--------|--------------|--------------|
| **Назначение** | Инкапсуляция бизнес-критериев | Настройки запроса |
| **Переиспользование** | Высокое (отдельный класс) | Низкое (inline создание) |
| **Тестируемость** | Легко тестировать отдельно | Сложнее тестировать |
| **Параметризация** | Через конструктор | Через свойства |
| **Композиция** | Через комбинацию классов | Через агрегацию свойств |

## См. также

- [Repository Pattern](repository.md) — использование спецификаций с репозиторием
- [Unit of Work Pattern](unit-of-work.md) — координация операций
- [Filtering & Sorting Guide](filtering-sorting-guide.md) — детальное руководство по фильтрации