// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>Allen Bradley Plc.</summary>
internal sealed class ABPlc : IDisposable
{
    /// <summary>Tag groups keyed by group name.</summary>
    private readonly Dictionary<string, PlcTagCollection> _tagList = [];

    /// <summary>Tags keyed by caller variable name.</summary>
    private readonly Dictionary<string, IPlcTag> _tagsByVariable = new(StringComparer.Ordinal);

    /// <summary>Synchronizes access to tag state.</summary>
    private readonly object _syncRoot = new();

    /// <summary>Publishes tags added to this controller.</summary>
    private readonly Signal<IPlcTag> _tagsAdded = new();

    /// <summary>Publishes tags removed from this controller.</summary>
    private readonly Signal<IPlcTag> _tagsRemoved = new();

    /// <summary>Cached tag group snapshot.</summary>
    private ReadOnlyCollection<PlcTagCollection>? _cachedTagCollections;

    /// <summary>Reusable ping client.</summary>
    private Ping? _ping;

    /// <summary>Tracks disposal state.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="ABPlc"/> class.</summary>
    /// <param name="address">The IP address of the PLC.</param>
    /// <param name="plcType">Type of the PLC.</param>
    public ABPlc(string address, PlcType plcType)
        : this(address, plcType, null, LibPlcTagNative.Instance)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ABPlc" /> class.</summary>
    /// <param name="address">The IP address of the PLC.</param>
    /// <param name="plcType">Type of the PLC.</param>
    /// <param name="slot">Required for LGX, Optional for PLC/SLC/MLGX IOI path to access the PLC from the gateway.
    /// <para></para>Communication Port Type: 1- Backplane, 2- Control Net/Ethernet, DH+ Channel A, DH+ Channel B, 3- Serial.
    /// <para></para>Slot number where cpu is installed: 0,1..</param>
    /// <exception cref="System.ArgumentException">PortType and Slot must be specified for ControlLogix / CompactLogix processors.</exception>
    public ABPlc(string address, PlcType plcType, string? slot)
        : this(address, plcType, slot, LibPlcTagNative.Instance)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ABPlc" /> class.</summary>
    /// <param name="address">The IP address of the PLC.</param>
    /// <param name="plcType">Type of the PLC.</param>
    /// <param name="slot">The PLC slot path.</param>
    /// <param name="native">The native tag adapter.</param>
    internal ABPlc(string address, PlcType plcType, string? slot, IPlcTagNative native)
    {
        if (plcType == PlcType.LGX && string.IsNullOrEmpty(slot))
        {
            throw new ArgumentException("plcType and slot must be specified for ControlLogix / CompactLogix processors");
        }

        ArgumentExceptionHelper.ThrowIfNull(native, nameof(native));

        IPAddress = address;
        Slot = slot;
        PlcType = plcType;
        Native = native;
    }

    /// <summary>Finalizes an instance of the <see cref="ABPlc"/> class.</summary>
    ~ABPlc()
    {
        Dispose(false);
    }

    /// <summary>Gets or sets a value indicating whether automatic Write when using value.</summary>
    public bool AutoWriteValue { get; set; }

    /// <summary>Gets aB CPU models.</summary>
    public PlcType PlcType { get; }

    /// <summary>
    /// Gets or sets optional allows the selection of varying levels of debugging output.
    /// 1 shows only the more urgent problems.
    /// 5 shows almost every action within the library and will generate a very large amount of output.
    /// Generally 3 or 4 is most useful when debugging.
    /// </summary>
    public int DebugLevel { get; set; }

    /// <summary>Gets or sets a value indicating whether raise Exception on failed operation.</summary>
    public bool FailOperationRaiseException { get; set; }

