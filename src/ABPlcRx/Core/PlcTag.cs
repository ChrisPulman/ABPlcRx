// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using ReactiveUI.Primitives.Signals;

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>Tag base definition.</summary>
/// <typeparam name="TType">The type of the type.</typeparam>
/// <seealso cref="System.IDisposable" />
internal sealed class PlcTag<TType> : IPlcTag<TType>
{
    /// <summary>Publishes tag read changes.</summary>
    private readonly Signal<PlcTagResult> _changedSubject = new();

    /// <summary>Native PLC tag adapter.</summary>
    private readonly IPlcTagNative _native;

    /// <summary>Tracks disposal state.</summary>
    private bool _disposed;

    /// <summary>Backs the local tag value.</summary>
    private TType? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcTag{TType}" /> class.
    /// Creates a tag. If the CPU type is LGX, the port type and slot has to be specified.
    /// </summary>
    /// <param name="plc">Controller reference.</param>
    /// <param name="variable">The key.</param>
    /// <param name="tagName">The textual name of the tag to access. The name is anything allowed by the protocol.
    /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.</param>
    /// <param name="size">The size of an element in bytes. The tag is assumed to be composed of elements of the same size.
    /// For structure tags, use the total size of the structure.</param>
    /// <param name="length">elements count: 1- single, n-array.</param>
    internal PlcTag(ABPlc plc, string variable, string tagName, int size, int length = 1)
    {
        ABPlc = plc;
        Variable = variable;
        TagName = tagName;
        Size = size;
        Length = length;
        _native = plc.Native;
        ValueManager = new(this, _native);
        TypeValue = typeof(TType);

        var url = $"protocol=ab_eip&gateway={plc.IPAddress}";
        if (!string.IsNullOrEmpty(plc.Slot))
        {
            url += $"&path={plc.Slot}";
        }

        url += $"&cpu={plc.PlcType}&elem_size={Size}&elem_count={Length}&name={TagName}";
        if (plc.DebugLevel > 0)
        {
            url += $"&debug={plc.DebugLevel}";
        }

        // create reference
        Handle = _native.Create(url, plc.Timeout);

        Value = TagHelper.CreateObject<TType>(Length);
    }

    /// <summary>Finalizes an instance of the <see cref="PlcTag{TType}"/> class.</summary>
    ~PlcTag()
    {
        Dispose(false);
    }

    /// <summary>Gets handle creation Tag.</summary>
    public int Handle { get; }

    /// <summary>Gets the changed.</summary>
    /// <value>
    /// The changed.
    /// </value>
    public IObservable<PlcTagResult> Changed => _changedSubject;

    /// <summary>Gets a value indicating whether indicates whether or not a value must be read from the PLC.</summary>
    public bool IsRead { get; private set; }

    /// <summary>Gets a value indicating whether indicates whether or not a value must be write to the PLC.</summary>
    public bool IsWrite { get; private set; }

    /// <summary>Gets elements length: 1- single, n-array.</summary>
    public int Length { get; }

    /// <summary>
    /// Gets the textual name of the tag to access. The name is anything allowed by the protocol.
    /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.
    /// </summary>
    public string TagName { get; }

    /// <summary>Gets the key.</summary>
    /// <value>
    /// The key.
    /// </value>
    public string Variable { get; }

    /// <summary>Gets or sets a value indicating whether indicate if Tag is in read only.async Write raise exception.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets the size of an element in bytes. The tag is assumed to be composed of elements of the same size.For structure tags,
    /// use the total size of the structure.
    /// </summary>
    public int Size { get; }

    /// <summary>Gets type value.</summary>
    public Type TypeValue { get; }

    /// <summary>Gets or sets value tag.</summary>
    public TType? Value
    {
        get => (TType?)ValueManager.Get(_value, 0);

        set
        {
            _value = value;

            if (!ABPlc.AutoWriteValue)
            {
                return;
            }

            _ = Write();
        }
    }

    /// <summary>Gets or sets the value.</summary>
    /// <value>
    /// The value.
    /// </value>
    object? IPlcTag.Value
    {
        get => Value;
        set => Value = (TType?)value;
    }

    /// <summary>Gets value manager.</summary>
    public PlcTagWrapper ValueManager { get; }

    /// <summary>Gets controller reference.</summary>
    internal ABPlc ABPlc { get; }

    /// <summary>Abort any outstanding IO to the PLC. <see cref="PlcTagStatus"/>.</summary>
    /// <returns>A Value.</returns>
    public int Abort() => _native.Abort(Handle);

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Get size tag read from PLC.</summary>
    /// <returns>A Value.</returns>
    public int GetSize() => _native.GetSize(Handle);

    /// <summary>Get status operation. <see cref="PlcTagStatus"/>.</summary>
    /// <returns>A Value.</returns>
    public int GetStatus() => _native.GetStatus(Handle);

    /// <summary>Lock for multitrading. <see cref="PlcTagStatus"/>.</summary>
    /// <returns>A Value.</returns>
    public int Lock() => _native.Lock(Handle);

    /// <summary>Performs read of Tag.</summary>
    /// <returns>A Value.</returns>
    public PlcTagResult Read()
    {
        var timestamp = DateTime.UtcNow;
        var watch = Stopwatch.StartNew();
        var statusCode = _native.Read(Handle, ABPlc.Timeout);

        watch.Stop();
        IsRead = true;

        var result = new PlcTagResult(this, timestamp, watch.ElapsedMilliseconds, statusCode);

        // check raise exception
        if (ABPlc.FailOperationRaiseException && PlcTagStatus.IsError(statusCode))
        {
            throw new PlcTagException(result);
        }

        if (!_changedSubject.IsDisposed)
        {
            _changedSubject.OnNext(result);
        }

        return result;
    }

    /// <summary>Unlock for multitrading <see cref="PlcTagStatus"/>.</summary>
    /// <returns>A Value.</returns>
    public int Unlock() => _native.Unlock(Handle);

    /// <summary>Performs write of Tag.</summary>
    /// <returns>A Value.</returns>
    public PlcTagResult Write()
    {
        if (ReadOnly)
        {
            throw new InvalidOperationException("Tag is set read only!");
        }

        ValueManager.Set(_value, 0);

        var timestamp = DateTime.UtcNow;
        var watch = Stopwatch.StartNew();
        var statusCode = _native.Write(Handle, ABPlc.Timeout);
        watch.Stop();
        IsWrite = true;

        var result = new PlcTagResult(this, timestamp, watch.ElapsedMilliseconds, statusCode);

        // check raise exception
        if (ABPlc.FailOperationRaiseException && PlcTagStatus.IsError(statusCode))
        {
            throw new PlcTagException(result);
        }

        return result;
    }

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _changedSubject.Dispose();
        }

        // Always destroy native handle
        _ = _native.Destroy(Handle);
        _disposed = true;
    }
}
