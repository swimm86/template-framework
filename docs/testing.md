# Руководство по тестированию

## Стек тестирования

### Core:
- **xUnit** — test discovery, fact/theory, async support.
- **FluentAssertions 7.x** — читаемые ассерты. Не переходить на 8.x+ без одобрения команды (commercial license).
- **coverlet.collector** с `src/Tests/test.runsettings` — сбор покрытия.

### Mocking & Test Data:
- **NSubstitute** — для сложных зависимостей с поведением (external services, message brokers).
- **Ручные Fakes** — для простых интерфейсов (репозитории, unit of work). Предпочтительнее моков когда возможно.
- **Test Data Builders** — explicit static factory-методы для создания тестовых данных. **Не использовать AutoFixture** — он создаёт "liar tests" с нерелевантными данными.

### Integration Testing:
- **Testcontainers** — disposable Docker-контейнеры (Postgres, Redis, RabbitMQ).
- **Respawn** — быстрая очистка БД между тестами (быстрее DELETE).

### Additional:
- **Verify** — snapshot testing для сложных объектов, JSON, exception messages.
- **Microsoft.Extensions.TimeProvider.Testing** — тестирование кода с `DateTime` без `DateTime.UtcNow` хаков.
- **Stryker.NET** — mutation testing для критичных библиотек (quality > coverage).

---

## Test Data Builders

Вместо AutoFixture используйте **explicit static factory-методы**:

```csharp
internal static class OrderBuilder
{
    public static CreateOrderCommand ValidCommand(Guid? customerId = null) =>
        new(
            CustomerId: customerId ?? Guid.NewGuid(),
            Items: new List<CreateOrderItemDto> {
                new() { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 100m }
            }
        );

    public static CreateOrderCommand WithEmptyItems() =>
        ValidCommand().With(x => x.Items, new List<CreateOrderItemDto>());

    public static CreateOrderCommand WithQuantity(int quantity) =>
        ValidCommand().With(x => x.Items, new List<CreateOrderItemDto> {
            new() { ProductId = Guid.NewGuid(), Quantity = quantity, UnitPrice = 100m }
        });
}

// Extension для удобной модификации
internal static class BuilderExtensions
{
    public static T With<T, TProp>(this T obj, Func<T, TProp> prop, TProp value)
    {
        var clone = obj with { }; // record copy
        // Для non-records: использовать reflection или ручные With-методы
        return clone;
    }
}
```

### Правила Test Data Builders:

- **Один builder = один aggregate/command**. Не создавать универсальные генераторы.
- **`Valid()`** — минимальный набор данных для успешного сценария.
- **`WithXxx()`** — модификация одного поля для edge case.
- **Никаких случайных данных** — каждый value должен быть понятен из контекста теста.
- Builder'ы располагаются в тестовом проекте, в папке `Builders/`.

---

## Ручные Fakes

Вместо моков для простых интерфейсов используйте **Fakes** — реализации интерфейсов с in-memory хранением:

```csharp
internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = new();

    public IReadOnlyList<Order> Orders => _orders.AsReadOnly();
    public Order? LastAdded => _orders.LastOrDefault();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));

    public Task AddAsync(Order order, CancellationToken ct = default)
    {
        _orders.Add(order);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        var index = _orders.FindIndex(o => o.Id == order.Id);
        if (index >= 0) _orders[index] = order;
        return Task.CompletedTask;
    }
}
```

### Когда использовать Fakes vs NSubstitute:

| Ситуация | Подход | Почему |
|----------|--------|--------|
| Репозиторий, CRUD | **Fake** | Простая in-memory коллекция |
| Unit of Work | **Fake** | Просто track'ит вызовы SaveChanges |
| External HTTP API | **NSubstitute** | Нужно симулировать ошибки, таймауты |
| Message Bus | **NSubstitute** | Нужно проверить publish с определёнными данными |
| File System | **Fake** | In-memory dictionary |
| Cache | **Fake** | In-memory dictionary |

### Правила Fakes:

- Fake должен быть **простым** — без сложной логики, только хранение данных.
- Fake должен **expose state** для ассертов (`Orders`, `LastAdded`, `SaveChangesCalled`).
- Fake **не должен** содержать бизнес-логику — только imitate поведение хранилища.
- Fake **thread-safe** не требуется — unit-тесты однопоточные.

## AAA-паттерн (Arrange-Act-Assert)

Каждый тест **обязан** следовать AAA-паттерну с явными регионами:

