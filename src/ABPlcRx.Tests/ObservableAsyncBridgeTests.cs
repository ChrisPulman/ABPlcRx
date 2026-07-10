// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Async;
using TUnit.Assertions;
using TUnit.Core;

namespace ABPlcRx.Tests;

/// <summary>Tests synchronous-to-async observable bridge behavior.</summary>
public sealed class ObservableAsyncBridgeTests
{
    /// <summary>Sample value forwarded by bridge tests.</summary>
    private const int SampleValue = 42;

    /// <summary>Verifies null source validation happens before adapter creation.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ToAsyncObservableRejectsNullSourceAsync()
    {
        _ = Assert.Throws<ArgumentNullException>(() => ObservableAsyncBridgeExtensions.ToAsyncObservable<int>(null!));
        await Task.CompletedTask;
    }

    /// <summary>Verifies subscribe validation happens before source subscription.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ToAsyncObservableRejectsNullObserverAsync()
    {
        var source = new ManualObservable<int>();
        var asyncObservable = ObservableAsyncBridgeExtensions.ToAsyncObservable(source);

        _ = Assert.Throws<ArgumentNullException>(() => asyncObservable.SubscribeAsync(null!).AsTask().GetAwaiter().GetResult());
        await Assert.That(source.SubscriberCount).IsEqualTo(0);
    }

    /// <summary>Verifies cancellation is observed before source subscription.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ToAsyncObservableRejectsCanceledSubscriptionAsync()
    {
        var source = new ManualObservable<int>();
        var asyncObservable = ObservableAsyncBridgeExtensions.ToAsyncObservable(source);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = Assert.Throws<OperationCanceledException>(() => asyncObservable.SubscribeAsync(new RecordingAsyncObserver<int>(), cancellation.Token).AsTask().GetAwaiter().GetResult());
        await Assert.That(source.SubscriberCount).IsEqualTo(0);
    }

    /// <summary>Verifies next, error, completion, and disposal are forwarded.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task ToAsyncObservableForwardsNotificationsAndDisposalAsync()
    {
        var source = new ManualObservable<int>();
        var observer = new RecordingAsyncObserver<int>();
        var asyncObservable = ObservableAsyncBridgeExtensions.ToAsyncObservable(source);

        var subscription = await asyncObservable.SubscribeAsync(observer, CancellationToken.None);
        var error = new InvalidOperationException("bridge");

        source.Next(SampleValue);
        source.Error(error);
        source.Completed();

        await Assert.That(observer.Values).IsEquivalentTo([SampleValue]);
        await Assert.That(observer.Error).IsEqualTo(error);
        await Assert.That(observer.Completed).IsTrue();
        await Assert.That(source.SubscriberCount).IsEqualTo(1);

        await subscription.DisposeAsync();

        await Assert.That(source.SubscriberCount).IsEqualTo(0);
    }

    /// <summary>Manual observable used to verify bridge subscription behavior.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    private sealed class ManualObservable<T> : IObservable<T>
    {
        /// <summary>Active observers.</summary>
        private readonly List<IObserver<T>> _observers = [];

        /// <summary>Gets the number of active subscribers.</summary>
        public int SubscriberCount => _observers.Count;

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            return new ActionDisposable(() => _ = _observers.Remove(observer));
        }

        /// <summary>Publishes a value.</summary>
        /// <param name="value">The value.</param>
        public void Next(T value)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(value);
            }
        }

        /// <summary>Publishes an error.</summary>
        /// <param name="error">The error.</param>
        public void Error(Exception error)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnError(error);
            }
        }

        /// <summary>Publishes completion.</summary>
        public void Completed()
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnCompleted();
            }
        }
    }

    /// <summary>Records async observer notifications.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    private sealed class RecordingAsyncObserver<T> : IObserverAsync<T>
    {
        /// <summary>Gets observed values.</summary>
        public List<T> Values { get; } = [];

        /// <summary>Gets the observed error.</summary>
        public Exception? Error { get; private set; }

        /// <summary>Gets a value indicating whether completion was observed.</summary>
        public bool Completed { get; private set; }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => default;

        /// <inheritdoc/>
        public ValueTask OnCompletedAsync(Result result)
        {
            Completed = true;
            return default;
        }

        /// <inheritdoc/>
        public ValueTask OnErrorResumeAsync(Exception error, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Error = error;
            return default;
        }

        /// <inheritdoc/>
        public ValueTask OnNextAsync(T value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Values.Add(value);
            return default;
        }
    }

    /// <summary>Disposable wrapper for a teardown action.</summary>
    /// <param name="dispose">The teardown action.</param>
    private sealed class ActionDisposable(Action dispose) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => dispose();
    }
}
