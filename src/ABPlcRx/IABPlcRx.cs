// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace ABPlcRx;

/// <summary>
/// IRxAB.
/// </summary>
/// <seealso cref="ICancelable" />
public interface IABPlcRx : ICancelable
{
    /// <summary>
    /// Gets the observe all.
    /// </summary>
    /// <value>
    /// The observe all.
    /// </value>
    IObservable<IPlcTag?> ObserveAll { get; }

    /// <summary>
    /// Gets or sets a value indicating whether [scan enabled].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [scan enabled]; otherwise, <c>false</c>.
    /// </value>
    bool ScanEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [automatic write value].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [automatic write value]; otherwise, <c>false</c>.
    /// </value>
    bool AutoWriteValue { get; set; }

    /// <summary>
    /// Adds the update tag item.
    /// </summary>
    /// <typeparam name="T">The tag type.</typeparam>
    /// <param name="tagName">Name of the PLC tag.</param>
    /// <exception cref="System.ArgumentNullException">tagName.</exception>
    /// <exception cref="System.Exception">Please use type of short, then use bool for other operations and set the bit number.</exception>
    void AddUpdateTagItem<T>(string tagName);

    /// <summary>
    /// Adds the update tag item.
    /// </summary>
    /// <typeparam name="T">The tag type.</typeparam>
    /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
    /// <param name="tagName">Name of the plc tag.</param>
    /// <exception cref="System.ArgumentNullException">tagName.</exception>
    /// <exception cref="System.Exception">Please use type of short, then use bool for other operations and set the bit number.</exception>
    void AddUpdateTagItem<T>(string variable, string tagName);

    /// <summary>
    /// Adds the update tag item.
    /// </summary>
    /// <typeparam name="T">The tag type.</typeparam>
    /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
    /// <param name="tagName">Name of the plc tag.</param>
    /// <param name="tagGroup">The tag group.</param>
    /// <exception cref="System.ArgumentNullException">tagName.</exception>
    /// <exception cref="System.Exception">Please use type of short, then use bool for other operations and set the bit number.</exception>
    void AddUpdateTagItem<T>(string variable, string tagName, string tagGroup);

    /// <summary>
    /// Observes the specified variable.
    /// </summary>
    /// <typeparam name="T">The PLC type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>
    /// An observable sequence of values of type T.
    /// </returns>
    IObservable<T?> Observe<T>(string? variable, int bit = -1);

    /// <summary>
    /// Observe values for many variables and emit a latest-value dictionary.
    /// </summary>
    /// <param name="variables">One or more variable names to observe.</param>
    /// <returns>Observable sequence of dictionary containing the latest values for each variable.</returns>
    IObservable<IReadOnlyDictionary<string, object?>> ObserveMany(params string[] variables);

    /// <summary>
    /// Observe a PLC tag group, emitting the tag whose value changed.
    /// </summary>
    /// <param name="groupName">The group name to observe.</param>
    /// <returns>Observable sequence of tags in the group that have changed.</returns>
    IObservable<IPlcTag> ObserveGroup(string groupName);

    /// <summary>
    /// Creates an observer that writes values to a PLC variable when OnNext is called.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to write to.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <returns>An observer that will write and commit values to the PLC.</returns>
    IObserver<T> CreateWriter<T>(string variable, int bit = -1);

    /// <summary>
    /// Observe a variable with sampling, reducing event rate while preserving latest value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="variable">The variable to observe.</param>
    /// <param name="sampleInterval">The sampling interval.</param>
    /// <param name="bit">The bit [ONLY use for bool tags].</param>
    /// <param name="scheduler">Optional scheduler for sampling.</param>
    /// <returns>Observable sequence of sampled values.</returns>
    IObservable<T?> ObserveSampled<T>(string variable, TimeSpan sampleInterval, int bit = -1, IScheduler? scheduler = null);

    /// <summary>
    /// Streams only error results across all tags.
    /// </summary>
    /// <returns>Observable sequence of error results.</returns>
    IObservable<PlcTagResult> ObserveErrors();

    /// <summary>
    /// Values the specified variable.
    /// </summary>
    /// <typeparam name="T">The PLC type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="bit">The bit.</param>
    /// <returns>
    /// A value of T.
    /// </returns>
    T? Value<T>(string? variable, int bit = -1);

    /// <summary>
    /// Values the specified variable.
    /// </summary>
    /// <typeparam name="T">The PLC type.</typeparam>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="bit">The bit.</param>
    void Value<T>(string? variable, T? value, int bit = -1);

    /// <summary>
    /// Writes all tags in this instance.
    /// </summary>
    /// <returns>A sequence of PlcTagResult.</returns>
    IEnumerable<PlcTagResult> Write();

    /// <summary>
    /// Writes the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>A PlcTagResult.</returns>
    PlcTagResult? Write(string? variable);

    /// <summary>
    /// Reads all tags in this instance.
    /// </summary>
    /// <returns>A sequence of PlcTagResult.</returns>
    IEnumerable<PlcTagResult> Read();

    /// <summary>
    /// Reads the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>A PlcTagResult.</returns>
    PlcTagResult? Read(string? variable);

    /// <summary>
    /// Ping the PLC.
    /// </summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <returns>True when ping succeeds; otherwise, false.</returns>
    bool Ping(bool echo = false);

    /// <summary>
    /// Ping the PLC asynchronously.
    /// </summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="cancellationToken">A token to cancel the ping operation.</param>
    /// <returns>A task producing true when ping succeeds; otherwise, false.</returns>
    Task<bool> PingAsync(bool echo = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Observe ping results on a schedule.
    /// </summary>
    /// <param name="interval">The interval between pings.</param>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="scheduler">Optional scheduler for the ping cadence.</param>
    /// <returns>Observable sequence of ping result states, deduplicated.</returns>
    IObservable<bool> ObservePing(TimeSpan interval, bool echo = false, IScheduler? scheduler = null);
}