```csharp
[Fact]
public async Task Handle_WithValidRequest_ShouldReturnSuccess()
{
    // Arrange
    var command = OrderBuilder.ValidCommand();
    var fakeRepo = new FakeOrderRepository();
    var handler = new CreateOrderCommandHandler(fakeRepo);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    fakeRepo.LastAdded.Should().NotBeNull();
    fakeRepo.LastAdded.CustomerId.Should().Be(command.CustomerId);
}
```

### Правила AAA:

- **Arrange**: подготовка данных через Test Data Builders, Fakes, NSubstitute. Только setup, без бизнес-логики.
- **Act**: один вызов тестируемого метода. Никакой дополнительной логики.
- **Assert**: проверка результата и state Fakes. Каждый ассерт — одна проверка.
- Пустая строка между секциями **обязательна** для визуального разделения.
- Комментарии `// Arrange`, `// Act`, `// Assert` **обязательны** в каждом тесте.

## XML-документация тестов

Каждый тестовый класс и метод **должен** иметь XML-документацию:

```csharp
/// <summary>
/// Тестирует обработку команды создания заказа.
/// </summary>
public class CreateOrderCommandHandlerTests
{
    /// <summary>
    /// Успешное создание заказа при валидных данных.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
}
```

### Правила XML-документации:

- `<summary>` для каждого тестового класса — описывает **что** тестируется.
- `<summary>` для каждого тестового метода — описывает **сценарий и ожидаемый результат**.
- Документация на **русском** языке (технические термины на английском).
- Не дублировать имя метода — объяснять **бизнес-сценарий**.

### Примеры хорошей документации:

| ❌ Плохо | ✅ Хорошо |
|----------|----------|
| `<summary>Тест хендлера</summary>` | `<summary>Возвращает ошибку валидации при пустом имени клиента</summary>` |
| `<summary>Проверка null</summary>` | `<summary>Выбрасывает ArgumentNullException при null customerId</summary>` |
| Без документации | `<summary>Успешно создаёт заказ и сохраняет в репозиторий</summary>` |

## Стиль ассертов

- Предпочитайте `actual.Should().Be(expected)` вместо `Assert.Equal(expected, actual)`.
- Предпочитайте ассерты для коллекций, такие как `Equal`, `ContainSingle`, `OnlyContain` и `BeEquivalentTo`, когда они дают более понятные ошибки.
- Используйте `BeEquivalentTo` для DTO и сериализованных/десериализованных структур. Не используйте его для сокрытия значимых различий в контрактах.
- Используйте `act.Should().Throw<TException>()` для тестов исключений и ассертите соответствующий `ParamName` или фрагмент сообщения, когда это часть контракта.
- **One assertion focus**: один Act, одна проверяемая концепция. Несколько ассертов допустимы только если они проверяют один аспект (например, `result.IsSuccess` + `result.Value`).

## Принципы проектирования тестов

### Test behavior, not implementation (Google SEB)

- Тестируйте через **публичные API**, а не через внутренние методы.
- Проверяйте **состояние/результат**, а не порядок вызовов моков.
- Тест должен падать только при изменении поведения, а не при рефакторинге.
- **Strive for unchanging tests**: после написания тест не должен меняться, если не изменились требования к системе.

### DAMP > DRY (Google SEB)

- Тесты должны быть "Descriptive And Meaningful Phrases", а не максимально DRY.
- Небольшая дупликация в тестах **допустима**, если она делает тест понятнее.
- Не выносите setup в общие методы, если это ухудшает читаемость отдельного теста.

### Minimally passing tests (Microsoft)

- Используйте **минимум данных** для прохождения теста — никаких лишних полей через Test Data Builders.
- Тест не должен зависеть от данных, которые не относятся к проверяемому поведению.

### No logic in tests (Microsoft)

- **Запрещена** условная логика (`if`, `switch`), циклы, тернарные операторы в телах тестов.
- Баг в тесте хуже, чем отсутствие теста — тест должен быть простым и очевидным.
- Исключение: `[Theory]`/`[InlineData]` для data-driven тестов.

### Mock only what crosses boundaries (Google)

- **Мокировать**: БД, внешние API, файловую систему, объекты с side effects.
- **НЕ мокировать**: Value Objects, DTO, простые объекты, строки, числа.
- Тесты, которые проверяют конфигурацию моков вместо бизнес-логики — бесполезны.

### Helper methods over Setup/Teardown (Microsoft)

- Предпочитать **явные helper-методы** атрибутам `[SetUp]`/`[TearDown]`.
- Каждый тест имеет разные требования к setup — общие методы скрывают зависимости.
- Helper-методы должны возвращать готовые объекты, а не модифицировать shared state.

### Test independence & determinism