    /// <summary>Gets the Tag List.</summary>
    /// <returns>A Value.</returns>
    public IReadOnlyList<PlcTagCollection> TagCollectionList
    {
        get
        {
            lock (_syncRoot)
            {
                return _cachedTagCollections ??= _tagList.Values.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>Gets observable of tags added to this controller.</summary>
    public IObservable<IPlcTag> TagsAdded => _tagsAdded;

    /// <summary>Gets observable of tags removed from this controller.</summary>
    public IObservable<IPlcTag> TagsRemoved => _tagsRemoved;

    /// <summary>Gets iP address of the gateway for this protocol. Could be the IP address of the PLC you want to access.</summary>
    public string IPAddress { get; }

    /// <summary>Gets required for LGX, Optional for PLC/SLC/MLGX IOI path to access the PLC from the gateway.</summary>
    public string? Slot { get; }

    /// <summary>Gets all Tags.</summary>
    /// <returns>A Value.</returns>
    public IReadOnlyList<IPlcTag> Tags
    {
        get
        {
            // Snapshot to avoid holding lock during potential long operations downstream
            List<PlcTagCollection> groups;
            lock (_syncRoot)
            {
                groups = [.. _tagList.Values];
            }

            return groups.SelectMany(a => a.Tags).ToList().AsReadOnly();
        }
    }

    /// <summary>Gets or sets communication timeout millisec.</summary>
    public int Timeout { get; set; } = 5000;

    /// <summary>Gets the native tag adapter.</summary>
    internal IPlcTagNative Native { get; }

    /// <summary>Creates new TagList.</summary>
    /// <param name="name">The name.</param>
    /// <param name="scanInterval">The scan interval.</param>
    /// <returns>
    /// A Value.
    /// </returns>
    public PlcTagCollection CreateTagList(string name, TimeSpan scanInterval)
    {
        var tags = new PlcTagCollection(this, scanInterval);
        lock (_syncRoot)
        {
            _tagList.Add(name, tags);
            _cachedTagCollections = null; // invalidate cache
        }

        return tags;
    }

    /// <summary>Removes a tag group, disposing its resources and cleaning lookups.</summary>
    /// <param name="tagGroup">The tag group.</param>
    /// <returns>True if removed.</returns>
    public bool RemoveTagGroup(string tagGroup)
    {
        PlcTagCollection? group;
        lock (_syncRoot)
        {
            if (!_tagList.TryGetValue(tagGroup, out group))
            {
                return false;
            }

            foreach (var tag in group.Tags.ToArray())
            {
                _ = _tagsByVariable.Remove(tag.Variable);
                _tagsRemoved.OnNext(tag);
            }

            _ = _tagList.Remove(tagGroup);
            _cachedTagCollections = null;
        }

        // Dispose outside lock
        group.Dispose();
        return true;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Ping controller.</summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <returns>A Value.</returns>
    public bool Ping(bool echo = false)
    {
        lock (_syncRoot)
        {
            _ping ??= new Ping();
            var reply = _ping.Send(IPAddress);
            if (echo)
            {
                Console.Out.WriteLine($"Address: {reply.Address}");
                Console.Out.WriteLine($"RoundTrip time: {reply.RoundtripTime}");
                Console.Out.WriteLine($"Time to live: {reply.Options?.Ttl}");
                Console.Out.WriteLine($"Don't fragment: {reply.Options?.DontFragment}");
                Console.Out.WriteLine($"Buffer size: {reply.Buffer?.Length}");
                Console.Out.WriteLine($"Status: {reply.Status}");
            }

            return reply.Status == IPStatus.Success;
        }
    }

    /// <summary>Ping controller asynchronously.</summary>
    /// <param name="echo">True echo result to standard output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Value.</returns>
    public async Task<bool> PingAsync(bool echo = false, CancellationToken cancellationToken = default)
    {
        Ping? ping;
        lock (_syncRoot)
        {
            _ping ??= new Ping();
            ping = _ping;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var reply = await ping.SendPingAsync(IPAddress).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (echo)
        {
            await WriteLineAsync($"Address: {reply.Address}", cancellationToken).ConfigureAwait(false);
            await WriteLineAsync($"RoundTrip time: {reply.RoundtripTime}", cancellationToken).ConfigureAwait(false);
            await WriteLineAsync($"Time to live: {reply.Options?.Ttl}", cancellationToken).ConfigureAwait(false);
            await WriteLineAsync($"Don't fragment: {reply.Options?.DontFragment}", cancellationToken).ConfigureAwait(false);
            await WriteLineAsync($"Buffer size: {reply.Buffer?.Length}", cancellationToken).ConfigureAwait(false);
            await WriteLineAsync($"Status: {reply.Status}", cancellationToken).ConfigureAwait(false);
        }

        return reply.Status == IPStatus.Success;
    }

    /// <summary>Gets the PLC tag.</summary>
    /// <param name="variable">The name.</param>
    /// <returns>A Tag.</returns>
    public IPlcTag? GetPlcTag(string variable)
    {
        lock (_syncRoot)
        {
            if (_tagsByVariable.TryGetValue(variable, out var tag))
            {
                return tag;
            }
        }

        // Fallback lookup if not yet in the cache (should be rare)
        return Tags.FirstOrDefault(a => a.Variable == variable);
    }

    /// <summary>Tries to get the PLC tag by variable key.</summary>
    /// <param name="variable">The variable key.</param>
    /// <param name="tag">The resolved tag.</param>
    /// <returns>True when the tag is found; otherwise, false.</returns>
    public bool TryGetPlcTag(string variable, out IPlcTag? tag)
    {
        lock (_syncRoot)
        {
            return _tagsByVariable.TryGetValue(variable, out tag);
        }
    }

    /// <summary>Determines whether [has tag group] [the specified tag group].</summary>
    /// <param name="tagGroup">The tag group.</param>
    /// <returns>
    ///   <c>true</c> if [has tag group] [the specified tag group]; otherwise, <c>false</c>.
    /// </returns>
    public bool HasTagGroup(string tagGroup)
    {
        lock (_syncRoot)
        {
            return _tagList.ContainsKey(tagGroup);
        }
    }

    /// <summary>Gets the tag group.</summary>
    /// <param name="tagGroup">The tag group.</param>
    /// <returns>A Plc Tag Collection.</returns>
    public PlcTagCollection GetTagGroup(string tagGroup)
    {
        lock (_syncRoot)
        {
            return _tagList[tagGroup];
        }
    }

    /// <summary>Adds the tag to group.</summary>
    /// <typeparam name="T">The tag type.</typeparam>
    /// <param name="variable">The key.</param>
    /// <param name="tagName">The name.</param>
    /// <param name="scanInterval">The scan interval.</param>
    /// <param name="tagGroup">The tag group.</param>
    public void AddTagToGroup<T>(string variable, string tagName, TimeSpan scanInterval, string tagGroup = "Default")
    {
        IPlcTag tag;
        lock (_syncRoot)
        {
            var group = _tagList.TryGetValue(tagGroup, out var existingGroup)
                ? existingGroup
                : CreateTagList(tagGroup, scanInterval);

            tag = group.CreateTagType<T>(variable, tagName);
            _tagsByVariable[variable] = tag; // fast future lookup
        }

        _tagsAdded.OnNext(tag);
    }

    /// <summary>Bulk read across all groups.</summary>
    /// <returns>The read results.</returns>
    public IReadOnlyList<PlcTagResult> ReadAll()
    {
        List<PlcTagCollection> groups;
        lock (_syncRoot)
        {
            groups = [.. _tagList.Values];
        }

        var results = new List<PlcTagResult>(groups.Sum(g => g.Tags.Count));
        foreach (var g in groups)
        {
            results.AddRange(g.Read());
        }

        return results;
    }

    /// <summary>Bulk write across all groups.</summary>
    /// <returns>The write results.</returns>
    public IReadOnlyList<PlcTagResult> WriteAll()
    {
        List<PlcTagCollection> groups;
        lock (_syncRoot)
        {
            groups = [.. _tagList.Values];
        }

        var results = new List<PlcTagResult>(groups.Sum(g => g.Tags.Count));
        foreach (var g in groups)
        {
            results.AddRange(g.Write());
        }

        return results;
    }

    /// <summary>Async bulk read across all groups.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task producing the read results.</returns>
    public Task<IReadOnlyList<PlcTagResult>> ReadAllAsync(CancellationToken cancellationToken = default) => Task.Run(ReadAll, cancellationToken);

    /// <summary>Async bulk write across all groups.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task producing the write results.</returns>
    public Task<IReadOnlyList<PlcTagResult>> WriteAllAsync(CancellationToken cancellationToken = default) => Task.Run(WriteAll, cancellationToken);

    /// <summary>Writes a line to standard output with target-specific cancellation support.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The write task.</returns>
    private static Task WriteLineAsync(string value, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        return Console.Out.WriteLineAsync(value.AsMemory(), cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return Console.Out.WriteLineAsync(value);
#endif
    }

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var group in _tagList.Values.ToArray())
            {
                group.Dispose();
            }

            _tagList.Clear();
            _tagsByVariable.Clear();
            _cachedTagCollections = null;
            _ping?.Dispose();

            _tagsAdded.OnCompleted();
            _tagsRemoved.OnCompleted();
            _tagsAdded.Dispose();
            _tagsRemoved.Dispose();
        }

        _disposed = true;
    }
}
