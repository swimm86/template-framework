// ----------------------------------------------------------------------------------------------
// <copyright file="ParamHolderTypeCache.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.ExpressionTransformers;

/// <summary>
/// Кеш динамических типов-холдеров для захваченных переменных
/// (по одному полю на каждый ключ параметров).
/// </summary>
internal static class ParamHolderTypeCache
{
    private static readonly ModuleBuilder Module = CreateModule();
    private static readonly ConcurrentDictionary<string, Type> Cache = new();

    /// <summary>
    /// Получить или создать тип с публичными полями для каждого ключа параметров.
    /// </summary>
    /// <param name="keys">Набор ключей.</param>
    /// <returns>Сгенерированный тип.</returns>
    public static Type GetOrCreate(HashSet<string> keys)
    {
        var key = string.Join("|", keys.OrderBy(k => k));

        return Cache.GetOrAdd(key, _ =>
        {
            var typeName = "<>c__DisplayClass_" + Guid.NewGuid().ToString("N");
            var tb = Module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
            tb.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!, []));

            foreach (var fieldName in keys)
            {
                tb.DefineField(fieldName, typeof(object), FieldAttributes.Public);
            }

            return tb.CreateType();
        });
    }

    private static ModuleBuilder CreateModule()
    {
        var assemblyName = new AssemblyName("AutoMapperDynamicParams_" + Guid.NewGuid().ToString("N"));
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        return assembly.DefineDynamicModule("MainModule");
    }
}
