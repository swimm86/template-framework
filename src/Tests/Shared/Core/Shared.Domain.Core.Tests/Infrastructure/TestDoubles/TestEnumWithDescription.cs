// ----------------------------------------------------------------------------------------------
// <copyright file="TestEnumWithDescription.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

internal enum TestEnumWithDescription
{
    [Description("Первое значение")]
    FirstValue = 1,

    SecondValue = 2
}
