// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
#if NET8_0_OR_GREATER
using ReactiveUI.Extensions.Async;
#endif

namespace ABPlcRx;

/// <summary>
/// RxAB.
/// </summary>
public class ABPlcRx : IABPlcRx
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ABPlc _plc;
    private readonly TimeSpan _scanInterval;
    private bool _scanEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABPlcRx" /> class.
    /// </summary>
    /// <param name="plcType">Type of the PLC.</param>
    /// <param name="ip">The ip.</param>
    /// <param name="scanInterval">The scan interval.</param>
    public ABPlcRx(PlcType plcType, string ip, TimeSpan scanInterval)
        : this(plcType, ip, scanInterval, TimeSpan.FromSeconds(1), null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ABPlcRx" /> class.
    /// </summary>
    /// <param name="plcType">Type of the PLC.</param>
    /// <param name="ip">The ip.</param>
    /// <param name="scanInterval">The scan interval.</param>
    /// <param name="timeOut">The time out.</param>
    /// <param name="path">The path.</param>
    public ABPlcRx(PlcType plcType, string ip, TimeSpan scanInterval, TimeSpan timeOut, string? path = "1,0")
    {
        _scanInterval = scanInterval;
        _plc = new ABPlc(ip, plcType, path)
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

    /// <summary>
    /// Gets or sets a value indicating whether [automatic write value].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [automatic write value]; otherwise, <c>false</c>.
    /// </value>
    public bool AutoWriteValue
    {
        get => _plc.AutoWriteValue;
        set => _plc.AutoWriteValue = value;
    }

    /// <summary>
    /// Gets a value indicating whether gets a value that indicates whether the object is disposed.
    /// </summary>
    public bool IsDisposed => _disposables.IsDisposed;

    /// <summary>
    /// Gets the data read.
    /// </summary>
    /// <value>The data read.</value>
    public IObservable<IPlcTag?> ObserveAll => _plc.Tags.Select(x => x.Changed).Merge().Select(c => c.Tag);

#if NET8_0_OR_GREATER
    /// <summary>
    /// Gets the data read as an async-native observable.
    /// </summary>
    /// <value>The async data read stream.</value>
    public IObservableAsync<IPlcTag?> ObserveAllAsync => ObserveAll.ToObservableAsync();
#endif

    /// <summary>
    /// Gets or sets a value indicating whether [scan enabled].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [scan enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool ScanEnabled
    {
        get => _scanEnabled;
        set
        {
            _scanEnabled = value;
            foreach (var list in _plc.TagCollectionList)
            {
                list.ScanEnabled = value;
            }
        }
    }

    /// <summary>
    /// Adds the update tag item.
    /// </summary>
    /// <typeparam name="T">The PLC type.</typeparam>
    /// <param name="tagName">Name of the tag.</param>
    public void AddUpdateTagItem<T>(string tagName) =>
        AddUpdateTagItem<T>(tagName!, tagName, "Default");

    /// <summary>
    /// Adds the update tag item.
    /// </summary>
    /// <typeparam name="T">The PLC type.</typeparam>
    /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
    /// <param name="tagName">Name of the tag.</param>
    public void AddUpdateTagItem<T>(string variable, string tagName) =>
        AddUpdateTagItem<T>(variable, tagName, "Default");

    /// <summary>
    /// Adds the update tag item.
    /// </summary>
    /// <typeparam name="T">The tag type.</typeparam>
    /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
    /// <param name="tagName">Name of the tag.</param>
    /// <param name="tagGroup">The tag group.</param>
    /// <exception cref="System.ArgumentNullException">tagName.</exception>
    public void AddUpdateTagItem<T>(string variable, string tagName, string tagGroup)
    {
        if (string.IsNullOrWhiteSpace(variable))
        {
            throw new ArgumentNullException(nameof(variable));
        }

        if (string.IsNullOrWhiteSpace(tagName))
        {
            throw new ArgumentNullException(nameof(tagName));
        }

        if (string.IsNullOrWhiteSpace(tagGroup))
        {
            throw new ArgumentNullException(nameof(tagGroup));
        }

        _plc.AddTagToGroup<T>(variable, tagName!, _scanInterval, tagGroup);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Observes the specified variable.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>
    /// An Observable of T.
    /// </returns>
    public IObservable<T?> Observe<T>(string? variable, int bit = -1) =>
        _plc.TagsAdded
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .Select(_ => _plc.GetPlcTag(variable!))
            .Where(t => t != null)
            .Select(t => t!.Changed.Select(_ => Unit.Default).StartWith(Unit.Default).Select(__ => GetTagValue<T>(bit, t)))
            .Switch()
            .DelaySubscription(_scanInterval)
            .DistinctUntilChanged()
            .Retry()
            .Publish()
            .RefCount();

#if NET8_0_OR_GREATER
    /// <summary>
    /// Observes the specified variable as an async-native observable.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>
    /// An async observable of T.
    /// </returns>
    public IObservableAsync<T?> ObserveAsync<T>(string? variable, int bit = -1) =>
        Observe<T>(variable, bit).ToObservableAsync();
#endif

    /// <summary>
    /// Observe values for many variables and emit a latest-value dictionary.
    /// </summary>
    /// <param name="variables">One or more variable names to observe.</param>
    /// <returns>Observable sequence of dictionary containing the latest values for each variable.</returns>
    public IObservable<IReadOnlyDictionary<string, object?>> ObserveMany(params string[] variables)
    {
        if (variables == null || variables.Length == 0)
        {
            return Observable.Return((IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>());
        }

        return _plc.TagsAdded
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .Select(_ => variables.Select(v => new { Variable = v, Tag = _plc.GetPlcTag(v) }).Where(x => x.Tag != null).ToArray())
            .Select(tags =>
            {
                if (tags.Length == 0)
                {
                    return Observable.Return((IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>());
                }

                var streams = tags.Select(x => x.Tag!.Changed.Select(_ => new KeyValuePair<string, object?>(x.Variable, x.Tag!.Value)));
                return streams
                    .CombineLatest()
                    .Select(list => (IReadOnlyDictionary<string, object?>)list.ToDictionary(kv => kv.Key, kv => kv.Value));
            })
            .Switch()
            .Publish()
            .RefCount();
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Observe values for many variables and emit a latest-value dictionary as an async-native observable.
    /// </summary>
    /// <param name="variables">One or more variable names to observe.</param>
    /// <returns>Async observable sequence of dictionary containing the latest values for each variable.</returns>
    public IObservableAsync<IReadOnlyDictionary<string, object?>> ObserveManyAsync(params string[] variables) =>
        ObserveMany(variables).ToObservableAsync();
#endif

    /// <summary>
    /// Observe a PLC tag group, emitting the tag whose value changed.
    /// </summary>
    /// <param name="groupName">The group name to observe.</param>
    /// <returns>Observable sequence of tags in the group that have changed.</returns>
    public IObservable<IPlcTag> ObserveGroup(string groupName) =>
        Observable.Defer(() =>
        {
            var group = _plc.GetTagGroup(groupName);

            // existing tags
            var current = group.Tags.Select(t => t.Changed.Select(_ => t)).Merge();

            // future tags that end up in the same group
            var future = _plc.TagsAdded
                            .Where(t => group.Tags.Contains(t))
                            .SelectMany(t => t.Changed.Select(_ => t));

            return current.Merge(future);
        })
        .Publish()
        .RefCount();

#if NET8_0_OR_GREATER
    /// <summary>
    /// Observe a PLC tag group as an async-native observable.
    /// </summary>
    /// <param name="groupName">The group name to observe.</param>
    /// <returns>Async observable sequence of tags in the group that have changed.</returns>
    public IObservableAsync<IPlcTag> ObserveGroupAsync(string groupName) =>
        ObserveGroup(groupName).ToObservableAsync();
#endif

    /// <summary>
    /// Creates an observer that writes values to a PLC variable when OnNext is called.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to write to.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <returns>An observer that will write and commit values to the PLC.</returns>
    public IObserver<T> CreateWriter<T>(string variable, int bit = -1) =>
        Observer.Create<T>(v =>
        {
            Value(variable, v, bit);
            Write(variable);
        });

    /// <summary>
    /// Observe a variable with sampling, reducing event rate while preserving latest value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to observe.</param>
    /// <param name="sampleInterval">The sampling interval.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <param name="scheduler">Optional scheduler for sampling.</param>
    /// <returns>Observable sequence of sampled values.</returns>
    public IObservable<T?> ObserveSampled<T>(string variable, TimeSpan sampleInterval, int bit = -1, IScheduler? scheduler = null)
        => Observe<T>(variable, bit).Sample(sampleInterval, scheduler ?? TaskPoolScheduler.Default).DistinctUntilChanged().Publish().RefCount();

#if NET8_0_OR_GREATER
    /// <summary>
    /// Observe a variable with sampling as an async-native observable.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to observe.</param>
    /// <param name="sampleInterval">The sampling interval.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <param name="scheduler">Optional scheduler for sampling.</param>
    /// <returns>Async observable sequence of sampled values.</returns>
    public IObservableAsync<T?> ObserveSampledAsync<T>(string variable, TimeSpan sampleInterval, int bit = -1, IScheduler? scheduler = null)
        => ObserveSampled<T>(variable, sampleInterval, bit, scheduler).ToObservableAsync();
#endif

    /// <summary>
    /// Streams only error results across all tags.
    /// </summary>
    /// <returns>Observable sequence of error results.</returns>
    public IObservable<PlcTagResult> ObserveErrors()
        => _plc.Tags.Select(x => x.Changed).Merge().Where(r => PlcTagStatus.IsError(r.StatusCode)).Publish().RefCount();

#if NET8_0_OR_GREATER
    /// <summary>
    /// Streams only error results across all tags as an async-native observable.
    /// </summary>
    /// <returns>Async observable sequence of error results.</returns>
    public IObservableAsync<PlcTagResult> ObserveErrorsAsync() =>
        ObserveErrors().ToObservableAsync();
#endif

    /// <summary>
    /// Ping the PLC.
    /// </summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <returns>True when ping succeeds; otherwise false.</returns>
    public bool Ping(bool echo = false) => _plc.Ping(echo);

    /// <summary>
    /// Ping the PLC asynchronously.
    /// </summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="cancellationToken">A token to cancel the ping operation.</param>
    /// <returns>A task producing true when ping succeeds; otherwise false.</returns>
    public Task<bool> PingAsync(bool echo = false, CancellationToken cancellationToken = default) => _plc.PingAsync(echo, cancellationToken);

    /// <summary>
    /// Observe ping results on a schedule.
    /// </summary>
    /// <param name="interval">The interval between pings.</param>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="scheduler">Optional scheduler for the ping cadence.</param>
    /// <returns>Observable sequence of ping result states, deduplicated.</returns>
    public IObservable<bool> ObservePing(TimeSpan interval, bool echo = false, IScheduler? scheduler = null)
        => Observable.Timer(TimeSpan.Zero, interval, scheduler ?? TaskPoolScheduler.Default)
                      .SelectMany(_ => Observable.FromAsync(ct => _plc.PingAsync(echo, ct)))
                      .DistinctUntilChanged()
                      .Publish()
                      .RefCount();

#if NET8_0_OR_GREATER
    /// <summary>
    /// Observe ping results on a schedule as an async-native observable.
    /// </summary>
    /// <param name="interval">The interval between pings.</param>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="scheduler">Optional scheduler for the ping cadence.</param>
    /// <returns>Async observable sequence of ping result states, deduplicated.</returns>
    public IObservableAsync<bool> ObservePingAsync(TimeSpan interval, bool echo = false, IScheduler? scheduler = null) =>
        ObservePing(interval, echo, scheduler).ToObservableAsync();
#endif

    /// <summary>
    /// Reads the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>
    /// A PlcTagResult.
    /// </returns>
    public PlcTagResult? Read(string? variable) => _plc.GetPlcTag(variable!)?.Read();

    /// <summary>
    /// Reads all the Tags in this instance.
    /// </summary>
    /// <returns>A PlcTagResult.</returns>
    public IEnumerable<PlcTagResult> Read() => _plc.ReadAll();

    /// <summary>
    /// Values the specified variable.
    /// </summary>
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

    /// <summary>
    /// Values the specified variable.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    public void Value<T>(string? variable, T? value, int bit = -1)
    {
        var tag = _plc.GetPlcTag(variable!);
        if (tag == null)
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

    /// <summary>
    /// Writes the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>
    /// A PlcTagResult.
    /// </returns>
    public PlcTagResult? Write(string? variable) => _plc.GetPlcTag(variable!)?.Write();

    /// <summary>
    /// Writes all the tags in this instance.
    /// </summary>
    /// <returns>
    /// A PlcTagResult.
    /// </returns>
    public IEnumerable<PlcTagResult> Write() => _plc.WriteAll();

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposables.IsDisposed && disposing)
        {
            _plc.Dispose();
            _disposables.Dispose();
        }
    }

    private static T? GetTagValue<T>(int bit, IPlcTag? tag)
    {
        if (typeof(T).Equals(typeof(bool)))
        {
            if (tag == null)
            {
                return default;
            }

            var boolVal = tag.TypeValue == typeof(bool)
                ? tag.Value
                : GetTagBitValue(tag, bit);

            return (T?)boolVal;
        }

        return (T?)tag?.Value;
    }

    private static bool GetTagBitValue(IPlcTag tag, int bit)
    {
        ValidateBitIndex(tag.TypeValue, bit);
        return (GetUnsignedIntegralValue(tag.Value, tag.TypeValue) & (1UL << bit)) != 0;
    }

    private static object SetTagBitValue<T>(IPlcTag tag, int bit, T? value)
    {
        ValidateBitIndex(tag.TypeValue, bit);
        var rawValue = GetUnsignedIntegralValue(tag.Value, tag.TypeValue);
        var mask = 1UL << bit;
        var updated = value is true ? rawValue | mask : rawValue & ~mask;
        return ConvertUnsignedIntegralValue(updated, tag.TypeValue);
    }

    private static void ValidateBitIndex(Type tagType, int bit)
    {
        var bitWidth = Type.GetTypeCode(tagType) switch
        {
            TypeCode.Byte or TypeCode.SByte => 8,
            TypeCode.UInt16 or TypeCode.Int16 => 16,
            TypeCode.UInt32 or TypeCode.Int32 => 32,
            TypeCode.UInt64 or TypeCode.Int64 => 64,
            _ => throw new ArgumentException("Bit operations require an integral PLC tag type.", nameof(tagType)),
        };

        if (bit < 0 || bit >= bitWidth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bit),
                $"Bit must be between 0 and {bitWidth - 1} for {tagType.Name} tags.");
        }
    }

    private static ulong GetUnsignedIntegralValue(object? value, Type tagType)
    {
        if (value is null)
        {
            return 0;
        }

        return Type.GetTypeCode(tagType) switch
        {
            TypeCode.Byte => (byte)value,
            TypeCode.SByte => unchecked((ulong)(sbyte)value),
            TypeCode.UInt16 => (ushort)value,
            TypeCode.Int16 => unchecked((ulong)(short)value),
            TypeCode.UInt32 => (uint)value,
            TypeCode.Int32 => unchecked((ulong)(int)value),
            TypeCode.UInt64 => (ulong)value,
            TypeCode.Int64 => unchecked((ulong)(long)value),
            _ => throw new ArgumentException("Bit operations require an integral PLC tag type.", nameof(tagType)),
        };
    }

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
}