- Тесты **не должны зависеть** от порядка выполнения.
- Никакого global state, ambient culture, wall-clock времени, сети, БД, ФС.
- Каждый тест должен работать в изоляции и быть воспроизводимым.

---

## Performance SLA

| Метрика | Целевое значение |
|---------|-----------------|
| Один unit-тест | < 50ms |
| 100 unit-тестов | < 3s |
| Integration test | Допустимо дольше, но с `[Trait("Category", "Integration")]` |

Если unit-тест превышает лимит — он делает реальный I/O и должен стать интеграционным.

---

## Покрытие кода (Coverage)

**Не цель 100% на весь репозиторий.** Дифференцированные цели по слоям:

| Слой | Target | Rationale |
|------|--------|-----------|
| Domain (бизнес-логика) | 90%+ | Высокий риск, высокая ценность |
| Application (use cases) | 85%+ | Важная бизнес-логика |
| Infrastructure | 70%+ | Много boilerplate, маппингов |
| Presentation (API) | 80%+ | HTTP handlers, валидация |
| Generated code | Skip | Нет ценности |
| Third-party wrappers | Skip | Тестировать что вызываем, не что они делают |

> **The coverage trap**: 100% coverage не означает что код корректен — означает только что каждая строка была выполнена. Aim for meaningful assertions, not coverage numbers.

---

## Flaky Test Policy

Flaky-тесты = **production bug в тестах**.

- ❌ Никаких `Thread.Sleep()` для ожидания состояния.
- ❌ Никакого игнорирования failures через `[Fact(Skip = "...")]` без issue.
- ❌ Никаких retry без выявления root cause.
- ✅ Flaky-тест должен быть исправлен или удалён в том же PR.
- ✅ Если тест flaky — проблема в тесте или в коде, а не в CI.

---

## Проектирование тестов

- Тестируйте публичные контракты, а не детали реализации.
- Каждый тест должен падать при реальной регрессии. Избегайте слабых ассертов, таких как только `NotNull` или только "не выбрасывает исключение".
- Unit-тесты должны быть детерминированными: без wall-clock времени, сети, базы данных, файловой системы или ambient culture, если это не явное поведение под тестом.
- Используйте `[InlineData]` для простых скалярных случаев и `TheoryData` для типизированных или nullable случаев.
- Не передавайте `null` через `[InlineData]` в non-nullable параметры. Сигнатура тестового метода должна соответствовать API-контракту.
- Делайте граничные случаи явными: `null`, пустые строки, пробелы, регистр, дублирующиеся значения, невалидный ввод, culture-sensitive парсинг и граничные числовые значения.

## Категории тестов

- Unit-тесты располагаются рядом с покрываемым проектом и должны быть достаточно быстрыми для каждого PR.
- Интеграционные тесты должны использовать disposable инфраструктуру, такую как Testcontainers, когда требуются внешние зависимости.
- Помечайте интеграционные тесты атрибутом `[Trait("Category", "Integration")]`. Стандартный `src/Tests/test.runsettings` исключает их из PR gate.
- Архитектурные тесты могут быть добавлены для проверки правил направления зависимостей, именования и слоёв, когда эти контракты станут стабильными.

## Команды

Запуск общих unit-тестов:

```powershell
dotnet test src/Tests/Shared/Core/Shared.Common.Tests/Shared.Common.Tests.csproj
```

Запуск с покрытием:

```powershell
dotnet test src/Tests/Shared/Core/Shared.Common.Tests/Shared.Common.Tests.csproj --settings src/Tests/test.runsettings --collect:"XPlat Code Coverage"
```

Запуск интеграционных тестов явно, когда доступен Docker или другая требуемая зависимость:

```powershell
dotnet test src/Tests/Shared/Utils/Shared.Utils.DatabaseUpgrade.Tests/Shared.Utils.DatabaseUpgrade.Tests.csproj --filter "Category=Integration"
```

Рекомендуемый PR gate:

- `dotnet build --no-incremental`
- `dotnet test --settings src/Tests/test.runsettings --collect:"XPlat Code Coverage"`
- Порог покрытия для изменённого/общего кода, а не слепая цель в 100% на весь репозиторий.
- Мутационное тестирование с помощью Stryker.NET для библиотек общего пользования с высоким риском, когда качество тестов важнее, чем сырое покрытие строк.

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Тестирование команд и запросов, pipeline behaviors |
| [Pipeline Behaviors](pipeline-behaviors.md) | ValidationBehavior — ключевой объект для unit-тестов |
| [Exception Mapping](exception-mapping.md) | Тестирование мапперов исключений |
| [Logging](logging.md) | Тестирование LogTask и [LogMethod] |
