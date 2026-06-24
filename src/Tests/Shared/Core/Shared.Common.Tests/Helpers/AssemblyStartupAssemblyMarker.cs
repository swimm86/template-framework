// ----------------------------------------------------------------------------------------------
// <copyright file="AssemblyStartupAssemblyMarker.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Attributes;

[assembly: StartupAssembly]

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Маркер тестовой сборки как startup-сборку через <see cref="Shared.Common.Attributes.StartupAssemblyAttribute"/>.
/// Позволяет тестам <c>AssemblyHelper.GetModuleName</c> проверить выбор startup через атрибут.
/// </summary>
internal static class AssemblyStartupAssemblyMarker;
