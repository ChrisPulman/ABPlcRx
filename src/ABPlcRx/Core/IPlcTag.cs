// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ABPlcRx;

/// <summary>
/// Interface Tag.
/// </summary>
public interface IPlcTag : IDisposable
{
    /// <summary>
    /// Gets the changed.
    /// </summary>
    /// <value>
    /// The changed.
    /// </value>
    IObservable<PlcTagResult> Changed { get; }

    /// <summary>
    /// Gets handle creation Tag.
    /// </summary>
    int Handle { get; }

    /// <summary>
    /// Gets a value indicating whether indicates whether or not a value must be read from the PLC.
    /// </summary>
    bool IsRead { get; }

    /// <summary>
    /// Gets a value indicating whether indicates whether or not a value must be write to the PLC.
    /// </summary>
    bool IsWrite { get; }

    /// <summary>
    /// Gets the key.
    /// </summary>
    /// <value>
    /// The key.
    /// </value>
    string Variable { get; }

    /// <summary>
    /// Gets elements length: 1- single, n-array.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets the textual name of the tag to access. The name is anything allowed by the protocol.
    /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether indicate if Tag is in read only.async Write raise exception.
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    /// Gets the size of an element in bytes. The tag is assumed to be composed of elements of the same size.For structure tags,
    /// use the total size of the structure.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets type value.
    /// </summary>
    Type TypeValue { get; }

    /// <summary>
    /// Gets or sets value tag.
    /// </summary>
    object? Value { get; set; }

    /// <summary>
    /// Gets value manager.
    /// </summary>
    PlcTagWrapper ValueManager { get; }

    /// <summary>
    /// Abort any outstanding IO to the PLC. <see cref="PlcTagStatus"/>.
    /// </summary>
    /// <returns>A Value.</returns>
    int Abort();

    /// <summary>
    /// Get size tag.
    /// </summary>
    /// <returns>A Value.</returns>
    int GetSize();

    /// <summary>
    /// Get status operation. <see cref="PlcTagStatus"/>.
    /// </summary>
    /// <returns>A Value.</returns>
    int GetStatus();

    /// <summary>
    /// Lock for multitrading. <see cref="PlcTagStatus"/>.
    /// </summary>
    /// <returns>A Value.</returns>
    int Lock();

    /// <summary>
    /// Performs read of Tag.
    /// </summary>
    /// <returns>A Value.</returns>
    PlcTagResult Read();

    /// <summary>
    /// Unlock for multitrading <see cref="PlcTagStatus"/>.
    /// </summary>
    /// <returns>A Value.</returns>
    int Unlock();

    /// <summary>
    /// Perform write of Tag.
    /// </summary>
    /// <returns>A Value.</returns>
    PlcTagResult Write();
}
