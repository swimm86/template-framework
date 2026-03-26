// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerTypeConvention.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Presentation.Core.Attributes.Base;

namespace Shared.Presentation.Core.Conventions;

/// <summary>
/// Конвенция для применения атрибутов маршрутизации к шаблонам контроллеров.
/// </summary>
/// <remarks>
/// <para>
/// Автоматически обнаруживает все атрибуты, наследующие
/// <see cref="ControllerRouteAttributeBase"/>, и применяет их значения
/// к шаблонам маршрутов контроллеров.
/// </para>
/// <para>
/// <b>Алгоритм работы:</b>
/// <list type="number">
/// <item>Получает все типы атрибутов из сборок</item>
/// <item>Для каждого контроллера ищет атрибуты (включая базовые классы)</item>
/// <item>Преобразует имя атрибута в ключ шаблона (например, "ControllerTypeAttribute" → "controllerType")</item>
/// <item>Заменяет плейсхолдер в маршруте на значение атрибута</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // До применения конвенции:
/// [Route("api/[appName]/[controllerType]/v1/[controller]")]
///
/// // После применения конвенции:
/// [Route("api/template/getter/v1/persons")]
/// </code>
/// </example>
public class ControllerTypeConvention
    : IApplicationModelConvention
{
    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        var attributesTypes = AssemblyHelper.GetDerivedTypesFromAssemblies<ControllerRouteAttributeBase>()
            .ToArray();
        application.Controllers
            .ForEach(controller => attributesTypes
                .ForEach(type =>
                {
                    var attribute = controller.ControllerType
                        .GetCustomAttributes(type, inherit: true)
                        .FirstOrDefault();
                    if (attribute == null)
                    {
                        return;
                    }

                    var attributeTypeName = type.Name;
                    var template = attributeTypeName
                        .ToLowerFirstChar()
                        .Remove(attributeTypeName.Length - nameof(Attribute).Length);
                    controller.Selectors.ForEach(selector =>
                        selector.AttributeRouteModel!.Template =
                            selector.AttributeRouteModel.Template!.Replace(
                                $"[{template}]",
                                (attribute as ControllerRouteAttributeBase)!.Value));
                }));
    }
}
