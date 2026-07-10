// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>Abstraction over libplctag native calls.</summary>
internal interface IPlcTagNative
{
    /// <summary>Creates a native PLC tag.</summary>
    /// <param name="url">The native connection URL.</param>
    /// <param name="timeout">The operation timeout.</param>
    /// <returns>The native handle.</returns>
    int Create(string url, int timeout);

    /// <summary>Destroys a native PLC tag.</summary>
    /// <param name="handle">The native handle.</param>
    /// <returns>The native status code.</returns>
    int Destroy(int handle);

    /// <summary>Aborts native PLC tag IO.</summary>
    /// <param name="handle">The native handle.</param>
    /// <returns>The native status code.</returns>
    int Abort(int handle);

    /// <summary>Gets the native PLC tag size.</summary>
    /// <param name="handle">The native handle.</param>
    /// <returns>The tag size.</returns>
    int GetSize(int handle);

    /// <summary>Gets the native PLC tag status.</summary>
    /// <param name="handle">The native handle.</param>
    /// <returns>The native status code.</returns>
    int GetStatus(int handle);

    /// <summary>Locks a native PLC tag.</summary>
    /// <param name="handle">The native handle.</param>
    /// <returns>The native status code.</returns>
    int Lock(int handle);

    /// <summary>Reads a native PLC tag.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="timeout">The operation timeout.</param>
    /// <returns>The native status code.</returns>
    int Read(int handle, int timeout);

    /// <summary>Unlocks a native PLC tag.</summary>
    /// <param name="handle">The native handle.</param>
    /// <returns>The native status code.</returns>
    int Unlock(int handle);

    /// <summary>Writes a native PLC tag.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="timeout">The operation timeout.</param>
    /// <returns>The native status code.</returns>
    int Write(int handle, int timeout);

    /// <summary>Gets a 32-bit floating-point value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    float GetFloat32(int handle, int offset);

    /// <summary>Gets a 64-bit floating-point value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    double GetFloat64(int handle, int offset);

    /// <summary>Gets a signed 16-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    short GetInt16(int handle, int offset);

    /// <summary>Gets a signed 32-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    int GetInt32(int handle, int offset);

    /// <summary>Gets a signed 64-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    long GetInt64(int handle, int offset);

    /// <summary>Gets a signed 8-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    sbyte GetInt8(int handle, int offset);

    /// <summary>Gets an unsigned 16-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    ushort GetUInt16(int handle, int offset);

    /// <summary>Gets an unsigned 32-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    uint GetUInt32(int handle, int offset);

    /// <summary>Gets an unsigned 64-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    ulong GetUInt64(int handle, int offset);

    /// <summary>Gets an unsigned 8-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    byte GetUInt8(int handle, int offset);

    /// <summary>Sets a 32-bit floating-point value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetFloat32(int handle, int offset, float value);

    /// <summary>Sets a 64-bit floating-point value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetFloat64(int handle, int offset, double value);

    /// <summary>Sets a signed 16-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetInt16(int handle, int offset, short value);

    /// <summary>Sets a signed 32-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetInt32(int handle, int offset, int value);

    /// <summary>Sets a signed 64-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetInt64(int handle, int offset, long value);

    /// <summary>Sets a signed 8-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetInt8(int handle, int offset, sbyte value);

    /// <summary>Sets an unsigned 16-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetUInt16(int handle, int offset, ushort value);

    /// <summary>Sets an unsigned 32-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetUInt32(int handle, int offset, uint value);

    /// <summary>Sets an unsigned 64-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetUInt64(int handle, int offset, ulong value);

    /// <summary>Sets an unsigned 8-bit value.</summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="value">The value.</param>
    void SetUInt8(int handle, int offset, byte value);
}
