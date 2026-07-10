// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using ReactiveUI.Primitives.Signals;
using TUnit.Assertions;
using TUnit.Core;

namespace ABPlcRx.Tests;

/// <summary>Tests PLC/tag behavior through a fake native adapter.</summary>
public sealed class NativeTagBehaviorTests
{
    /// <summary>Fake native handle used by direct wrapper tests.</summary>
    private const int DirectHandle = 901;

    /// <summary>First fake native handle returned from create.</summary>
    private const int FirstCreatedHandle = 1000;

    /// <summary>Fake native buffer length.</summary>
    private const int NativeBufferLength = 512;

    /// <summary>Sample PLC timeout.</summary>
    private const int OperationTimeout = 1234;

    /// <summary>Test scan interval.</summary>
    private const int ScanIntervalMilliseconds = 10_000;

    /// <summary>Sample bit index.</summary>
    private const int SampleBitIndex = 1;

    /// <summary>Sample PLC array length.</summary>
    private const int SampleArrayLength = 2;

    /// <summary>Sample int value.</summary>
    private const int SampleIntValue = 42;

    /// <summary>Updated sample int value.</summary>
    private const int UpdatedIntValue = 77;

    /// <summary>Sample short value.</summary>
    private const short SampleShortValue = 12;

    /// <summary>Sample unsigned short value.</summary>
    private const ushort SampleUInt16Value = 13;

    /// <summary>Sample byte value.</summary>
    private const byte SampleByteValue = 14;

    /// <summary>Sample signed byte value.</summary>
    private const sbyte SampleSByteValue = -5;

    /// <summary>Sample unsigned int value.</summary>
    private const uint SampleUInt32Value = 15U;

    /// <summary>Sample long value.</summary>
    private const long SampleLongValue = 16L;

    /// <summary>Sample unsigned long value.</summary>
    private const ulong SampleUInt64Value = 17UL;

    /// <summary>Sample float value.</summary>
    private const float SampleFloatValue = 18.5F;

    /// <summary>Sample double value.</summary>
    private const double SampleDoubleValue = 19.5D;

    /// <summary>Secondary array value.</summary>
    private const int SecondArrayValue = 64;

    /// <summary>Offset used by typed wrapper tests.</summary>
    private const int SecondaryOffset = 16;

    /// <summary>Sample string value.</summary>
    private const string SampleStringValue = "AB";

    /// <summary>Verifies PLC group management and bulk operations use the fake native adapter.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ABPlcManagesGroupsTagsAndBulkOperationsAsync()
    {
        var native = new FakePlcTagNative();
        using var plc = new ABPlc("127.0.0.1", PlcType.SLC, null, native)
        {
            Timeout = OperationTimeout,
        };
        var addedTags = new List<IPlcTag>();
        var removedTags = new List<IPlcTag>();

        using var addedSubscription = plc.TagsAdded.Subscribe(new CaptureObserver<IPlcTag>(addedTags.Add));
        using var removedSubscription = plc.TagsRemoved.Subscribe(new CaptureObserver<IPlcTag>(removedTags.Add));

        plc.AddTagToGroup<int>("Counter", "N7:0", TimeSpan.FromMilliseconds(ScanIntervalMilliseconds), "Fast");

        await Assert.That(plc.HasTagGroup("Fast")).IsTrue();
        await Assert.That(plc.TagCollectionList.Count).IsEqualTo(1);
        await Assert.That(plc.TagCollectionList.Count).IsEqualTo(1);
        await Assert.That(plc.Tags.Count).IsEqualTo(1);
        await Assert.That(plc.TryGetPlcTag("Counter", out var tag)).IsTrue();
        await Assert.That(plc.GetPlcTag("Counter")).IsEqualTo(tag);
        await Assert.That(addedTags.Count).IsEqualTo(1);

        var readResults = plc.ReadAll();
        var writeResults = plc.WriteAll();
        var asyncReadResults = await plc.ReadAllAsync();
        var asyncWriteResults = await plc.WriteAllAsync();

        await Assert.That(readResults.Count).IsEqualTo(1);
        await Assert.That(writeResults.Count).IsEqualTo(1);
        await Assert.That(asyncReadResults.Count).IsEqualTo(1);
        await Assert.That(asyncWriteResults.Count).IsEqualTo(1);
        await Assert.That(plc.RemoveTagGroup("Missing")).IsFalse();
        await Assert.That(plc.RemoveTagGroup("Fast")).IsTrue();
        await Assert.That(removedTags.Count).IsEqualTo(1);
        await Assert.That(plc.HasTagGroup("Fast")).IsFalse();

        plc.Dispose();

        await Assert.That(native.DestroyCalls).IsEqualTo(1);
    }

