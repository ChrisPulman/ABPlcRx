// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
using TUnit.Assertions;
using TUnit.Core;
using PlcController = ABPlcRx.ABPlcRx;
using ReactiveIPlcTag = ABPlcRx.Reactive.IPlcTag;
using ReactivePlcController = ABPlcRx.Reactive.ABPlcRx;
using ReactivePlcTagResult = ABPlcRx.Reactive.PlcTagResult;
using ReactivePlcType = ABPlcRx.Reactive.PlcType;

namespace ABPlcRx.Tests;

/// <summary>Tests the high-level reactive PLC facade.</summary>
public sealed class ReactiveSurfaceTests
{
    /// <summary>Default test scan interval in milliseconds.</summary>
    private const int TestScanIntervalMilliseconds = 10;

    /// <summary>Timeout for synchronous observable completion in seconds.</summary>
    private const int CompletionTimeoutSeconds = 5;

    /// <summary>Sample interval used by sampled observable tests.</summary>
    private const int SampleIntervalMilliseconds = 100;

    /// <summary>Sample value used when writing to a missing variable.</summary>
    private const int MissingVariableWriteValue = 42;

    /// <summary>Verifies an empty ObserveMany request emits an empty dictionary.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ObserveManyWithNoVariablesEmitsEmptyDictionaryAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));
        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, object?>>();

        using var subscription = plc.ObserveMany().Subscribe(new CaptureObserver<IReadOnlyDictionary<string, object?>>(completion.SetResult));
        var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(CompletionTimeoutSeconds));

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>Verifies argument validation happens before native tag creation.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task AddUpdateTagItemValidationRunsBeforeNativeTagCreationAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));

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
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));

        await Assert.That(plc.ObserveAllAsyncObservable).IsAssignableTo<IObservableAsync<IPlcTag?>>();
        await Assert.That(plc.ObserveAsyncObservable<int>("Counter")).IsAssignableTo<IObservableAsync<int>>();
        await Assert.That(plc.ObserveManyAsyncObservable()).IsAssignableTo<IObservableAsync<IReadOnlyDictionary<string, object?>>>();
        await Assert.That(plc.ObserveGroupAsyncObservable("Default")).IsAssignableTo<IObservableAsync<IPlcTag>>();
        await Assert.That(plc.ObserveSampledAsyncObservable<int>("Counter", TimeSpan.FromMilliseconds(SampleIntervalMilliseconds))).IsAssignableTo<IObservableAsync<int>>();
        await Assert.That(plc.ObserveErrorsAsyncObservable()).IsAssignableTo<IObservableAsync<PlcTagResult>>();
        await Assert.That(plc.ObservePingAsyncObservable(TimeSpan.FromSeconds(1))).IsAssignableTo<IObservableAsync<bool>>();
    }

    /// <summary>Verifies missing variables return default values without creating tags.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ValueReturnsDefaultForMissingVariableAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));

        await Assert.That(plc.Value<int>("Missing")).IsEqualTo(0);
        await Assert.That(plc.Value<string>("Missing")).IsNull();
        await Assert.That(() => plc.Value("Missing", MissingVariableWriteValue)).ThrowsNothing();
    }

    /// <summary>Verifies missing variables do not produce read or write results.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ReadWriteReturnNullForMissingVariableAsync()
    {
        using var plc = new PlcController(PlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));

        await Assert.That(plc.Read("Missing")).IsNull();
        await Assert.That(plc.Write("Missing")).IsNull();
    }

    /// <summary>Verifies the System.Reactive-flavoured facade exposes the same empty-stream behavior.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ReactiveObserveManyWithNoVariablesEmitsEmptyDictionaryAsync()
    {
        using var plc = new ReactivePlcController(ReactivePlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));
        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, object?>>();

        using var subscription = plc.ObserveMany().Subscribe(new CaptureObserver<IReadOnlyDictionary<string, object?>>(completion.SetResult));
        var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(CompletionTimeoutSeconds));

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>Verifies the System.Reactive-flavoured facade validates arguments before native tag creation.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ReactiveAddUpdateTagItemValidationRunsBeforeNativeTagCreationAsync()
    {
        using var plc = new ReactivePlcController(ReactivePlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));

        _ = Assert.Throws<ArgumentException>(() => plc.AddUpdateTagItem<int>(string.Empty, "N7:0", "Default"));
        _ = Assert.Throws<ArgumentException>(() => plc.AddUpdateTagItem<int>("Counter", string.Empty, "Default"));
        _ = Assert.Throws<ArgumentException>(() => plc.AddUpdateTagItem<int>("Counter", "N7:0", string.Empty));
        await Assert.That(() => plc.AddUpdateTagItem<bool>("Flag", "BoolTest", "Default")).ThrowsNothing();
    }

    /// <summary>Verifies the System.Reactive-flavoured async surface wraps observable pipelines.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ReactiveAsyncObservableSurfaceWrapsExistingObservablePipelinesAsync()
    {
        using var plc = new ReactivePlcController(ReactivePlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));

        await Assert.That(plc.ObserveAllAsyncObservable).IsAssignableTo<IObservableAsync<ReactiveIPlcTag?>>();
        await Assert.That(plc.ObserveAsyncObservable<int>("Counter")).IsAssignableTo<IObservableAsync<int>>();
        await Assert.That(plc.ObserveManyAsyncObservable()).IsAssignableTo<IObservableAsync<IReadOnlyDictionary<string, object?>>>();
        await Assert.That(plc.ObserveGroupAsyncObservable("Default")).IsAssignableTo<IObservableAsync<ReactiveIPlcTag>>();
        await Assert.That(plc.ObserveSampledAsyncObservable<int>("Counter", TimeSpan.FromMilliseconds(SampleIntervalMilliseconds))).IsAssignableTo<IObservableAsync<int>>();
        await Assert.That(plc.ObserveErrorsAsyncObservable()).IsAssignableTo<IObservableAsync<ReactivePlcTagResult>>();
        await Assert.That(plc.ObservePingAsyncObservable(TimeSpan.FromSeconds(1))).IsAssignableTo<IObservableAsync<bool>>();
    }

    /// <summary>Verifies missing variables return defaults through the System.Reactive-flavoured facade.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ReactiveMissingVariablesReturnDefaultsAsync()
    {
        using var plc = new ReactivePlcController(ReactivePlcType.SLC, "127.0.0.1", TimeSpan.FromMilliseconds(TestScanIntervalMilliseconds));

        await Assert.That(plc.Value<int>("Missing")).IsEqualTo(0);
        await Assert.That(plc.Value<string>("Missing")).IsNull();
        await Assert.That(() => plc.Value("Missing", MissingVariableWriteValue)).ThrowsNothing();
        await Assert.That(plc.Read("Missing")).IsNull();
        await Assert.That(plc.Write("Missing")).IsNull();
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
}
