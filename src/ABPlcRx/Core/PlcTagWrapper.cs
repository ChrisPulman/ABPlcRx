// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text;
using libplctag.NativeImport;

namespace ABPlcRx
{
    /// <summary>
    /// Plc Tag Wrapper.
    /// </summary>
    public class PlcTagWrapper
    {
        private const byte ByteHeaderLengthString = 4;
        private const byte MaxLengthString = 82;
        private readonly IPlcTag _tag;

        internal PlcTagWrapper(IPlcTag tag) => _tag = tag;

        /// <summary>
        /// Get bit from index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>A Value.</returns>
        public bool GetBit(int index) => (Convert.ToInt64(GetNumericValue()) & (1 << index)) != 0;

        /// <summary>
        /// Get bit array from value.
        /// </summary>
        /// <returns>A Value.</returns>
        public BitArray GetBits() => new(new[] { Convert.ToInt32(GetNumericValue()) });

        /// <summary>
        /// Get bit array from value.
        /// </summary>
        /// <returns>A Value.</returns>
        public bool[] GetBitsArray() => GetBits().Cast<bool>().ToArray();

        /// <summary>
        /// Get bit string format.
        /// </summary>
        /// <returns>A Value.</returns>
        public string GetBitsString() => new(GetBits().Cast<bool>().Select(a => a ? '1' : '0').ToArray());

        /// <summary>
        /// Get local value Float32.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public float GetFloat32(int offset = 0) => plctag.plc_tag_get_float32(_tag.Handle, offset);

        /// <summary>
        /// Get local value Float.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public double GetFloat64(int offset = 0) => plctag.plc_tag_get_float64(_tag.Handle, offset);

        /// <summary>
        /// Get local value Int16.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public short GetInt16(int offset = 0) => plctag.plc_tag_get_int16(_tag.Handle, offset);

        /// <summary>
        /// Get local value Int32.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public int GetInt32(int offset = 0) => plctag.plc_tag_get_int32(_tag.Handle, offset);

        /// <summary>
        /// Get local value Int64.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public long GetInt64(int offset = 0) => plctag.plc_tag_get_int64(_tag.Handle, offset);

        /// <summary>
        /// Get local value Int8.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public sbyte GetInt8(int offset = 0) => plctag.plc_tag_get_int8(_tag.Handle, offset);

        /// <summary>
        /// Get local value String.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public string GetString(int offset = 0)
        {
            var sb = new StringBuilder();

            // max length string
            var length = GetInt32(offset);

            // read only length of string
            for (var i = 0; i < length; i++)
            {
                sb.Append((char)GetUInt8(offset + ByteHeaderLengthString + i));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get local value form type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public object GetType(object obj, int offset = 0)
        {
            if (obj == null)
            {
                return null!;
            }

            foreach (var property in TagHelper.GetAccessableProperties(obj.GetType()))
            {
                var value = property.GetValue(obj);
                property.SetValue(obj, Get(value, offset));
                offset += DataLength.GetSizeObject(value);
            }

            return obj;
        }

        /// <summary>
        /// Get local value UInt16.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public ushort GetUInt16(int offset = 0) => plctag.plc_tag_get_uint16(_tag.Handle, offset);

        /// <summary>
        /// Get local value UInt32.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public uint GetUInt32(int offset = 0) => plctag.plc_tag_get_uint32(_tag.Handle, offset);

        /// <summary>
        /// Get local value UInt64.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public ulong GetUInt64(int offset = 0) => plctag.plc_tag_get_uint64(_tag.Handle, offset);

        /// <summary>
        /// Get local value UInt8.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        public byte GetUInt8(int offset = 0) => plctag.plc_tag_get_uint8(_tag.Handle, offset);

        /// <summary>
        /// Set bit from index and value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Index out of bound.</exception>
        public void SetBit(int index, bool value)
        {
            if (_tag.Size * 8 <= index)
            {
                throw new ArgumentOutOfRangeException("Index out of bound!");
            }

            var bits = GetBits();
            bits.Set(index, value);
            var data = new int[1];
            bits.CopyTo(data, 0);

            Set(data[0]);
        }

        /// <summary>
        /// Set bits from BitArray.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <exception cref="System.ArgumentNullException">binary.</exception>
        public void SetBits(BitArray bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException("binary");
            }

            for (var i = 0; i < _tag.Size * 8; i++)
            {
                SetBit(i, bits[i]);
            }
        }

        /// <summary>
        /// Set local value Float32.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetFloat32(float value, int offset = 0) => plctag.plc_tag_set_float32(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value Float.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetFloat64(double value, int offset = 0) => plctag.plc_tag_set_float64(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value Int16.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetInt16(short value, int offset = 0) => plctag.plc_tag_set_int16(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value Int32.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetInt32(int value, int offset = 0) => plctag.plc_tag_set_int32(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value Int64.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetInt64(long value, int offset = 0) => plctag.plc_tag_set_int64(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value Int8.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetInt8(sbyte value, int offset = 0) => plctag.plc_tag_set_int8(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value String.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetString(string value, int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLengthString)
            {
                throw new ArgumentOutOfRangeException($"Length of value <= {MaxLengthString}!");
            }

            // set length
            SetInt32(value.Length, offset);

            int strIdx;

            // copy data
            for (strIdx = 0; strIdx < value.Length; strIdx++)
            {
                SetUInt8((byte)value[strIdx], offset + ByteHeaderLengthString + strIdx);
            }

            // pad with zeros
            for (; strIdx < MaxLengthString; strIdx++)
            {
                SetUInt8(0, offset + ByteHeaderLengthString + strIdx);
            }
        }