    /// <summary>Verifies tag arrays, removal, and collection validation paths.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcTagCollectionCreatesArraysAndValidatesRemovalAsync()
    {
        var native = new FakePlcTagNative();
        using var plc = new ABPlc("127.0.0.1", PlcType.SLC, null, native);
        var collection = plc.CreateTagList("Manual", TimeSpan.FromMilliseconds(ScanIntervalMilliseconds));

        _ = Assert.Throws<ArgumentException>(() => collection.CreateTagArray<ArrayList>("Bad", "N7:0", 1));
        _ = Assert.Throws<ArgumentException>(() => collection.CreateTagArray<int[]>("BadLength", "N7:0", 0));

        var tag = collection.CreateTagArray<int[]>("Values", "N7:0", SampleArrayLength);
        await Assert.That(collection.Tags.Count).IsEqualTo(1);

        collection.RemoveTag(tag);

        await Assert.That(collection.Tags.Count).IsEqualTo(0);
        _ = Assert.Throws<ArgumentException>(() => collection.RemoveTag(tag));

        collection.ClearTags();
        collection.Dispose();
        collection.Dispose();

        await Assert.That(native.DestroyCalls).IsEqualTo(1);
    }

    /// <summary>Verifies PLC tag lifecycle calls and error paths.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcTagUsesNativeAdapterForLifecycleAndErrorsAsync()
    {
        var native = new FakePlcTagNative();
        using var plc = new ABPlc("10.0.0.5", PlcType.SLC, "1,0", native)
        {
            AutoWriteValue = true,
            Timeout = OperationTimeout,
        };
        var observedResults = new List<PlcTagResult>();
        var tag = new PlcTag<int>(plc, "Counter", "N7:0", DataLength.INT32);

        using var subscription = tag.Changed.Subscribe(new CaptureObserver<PlcTagResult>(observedResults.Add));

        tag.Value = SampleIntValue;
        var readResult = tag.Read();

        await Assert.That(native.LastCreatedUrl).Contains("gateway=10.0.0.5");
        await Assert.That(native.LastCreatedUrl).Contains("path=1,0");
        await Assert.That(native.LastCreatedUrl).Contains("elem_size=4");
        await Assert.That(native.ReadInt32(tag.Handle)).IsEqualTo(SampleIntValue);
        await Assert.That(native.LastWriteTimeout).IsEqualTo(OperationTimeout);
        await Assert.That(native.LastReadTimeout).IsEqualTo(OperationTimeout);
        await Assert.That(tag.IsWrite).IsTrue();
        await Assert.That(tag.IsRead).IsTrue();
        await Assert.That(readResult.Tag).IsEqualTo(tag);
        await Assert.That(observedResults.Single()).IsEqualTo(readResult);
        await Assert.That(tag.Abort()).IsEqualTo(PlcTagStatus.StatusOK);
        await Assert.That(tag.GetSize()).IsEqualTo(NativeBufferLength);
        await Assert.That(tag.GetStatus()).IsEqualTo(PlcTagStatus.StatusOK);
        await Assert.That(tag.Lock()).IsEqualTo(PlcTagStatus.StatusOK);
        await Assert.That(tag.Unlock()).IsEqualTo(PlcTagStatus.StatusOK);

        tag.ReadOnly = true;
        _ = Assert.Throws<InvalidOperationException>(() => tag.Write());
        tag.ReadOnly = false;
        plc.FailOperationRaiseException = true;
        native.ReadStatusCode = PlcTagStatus.ErrBadParam;
        _ = Assert.Throws<PlcTagException>(() => tag.Read());
        native.ReadStatusCode = PlcTagStatus.StatusOK;
        native.WriteStatusCode = PlcTagStatus.ErrBadParam;
        _ = Assert.Throws<PlcTagException>(() => tag.Write());

        tag.Dispose();
        tag.Dispose();

        await Assert.That(native.DestroyCalls).IsEqualTo(1);
    }

