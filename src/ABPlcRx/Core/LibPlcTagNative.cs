// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using libplctag.NativeImport;

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>libplctag-backed native PLC tag adapter.</summary>
internal sealed class LibPlcTagNative : IPlcTagNative
{
    /// <summary>Initializes a new instance of the <see cref="LibPlcTagNative"/> class.</summary>
    private LibPlcTagNative()
    {
    }

    /// <summary>Gets the shared native adapter instance.</summary>
    public static IPlcTagNative Instance { get; } = new LibPlcTagNative();

    /// <inheritdoc/>
    public int Create(string url, int timeout) => plctag.plc_tag_create(url, timeout);

    /// <inheritdoc/>
    public int Destroy(int handle) => plctag.plc_tag_destroy(handle);

    /// <inheritdoc/>
    public int Abort(int handle) => plctag.plc_tag_abort(handle);

    /// <inheritdoc/>
    public int GetSize(int handle) => plctag.plc_tag_get_size(handle);

    /// <inheritdoc/>
    public int GetStatus(int handle) => plctag.plc_tag_status(handle);

    /// <inheritdoc/>
    public int Lock(int handle) => plctag.plc_tag_lock(handle);

    /// <inheritdoc/>
    public int Read(int handle, int timeout) => plctag.plc_tag_read(handle, timeout);

    /// <inheritdoc/>
    public int Unlock(int handle) => plctag.plc_tag_unlock(handle);

    /// <inheritdoc/>
    public int Write(int handle, int timeout) => plctag.plc_tag_write(handle, timeout);

    /// <inheritdoc/>
    public float GetFloat32(int handle, int offset) => plctag.plc_tag_get_float32(handle, offset);

    /// <inheritdoc/>
    public double GetFloat64(int handle, int offset) => plctag.plc_tag_get_float64(handle, offset);

    /// <inheritdoc/>
    public short GetInt16(int handle, int offset) => plctag.plc_tag_get_int16(handle, offset);

    /// <inheritdoc/>
    public int GetInt32(int handle, int offset) => plctag.plc_tag_get_int32(handle, offset);

    /// <inheritdoc/>
    public long GetInt64(int handle, int offset) => plctag.plc_tag_get_int64(handle, offset);

    /// <inheritdoc/>
    public sbyte GetInt8(int handle, int offset) => plctag.plc_tag_get_int8(handle, offset);

    /// <inheritdoc/>
    public ushort GetUInt16(int handle, int offset) => plctag.plc_tag_get_uint16(handle, offset);

    /// <inheritdoc/>
    public uint GetUInt32(int handle, int offset) => plctag.plc_tag_get_uint32(handle, offset);

    /// <inheritdoc/>
    public ulong GetUInt64(int handle, int offset) => plctag.plc_tag_get_uint64(handle, offset);

    /// <inheritdoc/>
    public byte GetUInt8(int handle, int offset) => plctag.plc_tag_get_uint8(handle, offset);

    /// <inheritdoc/>
    public void SetFloat32(int handle, int offset, float value) => plctag.plc_tag_set_float32(handle, offset, value);

    /// <inheritdoc/>
    public void SetFloat64(int handle, int offset, double value) => plctag.plc_tag_set_float64(handle, offset, value);

    /// <inheritdoc/>
    public void SetInt16(int handle, int offset, short value) => plctag.plc_tag_set_int16(handle, offset, value);

    /// <inheritdoc/>
    public void SetInt32(int handle, int offset, int value) => plctag.plc_tag_set_int32(handle, offset, value);

    /// <inheritdoc/>
    public void SetInt64(int handle, int offset, long value) => plctag.plc_tag_set_int64(handle, offset, value);

    /// <inheritdoc/>
    public void SetInt8(int handle, int offset, sbyte value) => plctag.plc_tag_set_int8(handle, offset, value);

    /// <inheritdoc/>
    public void SetUInt16(int handle, int offset, ushort value) => plctag.plc_tag_set_uint16(handle, offset, value);

    /// <inheritdoc/>
    public void SetUInt32(int handle, int offset, uint value) => plctag.plc_tag_set_uint32(handle, offset, value);

    /// <inheritdoc/>
    public void SetUInt64(int handle, int offset, ulong value) => plctag.plc_tag_set_uint64(handle, offset, value);

    /// <inheritdoc/>
    public void SetUInt8(int handle, int offset, byte value) => plctag.plc_tag_set_uint8(handle, offset, value);
}
