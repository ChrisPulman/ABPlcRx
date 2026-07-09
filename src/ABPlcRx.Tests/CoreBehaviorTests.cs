// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using ReactiveUI.Primitives.Signals;
using TUnit.Assertions;
using TUnit.Core;
using PlcController = ABPlcRx.ABPlcRx;

namespace ABPlcRx.Tests;

/// <summary>Tests core PLC helper behavior that does not require a live controller.</summary>
public sealed class CoreBehaviorTests
{
    /// <summary>Verifies object creation normalizes nested strings.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task CreateObjectNormalizesStringValuesAsync()
    {
        var directValue = new NestedValue();
        var value = TagHelper.CreateObject<NestedValue>(1);

        await Assert.That(directValue.Child).IsNotNull();
        await Assert.That(value.Name).IsEqualTo(string.Empty);
        await Assert.That(value.Child.Text).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies string arrays are initialized with empty entries.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task CreateObjectCreatesStringArraysWithEmptyEntriesAsync()
    {
        var values = TagHelper.CreateObject<string[]>(3);

        await Assert.That(values.Length).IsEqualTo(3);
        await Assert.That(values.All(static value => value is { Length: 0 })).IsTrue();
    }

    /// <summary>Verifies bool creation returns the default value.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task CreateObjectCreatesBoolDefaultAsync()
    {
        var value = TagHelper.CreateObject<bool>(1);

        await Assert.That(value).IsFalse();
    }

    /// <summary>Verifies bit arrays round-trip known integer values.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task BitsRoundTripKnownValuesAsync()
    {
        foreach (var value in new[] { 0, 1, 2, 255, -1 })
        {
            await Assert.That(TagHelper.BitsToNumber(TagHelper.NumberToBits(value))).IsEqualTo(value);
        }
    }

    /// <summary>Verifies short bit helper extensions set, clear, and read bits.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ShortBitHelpersSetClearAndReadBitsAsync()
    {
        var value = TagMixins.SetBit((short)0, 0, true);
        await Assert.That(value).IsEqualTo((short)1);
        await Assert.That(TagMixins.GetBit(value, 0)).IsTrue();

        value = TagMixins.SetBit(value, 0, false);
        await Assert.That(value).IsEqualTo((short)0);
        await Assert.That(TagMixins.GetBit(value, 0)).IsFalse();
    }

    /// <summary>Verifies PLC status error classification rules.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcTagStatusClassifiesOnlyNonPendingNonOkCodesAsErrorsAsync()
    {
        await Assert.That(PlcTagStatus.IsError(PlcTagStatus.StatusOK)).IsFalse();
        await Assert.That(PlcTagStatus.IsError(PlcTagStatus.StatusPending)).IsFalse();
        await Assert.That(PlcTagStatus.IsError(PlcTagStatus.ErrBadParam)).IsTrue();
    }

    /// <summary>Verifies linear scaling maps raw values and rejects invalid raw bounds.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ScaleLinearMapsRawRangeAndRejectsInvalidRangesAsync()
    {
        var tag = new ScalingTag(50d);

        await Assert.That(TagHelper.ScaleLinear(tag, 0d, 100d, 0d, 10d)).IsEqualTo(5d);
        _ = Assert.Throws<InvalidOperationException>(() => TagHelper.ScaleLinear(tag, 1d, 1d, 0d, 10d));
        _ = Assert.Throws<InvalidOperationException>(() => TagHelper.ScaleLinear(tag, 2d, 1d, 0d, 10d));
        _ = Assert.Throws<InvalidOperationException>(() => TagHelper.ScaleLinear(tag, 0d, 100d, 10d, 0d));
    }

    /// <summary>Verifies square-root scaling maps raw values and rejects invalid raw bounds.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ScaleSquareRootMapsRawRangeAndRejectsInvalidRangesAsync()
    {
        var tag = new ScalingTag(25d);

        await Assert.That(TagHelper.ScaleSquareRoot(tag, 0d, 100d, 0d, 100d)).IsEqualTo(50d);
        _ = Assert.Throws<InvalidOperationException>(() => TagHelper.ScaleSquareRoot(tag, 1d, 1d, 0d, 10d));
        _ = Assert.Throws<InvalidOperationException>(() => TagHelper.ScaleSquareRoot(tag, 2d, 1d, 0d, 10d));
        _ = Assert.Throws<InvalidOperationException>(() => TagHelper.ScaleSquareRoot(tag, 0d, 100d, 10d, 0d));
    }

    /// <summary>Verifies private integral conversion helpers round-trip supported PLC value types.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task IntegralBitConversionHelpersRoundTripSupportedTypesAsync()
    {
        await Assert.That(GetUnsignedIntegralValue((byte)42, typeof(byte))).IsEqualTo(42UL);
        await Assert.That(GetUnsignedIntegralValue((sbyte)-1, typeof(sbyte))).IsEqualTo(byte.MaxValue);
        await Assert.That(GetUnsignedIntegralValue((ushort)42, typeof(ushort))).IsEqualTo(42UL);
        await Assert.That(GetUnsignedIntegralValue((short)-1, typeof(short))).IsEqualTo(ushort.MaxValue);
        await Assert.That(GetUnsignedIntegralValue(42U, typeof(uint))).IsEqualTo(42UL);
        await Assert.That(GetUnsignedIntegralValue(-1, typeof(int))).IsEqualTo(uint.MaxValue);
        await Assert.That(GetUnsignedIntegralValue(42UL, typeof(ulong))).IsEqualTo(42UL);
        await Assert.That(GetUnsignedIntegralValue(-1L, typeof(long))).IsEqualTo(ulong.MaxValue);

        await Assert.That(ConvertUnsignedIntegralValue(42UL, typeof(byte))).IsEqualTo((byte)42);
        await Assert.That(ConvertUnsignedIntegralValue(byte.MaxValue, typeof(sbyte))).IsEqualTo((sbyte)-1);
        await Assert.That(ConvertUnsignedIntegralValue(42UL, typeof(ushort))).IsEqualTo((ushort)42);
        await Assert.That(ConvertUnsignedIntegralValue(ushort.MaxValue, typeof(short))).IsEqualTo((short)-1);
        await Assert.That(ConvertUnsignedIntegralValue(42UL, typeof(uint))).IsEqualTo(42U);
        await Assert.That(ConvertUnsignedIntegralValue(uint.MaxValue, typeof(int))).IsEqualTo(-1);
        await Assert.That(ConvertUnsignedIntegralValue(42UL, typeof(ulong))).IsEqualTo(42UL);
        await Assert.That(ConvertUnsignedIntegralValue(ulong.MaxValue, typeof(long))).IsEqualTo(-1L);
    }

    /// <summary>Verifies private bit-index validation rejects invalid PLC value types and bit positions.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task BitIndexValidationRejectsInvalidTypesAndRangesAsync()
    {
        await Assert.That(() => ValidateBitIndex(typeof(byte), 7)).ThrowsNothing();
        await Assert.That(() => ValidateBitIndex(typeof(short), 15)).ThrowsNothing();
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => ValidateBitIndex(typeof(byte), -1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => ValidateBitIndex(typeof(byte), 8));
        _ = Assert.Throws<ArgumentException>(() => ValidateBitIndex(typeof(bool), 0));
    }

    /// <summary>Invokes the private unsigned integral reader.</summary>
    /// <param name="value">The boxed value.</param>
    /// <param name="type">The declared value type.</param>
    /// <returns>The unsigned integral value.</returns>
    private static ulong GetUnsignedIntegralValue(object value, Type type) =>
        (ulong)InvokePrivate(nameof(GetUnsignedIntegralValue), value, type);

    /// <summary>Invokes the private unsigned integral converter.</summary>
    /// <param name="value">The unsigned integral value.</param>
    /// <param name="type">The target value type.</param>
    /// <returns>The converted value.</returns>
    private static object ConvertUnsignedIntegralValue(ulong value, Type type) =>
        InvokePrivate(nameof(ConvertUnsignedIntegralValue), value, type);

    /// <summary>Invokes the private bit-index validator.</summary>
    /// <param name="type">The declared value type.</param>
    /// <param name="bit">The bit index.</param>
    private static void ValidateBitIndex(Type type, int bit) =>
        _ = InvokePrivate(nameof(ValidateBitIndex), type, bit);

    /// <summary>Invokes a private static method on the PLC controller facade.</summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="arguments">The method arguments.</param>
    /// <returns>The method result.</returns>
    private static object InvokePrivate(string methodName, params object[] arguments)
    {
        try
        {
            var method = typeof(PlcController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(PlcController).FullName, methodName);

            return method.Invoke(null, arguments) ?? DBNull.Value;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>Nested test value with string properties.</summary>
    private sealed class NestedValue
    {
        /// <summary>Gets or sets the string value.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the nested child value.</summary>
        public NestedChild Child { get; set; } = new();
    }

    /// <summary>Nested child value with string properties.</summary>
    private sealed class NestedChild
    {
        /// <summary>Gets or sets the text value.</summary>
        public string? Text { get; set; }
    }

    /// <summary>PLC tag stub used by scaling tests.</summary>
    /// <param name="value">The initial tag value.</param>
    private sealed class ScalingTag(object? value) : IPlcTag
    {
        /// <summary>The current tag value.</summary>
        private object? _value = value;

        IObservable<PlcTagResult> IPlcTag.Changed => Signal.Silent<PlcTagResult>();

        int IPlcTag.Handle => 0;

        bool IPlcTag.IsRead => false;

        bool IPlcTag.IsWrite => false;

        string IPlcTag.Variable => nameof(ScalingTag);

        int IPlcTag.Length => 1;

        string IPlcTag.TagName => nameof(ScalingTag);

        bool IPlcTag.ReadOnly { get; set; }

        int IPlcTag.Size => 8;

        Type IPlcTag.TypeValue => _value?.GetType() ?? typeof(double);

        object? IPlcTag.Value
        {
            get => _value;
            set => _value = value;
        }

        PlcTagWrapper IPlcTag.ValueManager => null!;

        int IPlcTag.Abort() => 0;

        void IDisposable.Dispose()
        {
        }

        int IPlcTag.GetSize() => 8;

        int IPlcTag.GetStatus() => PlcTagStatus.StatusOK;

        int IPlcTag.Lock() => PlcTagStatus.StatusOK;

        PlcTagResult IPlcTag.Read() => null!;

        int IPlcTag.Unlock() => PlcTagStatus.StatusOK;

        PlcTagResult IPlcTag.Write() => null!;
    }
}
