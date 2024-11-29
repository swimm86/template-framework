// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerTypeConvention.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Shared.Presentation.Core.Attributes;

namespace Shared.Presentation.Core.Conventions;

/// <summary>
/// Класс для применения типа контроллера к шаблонам маршрутов в приложении.
/// </summary>
public class ControllerTypeConvention : IApplicationModelConvention
{
    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var controllerTypeAttribute = controller.ControllerType.GetCustomAttribute<ControllerTypeAttribute>();
            if (controllerTypeAttribute == null)
            {
                continue;
            }

            var controllerType = controllerTypeAttribute.Name;
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel!.Template =
                    selector.AttributeRouteModel.Template!.Replace("[controllerType]", controllerType);
            }
        }
    }
}
