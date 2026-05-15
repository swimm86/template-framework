# Руководство по тестированию

## Стек тестирования

- Используйте xUnit для обнаружения тестов, фикстур и data-driven тестов.
- Используйте FluentAssertions для ассертов. Тестовые проекты должны оставаться на `FluentAssertions` 7.x, если команда явно не одобрит коммерческую лицензию для 8.x+.
- Используйте `coverlet.collector` с `src/Tests/test.runsettings` для сбора покрытия.

## Стиль ассертов

- Предпочитайте `actual.Should().Be(expected)` вместо `Assert.Equal(expected, actual)`.
- Предпочитайте ассерты для коллекций, такие как `Equal`, `ContainSingle`, `OnlyContain` и `BeEquivalentTo`, когда они дают более понятные ошибки.
- Используйте `BeEquivalentTo` для DTO и сериализованных/десериализованных структур. Не используйте его для сокрытия значимых различий в контрактах.
- Используйте `act.Should().Throw<TException>()` для тестов исключений и ассертите соответствующий `ParamName` или фрагмент сообщения, когда это часть контракта.

## Проектирование тестов

- Тестируйте публичные контракты, а не детали реализации.
- Каждый тест должен падать при реальной регрессии. Избегайте слабых ассертов, таких как только `NotNull` или только "не выбрасывает исключение".
- Unit-тесты должны быть детерминированными: без.wall-clock времени, сети, базы данных, файловой системы или ambient culture, если это не явное поведение под тестом.
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
