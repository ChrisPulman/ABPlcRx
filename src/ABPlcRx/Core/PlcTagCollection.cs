// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ABPlcRx
{
    /// <summary>
    /// Plc Tag Collection.
    /// </summary>
    internal class PlcTagCollection : IDisposable
    {
        private readonly object _lockScan = new();
        private readonly Subject<IEnumerable<PlcTagResult>> _readResultSubject = new();
        private readonly IDisposable? _scanDisposable;
        private readonly List<IPlcTag> _tags = new();
        private bool _disposed;

        internal PlcTagCollection(ABPlc plc, TimeSpan scanInterval)
        {
            Plc = plc;
            _scanDisposable = Observable.Timer(TimeSpan.Zero, scanInterval).Retry().Subscribe(_ =>
             {
                 if (ScanEnabled)
                 {
                     lock (_lockScan)
                     {
                         _readResultSubject.OnNext(Read());
                     }
                 }
             });
        }

        private PlcTagCollection()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PlcTagCollection"/> class.
        /// </summary>
        ~PlcTagCollection()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to read tags.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool ScanEnabled { get; set; } = true;

        /// <summary>
        /// Gets the read results.
        /// </summary>
        /// <value>
        /// The read results.
        /// </value>
        public IObservable<IEnumerable<PlcTagResult>> ReadResults => _readResultSubject.Publish().RefCount();

        /// <summary>
        /// Gets tags.
        /// </summary>
        /// <returns>A Value.</returns>
        public IReadOnlyList<IPlcTag> Tags => _tags.AsReadOnly();

        /// <summary>
        /// Gets controller.
        /// </summary>
        /// <value>
        /// The controller.
        /// </value>
        internal ABPlc? Plc { get; }

        /// <summary>
        /// Clears all Tags from the group.
        /// </summary>
        public void ClearTags() => _tags.Clear();

        /// <summary>
        /// Create Tag array.
        /// </summary>
        /// <typeparam name="TCustomType">Type to create.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="name">The textual name of the tag to access. The name is anything allowed by the protocol.
        /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.</param>
        /// <param name="length">elements count: 1- single, n-array.</param>
        /// <returns>
        /// A Value.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Is not array!
        /// or
        /// Length > 0.
        /// </exception>
        public IPlcTag<TCustomType> CreateTagArray<TCustomType>(string key, string name, int length)
            where TCustomType : IList
        {
            var type = typeof(TCustomType);
            if (!type.IsArray)
            {
                throw new ArgumentException("Is not array!");
            }

            if (length <= 0)
            {
                throw new ArgumentException("Length > 0!");
            }

            var obj = TagHelper.CreateObject<TCustomType>(length);
            return CreateTagType<TCustomType>(key, name, DataLength.GetSizeObject(obj[0]), length);
        }

        /// <summary>
        /// Create Tag custom Type Class.
        /// </summary>
        /// <typeparam name="TCustomType">Class to create.</typeparam>
        /// <param name="variable">The variable used by the end user.</param>
        /// <param name="tagName">The textual name of the tag to access. The name is anything allowed by the protocol.
        /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.</param>
        /// <returns>
        /// A Value.
        /// </returns>
        public IPlcTag<TCustomType> CreateTagType<TCustomType>(string variable, string tagName) => CreateTagType<TCustomType>(variable, tagName, DataLength.GetSizeObject(TagHelper.CreateObject<TCustomType>(1)));

        /// <summary>
        /// Create Tag using free definition.
        /// </summary>
        /// <typeparam name="TCustomType">The type of the custom type.</typeparam>
        /// <param name="variable">The key.</param>
        /// <param name="tagName">The textual name of the tag to access. The name is anything allowed by the protocol.
        /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.</param>
        /// <param name="size">The size of an element in bytes. The tag is assumed to be composed of elements of the same size.
        /// For structure tags, use the total size of the structure.</param>
        /// <param name="length">elements count: 1- single, n-array.</param>
        /// <returns>
        /// A Value.
        /// </returns>
        public IPlcTag<TCustomType> CreateTagType<TCustomType>(string variable, string tagName, int size, int length = 1)
        {
            var tag = new PlcTag<TCustomType>(Plc!, variable, tagName, size, length);
            _tags.Add(tag);
            return tag;
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
        /// Performs read of Group of Tags.
        /// </summary>
        /// <returns>A Value.</returns>
        public IEnumerable<PlcTagResult> Read() => Tags.Select(a => a.Read()).ToArray();

        /// <summary>
        /// Remove tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <exception cref="System.ArgumentException">Tag not exists in this collection.</exception>
        public void RemoveTag(IPlcTag tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (!Tags.Contains(tag))
            {
                throw new ArgumentException("Tag not exists in this collection!");
            }

            _tags.Remove(tag);
            CheckDisposeTag(tag);
        }

        /// <summary>
        /// Performs write of Group of Tags.
        /// </summary>
        /// <returns>A Value.</returns>
        public IEnumerable<PlcTagResult> Write()
        {
            return Tags.Select(a => a.Write());
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
                    _scanDisposable?.Dispose();
                    _readResultSubject.Dispose();
                    foreach (var tag in _tags.ToArray())
                    {
                        _tags.Remove(tag);
                        CheckDisposeTag(tag);
                    }
                }

                _disposed = true;
            }
        }

        private void CheckDisposeTag(IPlcTag tag)
        {
            // if not in Plc dispose
            if (!Plc!.Tags.Contains(tag))
            {
                tag.Dispose();
            }
        }
    }
}
