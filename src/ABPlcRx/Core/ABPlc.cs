// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.NetworkInformation;

namespace ABPlcRx
{
    /// <summary>
    /// Allen Bradley Plc.
    /// </summary>
    internal class ABPlc : IDisposable
    {
        private readonly Dictionary<string, PlcTagCollection> _tagList = new();
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
        public IReadOnlyList<PlcTagCollection> TagCollectionList => _tagList.Values.ToList().AsReadOnly();

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
        public IReadOnlyList<IPlcTag> Tags => TagCollectionList.SelectMany(a => a.Tags).Distinct().ToList().AsReadOnly();

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
            _tagList.Add(name, tags);
            return tags;
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
            using (var ping = new Ping())
            {
                var reply = ping.Send(IPAddress);
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
        /// Gets the PLC tag.
        /// </summary>
        /// <param name="variable">The name.</param>
        /// <returns>A Tag.</returns>
        public IPlcTag? GetPlcTag(string variable) => Tags.FirstOrDefault(a => a.Variable == variable)!;

        /// <summary>
        /// Determines whether [has tag group] [the specified tag group].
        /// </summary>
        /// <param name="tagGroup">The tag group.</param>
        /// <returns>
        ///   <c>true</c> if [has tag group] [the specified tag group]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasTagGroup(string tagGroup) => _tagList.ContainsKey(tagGroup);

        /// <summary>
        /// Gets the tag group.
        /// </summary>
        /// <param name="tagGroup">The tag group.</param>
        /// <returns>A Plc Tag Collection.</returns>
        public PlcTagCollection GetTagGroup(string tagGroup) => _tagList[tagGroup];

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
            if (!HasTagGroup(tagGroup))
            {
                CreateTagList(tagGroup, scanInterval);
            }

            _tagList[tagGroup].CreateTagType<T>(variable, tagName);
        }

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
                    foreach (var group in _tagList)
                    {
                        group.Value.Dispose();
                    }

                    _tagList.Clear();
                }

                _disposed = true;
            }
        }
    }
}
