# 📦 Response Types — Иерархия типов ответов API

> **Assembly:** `Shared.Application.Core.dll`  
> **Namespace:** `Shared.Application.Core.Dto.Responses`  
> **Исходники:** `src/Shared/Core/Shared.Application.Core/Dto/Responses/`, `src/Shared/Core/Shared.Application.Core/Dto/Interfaces/`

---

## 1. Обзор

Система типов ответов обеспечивает единообразный формат HTTP-ответов во всех микросервисах. Все response-типы наследуются от `ResponseBase` и содержат `StatusCode`, который используется для установки HTTP-статуса ответа, но **не сериализуется** в JSON (`[JsonIgnore]`).

### Иерархия типов

```
ResponseBase (abstract record)
├── Response
│   └── Response<T>
│       ├── ResponseWithMessage
│       └── PageableResponse<T>
└── ErrorResponse
```

Все типы — `record` с `init`-свойствами, за исключением `ResponseBase.StatusCode` (используется `set`, потому что фреймворк может обновлять его после построения).

---

## 2. Success Responses

### 2.1. `ResponseBase`

Базовый абстрактный record для всех ответов. Содержит только `StatusCode`.

```csharp
public abstract record ResponseBase
{
    protected ResponseBase() { }

    protected ResponseBase(int StatusCode) { this.StatusCode = StatusCode; }

    [JsonIgnore]
    public int StatusCode { get; set; }
}
```

| Свойство | Тип | Сериализуется? | Описание |
|----------|-----|----------------|----------|
| `StatusCode` | `int` | ❌ Нет (`[JsonIgnore]`) | HTTP-статус ответа; используется `set`, чтобы фреймворк мог обновлять его после построения |

### 2.2. `Response`

Пустой ответ с кодом статуса. Используется для операций без возвращаемого значения (`DELETE`, `PUT`).

```csharp
public record Response : ResponseBase
{
    public Response(int statusCode) : base(statusCode) { }
    public Response() { }
}
```

**Пример использования:**

```csharp
[HttpDelete("{id}")]
public async Task<Response> Delete(Guid id, CancellationToken ct)
{
    await _mediator.Send(new DeleteUserCommand(id), ct);
    return new Response(StatusCodes.Status204NoContent);
}
```

### 2.3. `Response<T>`

Ответ с payload. Дженерик-тип `T` — данные, возвращаемые клиенту.

```csharp
public record Response<T> : Response
{
    public Response(T? payload, int statusCode) : base(statusCode)
    {
        Payload = payload;
    }

    public Response() { }

    public T? Payload { get; set; }
}
```

| Свойство | Тип | Сериализуется? | Описание |
|----------|-----|----------------|----------|
| `Payload` | `T?` | ✅ Да | Данные ответа |

> В `Response<T>` свойство `Payload` использует `set` (а не `init`), так как `T?` может быть заменено post-construction в handler'ах (например, в `ReadListQueryHandler.FindAsync`).

**Пример использования:**

```csharp
[HttpGet("{id}")]
public async Task<Response<UserDto>> GetById(Guid id, CancellationToken ct)
{
    var user = await _mediator.Send(new GetUserQuery(id), ct);
    return new Response<UserDto>(user, StatusCodes.Status200OK);
}
```

### 2.4. `ResponseWithMessage`

Ответ с текстовым сообщением. Удобен для операций с подтверждением.

```csharp
public record ResponseWithMessage(
    string? Message = default,
    int StatusCode = StatusCodes.Status200OK)
    : Response(StatusCode);
```

| Свойство | Тип | Сериализуется? | Описание |
|----------|-----|----------------|----------|
| `Message` | `string?` | ✅ Да | Текстовое сообщение |

**Пример использования:**

```csharp
[HttpPost]
public async Task<ResponseWithMessage> Create(CreateUserCommand command, CancellationToken ct)
{
    await _mediator.Send(command, ct);
    return new ResponseWithMessage("Пользователь успешно создан");
}
```

