using System.ComponentModel;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

internal enum TestEnumWithDescription
{
    [Description("Первое значение")]
    FirstValue = 1,

    SecondValue = 2
}
