// ----------------------------------------------------------------------------------------------
// <copyright file="CtorParamApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Mapster;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;
using Shared.Infrastructure.Mapper.Mapster.Adapters.Helpers;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Applicators;

/// <summary>
/// Применяет настройки ctor-параметров (<see cref="ICtorParamMapDescription"/>) к Mapster-сеттеру.
/// </summary>
/// <remarks>
/// Сначала включает преобразование (mapping) через конструктор (<c>MapToConstructor(true)</c>),
/// затем для каждого параметра с заданным <see cref="ICtorParamMapDescription.SourceExpression"/>
/// вызывает <c>Map(string paramName, src =&gt; ...)</c>.
/// </remarks>
internal sealed class CtorParamApplicator : IDescriptionApplicator
{
    private static readonly MethodInfo MapToConstructorExtension = typeof(TypeAdapterSetterExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m =>
            m.Name == nameof(TypeAdapterSetterExtensions.MapToConstructor)
            && m.GetParameters().Length == 2
            && m.GetParameters()[1].ParameterType == typeof(bool));

    /// <inheritdoc />
    public void Apply(object setter, IMapDescription description)
    {
        if (description.CtorParams.Count == 0)
        {
            return;
        }

        var setterType = setter.GetType();
        var mapByParamNameMethod = setterType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m =>
                m.Name == nameof(TypeAdapterSetter<object, object>.Map)
                && m.GetParameters().Length == 3
                && m.GetParameters()[0].ParameterType == typeof(string));

        var closedMapToConstructor = MapToConstructorExtension.MakeGenericMethod(setterType);
        closedMapToConstructor.Invoke(null, [setter, true]);

        foreach (var ctorParam in description.CtorParams)
        {
            if (ctorParam.SourceExpression is null)
            {
                continue;
            }

            var sourceMemberType = ctorParam.SourceExpression.ReturnType;
            var funcType = typeof(Func<,>).MakeGenericType(description.SourceType, sourceMemberType);
            var typedLambda = LambdaTypeConverter.ConvertLambdaType(ctorParam.SourceExpression, funcType);

            var closedMap = mapByParamNameMethod.MakeGenericMethod(sourceMemberType);
            closedMap.Invoke(setter, [ctorParam.CtorParamName, typedLambda, null]);
        }
    }
}
