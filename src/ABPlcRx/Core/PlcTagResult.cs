// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ABPlcRx
{
    /// <summary>
    /// Plc Tag Result.
    /// </summary>
    public class PlcTagResult
    {
        internal PlcTagResult(IPlcTag tag, DateTime timestamp, long executionTime, int statusCode)
        {
            Tag = tag;
            Timestamp = timestamp;
            ExecutionTime = executionTime;
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets tag.
        /// </summary>
        /// <value>
        /// The tag.
        /// </value>
        public IPlcTag Tag { get; }

        /// <summary>
        /// Gets timestamp last operation.
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets millisecond execution operatorion.
        /// </summary>
        /// <value>
        /// The execution time.
        /// </value>
        public long ExecutionTime { get; }

        /// <summary>
        /// Gets the status code <see cref="PlcTagStatus" />
        /// STATUS_OK will be returned if the operation completed successfully.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        public int StatusCode { get; }

        /// <summary>
        /// Reduce multiple result to one.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <returns>
        /// A Value.
        /// </returns>
        public static PlcTagResult Reduce(IEnumerable<PlcTagResult> results) => new(
            null!,
            results.Min(a => a.Timestamp),
            results.Sum(a => a.ExecutionTime),
            results.Sum(a => a.StatusCode) != 0 ? results.Max(a => a.StatusCode) : 0);

        /// <summary>
        /// Information result.
        /// </summary>
        /// <returns>A Value.</returns>
        public override string ToString() =>
           $@"Tag Name:      {Tag.TagName}
            Tag Value:     {Tag.Value}
            Timestamp:     {Timestamp}
            ExecutionTime: {ExecutionTime}
            StatusCode:    {StatusCode}";
    }
}
