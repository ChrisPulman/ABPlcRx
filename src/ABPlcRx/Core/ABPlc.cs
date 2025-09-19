// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ABPlcRx;

/// <summary>
/// Allen Bradley Plc.
/// </summary>
internal class ABPlc : IDisposable
{
    private readonly Dictionary<string, PlcTagCollection> _tagList = [];
    private readonly Dictionary<string, IPlcTag> _tagsByVariable = new(StringComparer.Ordinal);
    private readonly object _syncRoot = new();
    private readonly Subject<IPlcTag> _tagsAdded = new();
    private readonly Subject<IPlcTag> _tagsRemoved = new();

    private ReadOnlyCollection<PlcTagCollection>? _cachedTagCollections;
    private Ping? _ping;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABPlc"/> class.
    /// </summary>
    /// <param name="ipAddress">The IP address of the PLC.</param>
    /// <param name="plcType">Type of the PLC.</param>
    public ABPlc(string ipAddress, PlcType plcType)
        : this(ipAddress, plcType, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ABPlc" /> class.
    /// </summary>
    /// <param name="ipAddress">The IP address of the PLC.</param>
    /// <param name="plcType">Type of the PLC.</param>
    /// <param name="slot">Required for LGX, Optional for PLC/SLC/MLGX IOI path to access the PLC from the gateway.
    /// <para></para>Communication Port Type: 1- Backplane, 2- Control Net/Ethernet, DH+ Channel A, DH+ Channel B, 3- Serial.
    /// <para></para>Slot number where cpu is installed: 0,1..</param>
    /// <exception cref="System.ArgumentException">PortType and Slot must be specified for ControlLogix / CompactLogix processors.</exception>
    public ABPlc(string ipAddress, PlcType plcType, string? slot)
    {
        if (plcType == PlcType.LGX && string.IsNullOrEmpty(slot))
        {
            throw new ArgumentException("plcType and slot must be specified for ControlLogix / CompactLogix processors");
        }

        IPAddress = ipAddress;
        Slot = slot;
        PlcType = plcType;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ABPlc"/> class.
    /// </summary>
    ~ABPlc()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets or sets a value indicating whether automatic Write when using value.
    /// </summary>
    public bool AutoWriteValue { get; set; }

    /// <summary>
    /// Gets aB CPU models.
    /// </summary>
    public PlcType PlcType { get; }

    /// <summary>
    /// Gets or sets optional allows the selection of varying levels of debugging output.
    /// 1 shows only the more urgent problems.
    /// 5 shows almost every action within the library and will generate a very large amount of output.
    /// Generally 3 or 4 is most useful when debugging.
    /// </summary>
    public int DebugLevel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether raise Exception on failed operation.
    /// </summary>
    public bool FailOperationRaiseException { get; set; }

    /// <summary>
    /// Gets the Tag List.
    /// </summary>
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

    /// <summary>
    /// Gets observable of tags added to this controller.
    /// </summary>
    public IObservable<IPlcTag> TagsAdded => _tagsAdded.AsObservable();

    /// <summary>
    /// Gets observable of tags removed from this controller.
    /// </summary>
    public IObservable<IPlcTag> TagsRemoved => _tagsRemoved.AsObservable();

    /// <summary>
    /// Gets iP address of the gateway for this protocol. Could be the IP address of the PLC you want to access.
    /// </summary>
    public string IPAddress { get; }

    /// <summary>
    /// Gets required for LGX, Optional for PLC/SLC/MLGX IOI path to access the PLC from the gateway.
    /// </summary>
    public string? Slot { get; }

    /// <summary>
    /// Gets all Tags.
    /// </summary>
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

    /// <summary>
    /// Gets or sets communication timeout millisec.
    /// </summary>
    public int Timeout { get; set; } = 5000;

    /// <summary>
    /// Creates new TagList.
    /// </summary>
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

    /// <summary>
    /// Removes a tag group, disposing its resources and cleaning lookups.
    /// </summary>
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
                _tagsByVariable.Remove(tag.Variable);
                _tagsRemoved.OnNext(tag);
            }

            _tagList.Remove(tagGroup);
            _cachedTagCollections = null;
        }

        // Dispose outside lock
        group.Dispose();
        return true;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Ping controller.
    /// </summary>
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

    /// <summary>
    /// Ping controller asynchronously.
    /// </summary>
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
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

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

    /// <summary>
    /// Gets the PLC tag.
    /// </summary>
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

    /// <summary>
    /// Tries to get the PLC tag by variable key.
    /// </summary>
    public bool TryGetPlcTag(string variable, out IPlcTag tag)
    {
        lock (_syncRoot)
        {
            return _tagsByVariable.TryGetValue(variable, out tag!);
        }
    }

    /// <summary>
    /// Determines whether [has tag group] [the specified tag group].
    /// </summary>
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

    /// <summary>
    /// Gets the tag group.
    /// </summary>
    /// <param name="tagGroup">The tag group.</param>
    /// <returns>A Plc Tag Collection.</returns>
    public PlcTagCollection GetTagGroup(string tagGroup)
    {
        lock (_syncRoot)
        {
            return _tagList[tagGroup];
        }
    }

    /// <summary>
    /// Adds the tag to group.
    /// </summary>
    /// <typeparam name="T">The tag type.</typeparam>
    /// <param name="variable">The key.</param>
    /// <param name="tagName">The name.</param>
    /// <param name="scanInterval">The scan interval.</param>
    /// <param name="tagGroup">The tag group.</param>
    public void AddTagToGroup<T>(string variable, string tagName, TimeSpan scanInterval, string tagGroup = "Default")
    {
        PlcTagCollection group;
        IPlcTag tag;
        lock (_syncRoot)
        {
            if (!_tagList.TryGetValue(tagGroup, out group!))
            {
                group = CreateTagList(tagGroup, scanInterval);
            }

            tag = group.CreateTagType<T>(variable, tagName);
            _tagsByVariable[variable] = tag; // fast future lookup
        }

        _tagsAdded.OnNext(tag);
    }

    /// <summary>
    /// Bulk read across all groups.
    /// </summary>
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

    /// <summary>
    /// Bulk write across all groups.
    /// </summary>
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

    /// <summary>
    /// Async bulk read across all groups.
    /// </summary>
    public Task<IReadOnlyList<PlcTagResult>> ReadAllAsync(CancellationToken cancellationToken = default) => Task.Run(ReadAll, cancellationToken);

    /// <summary>
    /// Async bulk write across all groups.
    /// </summary>
    public Task<IReadOnlyList<PlcTagResult>> WriteAllAsync(CancellationToken cancellationToken = default) => Task.Run(WriteAll, cancellationToken);

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
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
}