    /// <summary>Verifies wrapper native value access and composite mapping.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcTagWrapperReadsWritesNativeValuesAndCompositeTypesAsync()
    {
        var native = new FakePlcTagNative();
        native.EnsureHandle(DirectHandle);
        var tag = new StubTag("Wrapper", DirectHandle, typeof(int), DataLength.INT32);
        var wrapper = new PlcTagWrapper(tag, native);

        wrapper.SetInt32(SampleIntValue);
        await Assert.That(wrapper.GetInt32()).IsEqualTo(SampleIntValue);

        wrapper.SetUInt32(SampleUInt32Value, SecondaryOffset);
        await Assert.That(wrapper.GetUInt32(SecondaryOffset)).IsEqualTo(SampleUInt32Value);

        wrapper.SetInt16(SampleShortValue);
        await Assert.That(wrapper.GetInt16()).IsEqualTo(SampleShortValue);

        wrapper.SetUInt16(SampleUInt16Value);
        await Assert.That(wrapper.GetUInt16()).IsEqualTo(SampleUInt16Value);

        wrapper.SetInt8(SampleSByteValue);
        await Assert.That(wrapper.GetInt8()).IsEqualTo(SampleSByteValue);

        wrapper.SetUInt8(SampleByteValue);
        await Assert.That(wrapper.GetUInt8()).IsEqualTo(SampleByteValue);

        wrapper.SetInt64(SampleLongValue);
        await Assert.That(wrapper.GetInt64()).IsEqualTo(SampleLongValue);

        wrapper.SetUInt64(SampleUInt64Value);
        await Assert.That(wrapper.GetUInt64()).IsEqualTo(SampleUInt64Value);

        wrapper.SetFloat32(SampleFloatValue);
        await Assert.That(wrapper.GetFloat32()).IsEqualTo(SampleFloatValue);

        wrapper.SetFloat64(SampleDoubleValue);
        await Assert.That(wrapper.GetFloat64()).IsEqualTo(SampleDoubleValue);

        wrapper.SetBool(true);
        await Assert.That(wrapper.GetBool()).IsTrue();

        wrapper.SetString(SampleStringValue);
        await Assert.That(wrapper.GetString()).IsEqualTo(SampleStringValue);

        wrapper.SetInt32(0);
        wrapper.SetBit(SampleBitIndex, true);
        await Assert.That(wrapper.GetBit(SampleBitIndex)).IsTrue();
        await Assert.That(wrapper.GetBitsString()[SampleBitIndex]).IsEqualTo('1');

        wrapper.SetBits(new BitArray([UpdatedIntValue]));
        await Assert.That(wrapper.GetBitsArray().Length).IsGreaterThan(0);

        var values = new[] { SampleIntValue, SecondArrayValue };
        wrapper.Set(values);
        var readResult = wrapper.Get(new int[values.Length]);
        var readValues = readResult is int[] typedValues
            ? typedValues
            : throw new InvalidOperationException("Wrapper returned an unexpected array value.");

        await Assert.That(readValues).IsEquivalentTo(values);

        var composite = new WrapperComposite
        {
            Count = SampleIntValue,
            Enabled = true,
            Text = SampleStringValue,
        };
        wrapper.SetType(composite);
        var readComposite = (WrapperComposite)wrapper.GetType(new WrapperComposite());

        await Assert.That(readComposite.Count).IsEqualTo(composite.Count);
        await Assert.That(readComposite.Enabled).IsEqualTo(composite.Enabled);
        await Assert.That(readComposite.Text).IsEqualTo(composite.Text);
        _ = Assert.Throws<ArgumentException>(() => wrapper.Set(DateTime.UtcNow));
        _ = Assert.Throws<ArgumentException>(() => wrapper.Get(DateTime.UtcNow));

        var invalidWrapper = new PlcTagWrapper(new StubTag("Invalid", DirectHandle, typeof(DateTime), DataLength.INT32), native);
        _ = Assert.Throws<ArgumentException>(() => invalidWrapper.GetBit(0));
    }

