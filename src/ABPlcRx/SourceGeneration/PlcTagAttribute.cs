// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ABPlcRx.SourceGeneration;

/// <summary>
/// Describes a PLC tag stream that should be generated for a partial model.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class PlcTagAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlcTagAttribute"/> class for a property-declared tag.
    /// </summary>
    /// <param name="tagName">The PLC tag name.</param>
    public PlcTagAttribute(string tagName) => TagName = tagName;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcTagAttribute"/> class for a generated property.
    /// </summary>
    /// <param name="valueType">The PLC value type.</param>
    /// <param name="propertyName">The generated property name.</param>
    /// <param name="tagName">The PLC tag name.</param>
    public PlcTagAttribute(Type valueType, string propertyName, string tagName)
    {
        ValueType = valueType;
        PropertyName = propertyName;
        TagName = tagName;
    }

    /// <summary>
    /// Gets the PLC tag name.
    /// </summary>
    public string TagName { get; }

    /// <summary>
    /// Gets the generated property value type when the attribute is applied to a class.
    /// </summary>
    public Type? ValueType { get; }

    /// <summary>
    /// Gets the generated property name when the attribute is applied to a class.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Gets or sets the application variable key. Defaults to the property name.
    /// </summary>
    public string? Variable { get; set; }

    /// <summary>
    /// Gets or sets the tag group.
    /// </summary>
    public string Group { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the bit index for boolean bit access.
    /// </summary>
    public int Bit { get; set; } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether generated attach logic should register the tag.
    /// </summary>
    public bool RegisterTag { get; set; } = true;
}
