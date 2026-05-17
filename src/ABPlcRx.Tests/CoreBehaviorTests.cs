// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TUnit.Core;

namespace ABPlcRx.Tests;

public sealed class CoreBehaviorTests
{
    [Test]
    public async Task CreateObjectNormalizesStringValues()
    {
        var value = TagHelper.CreateObject<NestedValue>(1);

        Equal(string.Empty, value.Name);
        Equal(string.Empty, value.Child.Text);
        await Task.CompletedTask;
    }

    [Test]
    public async Task CreateObjectCreatesStringArraysWithEmptyEntries()
    {
        var values = TagHelper.CreateObject<string[]>(3);

        Equal(3, values.Length);
        True(values.All(static value => value == string.Empty), "Expected every generated string entry to be empty.");
        await Task.CompletedTask;
    }

    [Test]
    public async Task BitsRoundTripKnownValues()
    {
        foreach (var value in new[] { 0, 1, 2, 255, -1 })
        {
            Equal(value, TagHelper.BitsToNumber(TagHelper.NumberToBits(value)));
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task ShortBitHelpersSetClearAndReadBits()
    {
        var value = ((short)0).SetBit(0, true);
        Equal((short)1, value);
        True(value.GetBit(0), "Bit 0 should be set.");

        value = value.SetBit(0, false);
        Equal((short)0, value);
        False(value.GetBit(0), "Bit 0 should be cleared.");
        await Task.CompletedTask;
    }

    [Test]
    public async Task PlcTagStatusClassifiesOnlyNonPendingNonOkCodesAsErrors()
    {
        False(PlcTagStatus.IsError(PlcTagStatus.StatusOK), "StatusOK should not be an error.");
        False(PlcTagStatus.IsError(PlcTagStatus.StatusPending), "StatusPending should not be an error.");
        True(PlcTagStatus.IsError(PlcTagStatus.ErrBadParam), "Negative libplctag status codes should be errors.");
        await Task.CompletedTask;
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void False(bool condition, string message) => True(!condition, message);

    private sealed class NestedValue
    {
        public string? Name { get; set; }

        public NestedChild Child { get; set; } = new();
    }

    private sealed class NestedChild
    {
        public string? Text { get; set; }
    }
}
