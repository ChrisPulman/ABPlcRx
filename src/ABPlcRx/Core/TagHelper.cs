// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace ABPlcRx
{
    /// <summary>
    /// Helper Tag.
    /// </summary>
    public static class TagHelper
    {
        /// <summary>
        /// Create object from Type.
        /// </summary>
        /// <typeparam name="TType">The type of the type.</typeparam>
        /// <param name="length">The length.</param>
        /// <returns>A Value.</returns>
        public static TType CreateObject<TType>(int length)
        {
            TType? obj;
            var typeTType = typeof(TType);

            if (typeTType == typeof(string))
            {
                obj = (TType)((object)string.Empty);
            }
            else if (typeTType.IsArray)
            {
                obj = (TType)Activator.CreateInstance(typeTType, length)!;
            }
            else
            {
                obj = (TType)Activator.CreateInstance(typeTType)!;
            }

            FixStringNullToEmpty(obj);

            return obj;
        }

        /// <summary>
        /// Performs Linear scaling conversion.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="minRaw">The minimum raw.</param>
        /// <param name="maxRaw">The maximum raw.</param>
        /// <param name="minScale">The minimum scale.</param>
        /// <param name="maxScale">The maximum scale.</param>
        /// <returns>A Value.</returns>
        public static double ScaleLinear(this IPlcTag tag, double minRaw, double maxRaw, double minScale, double maxScale)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (minRaw > maxRaw || minScale > maxScale)
            {
                throw new InvalidOperationException();
            }

            return (((maxScale - minScale) / (maxRaw - minRaw)) * (((double)tag.Value!) - minRaw)) + minScale;
        }

        /// <summary>
        /// Performs SquareRoot conversion.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="minRaw">The minimum raw.</param>
        /// <param name="maxRaw">The maximum raw.</param>
        /// <param name="minScale">The minimum scale.</param>
        /// <param name="maxScale">The maximum scale.</param>
        /// <returns>A Value.</returns>
        public static double ScaleSquareRoot(this IPlcTag tag, double minRaw, double maxRaw, double minScale, double maxScale)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (minRaw > maxRaw || minScale > maxScale)
            {
                throw new InvalidOperationException();
            }

            return (Math.Sqrt((((double)tag.Value!) - minRaw) / (maxRaw - minRaw)) * (maxScale - minScale)) + minScale;
        }

        /// <summary>
        /// Number to bit array.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A Value.</returns>
        public static BitArray NumberToBits(int value) => new(new[] { value });

        /// <summary>
        /// Bite array to number.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <returns>A Value.</returns>
        public static int BitsToNumber(BitArray bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException(nameof(bits));
            }

            var result = new int[1];
            bits.CopyTo(result, 0);
            return result[0];
        }

        internal static IEnumerable<PropertyInfo> GetAccessableProperties(Type type) => type.GetProperties(BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public)
                       .Where(p => p.GetSetMethod() != null);

        internal static Array? GetArray(object? value)
        {
            var array = (Array?)value;
            if (array?.Length <= 0)
            {
                throw new Exception("Cannot determine size of class, " +
                                    "because an array is defined which has no fixed size greater than zero.");
            }

            return array;
        }

        /// <summary>
        /// Fix string null to empty.
        /// </summary>
        /// <param name="obj">The object.</param>
        private static void FixStringNullToEmpty(object? obj)
        {
            var type = obj?.GetType();
            if (type == typeof(string))
            {
                if (obj == null)
                {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                    obj = string.Empty;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                }
            }
            else if (type!.IsArray && type.GetElementType() == typeof(string))
            {
                var array = GetArray(obj);
                for (var i = 0; i < array?.Length; i++)
                {
                    if (array.GetValue(i) == null)
                    {
                        array.SetValue(string.Empty, i);
                    }
                }
            }
            else if (type.IsClass && !type.IsAbstract)
            {
                foreach (var property in GetAccessableProperties(type))
                {
                    if (property.PropertyType == typeof(string))
                    {
                        if (property.GetValue(obj) == null)
                        {
                            property.SetValue(obj, string.Empty);
                        }
                    }
                    else
                    {
                        FixStringNullToEmpty(property.GetValue(obj));
                    }
                }
            }
        }
    }
}
