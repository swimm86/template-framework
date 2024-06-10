// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerTypeConvention.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Shared.Presentation.Core.Attributes;

namespace Shared.Presentation.Core.Conventions;

public class ControllerTypeConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var controllerTypeAttribute = controller.ControllerType.GetCustomAttribute<ControllerTypeAttribute>();
            if (controllerTypeAttribute == null) continue;
            var controllerType = controllerTypeAttribute.Name;
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel!.Template =
                    selector.AttributeRouteModel.Template!.Replace("[controllerType]", controllerType);
            }
        }
    }
}
