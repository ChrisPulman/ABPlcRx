// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ABPlcRx;

/// <summary>
/// RxAB.
/// </summary>
public class ABPlcRx : IABPlcRx
{
    private readonly CompositeDisposable _disposables = new();
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
    /// <exception cref="System.Exception">Please use type of short, then use bool for other operations and set the bit number.</exception>
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

        if (typeof(T).Equals(typeof(bool)))
        {
            throw new Exception("Please use type of short, then use bool for other operations and set the bit number.");
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
        ObserveAll.Where(t => t?.Variable == variable)
                  .DelaySubscription(_scanInterval)
                  .Select(t => GetTagValue<T>(bit, t))
                  .StartWith(Value<T>(variable, bit))
                  .DistinctUntilChanged()
                  .Retry().Publish().RefCount();

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
    public IEnumerable<PlcTagResult> Read()
    {
        var results = new List<PlcTagResult>();
        foreach (var tag in _plc.TagCollectionList)
        {
            results.AddRange(tag.Read());
        }

        return results;
    }

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
            if (bit == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(bit), "You must set the bit value for bool types.");
            }

            object objValue = value!;
            tag!.Value = ((short)tag!.Value!).SetBit(bit, (bool)objValue!);
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
    public IEnumerable<PlcTagResult> Write()
    {
        var results = new List<PlcTagResult>();
        foreach (var tag in _plc.TagCollectionList)
        {
            results.AddRange(tag.Write());
        }

        return results;
    }

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
            if (bit == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(bit), "You must set the bit value for bool types.");
            }

            if (tag == null)
            {
                return default;
            }

            object? boolVal = ((short)tag?.Value!).GetBit(bit);
            return (T?)boolVal;
        }

        return (T?)tag?.Value;
    }
}
