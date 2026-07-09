using System.ComponentModel;
using Nuke.Common.Tooling;

namespace ABPlcRx.Build;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static readonly Configuration Debug = new() { Value = nameof(Debug) };
    public static readonly Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) =>
        configuration?.Value;

    public override string ToString() => Value;
}
