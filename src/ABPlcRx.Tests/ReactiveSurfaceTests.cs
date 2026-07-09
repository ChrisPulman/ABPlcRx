// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Async;
using TUnit.Assertions;
using TUnit.Core;
using PlcController = ABPlcRx.ABPlcRx;

namespace ABPlcRx.Tests;

/// <summary>Tests the high-level reactive PLC facade.</summary>
public sealed class ReactiveSurfaceTests
{
    /// <summary>Verifies an empty ObserveMany request emits an empty dictionary.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ObserveManyWithNoVariablesEmitsEmptyDictionaryAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));
        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, object?>>();

        using var subscription = plc.ObserveMany().Subscribe(completion.SetResult);
        var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>Verifies argument validation happens before native tag creation.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task AddUpdateTagItemValidationRunsBeforeNativeTagCreationAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));

        _ = Assert.Throws<ArgumentException>(() => plc.AddUpdateTagItem<int>(string.Empty, "N7:0", "Default"));
        _ = Assert.Throws<ArgumentException>(() => plc.AddUpdateTagItem<int>("Counter", string.Empty, "Default"));
        _ = Assert.Throws<ArgumentException>(() => plc.AddUpdateTagItem<int>("Counter", "N7:0", string.Empty));
        await Assert.That(() => plc.AddUpdateTagItem<bool>("Flag", "BoolTest", "Default")).ThrowsNothing();
        await Task.CompletedTask;
    }

    /// <summary>Verifies async-observable members wrap existing observable pipelines.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task AsyncObservableSurfaceWrapsExistingObservablePipelinesAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));

        await Assert.That(plc.ObserveAllAsyncObservable).IsAssignableTo<IObservableAsync<IPlcTag?>>();
        await Assert.That(plc.ObserveAsyncObservable<int>("Counter")).IsAssignableTo<IObservableAsync<int>>();
        await Assert.That(plc.ObserveManyAsyncObservable()).IsAssignableTo<IObservableAsync<IReadOnlyDictionary<string, object?>>>();
        await Assert.That(plc.ObserveGroupAsyncObservable("Default")).IsAssignableTo<IObservableAsync<IPlcTag>>();
        await Assert.That(plc.ObserveSampledAsyncObservable<int>("Counter", TimeSpan.FromMilliseconds(100))).IsAssignableTo<IObservableAsync<int>>();
        await Assert.That(plc.ObserveErrorsAsyncObservable()).IsAssignableTo<IObservableAsync<PlcTagResult>>();
        await Assert.That(plc.ObservePingAsyncObservable(TimeSpan.FromSeconds(1))).IsAssignableTo<IObservableAsync<bool>>();
    }

    /// <summary>Verifies missing variables return default values without creating tags.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ValueReturnsDefaultForMissingVariableAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));

        await Assert.That(plc.Value<int>("Missing")).IsEqualTo(0);
        await Assert.That(plc.Value<string>("Missing")).IsNull();
        await Assert.That(() => plc.Value("Missing", 42)).ThrowsNothing();
    }

    /// <summary>Verifies missing variables do not produce read or write results.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ReadWriteReturnNullForMissingVariableAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(10));

        await Assert.That(plc.Read("Missing")).IsNull();
        await Assert.That(plc.Write("Missing")).IsNull();
    }
}
