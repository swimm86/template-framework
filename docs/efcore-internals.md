# EF Core Internals

**Namespace:** `Shared.Infrastructure.Dal.EFCore`  
**Assembly:** `Shared.Infrastructure.Dal.EFCore`

---

## Обзор

EF Core слой Shared — это абстракция над Entity Framework Core, обеспечивающая:

- Автоматическую конфигурацию DbContext (conventions, configurations, logging)
- Единый query building pipeline через `EfQueryEvaluator`
- Snake_case naming convention для всех колонок
- Audit columns auto-configuration
- Transaction management в Unit of Work
- Extension points для кастомизации

```
DbContextBase
├── OnModelCreating → ApplyConfigurationsFromAssembly
├── ConfigureConventions → ColumnsNamesConvention (snake_case)
└── OnConfiguring → SensitiveDataLogging (Dev), DateCulture (en-US)

EfQueryEvaluator
├── CustomQueryBeforeProcesses
├── Filters (aggregated)
├── Includes (untyped via IIncludable)
├── AsSplitQuery (+ auto OrderBy)
├── OrderBy (first + ThenBy)
├── AsNoTracking
├── CustomQueryPostProcesses
├── Distinct / DistinctBy
└── Projection via IMapper
```

---

## DbContextBase

Базовый класс для всех DbContext в проекте:

```csharp
public abstract class DbContextBase(
    DbContextOptions options,
    IHostEnvironment environment)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetCallingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (environment.IsDevelopment())
            optionsBuilder.EnableSensitiveDataLogging();

        ConfigureDateCulture();
        base.OnConfiguring(optionsBuilder);
    }

    private static void ConfigureDateCulture()
    {
        var cultureInfo = new CultureInfo("en-US")
        {
            DateTimeFormat = { ShortDatePattern = "dd/MM/yyyy" }
        };
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    }
}
```

### Автоматическая конфигурация

| Метод | Что делает |
|-------|-----------|
| `OnModelCreating` | Применяет все `EntityConfigurationBase<>` из calling assembly |
| `ConfigureConventions` | Регистрирует `ColumnsNamesConvention` (snake_case для всех колонок) |
| `OnConfiguring` | Включает `EnableSensitiveDataLogging` в Development, настраивает date culture |

**Важно:** `ApplyConfigurationsFromAssembly` использует `Assembly.GetCallingAssembly()` — конфигурации должны быть в той же сборке, что и производный DbContext.

---

## EntityConfigurationBase

Базовая конфигурация для каждой сущности:

```csharp
public abstract class EntityConfigurationBase<TEntity>
    : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.UseTptMappingStrategy();
        builder.HasKey(idName);
        builder.Property(idName).ValueGeneratedNever().HasComment("Идентификатор.");

        ConfigureLifecycleActions(builder);
        ConfigureMeta(builder);
        ConfigureProcess(builder);
    }

    protected virtual void ConfigureProcess(EntityTypeBuilder<TEntity> builder) { }
}
```

### TPT Mapping Strategy

Все сущности используют **Table-Per-Type** наследование:

```csharp
builder.UseTptMappingStrategy();
```

### Id конфигурация

```csharp
builder.Property(e => e.Id).ValueGeneratedNever();
```

Guid генерируется **на стороне приложения** (не database-generated).

### Audit Columns

Автоматическая конфигурация audit-полей на основе реализуемых интерфейсов:

| Интерфейс | Колонка | Required | Comment |
|-----------|---------|----------|---------|
| `IWithCreated` | `created_by` | ✅ | Идентификатор пользователя-создателя |
| `IWithCreated` | `created_by_user_name` | ✅ | Имя пользователя-создателя |
| `IWithUpdated` | `updated_by` | ❌ | Идентификатор пользователя-обновителя |
| `IWithDeleted` | `deleted_by_id` | ❌ | Идентификатор пользователя-удалителя |
| `IWithDateCreated` | `created_at` | ✅ | Время создания |
| `IWithDateUpdated` | `updated_at` | ❌ | Время обновления |
| `IWithDateDeleted` | `deleted_at` | ❌ | Время удаления |

### Lifecycle Actions Ignoring

