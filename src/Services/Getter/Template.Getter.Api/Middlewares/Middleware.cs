// ----------------------------------------------------------------------------------------------
// <copyright file="Middleware.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Template.Getter.Api.Middlewares;

/// <summary>
/// Мидлвар.
/// </summary>
public class Middleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="next">Следующий делегат в цепочке вызовов.</param>
    public Middleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>
    /// Логика мидлвара.
    /// </summary>
    /// <param name="context">Http контекст.</param>
    /// <returns><see cref="Task"/>.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        return _next.Invoke(context);
    }
}