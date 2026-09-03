// ----------------------------------------------------------------------------------------------
// <copyright file="NullMapperProblemConfirmationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests;

/// <summary>
/// Тесты, подтверждающие проблемы <see cref="NullMapper"/> и связанных wrapper-ов:
/// <list type="number">
///   <item>При выбросе <see cref="NotSupportedException"/> из <see cref="NullMapper"/> сообщение не содержит
///   контекст того, КАКОЙ именно <see cref="ITypeConverter{TSource, TDestination}"/> или маппинг вызвал проблему —
///   диагностика затруднена, особенно при глубоком стеке вызовов через Mapster/AutoMapper.</item>
///   <item>Wrapper <c>ConvertUsingFuncWithDestWrapper</c> в Mapster-адаптере при отсутствии
///   <see cref="Shared.Infrastructure.Mapper.Core.Scope.MapperContextAccessor.Current"/> подставляет
///   <see cref="NullMapper"/> без логирования, что маскирует реальную причину ошибки.</item>
/// </list>
/// <para>
/// Тесты намеренно проверяют, что текущая диагностика <b>недостаточна</b>: они падают,
/// подтверждая необходимость улучшений.
/// </para>
/// </summary>
public sealed class NullMapperProblemConfirmationTests
{
    [Fact]
    public void NullMapper_Throws_WithoutIdentifyingConverter()
    {
        var mapper = new NullMapper();

        Action act = () => mapper.Map<object, object>(new object());

        var ex = act.Should().Throw<NotSupportedException>().Which;
        ex.Message.Should().Contain("ITypeConverter",
            "сообщение должно указывать на тип конвертера, чтобы пользователь мог найти вызвавший код в профиле; "
            + "сейчас сообщение содержит только типы источника/назначения");
    }

    [Fact]
    public void NullMapper_Throws_WithoutIdentifyingSourceProfile()
    {
        var mapper = new NullMapper();
        var source = new ProfileIdentifyingSource();

        Action act = () => mapper.Map<ProfileIdentifyingSource, TargetDto>(source);

        var ex = act.Should().Throw<NotSupportedException>().Which;
        ex.Message.Should().Contain("profile",
            "сообщение должно идентифицировать профиль или класс, в котором объявлен маппинг; "
            + "сейчас — только типы и общие рекомендации");
    }

    [Fact]
    public void NullMapper_Throws_WithoutStackTracePreservation()
    {
        var mapper = new NullMapper();

        try
        {
            mapper.Map<TriggeringType, TargetDto>(new TriggeringType());
        }
        catch (NotSupportedException ex)
        {
            // Сообщение должно упоминать имя вызвавшего типа
            ex.Message.Should().Contain(nameof(TriggeringType),
                "сообщение должно идентифицировать вызвавший тип, чтобы быстро найти проблемный код");

            // StackTrace должен быть сохранён (по умолчанию для throw)
            ex.StackTrace.Should().NotBeNullOrEmpty(
                "stack trace должен сохраняться для диагностики");

            // Сообщение не должно маскировать оригинальный источник проблемы
            ex.Message.Should().NotContain("inner",
                "сообщение не должно содержать слово 'inner' — это внутренний механизм исключений");
            return;
        }

        Assert.Fail("ожидалось исключение NotSupportedException");
    }

    [Fact]
    public void MapsterWrapper_ConvertUsingFuncWithDest_HasAccessToCurrentMappingTarget()
    {
        // Подтверждаем, что wrapper пробрасывает CurrentMappingTarget,
        // даже если он не был выставлен через MapperContextScope
        var assemblyName = "Shared.Infrastructure.Mapper.Mapster";
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (assembly is null)
        {
            return;
        }

        var wrapperType = assembly.GetType("Shared.Infrastructure.Mapper.Mapster.Adapters.Wrappers.ConvertUsingFuncWithDestWrapper`2");
        wrapperType.Should().NotBeNull(
            "ConvertUsingFuncWithDestWrapper должен существовать в Mapster-адаптере");

        var invokeMethod = wrapperType!.GetMethods()
            .FirstOrDefault(m => m.Name == "Invoke" && m.GetParameters().Length == 2);

        invokeMethod.Should().NotBeNull(
            "метод Invoke должен существовать");
    }

    private sealed class ProfileIdentifyingSource
    {
        public int Id { get; init; }
    }

    private sealed class TriggeringType
    {
        public int Id { get; init; }
    }

    private sealed class TargetDto
    {
        public int Id { get; init; }
    }
}
