// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using ABPlcRx.SourceGeneration;
using ReactiveUI.Primitives.Signals;
using TUnit.Assertions;
using TUnit.Core;

namespace ABPlcRx.Tests;

/// <summary>Tests value objects and validation helpers that do not require PLC IO.</summary>
public sealed class ValueObjectTests
{
    /// <summary>Sample integer array used by size calculation tests.</summary>
    private static readonly int[] SampleIntegers = [1, 2, 3];

    /// <summary>Verifies data length calculations for native, array, and nested class values.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task DataLengthComputesNativeArraysAndNestedClassesAsync()
    {
        await Assert.That(GetSizeObject(null)).IsEqualTo(0);
        await Assert.That(GetSizeObject(true)).IsEqualTo(1);
        await Assert.That(GetSizeObject("hello")).IsEqualTo(88);
        await Assert.That(GetSizeObject(SampleIntegers)).IsEqualTo(12);
        await Assert.That(GetSizeObject(new CompositeValue { Count = 7, Code = 4, Text = "A" })).IsEqualTo(94);
        await Assert.That(IsNativeType(typeof(double))).IsTrue();
        await Assert.That(IsNativeType(typeof(CompositeValue))).IsFalse();
    }

    /// <summary>Verifies result reduction chooses the earliest timestamp, sums execution time, and keeps the worst status.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcTagResultReduceAggregatesResultsAsync()
    {
        var tag = new StubTag("Counter", 42);
        var timestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var first = CreateResult(tag, timestamp.AddSeconds(2), 5, PlcTagStatus.StatusOK);
        var second = CreateResult(tag, timestamp, 7, PlcTagStatus.ErrBadParam);

        var reduced = PlcTagResult.Reduce([first, second]);
        var text = second.ToString();

        await Assert.That(reduced.Tag).IsEqualTo(tag);
        await Assert.That(reduced.Timestamp).IsEqualTo(timestamp);
        await Assert.That(reduced.ExecutionTime).IsEqualTo(12);
        await Assert.That(reduced.StatusCode).IsEqualTo(PlcTagStatus.ErrBadParam);
        await Assert.That(text).Contains("Counter");
        await Assert.That(text).Contains("42");
        _ = Assert.Throws<ArgumentNullException>(() => PlcTagResult.Reduce(null!));
    }

    /// <summary>Verifies PLC tag exception constructors preserve messages, inner exceptions, and result context.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcTagExceptionConstructorsPreserveDetailsAsync()
    {
        var inner = new InvalidOperationException("inner");
        var result = CreateResult(new StubTag("Counter", 42), DateTime.UtcNow, 1, PlcTagStatus.ErrBadParam);

        var defaultException = new PlcTagException();
        var messageException = new PlcTagException("custom");
        var innerException = new PlcTagException("wrapped", inner);
        var resultException = new PlcTagException(result);

        await Assert.That(defaultException.Message).IsEqualTo("Error executing PlcTag operation.");
        await Assert.That(messageException.Message).IsEqualTo("custom");
        await Assert.That(innerException.InnerException).IsEqualTo(inner);
        await Assert.That(resultException.Result).IsEqualTo(result);
    }

    /// <summary>Verifies source generation attributes expose configured metadata.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task SourceGenerationAttributesExposeConfiguredMetadataAsync()
    {
        var propertyAttribute = new PlcTagAttribute("N7:0")
        {
            Variable = "Counter",
            Group = "Fast",
            Bit = 3,
            RegisterTag = false,
        };
        var classAttribute = new PlcTagAttribute(typeof(int), "Counter", "N7:0");
        var modelAttribute = new PlcModelAttribute();

        await Assert.That(propertyAttribute.TagName).IsEqualTo("N7:0");
        await Assert.That(propertyAttribute.ValueType).IsNull();
        await Assert.That(propertyAttribute.PropertyName).IsNull();
        await Assert.That(propertyAttribute.Variable).IsEqualTo("Counter");
        await Assert.That(propertyAttribute.Group).IsEqualTo("Fast");
        await Assert.That(propertyAttribute.Bit).IsEqualTo(3);
        await Assert.That(propertyAttribute.RegisterTag).IsFalse();
        await Assert.That(classAttribute.ValueType).IsEqualTo(typeof(int));
        await Assert.That(classAttribute.PropertyName).IsEqualTo("Counter");
        await Assert.That(classAttribute.TagName).IsEqualTo("N7:0");
        await Assert.That(modelAttribute).IsAssignableTo<Attribute>();
    }