```csharp
private static void ConfigureLifecycleActions(EntityTypeBuilder builder)
{
    if (typeof(IWithLifecycleActions).IsAssignableFrom(typeof(TEntity)))
    {
        builder.Ignore(nameof(IWithLifecycleActions.RequiredToSaveNavigationPropertiesNames));
    }
}
```

Свойства `IWithLifecycleActions` игнорируются EF Core — они не маппятся на колонки.

### Extension Point: ConfigureProcess

```csharp
// В кастомной конфигурации:
public class OrderConfiguration : EntityConfigurationBase<Order>
{
    protected override void ConfigureProcess(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId);
    }
}
```

---

## ColumnsNamesConvention

EF Core convention, конвертирующая все имена колонок в snake_case:

```csharp
public class ColumnsNamesConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        modelBuilder.Metadata
            .GetEntityTypes()
            .ToList()
            .ForEach(entity =>
                entity.GetProperties().ForEach(prop =>
                    prop.SetColumnName(prop.GetColumnName().ToSnakeCase())));
    }
}
```

**Результат:**

| C# Property | DB Column |
|-------------|-----------|
| `CreatedDate` | `created_date` |
| `UserId` | `user_id` |
| `OrderNumber` | `order_number` |

Convention применяется на этапе `ModelFinalizing` — после всех конфигураций, но до генерации миграций.

---

## EfQueryEvaluator — Query Building Pipeline

`EfQueryEvaluator` — центральный компонент построения запросов:

```csharp
public class EfQueryEvaluator(IMapper mapper) : IQueryEvaluator
{
    public IQueryable<TEntity> Build<TEntity>(
        IQueryable<TEntity> queryable,
        QueryOptions<TEntity>? options = null)
        where TEntity : class, IEntity
    {
        if (options is null) return queryable;

        // 1. Custom pre-processes
        queryable = options.CustomQueryBeforeProcesses
            .Aggregate(queryable, (acc, func) => func(acc));

        // 2. Filters (aggregated)
        queryable = options.Filters
            .Aggregate(queryable, (acc, x) => acc.Where(x));

        // 3. Includes (untyped)
        queryable = options.Includes
            .Aggregate(queryable, (acc, x) => acc.IncludeUntyped(x));

        // 4. SplitQuery (+ auto OrderBy)
        if (options.AsSplitQuery)
        {
            queryable = queryable.AsSplitQuery();
            if (!options.OrderBy.Any() && options.Includes.Any())
                options.AddOrderBy(x => x.Id, OrderDirectionType.Ascending);
        }

        // 5. Ordering (first + ThenBy)
        if (options.OrderBy.Count != 0)
        {
            var first = options.OrderBy.First();
            var ordered = first.Direction == OrderDirectionType.Ascending
                ? queryable.OrderBy(first.Expression)
                : queryable.OrderByDescending(first.Expression);

            queryable = options.OrderBy.Count == 1
                ? ordered
                : options.OrderBy.Skip(1)
                    .Aggregate(ordered, (acc, x) =>
                        x.Direction == OrderDirectionType.Ascending
                            ? acc.ThenBy(x.Expression)
                            : acc.ThenByDescending(x.Expression));
        }

        // 6. Tracking
        if (!options.WithTracking)
            queryable = queryable.AsNoTracking();

        // 7. Custom post-processes
        queryable = options.CustomQueryPostProcesses
            .Aggregate(queryable, (acc, func) => func(acc));

        // 8. Distinct
        if (options.Distinct)
            queryable = queryable.Distinct();

        // 9. DistinctBy (GroupBy.First)
        if (options.DistinctBy is not null)
            queryable = queryable.GroupBy(options.DistinctBy).Select(g => g.First());

        return queryable;
    }
}
```

### Pipeline Order

```
CustomQueryBeforeProcesses → Filters → Includes → SplitQuery → OrderBy → AsNoTracking → CustomQueryPostProcesses → Distinct → DistinctBy
```

**Почему порядок важен:**
- `AsNoTracking` применяется **после** Includes — чтобы Include'и тоже были no-tracking
- `OrderBy` применяется **до** `AsNoTracking` — для корректной работы SplitQuery
- `DistinctBy` в конце — `GroupBy.First` требует, чтобы все предыдущие операции уже были применены

