// ----------------------------------------------------------------------------------------------
// <copyright file="ConstructUsingApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Mapster;
using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Applicators;

/// <summary>
/// Применяет настройки <see cref="IMapDescription.ConstructUsing"/> к Mapster-сеттеру.
/// </summary>
internal sealed class ConstructUsingApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object setter, IMapDescription description)
    {
        if (description.ConstructUsing is null)
        {
            return;
        }

        var setterType = setter.GetType();
        var constructUsingMethods = setterType.GetMethods()
            .Where(m =>
                m.Name == nameof(TypeAdapterSetter<object, object>.ConstructUsing)
                && m.GetParameters().Length == 1)
            .ToArray();

        // Mapster поддерживает ConstructUsing(Expression<Func<TSource, TDestination>>)
        var withoutContextMethod = constructUsingMethods.FirstOrDefault(m =>
        {
            var p = m.GetParameters()[0].ParameterType;
            if (!p.IsGenericType || p.GetGenericTypeDefinition() != typeof(Expression<>))
            {
                return false;
            }

            var funcType = p.GetGenericArguments()[0];
            return funcType == typeof(Func<,>).MakeGenericType(description.SourceType, description.DestinationType);
        });

        if (withoutContextMethod is null)
        {
            return;
        }

        var func = description.ConstructUsing;
        var srcParam = Expression.Parameter(description.SourceType, "src");

        // func-сигнатура Func<TSource, ResolutionContext, TDestination>:
        // вызываем func(src, null) — ResolutionContext игнорируется.
        var funcConst = Expression.Constant(func);
        var funcInvokeMethod = func.GetType().GetMethod("Invoke")!;
        var nullContext = Expression.Constant(null, typeof(ResolutionContext));
        var funcCall = Expression.Call(funcConst, funcInvokeMethod, srcParam, nullContext);

        // Результат func — object; конвертируем в TDestination для lambda-сигнатуры.
        var convertedBody = Expression.Convert(funcCall, description.DestinationType);

        var funcType = typeof(Func<,>).MakeGenericType(description.SourceType, description.DestinationType);
        var lambdaObj = Expression.Lambda(funcType, convertedBody, srcParam);

        withoutContextMethod.Invoke(setter, [lambdaObj]);
    }
}