---

## 3. Paginated Responses

### 3.1. `PageableResponse<T>`

Ответ для постраничных результатов. Содержит мета-информацию о пагинации.

```csharp
public record PageableResponse<T> : Response<T>
{
    public PageableResponse() { }

    public PageableResponse(
        int totalPages,
        int pageNumber,
        T? payload,
        int statusCode = StatusCodes.Status200OK)
        : base(payload, statusCode)
    {
        TotalPages = totalPages;
        PageNumber = pageNumber;
    }

    public int TotalPages { get; init; }
    public int PageNumber { get; init; }
}
```

| Свойство | Тип | Описание |
|----------|-----|----------|
| `TotalPages` | `int` | Общее количество страниц |
| `PageNumber` | `int` | Номер текущей страницы |
| `Payload` | `T?` | Данные текущей страницы (наследуется от `Response<T>`) |

**Пример использования в Query Handler:**

```csharp
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PageableResponse<UserDto>>
{
    public async Task<PageableResponse<UserDto>> Handle(
        GetUsersQuery request,
        CancellationToken ct)
    {
        var users = await _repository.GetPagedAsync(request.Page, request.PageSize, ct);
        var totalPages = (int)Math.Ceiling(users.TotalCount / (double)request.PageSize);

        return new PageableResponse<UserDto>(
            totalPages: totalPages,
            pageNumber: request.Page,
            payload: users.Items,
            statusCode: StatusCodes.Status200OK);
    }
}
```

В `ReadListQueryHandler` (`Shared.Application.Cqrs.Core`) построение выполняется автоматически:

```csharp
var response = new TResponse
{
    Payload = dtoList,
    StatusCode = StatusCodes.Status200OK,
    TotalPages = pagesCount,
    PageNumber = query.PageNumber,
};
```

**Пример JSON-ответа:**

```json
{
  "totalPages": 5,
  "pageNumber": 1,
  "payload": [
    { "id": "a1b2c3", "name": "User 1" },
    { "id": "d4e5f6", "name": "User 2" }
  ]
}
```

---

## 4. Error Responses

### 4.1. `ErrorResponse`

Ответ об ошибке, совместимый с **RFC 7807 Problem Details**. Реализует `IWithAdditionalData` для расширенных данных.