### SplitQuery Auto-OrderBy

```csharp
if (options.AsSplitQuery && !options.OrderBy.Any() && options.Includes.Any())
{
    options.AddOrderBy(x => x.Id, OrderDirectionType.Ascending);
}
```

EF Core требует ORDER BY при SplitQuery с Includes для стабильной пагинации связанных сущностей.

### DistinctBy Implementation

```csharp
if (options.DistinctBy is not null)
    queryable = queryable.GroupBy(options.DistinctBy).Select(g => g.First());
```

Эквивалент `DISTINCT ON` в PostgreSQL — группировка по выражению + первый элемент из каждой группы.

### QueryOptions<TEntity>

Модель настроек запроса:

```csharp
public class QueryOptions<TEntity>(
    bool withTracking = false,
    bool asSplitQuery = false,
    bool distinct = false)
    where TEntity : IEntity
{
    public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> CustomQueryBeforeProcesses { get; }
    public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> CustomQueryPostProcesses { get; }
    public List<Expression<Func<TEntity, bool>>> Filters { get; }
    public List<QueryOrderByOption<TEntity>> OrderBy { get; }
    public List<IIncludable<TEntity>> Includes { get; }
    public bool WithTracking { get; set; }
    public bool AsSplitQuery { get; set; }
    public bool Distinct { get; set; }
    public Expression<Func<TEntity, bool>>? DistinctBy { get; set; }

    // Fluent API
    public QueryOptions<TEntity> AddFilter(Expression<Func<TEntity, bool>> expression);
    public QueryOptions<TEntity> AddFilterIf(bool condition, Expression<Func<TEntity, bool>> expression);
    public QueryOptions<TEntity> AddOrderBy(Expression<Func<TEntity, object>> expression, OrderDirectionType direction, int? index = null);
    public QueryOptions<TEntity> AddOrderByIf(bool condition, Expression<Func<TEntity, object>> expression, OrderDirectionType direction, int? index = null);
    public Includable<TEntity, TProperty> AddInclude<TProperty>(Expression<Func<TEntity, TProperty>> expression);
}
```

### BuildWithTransform — Projection

```csharp
public IQueryable<TOut> BuildWithTransform<TEntity, TOut>(
    IQueryable<TEntity> queryable,
    QueryOptions<TEntity>? options = null,
    object? parameters = null)
    where TEntity : class, IEntity
    => mapper.ProjectTo<TOut>(Build(queryable, options), parameters);

public IQueryable<TOut> BuildWithTransform<TEntity, TIntermediate, TOut>(
    IQueryable<TEntity> queryable,
    Func<IQueryable<TEntity>, IQueryable<TIntermediate>> postBuildProcess,
    QueryOptions<TEntity>? options = null)
    where TEntity : class, IEntity
    => mapper.ProjectTo<TOut>(postBuildProcess(Build(queryable, options)));
```

Проекция через `IMapper.ProjectTo` — трансляция Expression Tree для server-side projection.

---

## DbUpdaterBase — Миграции

```csharp
public abstract class DbUpdaterBase(DbContext dbContext)
    : IDbUpdater, IDisposable, IAsyncDisposable
{
    public void CreateDbIfNotExists()
    {
        if (!dbContext.Database.GetPendingMigrations().Any())
            dbContext.Database.EnsureCreated();
    }

    public virtual void Migrate()
    {
        if (dbContext.Database.GetPendingMigrations().Any())
            dbContext.Database.Migrate();
    }

    public virtual void Initialize() { }
}
```

| Метод | Когда использовать |
|-------|-------------------|
| `CreateDbIfNotExists` | Первый запуск, нет миграций — создаёт БД по модели |
| `Migrate` | Есть pending migrations — применяет их |
| `Initialize` | Override point для кастомной инициализации (seed data, индексы) |

### [MigrationAssembly] Attribute

```csharp
[AttributeUsage(AttributeTargets.Assembly)]
public class MigrationAssemblyAttribute : Attribute;
```

Помечает сборку, содержащую EF Core миграции. Используется в `ServiceCollectionExtensions.AddDbContexts()` для автоматического обнаружения:

