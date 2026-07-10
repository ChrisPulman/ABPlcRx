// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using PrimitivesResult = ReactiveUI.Primitives.Result;

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>Bridges synchronous observable streams to ReactiveUI.Primitives async observables.</summary>
public static class ObservableAsyncBridgeExtensions
{
    /// <summary>Wraps a synchronous observable as an async-native observable.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <returns>An async observable that forwards source notifications.</returns>
    public static IObservableAsync<T> ToAsyncObservable<T>(IObservable<T> source)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
#endif
        return new ObservableAsyncAdapter<T>(source);
    }

    /// <summary>Adapts a synchronous observable to the async observable contract.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="source">The source observable.</param>
    private sealed class ObservableAsyncAdapter<T>(IObservable<T> source) : IObservableAsync<T>
    {
        /// <summary>Subscribes an async observer to the source observable.</summary>
        /// <param name="observer">The async observer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async disposable subscription.</returns>
        public ValueTask<IAsyncDisposable> SubscribeAsync(IObserverAsync<T> observer, CancellationToken cancellationToken = default)
        {
            ArgumentExceptionHelper.ThrowIfNull(observer, nameof(observer));
            cancellationToken.ThrowIfCancellationRequested();

            var subscription = source.Subscribe(new ObserverAsyncAdapter<T>(observer, cancellationToken));
            return new ValueTask<IAsyncDisposable>(new SubscriptionAsyncDisposable(subscription));
        }
    }

    /// <summary>Adapts async observer callbacks to synchronous observer notifications.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="observer">The async observer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private sealed class ObserverAsyncAdapter<T>(IObserverAsync<T> observer, CancellationToken cancellationToken) : IObserver<T>
    {
        /// <summary>Forwards completion to the async observer.</summary>
        public void OnCompleted() =>
            observer.OnCompletedAsync(PrimitivesResult.Success).AsTask().GetAwaiter().GetResult();

        /// <summary>Forwards errors to the async observer.</summary>
        /// <param name="error">The observed error.</param>
        public void OnError(Exception error) =>
            observer.OnErrorResumeAsync(error, cancellationToken).AsTask().GetAwaiter().GetResult();

        /// <summary>Forwards values to the async observer.</summary>
        /// <param name="value">The observed value.</param>
        public void OnNext(T value) =>
            observer.OnNextAsync(value, cancellationToken).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>Disposes a synchronous subscription through the async disposable contract.</summary>
    /// <param name="subscription">The source subscription.</param>
    private sealed class SubscriptionAsyncDisposable(IDisposable subscription) : IAsyncDisposable
    {
        /// <summary>Disposes the wrapped subscription.</summary>
        /// <returns>A completed value task.</returns>
        public ValueTask DisposeAsync()
        {
            subscription.Dispose();
            return default;
        }
    }
}
