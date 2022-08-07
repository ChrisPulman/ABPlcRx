// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using libplctag.NativeImport;

namespace ABPlcRx
{
    /// <summary>
    /// Tag base definition.
    /// </summary>
    /// <typeparam name="TType">The type of the type.</typeparam>
    /// <seealso cref="System.IDisposable" />
    internal sealed class PlcTag<TType> : IPlcTag<TType>
    {
        private readonly Subject<PlcTagResult> _changedSubject = new();
        private bool _disposed;
        private TType? _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlcTag{TType}"/> class.
        /// Creates a tag. If the CPU type is LGX, the port type and slot has to be specified.
        /// </summary>
        /// <param name="abPlc">Controller reference.</param>
        /// <param name="name">The textual name of the tag to access. The name is anything allowed by the protocol.
        /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.</param>
        /// <param name="size">The size of an element in bytes. The tag is assumed to be composed of elements of the same size.
        /// For structure tags, use the total size of the structure.</param>
        /// <param name="length">elements count: 1- single, n-array.</param>
        internal PlcTag(ABPlc abPlc, string name, int size, int length = 1)
        {
            ABPlc = abPlc;
            Name = name;
            Size = size;
            Length = length;
            ValueManager = new PlcTagWrapper(this);
            TypeValue = typeof(TType);

            var url = $"protocol=ab_eip&gateway={abPlc.IPAddress}";
            if (!string.IsNullOrEmpty(abPlc.Slot))
            {
                url += $"&path={abPlc.Slot}";
            }

            url += $"&cpu={abPlc.PlcType}&elem_size={Size}&elem_count={Length}&name={Name}";
            if (abPlc.DebugLevel > 0)
            {
                url += $"&debug={abPlc.DebugLevel}";
            }

            // create reference
            Handle = plctag.plc_tag_create(url, abPlc.Timeout);

            Value = TagHelper.CreateObject<TType>(Length);
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        private PlcTag()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PlcTag{TType}"/> class.
        /// </summary>
        ~PlcTag()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets handle creation Tag.
        /// </summary>
        public int Handle { get; }

        /// <summary>
        /// Gets the changed.
        /// </summary>
        /// <value>
        /// The changed.
        /// </value>
        public IObservable<PlcTagResult> Changed => _changedSubject;

        /// <summary>
        /// Gets a value indicating whether indicates whether or not a value must be read from the PLC.
        /// </summary>
        public bool IsRead { get; private set; }

        /// <summary>
        /// Gets a value indicating whether indicates whether or not a value must be write to the PLC.
        /// </summary>
        public bool IsWrite { get; private set; }

        /// <summary>
        /// Gets elements length: 1- single, n-array.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the textual name of the tag to access. The name is anything allowed by the protocol.
        /// E.g. myDataStruct.rotationTimer.ACC, myDINTArray[42] etc.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets a value indicating whether indicate if Tag is in read only.async Write raise exception.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets the size of an element in bytes. The tag is assumed to be composed of elements of the same size.For structure tags,
        /// use the total size of the structure.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets type value.
        /// </summary>
        public Type TypeValue { get; }

        /// <summary>
        /// Gets or sets value tag.
        /// </summary>
        public TType? Value
        {
            get => (TType?)ValueManager.Get(_value, 0);

            set
            {
                _value = value;

                if (ABPlc.AutoWriteValue)
                {
                    Write();
                }
            }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        object? IPlcTag.Value
        {
            get => Value;
            set => Value = (TType?)value;
        }

        /// <summary>
        /// Gets value manager.
        /// </summary>
        public PlcTagWrapper ValueManager { get; }

        /// <summary>
        /// Gets controller reference.
        /// </summary>
        internal ABPlc ABPlc { get; }

        /// <summary>
        /// Abort any outstanding IO to the PLC. <see cref="PlcTagStatus"/>.
        /// </summary>
        /// <returns>A Value.</returns>
        public int Abort() => plctag.plc_tag_abort(Handle);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get size tag read from PLC.
        /// </summary>
        /// <returns>A Value.</returns>
        public int GetSize() => plctag.plc_tag_get_size(Handle);

        /// <summary>
        /// Get status operation. <see cref="PlcTagStatus"/>.
        /// </summary>
        /// <returns>A Value.</returns>
        public int GetStatus() => plctag.plc_tag_status(Handle);

        /// <summary>
        /// Lock for multitrading. <see cref="PlcTagStatus"/>.
        /// </summary>
        /// <returns>A Value.</returns>
        public int Lock() => plctag.plc_tag_lock(Handle);

        /// <summary>
        /// Performs read of Tag.
        /// </summary>
        /// <returns>A Value.</returns>
        public PlcTagResult Read()
        {
            var timestamp = DateTime.Now;
            var watch = Stopwatch.StartNew();
            var statusCode = plctag.plc_tag_read(Handle, ABPlc.Timeout);

            watch.Stop();
            IsRead = true;

            var result = new PlcTagResult(this, timestamp, watch.ElapsedMilliseconds, statusCode);

            // check raise exception
            if (ABPlc.FailOperationRaiseException && PlcTagStatus.IsError(statusCode))
            {
                throw new PlcTagException(result);
            }

            _changedSubject?.OnNext(result);

            return result;
        }

        /// <summary>
        /// Unlock for multitrading <see cref="PlcTagStatus"/>.
        /// </summary>
        /// <returns>A Value.</returns>
        public int Unlock() => plctag.plc_tag_unlock(Handle);

        /// <summary>
        /// Performs write of Tag.
        /// </summary>
        /// <returns>A Value.</returns>
        public PlcTagResult Write()
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException("Tag is set read only!");
            }

            ValueManager.Set(_value, 0);

            var timestamp = DateTime.Now;
            var watch = Stopwatch.StartNew();
            var statusCode = plctag.plc_tag_write(Handle, ABPlc.Timeout);
            watch.Stop();
            IsWrite = true;

            var result = new PlcTagResult(this, timestamp, watch.ElapsedMilliseconds, statusCode);

            // check raise exception
            if (ABPlc.FailOperationRaiseException && PlcTagStatus.IsError(statusCode))
            {
                throw new PlcTagException(result);
            }

            Read();
            var dummy = Value;

            return result;
        }

        private static T DeepClone<T>(T obj)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                formatter.Serialize(stream, obj!);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }

        private static int GetHashCode(byte[] data)
        {
            if (data == null)
            {
                return 0;
            }

            var i = data.Length;
            var hc = i + 1;

            while (--i >= 0)
            {
                hc *= 257;
                hc ^= data[i];
            }

            return hc;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _changedSubject.Dispose();
                    plctag.plc_tag_destroy(Handle);
                }

                _disposed = true;
            }
        }
    }
}