```csharp
var migrationAssembly = AppDomain.CurrentDomain
    .GetAssemblies()
    .FirstOrDefault(a => a.GetCustomAttributes(typeof(MigrationAssemblyAttribute), false).Any());
```

---

## Extension Points

### IBeforeSaveChangesService

Pre-save hook — вызывается перед `SaveChangesAsync` в EfUnitOfWork:

```csharp
public interface IBeforeSaveChangesService
{
    Task ProcessAsync(DbContext dbContext, CancellationToken cancellationToken = default);
    void Process(DbContext dbContext);
}
```

**Use cases:**
- Аудит изменений (audit trail)
- Обновление audit columns (`created_at`, `updated_by`)
- Валидация cross-entity constraints
- Каскадные операции, не покрытые lifecycle actions

**Регистрация:** внедряется в `EfUnitOfWork` через constructor:

```csharp
public EfUnitOfWork(
    TDbContext dbContext,
    IServiceProvider serviceProvider,
    DbSettingsBase settings,
    IBeforeSaveChangesService? beforeSaveChangesService = default)
```

### IDbContextOptionsBuilderInitializer

Кастомная инициализация DbContext options:

```csharp
public interface IDbContextOptionsBuilderInitializer
{
    void Initialize<TSettings>(
        DbContextOptionsBuilder options,
        string migrationAssemblyName)
        where TSettings : DbSettingsBase;
}
```

**Use cases:**
- Кастомный provider configuration (Npgsql, SqlServer)
- Interceptors (soft delete, audit)
- Performance tuning (MaxBatchSize, CommandTimeout)

### EfDbSettingsBase

```csharp
public abstract class EfDbSettingsBase<TDbContext> : DbSettingsBase
    where TDbContext : DbContextBase;
```

Наследует `DbSettingsBase` и добавляет типизацию по DbContext. `DbSettingsBase` содержит `TransactionsEnabled` — флаг управления транзакциями.

---

## ServiceCollectionExtensions — Автоматическая регистрация

```csharp
public static IServiceCollection AddDbContexts(this IServiceCollection serviceCollection)
{
    var migrationAssembly = AppDomain.CurrentDomain
        .GetAssemblies()
        .FirstOrDefault(a => a.GetCustomAttributes(typeof(MigrationAssemblyAttribute), false).Any());

    AssemblyHelper.GetDerivedTypesFromAssemblies<DbContextBase>(
            excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
        .ForEach(type =>
        {
            var settings = AssemblyHelper.GetDerivedTypesFromAssemblies(
                    typeof(EfDbSettingsBase<>).MakeGenericType(type),
                    excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
                .FirstOrDefault();

            var migrationAssemblyName = (migrationAssembly ?? type.Assembly).FullName;

            typeof(ServiceCollectionExtensions)
                .GetMethods()
                .First(m => m is { Name: nameof(AddDbContext), IsGenericMethod: true })
                .MakeGenericMethod(settings, type)
                .Invoke(null, [serviceCollection, migrationAssemblyName]);
        });

    return serviceCollection;
}
```

**Что регистрируется:**
- `IDbContextFactory<TContext>` — factory для безопасного извлечения DbContext
- `TContext` (scoped) — через factory
- `DbContext` (scoped) — alias для базового типа
- `IRepository<>` → `EfRepository<>`
- `IUnitOfWork` → `EfUnitOfWork<TContext>`

---

## RepositoryExtensions

### ProcessBatchesAsync

```csharp
public static Task ProcessBatchesAsync<TEntity>(
    this IRepository<TEntity> repository,
    QueryOptions<TEntity> options,
    int batchSize = Constants.DefaultBatchSize,
    Func<ICollection<TEntity>, Task>? processBatchAction = null,
    CancellationToken cancellationToken = default)
    where TEntity : class, IEntity
{
    return BatchHelper.ProcessBatchesAsync(
        async (skip, take) => await repository.GetRangeAsync(options, skip, take, cancellationToken),
        batchSize,
        processBatchAction,
        cancellationToken);
}
```

Обработка сущностей батчами — pagination через `skip/take`, один батч = один запрос.

