// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ABPlcRx;

/// <summary>Plc Tag Exception.</summary>
[Serializable]
public class PlcTagException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PlcTagException"/> class.</summary>
    public PlcTagException()
        : this("Error executing PlcTag operation.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PlcTagException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    public PlcTagException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PlcTagException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlcTagException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PlcTagException"/> class.</summary>
    /// <param name="result">The PLC tag result that caused the exception.</param>
    public PlcTagException(PlcTagResult result)
        : this()
    {
        Result = result;
    }

    /// <summary>Gets result operation.</summary>
    /// <value>ResultOperation.</value>
    public PlcTagResult? Result { get; }
}
