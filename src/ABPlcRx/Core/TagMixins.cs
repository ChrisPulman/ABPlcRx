// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace ABPlcRx;

/// <summary>
/// TagMixins.
/// </summary>
public static class TagMixins
{
    /// <summary>
    /// Sets the bit.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="bit">The bit.</param>
    /// <param name="value">The value.</param>
    public static void SetBit(this IPlcTag<short> source, int bit, bool value)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var bits = new BitArray(BitConverter.GetBytes(source.Value));
        bits[bit] = value;
        var raw = new byte[2];
        bits.CopyTo(raw, 0);
        source.Value = BitConverter.ToInt16(raw, 0);
        source.Write();
    }

    /// <summary>
    /// Sets the bit.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="bit">The bit.</param>
    /// <param name="value">if set to <c>true</c> [value].</param>
    /// <returns>A short.</returns>
    public static short SetBit(this short source, int bit, bool value)
    {
        var bits = new BitArray(BitConverter.GetBytes(source));
        bits[bit] = value;
        var raw = new byte[2];
        bits.CopyTo(raw, 0);
        return BitConverter.ToInt16(raw, 0);
    }

    /////// <summary>
    /////// Sets the bit.
    /////// </summary>
    /////// <param name="source">The source.</param>
    /////// <param name="bit">The bit.</param>
    /////// <param name="value">if set to <c>true</c> [value].</param>
    /////// <returns>A short.</returns>
    ////public static short SetBit(short source, int bit, bool value)
    ////{
    ////    var bits = new BitArray(BitConverter.GetBytes(source));
    ////    bits[bit] = value;
    ////    var raw = new byte[2];
    ////    bits.CopyTo(raw, 0);
    ////    return BitConverter.ToInt16(raw, 0);
    ////}

    /// <summary>
    /// Gets the bit.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>A Value.</returns>
    public static bool GetBit(this IPlcTag<short> source, int bit)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        source.Read();
        return source.ValueManager.GetBit(bit);
    }

    /// <summary>
    /// Gets the bit.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>A bool from the source at bit x.</returns>
    public static bool GetBit(this short source, int bit)
    {
        var bits = new BitArray(BitConverter.GetBytes(source));
        return bits[bit];
    }
}