    /// <summary>Verifies wrapper validation paths complete before native PLC calls.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcTagWrapperValidationPathsAvoidNativeCallsAsync()
    {
        var wrapper = CreateWrapper(new StubTag("Counter", 0) { Size = 1, TypeValue = typeof(int) });

        _ = Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.SetBit(8, true));
        _ = Assert.Throws<ArgumentNullException>(() => wrapper.SetBits(null!));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.SetString(string.Empty));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.SetString(new string('x', 83)));
        await Assert.That(wrapper.GetType(null!)).IsNull();
        await Assert.That(InvokeWrapperGet(wrapper, null)).IsNull();
        await Assert.That(() => InvokeWrapperSet(wrapper, null)).ThrowsNothing();
    }

    /// <summary>Creates a PLC tag result through its internal constructor.</summary>
    /// <param name="tag">The source tag.</param>
    /// <param name="timestamp">The result timestamp.</param>
    /// <param name="executionTime">The execution time.</param>
    /// <param name="statusCode">The status code.</param>
    /// <returns>The result instance.</returns>
    private static PlcTagResult CreateResult(IPlcTag tag, DateTime timestamp, long executionTime, int statusCode)
    {
        var constructor = typeof(PlcTagResult).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IPlcTag), typeof(DateTime), typeof(long), typeof(int)],
            null)
            ?? throw new MissingMethodException(typeof(PlcTagResult).FullName, ".ctor");

        var result = constructor.Invoke([tag, timestamp, executionTime, statusCode]);
        return result is PlcTagResult plcTagResult
            ? plcTagResult
            : throw new InvalidOperationException("The PLC tag result constructor returned an unexpected value.");
    }

    /// <summary>Creates a PLC tag wrapper through its internal constructor.</summary>
    /// <param name="tag">The wrapped tag.</param>
    /// <returns>The wrapper instance.</returns>
    private static PlcTagWrapper CreateWrapper(IPlcTag tag)
    {
        var result = Activator.CreateInstance(
            typeof(PlcTagWrapper),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [tag],
            null);
        return result is PlcTagWrapper wrapper
            ? wrapper
            : throw new InvalidOperationException("The PLC tag wrapper constructor returned an unexpected value.");
    }

    /// <summary>Invokes the internal data length size helper.</summary>
    /// <param name="value">The value to measure.</param>
    /// <returns>The computed size.</returns>
    private static int GetSizeObject(object? value) =>
        InvokeDataLengthMethod<int>(nameof(GetSizeObject), value);

    /// <summary>Invokes the internal data length native type helper.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><c>true</c> when the type is native; otherwise <c>false</c>.</returns>
    private static bool IsNativeType(Type type) =>
        InvokeDataLengthMethod<bool>(nameof(IsNativeType), type);

    /// <summary>Gets the internal data length type.</summary>
    /// <returns>The data length type.</returns>
    private static Type GetDataLengthType() =>
        typeof(IPlcTag).Assembly.GetType("ABPlcRx.DataLength")
        ?? throw new TypeLoadException("Could not load ABPlcRx.DataLength.");

    /// <summary>Invokes a static method on the internal data length type.</summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="methodName">The method name.</param>
    /// <param name="arguments">The method arguments.</param>
    /// <returns>The typed method result.</returns>
    private static T InvokeDataLengthMethod<T>(string methodName, params object?[] arguments)
    {
        var dataLengthType = GetDataLengthType();
        var method = dataLengthType.GetMethod(methodName)
            ?? throw new MissingMethodException(dataLengthType.FullName, methodName);

        var result = method.Invoke(null, arguments);
        return result is T typedResult
            ? typedResult
            : throw new InvalidOperationException($"The {methodName} method returned an unexpected value.");
    }

    /// <summary>Invokes the internal wrapper get helper.</summary>
    /// <param name="wrapper">The wrapper.</param>
    /// <param name="value">The source value.</param>
    /// <returns>The returned value.</returns>
    private static object? InvokeWrapperGet(PlcTagWrapper wrapper, object? value)
    {
        var method = typeof(PlcTagWrapper).GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(PlcTagWrapper).FullName, "Get");

        return method.Invoke(wrapper, [value, 0]);
    }

    /// <summary>Invokes the internal wrapper set helper.</summary>
    /// <param name="wrapper">The wrapper.</param>
    /// <param name="value">The source value.</param>
    private static void InvokeWrapperSet(PlcTagWrapper wrapper, object? value)
    {
        var method = typeof(PlcTagWrapper).GetMethod("Set", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(PlcTagWrapper).FullName, "Set");

        _ = method.Invoke(wrapper, [value, 0]);
    }

    /// <summary>Composite value used by data length tests.</summary>
    private sealed class CompositeValue
    {
        /// <summary>Gets or sets the count.</summary>
        public int Count { get; set; }

        /// <summary>Gets or sets the code.</summary>
        public short Code { get; set; }

        /// <summary>Gets or sets the text.</summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>PLC tag stub used by value object tests.</summary>
    /// <param name="tagName">The tag name.</param>
    /// <param name="value">The tag value.</param>
    private sealed class StubTag(string tagName, object? value) : IPlcTag
    {
        IObservable<PlcTagResult> IPlcTag.Changed => Signal.Silent<PlcTagResult>();

        int IPlcTag.Handle => 0;

        bool IPlcTag.IsRead => false;

        bool IPlcTag.IsWrite => false;

        string IPlcTag.Variable => tagName;

        int IPlcTag.Length => 1;

        string IPlcTag.TagName => tagName;

        bool IPlcTag.ReadOnly { get; set; }

        /// <summary>Gets or sets the test tag element size.</summary>
        public int Size { get; set; } = 4;

        int IPlcTag.Size => Size;

        /// <summary>Gets or sets the test tag value type.</summary>
        public Type TypeValue { get; set; } = value?.GetType() ?? typeof(int);

        Type IPlcTag.TypeValue => TypeValue;

        object? IPlcTag.Value { get; set; } = value;

        PlcTagWrapper IPlcTag.ValueManager => null!;

        int IPlcTag.Abort() => 0;

        void IDisposable.Dispose()
        {
        }

        int IPlcTag.GetSize() => Size;

        int IPlcTag.GetStatus() => PlcTagStatus.StatusOK;

        int IPlcTag.Lock() => PlcTagStatus.StatusOK;

        PlcTagResult IPlcTag.Read() => null!;

        int IPlcTag.Unlock() => PlcTagStatus.StatusOK;

        PlcTagResult IPlcTag.Write() => null!;
    }
}