        /// <summary>
        /// Set local valute from type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="offset">The offset.</param>
        public void SetType(object obj, int offset = 0)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            foreach (var property in TagHelper.GetAccessableProperties(obj.GetType()))
            {
                var value = property.GetValue(obj);
                Set(value, offset);
                offset += DataLength.GetSizeObject(value);
            }
        }

        /// <summary>
        /// Set local value UInt16.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetUInt16(ushort value, int offset = 0) => plctag.plc_tag_set_uint16(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value UInt32.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetUInt32(uint value, int offset = 0) => plctag.plc_tag_set_uint32(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value UInt64.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetUInt64(ulong value, int offset = 0) => plctag.plc_tag_set_uint64(_tag.Handle, offset, value);

        /// <summary>
        /// Set local value UInt8.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public void SetUInt8(byte value, int offset = 0) => plctag.plc_tag_set_uint8(_tag.Handle, offset, value);

        /// <summary>
        /// Get local value.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>A Value.</returns>
        /// <exception cref="System.ArgumentException">Error data type!.</exception>
        internal object? Get(object? obj, int offset = 0)
        {
            var type = obj?.GetType();
            if (type!.IsArray)
            {
                var array = TagHelper.GetArray(obj);
                for (var i = 0; i < array?.Length; i++)
                {
                    var el = array.GetValue(i);
                    array.SetValue(Get(el, offset), i);
                    offset += DataLength.GetSizeObject(el);
                }

                return array;
            }
            else if (type == typeof(long))
            {
                return GetInt64(offset);
            }
            else if (type == typeof(ulong))
            {
                return GetUInt64(offset);
            }
            else if (type == typeof(int))
            {
                return GetInt32(offset);
            }
            else if (type == typeof(uint))
            {
                return GetUInt32(offset);
            }
            else if (type == typeof(short))
            {
                return GetInt16(offset);
            }
            else if (type == typeof(ushort))
            {
                return GetUInt16(offset);
            }
            else if (type == typeof(sbyte))
            {
                return GetInt8(offset);
            }
            else if (type == typeof(byte))
            {
                return GetUInt8(offset);
            }
            else if (type == typeof(float))
            {
                return GetFloat32(offset);
            }
            else if (type == typeof(double))
            {
                return GetFloat64(offset);
            }
            else if (type == typeof(string))
            {
                return GetString(offset);
            }
            else if (type.IsClass && !type.IsAbstract)
            {
                return GetType(obj!, offset);
            }
            else
            {
                throw new ArgumentException("Error data type!");
            }
        }

        /// <summary>
        /// Set local value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        /// <exception cref="System.ArgumentException">Error data type!.</exception>
        internal void Set(object? value, int offset = 0)
        {
            var type = value?.GetType();
            if (type!.IsArray)
            {
                foreach (var el in TagHelper.GetArray(value)!)
                {
                    Set(el, offset);
                    offset += DataLength.GetSizeObject(el);
                }
            }
            else if (type == typeof(long))
            {
                SetInt64((long)value!, offset);
            }
            else if (type == typeof(ulong))
            {
                SetUInt64((ulong)value!, offset);
            }
            else if (type == typeof(int))
            {
                SetInt32((int)value!, offset);
            }
            else if (type == typeof(uint))
            {
                SetUInt32((uint)value!, offset);
            }
            else if (type == typeof(short))
            {
                SetInt16((short)value!, offset);
            }
            else if (type == typeof(ushort))
            {
                SetUInt16((ushort)value!, offset);
            }
            else if (type == typeof(sbyte))
            {
                SetInt8((sbyte)value!, offset);
            }
            else if (type == typeof(byte))
            {
                SetUInt8((byte)value!, offset);
            }
            else if (type == typeof(float))
            {
                SetFloat32((float)value!, offset);
            }
            else if (type == typeof(double))
            {
                SetFloat64((double)value!, offset);
            }
            else if (type == typeof(string))
            {
                SetString((string)value!, offset);
            }
            else if (type.IsClass && !type.IsAbstract)
            {
                SetType(value!, offset);
            }
            else
            {
                throw new ArgumentException("Error data type!");
            }
        }

        private object? GetNumericValue(int offset = 0)
        {
            if (IsNumericInteger())
            {
                return Get(_tag.Value, offset);
            }

            throw new ArgumentException("Error data type!");
        }

        private bool IsNumericInteger()
        {
            return Type.GetTypeCode(_tag.TypeValue) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => true,
                _ => false,
            };
        }
    }
}
