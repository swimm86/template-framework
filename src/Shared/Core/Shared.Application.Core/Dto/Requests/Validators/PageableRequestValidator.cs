// ----------------------------------------------------------------------------------------------
// <copyright file="PageableRequestValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;

namespace Shared.Application.Core.Dto.Requests.Validators;

/// <summary>
/// Валидатор базовых инвариантов <see cref="PageableRequest"/>.
/// </summary>
/// <typeparam name="TPageable">
/// Конкретный тип наследника <see cref="PageableRequest"/>. Generic-параметр
/// обязателен для корректной работы <c>Include</c> в наследниках: FluentValidation
/// включает правила только между валидаторами, валидирующими один и тот же тип.
/// </typeparam>
/// <remarks>
/// <para>
/// Проверяет инварианты, общие для всех наследников <see cref="PageableRequest"/>:
/// номер страницы и размер страницы должны быть положительными числами.
/// </para>
/// <para>
/// Конкретные наследники подключают правила через наследование:
/// <c>class XxxRequestValidator : PageableRequestValidator&lt;XxxRequest&gt;</c>.
/// При инстанцировании через DI закрытый generic <c>PageableRequestValidator&lt;XxxRequest&gt;</c>
/// регистрируется автоматически, что позволяет <c>SetValidator</c> в родительских валидаторах
/// находить базовые правила через цепочку <c>IValidator&lt;XxxRequest&gt;</c>.
/// </para>
/// </remarks>
public class PageableRequestValidator<TPageable>
    : AbstractValidator<TPageable>
    where TPageable : PageableRequest
{
    /// <summary>
    /// Текст ошибки для <see cref="PageableRequest.PageNumber"/>, не являющегося положительным числом.
    /// </summary>
    public const string PageNumberMustBePositive = "Номер страницы должен быть положительным числом.";

    /// <summary>
    /// Текст ошибки для <see cref="PageableRequest.PageSize"/>, не являющегося положительным числом.
    /// </summary>
    public const string PageSizeMustBePositive = "Размер страницы должен быть положительным числом.";

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="PageableRequestValidator{TPageable}"/>.
    /// </summary>
    public PageableRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage(PageNumberMustBePositive);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage(PageSizeMustBePositive);
    }
}
