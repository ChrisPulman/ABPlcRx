// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using SignalFactory = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using SignalFactory = ReactiveUI.Primitives.Signals.Signal;
#endif

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>Reactive Allen Bradley PLC facade.</summary>
public class ABPlcRx : IABPlcRx
{
    /// <summary>Number of bits in a byte or signed byte.</summary>
    private const int ByteBitWidth = 8;

    /// <summary>Number of bits in a 16-bit integer.</summary>
    private const int Int16BitWidth = 16;

    /// <summary>Number of bits in a 32-bit integer.</summary>
    private const int Int32BitWidth = 32;

    /// <summary>Number of bits in a 64-bit integer.</summary>
    private const int Int64BitWidth = 64;

    /// <summary>Tracks subscriptions and owned disposable resources.</summary>
    private readonly MultipleDisposable _disposables = [];

    /// <summary>Backs PLC communication and tag management.</summary>
    private readonly ABPlc _plc;

    /// <summary>Default polling interval for tag groups.</summary>
    private readonly TimeSpan _scanInterval;

    /// <summary>Initializes a new instance of the <see cref="ABPlcRx" /> class.</summary>
    /// <param name="plcType">Type of the PLC.</param>
    /// <param name="ip">The ip.</param>
    /// <param name="scanInterval">The scan interval.</param>
    public ABPlcRx(PlcType plcType, string ip, TimeSpan scanInterval)
        : this(plcType, ip, scanInterval, TimeSpan.FromSeconds(1), null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ABPlcRx" /> class.</summary>
    /// <param name="plcType">Type of the PLC.</param>
    /// <param name="ip">The ip.</param>
    /// <param name="scanInterval">The scan interval.</param>
    /// <param name="timeOut">The time out.</param>
    /// <param name="path">The path.</param>
    public ABPlcRx(PlcType plcType, string ip, TimeSpan scanInterval, TimeSpan timeOut, string? path = "1,0")
    {
        _scanInterval = scanInterval;
        _plc = new(ip, plcType, path)
        {
            Timeout = (int)timeOut.TotalMilliseconds,
            AutoWriteValue = true,
        };

        // Reactive: surface tag added/removed as part of this instance lifetime
        var sub1 = _plc.TagsAdded.Subscribe(_ => { /* hook for external listeners if needed */ });
        _disposables.Add(sub1);

        var sub2 = _plc.TagsRemoved.Subscribe(_ => { /* hook for external listeners if needed */ });
        _disposables.Add(sub2);
    }

    /// <summary>Gets or sets a value indicating whether [automatic write value].</summary>
    /// <value>
    ///   <c>true</c> if [automatic write value]; otherwise, <c>false</c>.
    /// </value>
    public bool AutoWriteValue
    {
        get => _plc.AutoWriteValue;
        set => _plc.AutoWriteValue = value;
    }

    /// <summary>Gets a value indicating whether gets a value that indicates whether the object is disposed.</summary>
    public bool IsDisposed => _disposables.IsDisposed;

    /// <summary>Gets the data read.</summary>
    /// <value>The data read.</value>
    public IObservable<IPlcTag?> ObserveAll => MergeTagChanges(_plc.Tags).Select(c => c.Tag);

    /// <summary>Gets the data read as an async-native observable.</summary>
    /// <value>The async data read stream.</value>
    public IObservableAsync<IPlcTag?> ObserveAllAsyncObservable =>
        ObservableAsyncBridgeExtensions.ToAsyncObservable(ObserveAll);

    /// <summary>Gets or sets a value indicating whether [scan enabled].</summary>
    /// <value>
    ///   <c>true</c> if [scan enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool ScanEnabled
    {
        get;

        set
        {
            field = value;
            foreach (var list in _plc.TagCollectionList)
            {
                list.ScanEnabled = value;
            }
        }
    }

    /// <summary>Adds the update tag item.</summary>
    /// <typeparam name="T">The PLC type.</typeparam>
    /// <param name="tagName">Name of the tag.</param>
    /// <param name="typeWitness">Optional type witness for callers that infer <typeparamref name="T"/> from a value.</param>
    public void AddUpdateTagItem<T>(string tagName, T? typeWitness = default) =>
        AddUpdateTagItem<T>(tagName, tagName, "Default", typeWitness);

    /// <summary>Adds the update tag item.</summary>
    /// <typeparam name="T">The PLC type.</typeparam>
    /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
    /// <param name="tagName">Name of the tag.</param>
    /// <param name="typeWitness">Optional type witness for callers that infer <typeparamref name="T"/> from a value.</param>
    public void AddUpdateTagItem<T>(string variable, string tagName, T? typeWitness = default) =>
        AddUpdateTagItem<T>(variable, tagName, "Default", typeWitness);

    /// <summary>Adds the update tag item.</summary>
    /// <typeparam name="T">The tag type.</typeparam>
    /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
    /// <param name="tagName">Name of the tag.</param>
    /// <param name="tagGroup">The tag group.</param>
    /// <param name="typeWitness">Optional type witness for callers that infer <typeparamref name="T"/> from a value.</param>
    /// <exception cref="System.ArgumentNullException">tagName.</exception>
    public void AddUpdateTagItem<T>(string variable, string tagName, string tagGroup, T? typeWitness = default)
    {
        _ = typeWitness;
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(variable, nameof(variable));
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(tagName, nameof(tagName));
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(tagGroup, nameof(tagGroup));

        _plc.AddTagToGroup<T>(variable, tagName, _scanInterval, tagGroup);
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Observes the specified variable.</summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>
    /// An Observable of T.
    /// </returns>
    public IObservable<T?> Observe<T>(string? variable, int bit = -1) =>
        _plc.TagsAdded
            .Select(_ => RxVoid.Default)
            .StartWith(RxVoid.Default)
            .Select(_ => _plc.GetPlcTag(variable!))
            .Where(t => t is not null)
            .Select(t => t!.Changed.Select(_ => RxVoid.Default).StartWith(RxVoid.Default).Select(__ => GetTagValue<T>(bit, t)))
            .Switch()
            .DelaySubscription(_scanInterval)
            .DistinctUntilChanged()
            .OnErrorRetry()
            .Publish()
            .RefCount();

#if NET8_0_OR_GREATER
    /// <summary>Observes the specified variable as an async-native observable.</summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>
    /// An async observable of T.
    /// </returns>
    public IObservableAsync<T?> ObserveAsyncObservable<T>(string? variable, int bit = -1) =>
        ObservableAsyncBridgeExtensions.ToAsyncObservable(Observe<T>(variable, bit));
#endif

    /// <summary>Observe values for many variables and emit a latest-value dictionary.</summary>
    /// <param name="variables">One or more variable names to observe.</param>
    /// <returns>Observable sequence of dictionary containing the latest values for each variable.</returns>
    public IObservable<IReadOnlyDictionary<string, object?>> ObserveMany(params string[] variables)
    {
        return variables is null || variables.Length == 0
            ? SignalFactory.Return((IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>())
            : _plc.TagsAdded
            .Select(_ => RxVoid.Default)
            .StartWith(RxVoid.Default)
            .Select(_ => ObserveManySnapshot(variables))
            .Switch()
            .Publish()
            .RefCount();
    }

#if NET8_0_OR_GREATER
    /// <summary>Observe values for many variables and emit a latest-value dictionary as an async-native observable.</summary>
    /// <param name="variables">One or more variable names to observe.</param>
    /// <returns>Async observable sequence of dictionary containing the latest values for each variable.</returns>
    public IObservableAsync<IReadOnlyDictionary<string, object?>> ObserveManyAsyncObservable(params string[] variables) =>
        ObservableAsyncBridgeExtensions.ToAsyncObservable(ObserveMany(variables));
#endif

    /// <summary>Observe a PLC tag group, emitting the tag whose value changed.</summary>
    /// <param name="groupName">The group name to observe.</param>
    /// <returns>Observable sequence of tags in the group that have changed.</returns>
    public IObservable<IPlcTag> ObserveGroup(string groupName) =>
        SignalFactory.Lazy(() =>
        {
            var group = _plc.GetTagGroup(groupName);

            // existing tags
            var current = SignalFactory.Merge(group.Tags.Select(t => t.Changed.Select(_ => t)));

            // future tags that end up in the same group
            var future = _plc.TagsAdded
                            .Where(t => group.Tags.Contains(t))
                            .SelectMany(t => t.Changed.Select(_ => t));

            return current.Merge(future);
        })
        .Publish()
        .RefCount();

#if NET8_0_OR_GREATER
    /// <summary>Observe a PLC tag group as an async-native observable.</summary>
    /// <param name="groupName">The group name to observe.</param>
    /// <returns>Async observable sequence of tags in the group that have changed.</returns>
    public IObservableAsync<IPlcTag> ObserveGroupAsyncObservable(string groupName) =>
        ObservableAsyncBridgeExtensions.ToAsyncObservable(ObserveGroup(groupName));
#endif

    /// <summary>Creates an observer that writes values to a PLC variable when OnNext is called.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to write to.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <returns>An observer that will write and commit values to the PLC.</returns>
    public IObserver<T> CreateWriter<T>(string variable, int bit = -1) =>
        new ActionObserver<T>(v =>
        {
            Value(variable, v, bit);
            _ = Write(variable);
        });

    /// <summary>Observe a variable with sampling, reducing event rate while preserving latest value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to observe.</param>
    /// <param name="sampleInterval">The sampling interval.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <param name="scheduler">Optional scheduler for sampling.</param>
    /// <returns>Observable sequence of sampled values.</returns>
    public IObservable<T?> ObserveSampled<T>(string variable, TimeSpan sampleInterval, int bit = -1, ISequencer? scheduler = null)
        => Observe<T>(variable, bit).Sample(sampleInterval, scheduler ?? TaskPoolSequencer.Default).DistinctUntilChanged().Publish().RefCount();

#if NET8_0_OR_GREATER
    /// <summary>Observe a variable with sampling as an async-native observable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to observe.</param>
    /// <param name="sampleInterval">The sampling interval.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <param name="scheduler">Optional scheduler for sampling.</param>
    /// <returns>Async observable sequence of sampled values.</returns>
    public IObservableAsync<T?> ObserveSampledAsyncObservable<T>(string variable, TimeSpan sampleInterval, int bit = -1, ISequencer? scheduler = null)
        => ObservableAsyncBridgeExtensions.ToAsyncObservable(ObserveSampled<T>(variable, sampleInterval, bit, scheduler));
#endif

    /// <summary>Streams only error results across all tags.</summary>
    /// <returns>Observable sequence of error results.</returns>
    public IObservable<PlcTagResult> ObserveErrors()
        => MergeTagChanges(_plc.Tags).Where(r => PlcTagStatus.IsError(r.StatusCode)).Publish().RefCount();

#if NET8_0_OR_GREATER
    /// <summary>Streams only error results across all tags as an async-native observable.</summary>
    /// <returns>Async observable sequence of error results.</returns>
    public IObservableAsync<PlcTagResult> ObserveErrorsAsyncObservable() =>
        ObservableAsyncBridgeExtensions.ToAsyncObservable(ObserveErrors());
#endif

    /// <summary>Ping the PLC.</summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <returns>True when ping succeeds; otherwise false.</returns>
    public bool Ping(bool echo = false) => _plc.Ping(echo);

    /// <summary>Ping the PLC asynchronously.</summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="cancellationToken">A token to cancel the ping operation.</param>
    /// <returns>A task producing true when ping succeeds; otherwise false.</returns>
    public Task<bool> PingAsync(bool echo = false, CancellationToken cancellationToken = default) => _plc.PingAsync(echo, cancellationToken);

    /// <summary>Observe ping results on a schedule.</summary>
    /// <param name="interval">The interval between pings.</param>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="scheduler">Optional scheduler for the ping cadence.</param>
    /// <returns>Observable sequence of ping result states, deduplicated.</returns>
    public IObservable<bool> ObservePing(TimeSpan interval, bool echo = false, ISequencer? scheduler = null)
        => SignalFactory.Timer(TimeSpan.Zero, interval, scheduler ?? TaskPoolSequencer.Default)
                      .SelectMany(_ => SignalFactory.FromAsync(ct => _plc.PingAsync(echo, ct)))
                      .DistinctUntilChanged()
                      .Publish()
                      .RefCount();

#if NET8_0_OR_GREATER
    /// <summary>Observe ping results on a schedule as an async-native observable.</summary>
    /// <param name="interval">The interval between pings.</param>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="scheduler">Optional scheduler for the ping cadence.</param>
    /// <returns>Async observable sequence of ping result states, deduplicated.</returns>
    public IObservableAsync<bool> ObservePingAsyncObservable(TimeSpan interval, bool echo = false, ISequencer? scheduler = null) =>
        ObservableAsyncBridgeExtensions.ToAsyncObservable(ObservePing(interval, echo, scheduler));
#endif

    /// <summary>Reads the specified variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <returns>
    /// A PlcTagResult.
    /// </returns>
    public PlcTagResult? Read(string? variable) => _plc.GetPlcTag(variable!)?.Read();

    /// <summary>Reads all the Tags in this instance.</summary>
    /// <returns>A PlcTagResult.</returns>
    public IEnumerable<PlcTagResult> Read() => _plc.ReadAll();

    /// <summary>Values the specified variable.</summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <returns>
    /// A value of T.
    /// </returns>
    public T? Value<T>(string? variable, int bit = -1)
    {
        var tag = _plc.GetPlcTag(variable!);
        return GetTagValue<T>(bit, tag);
    }

    /// <summary>Values the specified variable.</summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    public void Value<T>(string? variable, T? value, int bit = -1)
    {
        var tag = _plc.GetPlcTag(variable!);
        if (tag is null)
        {
            return;
        }

        if (typeof(T).Equals(typeof(bool)))
        {
            tag.Value = tag.TypeValue == typeof(bool)
                ? value
                : SetTagBitValue(tag, bit, value);
        }
        else
        {
            tag!.Value = value;
        }
    }

    /// <summary>Writes the specified variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <returns>
    /// A PlcTagResult.
    /// </returns>
    public PlcTagResult? Write(string? variable) => _plc.GetPlcTag(variable!)?.Write();

    /// <summary>Writes all the tags in this instance.</summary>
    /// <returns>
    /// A PlcTagResult.
    /// </returns>
    public IEnumerable<PlcTagResult> Write() => _plc.WriteAll();

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposables.IsDisposed || !disposing)
        {
            return;
        }

        _plc.Dispose();
        _disposables.Dispose();
    }

    /// <summary>Reads the typed tag value, including optional bit access for integer-backed Boolean values.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="bit">The bit index for Boolean values backed by integral tags.</param>
    /// <param name="tag">The PLC tag to read.</param>
    /// <returns>The converted tag value.</returns>
    private static T? GetTagValue<T>(int bit, IPlcTag? tag)
    {
        if (tag is null)
        {
            return default;
        }

        if (!typeof(T).Equals(typeof(bool)))
        {
            return tag.Value is null ? default : (T?)tag.Value;
        }

        var boolVal = tag.TypeValue == typeof(bool)
            ? tag.Value
            : GetTagBitValue(tag, bit);

        return (T?)boolVal;
    }

    /// <summary>Reads a Boolean bit from an integral tag value.</summary>
    /// <param name="tag">The source tag.</param>
    /// <param name="bit">The bit index.</param>
    /// <returns>The bit value.</returns>
    private static bool GetTagBitValue(IPlcTag tag, int bit)
    {
        ValidateBitIndex(tag.TypeValue, bit);
        return (GetUnsignedIntegralValue(tag.Value, tag.TypeValue) & (1UL << bit)) != 0;
    }

    /// <summary>Writes a Boolean bit into an integral tag value.</summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <param name="tag">The target tag.</param>
    /// <param name="bit">The bit index.</param>
    /// <param name="value">The Boolean value.</param>
    /// <returns>The updated integral value converted back to the tag type.</returns>
    private static object SetTagBitValue<T>(IPlcTag tag, int bit, T? value)
    {
        ValidateBitIndex(tag.TypeValue, bit);
        var rawValue = GetUnsignedIntegralValue(tag.Value, tag.TypeValue);
        var mask = 1UL << bit;
        var updated = value is true ? rawValue | mask : rawValue & ~mask;
        return ConvertUnsignedIntegralValue(updated, tag.TypeValue);
    }

    /// <summary>Validates that a bit index is in range for an integral PLC tag type.</summary>
    /// <param name="tagType">The PLC tag type.</param>
    /// <param name="bit">The bit index.</param>
    private static void ValidateBitIndex(Type tagType, int bit)
    {
        var bitWidth = Type.GetTypeCode(tagType) switch
        {
            TypeCode.Byte or TypeCode.SByte => ByteBitWidth,
            TypeCode.UInt16 or TypeCode.Int16 => Int16BitWidth,
            TypeCode.UInt32 or TypeCode.Int32 => Int32BitWidth,
            TypeCode.UInt64 or TypeCode.Int64 => Int64BitWidth,
            _ => throw new ArgumentException("Bit operations require an integral PLC tag type.", nameof(tagType)),
        };

        if (bit >= 0 && bit < bitWidth)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(
            nameof(bit),
            $"Bit must be between 0 and {bitWidth - 1} for {tagType.Name} tags.");
    }

    /// <summary>Converts an integral tag value to an unsigned representation for bit operations.</summary>
    /// <param name="value">The source value.</param>
    /// <param name="tagType">The PLC tag type.</param>
    /// <returns>The unsigned integral value.</returns>
    private static ulong GetUnsignedIntegralValue(object? value, Type tagType) =>
        value is null
            ? 0
            : Type.GetTypeCode(tagType) switch
        {
            TypeCode.Byte => (byte)value,
            TypeCode.SByte => unchecked((byte)(sbyte)value),
            TypeCode.UInt16 => (ushort)value,
            TypeCode.Int16 => unchecked((ushort)(short)value),
            TypeCode.UInt32 => (uint)value,
            TypeCode.Int32 => unchecked((uint)(int)value),
            TypeCode.UInt64 => (ulong)value,
            TypeCode.Int64 => unchecked((ulong)(long)value),
            _ => throw new ArgumentException("Bit operations require an integral PLC tag type.", nameof(tagType)),
        };

    /// <summary>Converts an unsigned integral value back to the PLC tag type.</summary>
    /// <param name="value">The unsigned value.</param>
    /// <param name="tagType">The PLC tag type.</param>
    /// <returns>The converted value.</returns>
    private static object ConvertUnsignedIntegralValue(ulong value, Type tagType) => Type.GetTypeCode(tagType) switch
    {
        TypeCode.Byte => unchecked((byte)value),
        TypeCode.SByte => unchecked((sbyte)value),
        TypeCode.UInt16 => unchecked((ushort)value),
        TypeCode.Int16 => unchecked((short)value),
        TypeCode.UInt32 => unchecked((uint)value),
        TypeCode.Int32 => unchecked((int)value),
        TypeCode.UInt64 => value,
        TypeCode.Int64 => unchecked((long)value),
        _ => throw new ArgumentException("Bit operations require an integral PLC tag type.", nameof(tagType)),
    };

    /// <summary>Merges tag change streams for the supplied tag set.</summary>
    /// <param name="tags">The tags to observe.</param>
    /// <returns>A merged tag result observable.</returns>
    private static IObservable<PlcTagResult> MergeTagChanges(IEnumerable<IPlcTag> tags)
    {
        var streams = tags.Select(tag => tag.Changed).ToArray();
        return streams.Length == 0 ? SignalFactory.Silent<PlcTagResult>() : SignalFactory.Merge(streams);
    }

    /// <summary>Creates a latest-value snapshot for observed tags.</summary>
    /// <param name="tags">The tags to snapshot.</param>
    /// <returns>The latest values by variable name.</returns>
    private static Dictionary<string, object?> CreateSnapshot((string Variable, IPlcTag Tag)[] tags) =>
        tags.ToDictionary(static item => item.Variable, static item => item.Tag.Value);

    /// <summary>Observes the current snapshot for a set of variables.</summary>
    /// <param name="variables">The variables to observe.</param>
    /// <returns>An observable dictionary of current values.</returns>
    private IObservable<IReadOnlyDictionary<string, object?>> ObserveManySnapshot(string[] variables) =>
        SignalFactory.Create<IReadOnlyDictionary<string, object?>>(observer =>
        {
            var tags = variables
                .Select(variable => (Variable: variable, Tag: _plc.GetPlcTag(variable)))
                .Where(static item => item.Tag is not null)
                .Select(static item => (item.Variable, Tag: item.Tag!))
                .ToArray();

            if (tags.Length == 0)
            {
                observer.OnNext(new Dictionary<string, object?>());
                return EmptyDisposable.Instance;
            }

            var subscriptions = new MultipleDisposable();
            foreach (var item in tags)
            {
                subscriptions.Add(item.Tag.Changed.Subscribe(_ => observer.OnNext(CreateSnapshot(tags))));
            }

            return subscriptions;
        });

    /// <summary>Observer wrapper around an OnNext action.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="onNext">The action to run for each value.</param>
    private sealed class ActionObserver<T>(Action<T> onNext) : IObserver<T>
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

        /// <summary>Handles the next observed value.</summary>
        /// <param name="value">The observed value.</param>
        public void OnNext(T value) => onNext(value);
    }
}
