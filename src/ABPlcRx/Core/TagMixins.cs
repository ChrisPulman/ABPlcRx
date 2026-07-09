// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;

namespace ABPlcRx;

/// <summary>PLC tag bit helper extensions.</summary>
public static class TagMixins
{
    /// <summary>Performs Linear scaling conversion.</summary>
    /// <param name="tag">The PLC tag.</param>
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

        if (minRaw >= maxRaw || minScale > maxScale)
        {
            throw new InvalidOperationException();
        }

        var rawValue = (double)(tag.Value ?? throw new InvalidOperationException());
        return (((maxScale - minScale) / (maxRaw - minRaw)) * (rawValue - minRaw)) + minScale;
    }

    /// <summary>Performs SquareRoot conversion.</summary>
    /// <param name="tag">The PLC tag.</param>
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

        if (minRaw >= maxRaw || minScale > maxScale)
        {
            throw new InvalidOperationException();
        }

        var rawValue = (double)(tag.Value ?? throw new InvalidOperationException());
        return (Math.Sqrt((rawValue - minRaw) / (maxRaw - minRaw)) * (maxScale - minScale)) + minScale;
    }

    /// <summary>Sets the bit.</summary>
    /// <param name="source">The PLC tag source.</param>
    /// <param name="bit">The bit.</param>
    /// <param name="value">The value.</param>
    public static void SetBit(IPlcTag<short> source, int bit, bool value)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
#endif

        var bits = new BitArray(BitConverter.GetBytes(source.Value));
        bits[bit] = value;
        var raw = new byte[2];
        bits.CopyTo(raw, 0);
        source.Value = BitConverter.ToInt16(raw, 0);
        _ = source.Write();
    }

    /// <summary>Gets the bit.</summary>
    /// <param name="source">The PLC tag source.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>A Value.</returns>
    public static bool GetBit(IPlcTag<short> source, int bit)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
#endif

        _ = source.Read();
        return source.ValueManager.GetBit(bit);
    }

    /// <summary>Sets the bit.</summary>
    /// <param name="source">The signed 16-bit source value.</param>
    /// <param name="bit">The bit.</param>
    /// <param name="value">if set to <c>true</c> [value].</param>
    /// <returns>A short.</returns>
    public static short SetBit(short source, int bit, bool value)
    {
        var bits = new BitArray(BitConverter.GetBytes(source));
        bits[bit] = value;
        var raw = new byte[2];
        bits.CopyTo(raw, 0);
        return BitConverter.ToInt16(raw, 0);
    }

    /// <summary>Gets the bit.</summary>
    /// <param name="source">The signed 16-bit source value.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>A bool from the source at bit x.</returns>
    public static bool GetBit(short source, int bit)
    {
        var bits = new BitArray(BitConverter.GetBytes(source));
        return bits[bit];
    }
}
