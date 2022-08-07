// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using libplctag.NativeImport;

namespace ABPlcRx
{
    /// <summary>
    /// Status code operation.
    /// </summary>
    public static class PlcTagStatus
    {
        /// <summary>
        /// Operation in progress. Not an error.
        /// </summary>
        public const int StatusPending = 1;

        /// <summary>
        /// No error.
        /// </summary>
        public const int StatusOK = 0;

        /// <summary>
        /// The operation was aborted.
        /// </summary>
        public const int ErrErrAbort = -1;

        /// <summary>
        /// The operation failed due to incorrect configuration.
        /// Usually returned from a remote system.
        /// </summary>
        public const int ErrBadConfig = -2;

        /// <summary>
        /// The connection failed for some reason.
        /// This can mean that the remote PLC was power cycled, for instance.
        /// </summary>
        public const int ErrBadConnection = -3;

        /// <summary>
        /// The data received from the remote PLC was undecipherable or otherwise not able to be processed.
        /// Can also be returned from a remote system that cannot process the data sent to it.
        /// </summary>
        public const int ErrBadData = -4;

        /// <summary>
        /// Usually returned from a remote system when something addressed does not exist.
        /// </summary>
        public const int ErrBadDevice = -5;

        /// <summary>
        /// Usually returned when the library is unable to connect to a remote system.
        /// </summary>
        public const int ErrBadGateway = -6;

        /// <summary>
        /// A common error return when something is not correct with the tag creation attribute string.
        /// </summary>
        public const int ErrBadParam = -7;

        /// <summary>
        /// Usually returned when the remote system returned an unexpected response.
        /// </summary>
        public const int ErrBadReply = -8;

        /// <summary>
        /// Usually returned by a remote system when something is not in a good state.
        /// </summary>
        public const int ErrBadStatus = -9;

        /// <summary>
        /// An error occurred trying to close some resource.
        /// </summary>
        public const int ErrClose = -10;

        /// <summary>
        /// An error occurred trying to create some internal resource.
        /// </summary>
        public const int ErrCreate = -11;

        /// <summary>
        /// An error returned by a remote system when something is incorrectly duplicated
        /// (i.e. a duplicate connection ID).
        /// </summary>
        public const int ErrDuplicate = -12;

        /// <summary>
        /// An error was returned when trying to encode some data such as a tag name.
        /// </summary>
        public const int ErrEncode = -13;

        /// <summary>
        /// An internal library error.
        /// It would be very unusual to see this.
        /// </summary>
        public const int ErrMutexDestroy = -14;

        /// <summary>
        /// An internal library error.
        /// It would be very unusual to see this.
        /// </summary>
        public const int ErrMutexInit = -15;

        /// <summary>
        /// An internal library error.
        /// It would be very unusual to see this.
        /// </summary>
        public const int ErrMutexLock = -16;

        /// <summary>
        /// An internal library error.
        /// It would be very unusual to see this.
        /// </summary>
        public const int ErrMutexUnlock = -17;

        /// <summary>
        /// Often returned from the remote system when an operation is not permitted.
        /// </summary>
        public const int ErrNotAllowed = -18;

        /// <summary>
        /// Often returned from the remote system when something is not found.
        /// </summary>
        public const int ErrNotFound = -19;

        /// <summary>
        /// Returned when a valid operation is not implemented.
        /// </summary>
        public const int ErrNotImplemented = -20;

        /// <summary>
        /// Returned when expected data is not present.
        /// </summary>
        public const int ErrNoData = -21;

        /// <summary>
        /// Similar to NOT_FOUND.
        /// </summary>
        public const int ErrNoMatch = -22;

        /// <summary>
        /// Returned by the library when memory allocation fails.
        /// </summary>
        public const int ErrNoMem = -23;

        /// <summary>
        /// Returned by the remote system when some resource allocation fails.
        /// </summary>
        public const int ErrNoResources = -24;

        /// <summary>
        /// Usually an internal error, but can be returned when an invalid handle is used with an API call.
        /// </summary>
        public const int ErrNullPtr = -25;

        /// <summary>
        /// Returned when an error occurs opening a resource such as a socket.
        /// </summary>
        public const int ErrOpen = -26;

        /// <summary>
        /// Usually returned when trying to write a value into a tag outside of the tag data bounds.
        /// </summary>
        public const int ErrOutOfBounds = -27;

        /// <summary>
        /// Returned when an error occurs during a read operation.
        /// Usually related to socket problems.
        /// </summary>
        public const int ErrRead = -28;

        /// <summary>
        /// An unspecified or untranslatable remote error causes this.
        /// </summary>
        public const int ErrRemoteErr = -29;

        /// <summary>
        /// An internal library error. If you see this, it is likely that everything is about to crash.
        /// </summary>
        public const int ErrThreadCreate = -30;

        /// <summary>
        /// Another internal library error.
        /// It is very unlikely that you will see this.
        /// </summary>
        public const int ErrThreadJoin = -31;

        /// <summary>
        /// An operation took too long and timed out.
        /// </summary>
        public const int ErrTimeout = -32;

        /// <summary>
        /// More data was returned than was expected.
        /// </summary>
        public const int ErrTooLarge = -33;

        /// <summary>
        /// Insufficient data was returned from the remote system.
        /// </summary>
        public const int ErrTooSmall = -34;

        /// <summary>
        /// The operation is not supported on the remote system.
        /// </summary>
        public const int ErrUnsupported = -35;

        /// <summary>
        /// A Winsock-specific error occurred (only on Windows).
        /// </summary>
        public const int ErrWinsock = -36;

        /// <summary>
        /// An error occurred trying to write, usually to a socket.
        /// </summary>
        public const int ErrWrite = -37;

        /// <summary>
        /// Check code in error.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>
        /// A Value.
        /// </returns>
        public static bool IsError(int code) => code != StatusPending && code != StatusOK;

        /// <summary>
        /// Decode error.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <returns>A Value.</returns>
        public static string DecodeError(int code) => plctag.plc_tag_decode_error(code);
    }
}