### GetByIdOrThrowAsync

```csharp
public static async Task<TEntity> GetByIdOrThrowAsync<TEntity, TKey>(
    this IRepository<TEntity> repository,
    TKey id,
    QueryOptions<TEntity>? options = null,
    CancellationToken cancellationToken = default)
    where TEntity : class, IEntity
{
    var entity = await repository.GetAsync(id, options, cancellationToken);
    if (entity is null)
        throw new NotFoundException(typeof(TEntity), id);
    return entity;
}
```

### UpdateNavigationPropertiesAsync

Синхронизация navigation properties на основе DTO:

```csharp
public static Task UpdateNavigationPropertiesAsync<TEntity, TNavigationDto, TNavigationEntity>(
    this IRepository<TNavigationEntity> repository,
    ICollection<TEntity> entities,
    ICollection<TNavigationDto> navigationDtos,
    Func<TEntity, TNavigationEntity, bool> comparisonFunc,
    IMapper mapper,
    Action<TEntity, TNavigationEntity>? addAction = null,
    Action<TEntity, TNavigationEntity>? removeAction = null,
    CancellationToken cancellationToken = default);
```

**Логика:**
1. Загружает существующие navigation entities
2. `MergeAsync` — добавляет новые, удаляет отсутствующие, обновляет существующие
3. Применяет `addAction`/`removeAction` к parent entities

---

## QueryableExtensions

### IncludeUntyped

Type-safe Include без дженерик-параметров на каждом уровне:

```csharp
public static IQueryable<TEntity> IncludeUntyped<TEntity>(
    this IQueryable<TEntity> queryable,
    IIncludable<TEntity> includable)
{
    // Reflection-based вызов EF Core Include/ThenInclude
    var genericInclude = IncludeMethod.MakeGenericMethod(entityType, propertyType);
    var result = genericInclude.Invoke(null, [queryable, includable.Expression]);

    // Chain ThenInclude для вложенных свойств
    var child = includable.Child;
    while (child is not null) { ... }

    return (IQueryable<TEntity>)result;
}
```

Позволяет строить Include chains динамически — используется в `EfQueryEvaluator` для применения `options.Includes`.

---

## ExpressionExtensions

### WrapWithCollate

Применение PostgreSQL collation к строковым полям:

```csharp
public static Expression<Func<TEntity, object>> WrapWithCollate<TEntity>(
    this Expression<Func<TEntity, object>> expression,
    string collation)
{
    var visitor = new CollateExpressionVisitor(collation);
    var newBody = visitor.Visit(expression.Body);
    return Expression.Lambda<Func<TEntity, object>>(newBody, expression.Parameters);
}
```

**Использование:**

```csharp
// Применяет collation "und-x-icu" к строковым полям
query.OrderBy(x => x.Name.WrapWithCollate("und-x-icu"));
```

---

### Базовый класс для DI-регистрации: EfCoreDependencyInjectorBase

`EfCoreDependencyInjectorBase` — абстрактный базовый класс для DI-регистрации EF Core. Автоматически:
- Обнаруживает `DbContextBase` в сборке
- Обнаруживает `DbSettingsBase` для конфигурации подключения
- Регистрирует репозитории через `AddRepositories()`
- Регистрирует `IDbUpdater` для миграций

### Специфичная конфигурация PostgreSQL

`Shared.Infrastructure.Dal.EFCore.Postgres` предоставляет `DbContextOptionsBuilderInitializer` — настройку `DbContextOptionsBuilder` для PostgreSQL:
- Npgsql типы данных (NpgsqlDate, NpgsqlInet и т.д.)
- Стратегия команд (`CommandTimeout`)
- Пулы подключений

Используется при регистрации контекста в DI.

---

## См. также

| Документ | Описание |
|----------|----------|
| [Repository Pattern](repository.md) | Repository pattern и IRepository<> |
| [Unit of Work](unit-of-work.md) | Unit of Work и транзакции |
| [Entity Interfaces](entity-interfaces.md) | Интерфейсы сущностей (IEntity, IWithCreated, IWithDeleted) |
| [DB Seeder](db-seeder.md) | Seed data и начальная инициализация БД |
