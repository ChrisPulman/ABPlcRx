// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace ABPlcRx
{
    /// <summary>
    /// Plc Tag Exception.
    /// </summary>
    [System.Serializable]
#pragma warning disable RCS1194 // Implement exception constructors.
    public class PlcTagException : Exception
#pragma warning restore RCS1194 // Implement exception constructors.
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlcTagException"/> class.
        /// </summary>
        /// <param name="result">The result.</param>
        public PlcTagException(PlcTagResult result)
            : base("Error executing PlcTag operation.") => Result = result;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlcTagException"/> class.
        /// </summary>
        /// <param name="serializationInfo">The serialization information.</param>
        /// <param name="streamingContext">The streaming context.</param>
        protected PlcTagException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        /// <summary>
        /// Gets result operation.
        /// </summary>
        /// <value>ResultOperation.</value>
        public PlcTagResult? Result { get; }
    }
}