    /// <summary>Verifies tag mixin helpers operate through typed PLC tags.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task TagMixinsOperateOnTypedPlcTagsAsync()
    {
        var native = new FakePlcTagNative();
        using var plc = new ABPlc("127.0.0.1", PlcType.SLC, null, native)
        {
            AutoWriteValue = false,
        };
        using var tag = new PlcTag<short>(plc, "Flag", "B3:0", DataLength.INT16);

        TagMixins.SetBit(tag, SampleBitIndex, true);

        await Assert.That(TagMixins.GetBit(tag, SampleBitIndex)).IsTrue();
        _ = Assert.Throws<ArgumentNullException>(() => TagMixins.SetBit(null!, SampleBitIndex, true));
        _ = Assert.Throws<ArgumentNullException>(() => TagMixins.GetBit(null!, SampleBitIndex));
        _ = Assert.Throws<ArgumentNullException>(() => TagHelper.ScaleLinear(null!, 0D, 1D, 0D, 1D));
        _ = Assert.Throws<ArgumentNullException>(() => TagHelper.ScaleSquareRoot(null!, 0D, 1D, 0D, 1D));
        _ = Assert.Throws<ArgumentNullException>(() => TagHelper.BitsToNumber(null!));
        _ = Assert.Throws<InvalidOperationException>(() => DataLength.GetSizeObject(Array.Empty<int>()));
        await Assert.That(PlcTagStatus.DecodeError(PlcTagStatus.StatusOK)).IsNotNull();
    }

    /// <summary>Observer that forwards values to an action.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="onNext">The action invoked for each value.</param>
    private sealed class CaptureObserver<T>(Action<T> onNext) : IObserver<T>
    {
        /// <summary>Handles completion.</summary>
        public void OnCompleted()
        {
        }

        /// <summary>Handles errors.</summary>
        /// <param name="error">The observed error.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>Handles values.</summary>
        /// <param name="value">The observed value.</param>
        public void OnNext(T value) => onNext(value);
    }

    /// <summary>Fake native adapter backed by in-memory buffers.</summary>
    private sealed class FakePlcTagNative : IPlcTagNative
    {
        /// <summary>Native buffers keyed by handle.</summary>
        private readonly Dictionary<int, byte[]> _buffers = [];

        /// <summary>Next native handle.</summary>
        private int _nextHandle = FirstCreatedHandle;

        /// <summary>Gets or sets the read status code.</summary>
        public int ReadStatusCode { get; set; } = PlcTagStatus.StatusOK;

        /// <summary>Gets or sets the write status code.</summary>
        public int WriteStatusCode { get; set; } = PlcTagStatus.StatusOK;

        /// <summary>Gets the destroy call count.</summary>
        public int DestroyCalls { get; private set; }

        /// <summary>Gets the last created URL.</summary>
        public string LastCreatedUrl { get; private set; } = string.Empty;

        /// <summary>Gets the last read timeout.</summary>
        public int LastReadTimeout { get; private set; }

        /// <summary>Gets the last write timeout.</summary>
        public int LastWriteTimeout { get; private set; }

        /// <summary>Ensures a buffer exists for a handle.</summary>
        /// <param name="handle">The native handle.</param>
        public void EnsureHandle(int handle) => _ = GetBuffer(handle);

        /// <summary>Reads a signed 32-bit value.</summary>
        /// <param name="handle">The native handle.</param>
        /// <returns>The value.</returns>
        public int ReadInt32(int handle) => BitConverter.ToInt32(GetBuffer(handle), 0);

        int IPlcTagNative.Abort(int handle) => PlcTagStatus.StatusOK;

        int IPlcTagNative.Create(string url, int timeout)
        {
            LastCreatedUrl = url;
            var handle = _nextHandle++;
            _buffers.Add(handle, new byte[NativeBufferLength]);
            return handle;
        }

        int IPlcTagNative.Destroy(int handle)
        {
            DestroyCalls++;
            _ = _buffers.Remove(handle);
            return PlcTagStatus.StatusOK;
        }

        float IPlcTagNative.GetFloat32(int handle, int offset) => BitConverter.ToSingle(GetBuffer(handle), offset);

        double IPlcTagNative.GetFloat64(int handle, int offset) => BitConverter.ToDouble(GetBuffer(handle), offset);

        short IPlcTagNative.GetInt16(int handle, int offset) => BitConverter.ToInt16(GetBuffer(handle), offset);

        int IPlcTagNative.GetInt32(int handle, int offset) => BitConverter.ToInt32(GetBuffer(handle), offset);

        long IPlcTagNative.GetInt64(int handle, int offset) => BitConverter.ToInt64(GetBuffer(handle), offset);

        sbyte IPlcTagNative.GetInt8(int handle, int offset) => unchecked((sbyte)GetBuffer(handle)[offset]);

        int IPlcTagNative.GetSize(int handle) => GetBuffer(handle).Length;

