using System;

namespace Meow.Core.Services;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class StarterTemplateAttribute : Attribute
{
    /// <summary>
    /// Optional language names this template should register for (e.g. "c", "cpp").
    /// If omitted, the compiler's `Name` property will be used when available.
    /// </summary>
    public string[] Names { get; }

    public StarterTemplateAttribute(params string[] names)
    {
        Names = names ?? Array.Empty<string>();
    }
}
