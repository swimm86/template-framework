// ----------------------------------------------------------------------------------------------
// <copyright file="StartupAssemblyAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Attributes;

/// <summary>
/// Маркер исходной сборки приложения, используемый для её идентификации
/// в случаях, когда стандартное определение точки входа недоступно.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class StartupAssemblyAttribute
    : Attribute;
