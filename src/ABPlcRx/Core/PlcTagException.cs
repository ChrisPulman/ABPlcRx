// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace ABPlcRx;

/// <summary>
/// Plc Tag Exception.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlcTagException"/> class.
/// </remarks>
/// <param name="result">The result.</param>
[System.Serializable]
#pragma warning disable RCS1194 // Implement exception constructors.
public class PlcTagException(PlcTagResult result) : Exception("Error executing PlcTag operation.")
#pragma warning restore RCS1194 // Implement exception constructors.
{
    /// <summary>
    /// Gets result operation.
    /// </summary>
    /// <value>ResultOperation.</value>
    public PlcTagResult? Result { get; } = result;
}
