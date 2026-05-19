namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

[Flags]
public enum TestEnum
{
    None = 0,
    BeforeCreate = 1,
    AfterCreate = 2,
    BeforeUpdate = 4,
    AfterUpdate = 8,
}
