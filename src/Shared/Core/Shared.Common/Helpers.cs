// ----------------------------------------------------------------------------------------------
// <copyright file="Helpers.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;

namespace Shared.Common;

/// <summary>
/// Предоставляет вспомогательные методы общего назначения.
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Получает название модуля, представляющее собой имя сборки, содержащей точку входа в приложение.
    /// </summary>
    /// <returns>Имя сборки, которая была определена как точка входа в приложение.</returns>
    public static string GetModuleName() => Assembly.GetEntryAssembly()!.GetName().Name!;
}