```csharp
public record ErrorResponse
    : ResponseBase, IWithAdditionalData
{
    public IReadOnlyCollection<ProblemDetails> Errors { get; init; }
    public string? Details { get; init; }
    public IReadOnlyDictionary<string, object>? AdditionalData { get; init; }
}
```

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Errors` | `IReadOnlyCollection<ProblemDetails>` | Коллекция деталей ошибок (RFC 7807) |
| `Details` | `string?` | Человекочитаемое описание ошибки |
| `AdditionalData` | `IReadOnlyDictionary<string, object>?` | Дополнительные данные (расширение `IWithAdditionalData`) |

> `StatusCode` наследуется от `ResponseBase` как `set` (см. примечание в 2.1).

**Пример JSON-ответа:**

```json
{
  "errors": [
    {
      "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
      "title": "Validation Error",
      "status": 400,
      "detail": "Поле 'Email' имеет неверный формат",
      "instance": "/api/users"
    }
  ],
  "details": "Ошибка валидации входных данных",
  "additionalData": {
    "traceId": "00-abc123-def456",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

**Пример использования в ExceptionMapper:**

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var response = new ErrorResponse
        {
            Errors = new[]
            {
                new ProblemDetails
                {
                    Title = exception?.GetType().Name ?? "Unknown Error",
                    Detail = exception?.Message,
                    Status = context.Response.StatusCode,
                }
            },
            Details = "Произошла непредвиденная ошибка",
            AdditionalData = new Dictionary<string, object>
            {
                ["traceId"] = context.TraceIdentifier,
            },
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

---

## 5. Marker Interfaces (DTO)

### 5.1. `IWithFile`

Маркерный интерфейс для DTO, содержащих файл для загрузки. Свойство `File` — **только для чтения** (`get` без `set`).

```csharp
public interface IWithFile
{
    IFormFile? File { get; }
}
```

**Пример реализации:**

```csharp
public record UploadAvatarRequest : IWithFile
{
    public Guid UserId { get; init; }
    public IFormFile? File { get; init; }
}
```

Используется в:
- `RequestLoggingFilter` — замена `IFormFile` на заглушку `<file>` при логировании.
- `ApiClient.PostFilesAsync()` — автоматическое формирование `multipart/form-data` запроса. Дополнительные свойства DTO добавляются как текстовые поля.

### 5.2. `IWithIdsFilter<TKey>`

**Дженерик-интерфейс** для фильтрации запросов по коллекции идентификаторов. Используется вместе с базовым `ListFilterBase` из `Shared.Application.Cqrs.Core.Abstractions`.

```csharp
public interface IWithIdsFilter<TKey>
{
    ICollection<TKey>? Ids { get; init; }
}
```

> Контракт: `get; init;` (не `set`). Тип ключа параметризуется через `TKey` (например, `Guid`).

Базовая реализация в `Shared.Application.Cqrs.Core`:

```csharp
public abstract record ListFilterBase : IWithIdsFilter<Guid>
{
    public ICollection<Guid>? Ids { get; init; }
}
```

`ReadListQueryHandler` автоматически применяет фильтр по `Ids`, если `TFilter : ListFilterBase` и `Ids.Any()`:

```csharp
protected override QueryOptions<TEntity> ConstructOptions(TQuery request)
{
    var options = base.ConstructOptions(request);
    if (request.Filter is ListFilterBase baseFilter && (baseFilter.Ids?.Any() ?? false))
    {
        options.AddFilter(x => baseFilter.Ids.Any(id => id.Equals(x.Id)));
    }
    return options;
}
```

**Пример реализации (из проекта):**

```csharp
// В отдельном сервисе
public record PersonListFilter : ListFilterBase
{
    public string? Name { get; init; }
    public string? NameContains { get; init; }
    public string? Email { get; init; }
    public string? EmailContains { get; init; }
}
```

Используется в репозиториях для фильтрации:

```csharp
public async Task<IReadOnlyList<User>> GetByIdsAsync(
    ICollection<Guid> ids,
    CancellationToken ct)
{
    return await _dbContext.Users
        .Where(u => ids.Contains(u.Id))
        .ToListAsync(ct);
}
```

---

## 6. Сводная таблица типов

| Тип | Payload | Message | Пагинация | Ошибки | Когда использовать |
|-----|---------|---------|-----------|--------|-------------------|
| `Response` | ❌ | ❌ | ❌ | ❌ | `DELETE`, `PUT` без возврата |
| `Response<T>` | ✅ | ❌ | ❌ | ❌ | `GET` by id, `POST` с возвратом |
| `ResponseWithMessage` | ❌ | ✅ | ❌ | ❌ | Операции с подтверждением |
| `PageableResponse<T>` | ✅ | ❌ | ✅ | ❌ | Списки с пагинацией |
| `ErrorResponse` | ❌ | ❌ | ❌ | ✅ | Обработка ошибок |

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | `CreateResponse<TDto>`, `UpdateResponse<TDto>` для команд |
| [Pipeline Behaviors](pipeline-behaviors.md) | `ValidationException` → `ErrorResponse` |
| [FluentValidation Integration](fluent-validation-integration.md) | Двухуровневая валидация |
| [Exception Mapping](exception-mapping.md) | Маппинг исключений в `ErrorResponse` |
| [Controllers](controllers.md) | Использование response типов в контроллерах |
| [API Client](api-client.md) | Клиент для вызова API (PostFilesAsync) |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Фильтрация и сортировка (IWithIdsFilter) |
