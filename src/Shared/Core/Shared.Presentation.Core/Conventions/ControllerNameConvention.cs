// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerNameConvention.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Shared.Common.Extensions;

namespace Shared.Presentation.Core.Conventions;

public class ControllerNameConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var classType = "Controller";
        foreach (var controller in application.Controllers)
        {
            var name = controller.ControllerType.Name;
            if (name.EndsWith(classType)) name = name[..^classType.Length];
            name = name.ToKebabCase();
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel!.Template =
                    selector.AttributeRouteModel.Template!.Replace("[controller]", name);
            }
        }
    }
}
