// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ReactiveUI.Extensions.Async;
using TUnit.Core;

namespace ABPlcRx.Tests;

public sealed class ReactiveSurfaceTests
{
    [Test]
    public async Task ObserveManyWithNoVariablesEmitsEmptyDictionary()
    {
        using var plc = new global::ABPlcRx.ABPlcRx(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));
        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, object?>>();

        using var subscription = plc.ObserveMany().Subscribe(completion.SetResult);
        var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Equal(0, result.Count);
    }

    [Test]
    public async Task AddUpdateTagItemValidationRunsBeforeNativeTagCreation()
    {
        using var plc = new global::ABPlcRx.ABPlcRx(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));

        Throws<ArgumentNullException>(() => plc.AddUpdateTagItem<int>(string.Empty, "N7:0", "Default"));
        Throws<ArgumentNullException>(() => plc.AddUpdateTagItem<int>("Counter", string.Empty, "Default"));
        Throws<ArgumentNullException>(() => plc.AddUpdateTagItem<int>("Counter", "N7:0", string.Empty));
        Throws<Exception>(() => plc.AddUpdateTagItem<bool>("Flag", "B3:0", "Default"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task AsyncObservableSurfaceWrapsExistingObservablePipelines()
    {
        using var plc = new global::ABPlcRx.ABPlcRx(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));

        IsAssignableTo<IObservableAsync<IPlcTag?>>(plc.ObserveAllAsync);
        IsAssignableTo<IObservableAsync<IReadOnlyDictionary<string, object?>>>(plc.ObserveManyAsync());
        IsAssignableTo<IObservableAsync<PlcTagResult>>(plc.ObserveErrorsAsync());
        await Task.CompletedTask;
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected exception of type {typeof(TException).Name}.");
    }

    private static void IsAssignableTo<T>(object value)
    {
        if (value is not T)
        {
            throw new InvalidOperationException($"Expected value assignable to {typeof(T).FullName}.");
        }
    }
}
