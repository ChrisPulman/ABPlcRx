// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>Helper Tag.</summary>
public static class TagHelper
{
    /// <summary>Create object from Type.</summary>
    /// <typeparam name="TType">The type of the type.</typeparam>
    /// <param name="length">The length.</param>
    /// <returns>A Value.</returns>
    public static TType CreateObject<TType>(int length)
    {
        var obj = CreateInstance<TType>(typeof(TType), length);

        FixStringNullToEmpty(obj);

        return obj;
    }

    /// <summary>Performs Linear scaling conversion.</summary>
    /// <param name="tag">The tag.</param>
    /// <param name="minRaw">The minimum raw.</param>
    /// <param name="maxRaw">The maximum raw.</param>
    /// <param name="minScale">The minimum scale.</param>
    /// <param name="maxScale">The maximum scale.</param>
    /// <returns>A Value.</returns>
    public static double ScaleLinear(IPlcTag tag, double minRaw, double maxRaw, double minScale, double maxScale)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(tag);
#else
        if (tag is null)
        {
            throw new ArgumentNullException(nameof(tag));
        }
#endif

        return TagMixins.ScaleLinear(tag, minRaw, maxRaw, minScale, maxScale);
    }

    /// <summary>Performs SquareRoot conversion.</summary>
    /// <param name="tag">The tag.</param>
    /// <param name="minRaw">The minimum raw.</param>
    /// <param name="maxRaw">The maximum raw.</param>
    /// <param name="minScale">The minimum scale.</param>
    /// <param name="maxScale">The maximum scale.</param>
    /// <returns>A Value.</returns>
    public static double ScaleSquareRoot(IPlcTag tag, double minRaw, double maxRaw, double minScale, double maxScale)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(tag);
#else
        if (tag is null)
        {
            throw new ArgumentNullException(nameof(tag));
        }
#endif

        return TagMixins.ScaleSquareRoot(tag, minRaw, maxRaw, minScale, maxScale);
    }

    /// <summary>Number to bit array.</summary>
    /// <param name="value">The value.</param>
    /// <returns>A Value.</returns>
    public static BitArray NumberToBits(int value) => new([value]);

    /// <summary>Bite array to number.</summary>
    /// <param name="bits">The bits.</param>
    /// <returns>A Value.</returns>
    public static int BitsToNumber(BitArray bits)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bits);
#else
        if (bits is null)
        {
            throw new ArgumentNullException(nameof(bits));
        }
#endif

        var result = new int[1];
        bits.CopyTo(result, 0);
        return result[0];
    }

    /// <summary>Gets public instance properties that can be assigned.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The assignable properties.</returns>
    internal static IEnumerable<PropertyInfo> GetAccessableProperties(Type type) => type.GetProperties(BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public)
                   .Where(p => p.GetSetMethod() is not null);

    /// <summary>Gets an array value and verifies it has a fixed size.</summary>
    /// <param name="value">The array candidate.</param>
    /// <returns>The array instance.</returns>
    internal static Array? GetArray(object? value)
    {
        var array = (Array?)value;
        return array?.Length <= 0
            ? throw new InvalidOperationException("Cannot determine size of class, " +
                                                  "because an array is defined which has no fixed size greater than zero.")
            : array;
    }

    /// <summary>Creates a typed instance for a PLC tag value.</summary>
    /// <typeparam name="TType">The value type.</typeparam>
    /// <param name="type">The runtime type to create.</param>
    /// <param name="length">The array length when creating arrays.</param>
    /// <returns>The created value.</returns>
    private static TType CreateInstance<TType>(Type type, int length)
    {
        if (type == typeof(string))
        {
            return (TType)(object)string.Empty;
        }

        var instance = type.IsArray
            ? Activator.CreateInstance(type, length)
            : Activator.CreateInstance(type);

        return instance is TType typedValue
            ? typedValue
            : throw new InvalidOperationException($"Could not create an instance of {type.FullName}.");
    }

    /// <summary>Fix string null to empty.</summary>
    /// <param name="obj">The object.</param>
    private static void FixStringNullToEmpty(object? obj)
    {
        if (obj is null)
        {
            return;
        }

        var type = obj.GetType();
        if (type.IsArray && type.GetElementType() == typeof(string))
        {
            FixStringArrayNullToEmpty(obj);
            return;
        }

        if (!type.IsClass || type.IsAbstract || type == typeof(string))
        {
            return;
        }

        FixClassStringNullToEmpty(obj, type);
    }

    /// <summary>Replaces null string array entries with empty strings.</summary>
    /// <param name="obj">The string array object.</param>
    private static void FixStringArrayNullToEmpty(object obj)
    {
        var array = GetArray(obj);
        for (var i = 0; i < array?.Length; i++)
        {
            if (array.GetValue(i) is null)
            {
                array.SetValue(string.Empty, i);
            }
        }
    }

    /// <summary>Replaces null string properties within a class graph.</summary>
    /// <param name="obj">The object to update.</param>
    /// <param name="type">The object type.</param>
    private static void FixClassStringNullToEmpty(object obj, Type type)
    {
        foreach (var property in GetAccessableProperties(type))
        {
            if (property.PropertyType == typeof(string))
            {
                FixStringPropertyNullToEmpty(obj, property);
                continue;
            }

            FixStringNullToEmpty(property.GetValue(obj));
        }
    }

    /// <summary>Replaces a null string property value with an empty string.</summary>
    /// <param name="obj">The owning object.</param>
    /// <param name="property">The string property.</param>
    private static void FixStringPropertyNullToEmpty(object obj, PropertyInfo property)
    {
        if (property.GetValue(obj) is not null)
        {
            return;
        }

        property.SetValue(obj, string.Empty);
    }
}
