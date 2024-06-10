namespace Shared.Presentation.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ControllerTypeAttribute(string name) : Attribute
{
    public readonly string Name = name;
}
