/*
 * This file is part of libskybrush.
 *
 * Copyright 2020-2025 CollMot Robotics Ltd.
 *
 * libskybrush is free software: you can redistribute it and/or modify it under
 * the terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 *
 * libskybrush is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along with
 * this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Runtime.InteropServices;

namespace Skybrush
{
    /// <summary>
    /// Error codes used throughout libskybrush.
    /// </summary>
    public enum SkybrushError
    {
        /// <summary>No error</summary>
        Success = 0,
        /// <summary>Not enough memory</summary>
        NoMemory = 1,
        /// <summary>Invalid value</summary>
        InvalidValue = 2,
        /// <summary>Error while opening an IO channel</summary>
        OpenError = 3,
        /// <summary>Error while closing an IO channel</summary>
        CloseError = 4,
        /// <summary>Error while reading from an IO channel</summary>
        ReadError = 5,
        /// <summary>Error while writing to an IO channel</summary>
        WriteError = 6,
        /// <summary>Error while reading and writing an IO channel in duplex mode</summary>
        ReadWriteError = 7,
        /// <summary>Error while parsing some protocol</summary>
        ParseError = 8,
        /// <summary>Timeout while reading from an IO channel</summary>
        Timeout = 9,
        /// <summary>IO channel locked by another process</summary>
        Locked = 10,
        /// <summary>Generic failure code</summary>
        Failure = 11,
        /// <summary>Unsupported operation</summary>
        Unsupported = 12,
        /// <summary>Unimplemented operation</summary>
        Unimplemented = 13,
        /// <summary>Operation not permitted</summary>
        PermissionDenied = 14,
        /// <summary>Some internal buffer is full</summary>
        BufferFull = 15,
        /// <summary>Some internal buffer is empty</summary>
        BufferEmpty = 16,
        /// <summary>Resource temporarily unavailable</summary>
        TryAgain = 17,
        /// <summary>File does not exist</summary>
        NotFound = 18,
        /// <summary>Corrupted data</summary>
        Corrupted = 19,
        /// <summary>Overflow error</summary>
        Overflow = 20
    }

    /// <summary>
    /// Native methods for error handling.
    /// </summary>
    internal static class SkybrushErrorNative
    {
        private const string LibName = "skybrush";

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sb_error_to_string(int code);

        /// <summary>
        /// Converts a Skybrush error code to a human-readable string.
        /// </summary>
        public static string ErrorToString(SkybrushError error)
        {
            IntPtr ptr = sb_error_to_string((int)error);
            return Marshal.PtrToStringAnsi(ptr) ?? "Unknown error";
        }
    }

    /// <summary>
    /// Exception thrown when a Skybrush operation fails.
    /// </summary>
    public class SkybrushException : Exception
    {
        public SkybrushError ErrorCode { get; }

        public SkybrushException(SkybrushError errorCode)
            : base(SkybrushErrorNative.ErrorToString(errorCode))
        {
            ErrorCode = errorCode;
        }

        public SkybrushException(SkybrushError errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