        int IPlcTagNative.GetStatus(int handle) => PlcTagStatus.StatusOK;

        ushort IPlcTagNative.GetUInt16(int handle, int offset) => BitConverter.ToUInt16(GetBuffer(handle), offset);

        uint IPlcTagNative.GetUInt32(int handle, int offset) => BitConverter.ToUInt32(GetBuffer(handle), offset);

        ulong IPlcTagNative.GetUInt64(int handle, int offset) => BitConverter.ToUInt64(GetBuffer(handle), offset);

        byte IPlcTagNative.GetUInt8(int handle, int offset) => GetBuffer(handle)[offset];

        int IPlcTagNative.Lock(int handle) => PlcTagStatus.StatusOK;

        int IPlcTagNative.Read(int handle, int timeout)
        {
            LastReadTimeout = timeout;
            return ReadStatusCode;
        }

        void IPlcTagNative.SetFloat32(int handle, int offset, float value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetFloat64(int handle, int offset, double value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetInt16(int handle, int offset, short value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetInt32(int handle, int offset, int value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetInt64(int handle, int offset, long value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetInt8(int handle, int offset, sbyte value) =>
            GetBuffer(handle)[offset] = unchecked((byte)value);

        void IPlcTagNative.SetUInt16(int handle, int offset, ushort value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetUInt32(int handle, int offset, uint value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetUInt64(int handle, int offset, ulong value) =>
            WriteBytes(handle, offset, BitConverter.GetBytes(value));

        void IPlcTagNative.SetUInt8(int handle, int offset, byte value) =>
            GetBuffer(handle)[offset] = value;

        int IPlcTagNative.Unlock(int handle) => PlcTagStatus.StatusOK;

        int IPlcTagNative.Write(int handle, int timeout)
        {
            LastWriteTimeout = timeout;
            return WriteStatusCode;
        }

        /// <summary>Gets a native buffer.</summary>
        /// <param name="handle">The native handle.</param>
        /// <returns>The buffer.</returns>
        private byte[] GetBuffer(int handle)
        {
            if (!_buffers.TryGetValue(handle, out var buffer))
            {
                buffer = new byte[NativeBufferLength];
                _buffers.Add(handle, buffer);
            }

            return buffer;
        }

        /// <summary>Writes bytes to a native buffer.</summary>
        /// <param name="handle">The native handle.</param>
        /// <param name="offset">The byte offset.</param>
        /// <param name="bytes">The bytes to write.</param>
        private void WriteBytes(int handle, int offset, byte[] bytes) =>
            bytes.CopyTo(GetBuffer(handle), offset);
    }

    /// <summary>Composite value used by wrapper tests.</summary>
    private sealed class WrapperComposite
    {
        /// <summary>Gets or sets the count.</summary>
        public int Count { get; set; }

        /// <summary>Gets or sets a value indicating whether the composite is enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the text.</summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>PLC tag stub used by wrapper tests.</summary>
    /// <param name="tagName">The tag name.</param>
    /// <param name="handle">The fake native handle.</param>
    /// <param name="typeValue">The PLC value type.</param>
    /// <param name="size">The PLC element size.</param>
    private sealed class StubTag(string tagName, int handle, Type typeValue, int size) : IPlcTag
    {
        IObservable<PlcTagResult> IPlcTag.Changed => Signal.Silent<PlcTagResult>();

        int IPlcTag.Handle => handle;

        bool IPlcTag.IsRead => false;

        bool IPlcTag.IsWrite => false;

        string IPlcTag.Variable => tagName;

        int IPlcTag.Length => 1;

        string IPlcTag.TagName => tagName;

        bool IPlcTag.ReadOnly { get; set; }

        int IPlcTag.Size => size;

        Type IPlcTag.TypeValue => typeValue;

        object? IPlcTag.Value { get; set; }

        PlcTagWrapper IPlcTag.ValueManager => null!;

        int IPlcTag.Abort() => PlcTagStatus.StatusOK;

        void IDisposable.Dispose()
        {
        }

        int IPlcTag.GetSize() => size;

        int IPlcTag.GetStatus() => PlcTagStatus.StatusOK;

        int IPlcTag.Lock() => PlcTagStatus.StatusOK;

        PlcTagResult IPlcTag.Read() => null!;

        int IPlcTag.Unlock() => PlcTagStatus.StatusOK;

        PlcTagResult IPlcTag.Write() => null!;
    }
}
