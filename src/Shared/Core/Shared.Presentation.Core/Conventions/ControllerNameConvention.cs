// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerNameConvention.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Shared.Common.Extensions;

namespace Shared.Presentation.Core.Conventions;

/// <summary>
/// Конвенция для нормализации имён контроллеров в маршрутах.
/// </summary>
/// <remarks>
/// <para>
/// Преобразует имена контроллеров следующим образом:
/// </para>
/// <list type="bullet">
/// <item>Удаляет суффикс "Controller" (если присутствует)</item>
/// <item>Преобразует PascalCase в kebab-case (например, "PersonsController" → "persons")</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// PersonsController     → "persons"
/// OrdersController      → "orders"
/// UserProfilesController → "user-profiles"
/// </code>
/// </example>
public class ControllerNameConvention
    : IApplicationModelConvention
{
    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        const string classType = "Controller";
        foreach (var controller in application.Controllers)
        {
            var name = controller.ControllerType.Name;
            if (name.EndsWith(classType))
            {
                name = name[..^classType.Length];
            }

            name = name.ToKebabCase();
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel!.Template =
                    selector.AttributeRouteModel.Template!.Replace("[controller]", name);
            }
        }
    }
}
