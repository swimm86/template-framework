// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfileBuilderBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.Core.ProfileBuilder;

/// <summary>
/// Адаптер, применяющий провайдеро-независимое описание профиля (<see cref="IMappingProfile"/>)
/// к конкретному накопителю настроек преобразования (mapping).
/// </summary>
/// <typeparam name="TMappingSettingAccumulator">Тип накопителя настроек преобразования.</typeparam>
internal abstract class MapperProfileBuilderBase<TMappingSettingAccumulator>
{
    /// <summary>
    /// applicator-ы: применяют соответствующие секции
    /// <see cref="IMapDescription"/> к провайдеро-специфичному контексту преобразования.
    /// </summary>
    protected abstract ICollection<IDescriptionApplicator> Applicators { get; }

    /// <summary>
    /// Применяет все описания преобразований (mapping) из <paramref name="profile"/> к <paramref name="accumulator"/>.
    /// </summary>
    /// <param name="accumulator">Целевой накопитель настроек преобразования.</param>
    /// <param name="profile">Провайдеро-независимое описание профиля.</param>
    public void Apply(TMappingSettingAccumulator accumulator, IMappingProfile profile) =>
        profile.GetMapDescriptions()
            .ForEach(description => ApplyDescription(accumulator, description));

    /// <summary>
    /// Применяет описание преобразования (mapping) к <paramref name="accumulator"/>.
    /// </summary>
    /// <param name="accumulator">Целевой накопитель настроек преобразования.</param>
    /// <param name="description">Описание преобразования.</param>
    protected void ApplyDescription(
        TMappingSettingAccumulator accumulator,
        IMapDescription description)
    {
        var mappingContext = CreateMappingContext(accumulator, description.SourceType, description.DestinationType);

        Applicators.ForEach(applicator => applicator.Apply(mappingContext, description));
    }

    /// <summary>
    /// Создаёт контекст преобразования (mapping).
    /// </summary>
    /// <param name="accumulator">Целевой накопитель настроек преобразования.</param>
    /// <param name="sourceType">Исходный тип преобразования.</param>
    /// <param name="destinationType">Целевой тип преобразования.</param>
    /// <returns>Объект контекста преобразования.</returns>
    protected abstract object CreateMappingContext(
        TMappingSettingAccumulator accumulator,
        Type sourceType,
        Type destinationType);
}
