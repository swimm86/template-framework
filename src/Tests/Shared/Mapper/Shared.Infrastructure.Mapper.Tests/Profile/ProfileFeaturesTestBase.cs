// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileFeaturesTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.Tests.Profile;

/// <summary>
/// Тесты fluent-цепочки профиля: ForMember+MapFrom, Ignore, ConstructUsing, ConvertUsing,
/// Before/AfterMap, ReverseMap, ITypeConverter, ConfigureCollection, IncludeBase.
/// </summary>
/// <remarks>
/// Это общий базовый класс для тестов fluent API преобразователей (mapper) (AutoMapper, Mapster).
/// Наследники обязаны реализовать <see cref="CreateMapper"/> для конкретного провайдера.
/// <para>
/// Тесты разнесены по тематическим partial-файлам:
/// <list type="bullet">
///   <item><c>ProfileFeaturesTestBase.Members.cs</c> — ForMember/MapFrom, Ignore.</item>
///   <item><c>ProfileFeaturesTestBase.Construction.cs</c> — ConstructUsing, ForCtorParam.</item>
///   <item><c>ProfileFeaturesTestBase.ConvertersAndHooks.cs</c> — ConvertUsing, ITypeConverter, Before/AfterMap, ReverseMap, IncludeBase.</item>
///   <item><c>ProfileFeaturesTestBase.Collections.cs</c> — ConfigureCollection, вложенные объекты, несколько профилей.</item>
///   <item><c>ProfileFeaturesTestBase.Projections.cs</c> — ProjectTo с параметризованным MapFrom.</item>
/// </list>
/// </para>
/// </remarks>
public abstract partial class ProfileFeaturesTestBase
{
    /// <summary>
    /// Создаёт экземпляр <see cref="IMapper"/> для тестируемого провайдера.
    /// </summary>
    /// <returns>Готовый к использованию <see cref="IMapper"/>.</returns>
    protected abstract IMapper CreateMapper();
}
