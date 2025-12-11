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
    /// Structure representing the yaw control deltas in a Skybrush mission.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct YawControlNative
    {
        public IntPtr buffer;
        public UIntPtr buffer_length;
        public byte owner;
        public UIntPtr header_length;
        public UIntPtr num_deltas;
        public byte auto_yaw;
        public short yaw_offset_ddeg;
    }

    /// <summary>
    /// Represents the yaw control deltas in a Skybrush mission.
    /// This class provides methods to load and manage yaw control data from Skybrush binary files.
    /// </summary>
    public class YawControl : IDisposable
    {
        private const string LibName = "skybrush";
        private YawControlNative native;
        private bool disposed = false;

        /// <summary>
        /// Gets whether auto yaw mode is in use.
        /// </summary>
        public bool AutoYaw => native.auto_yaw != 0;

        /// <summary>
        /// Gets the yaw offset in degrees.
        /// </summary>
        public float YawOffsetDegrees => native.yaw_offset_ddeg / 10.0f;

        /// <summary>
        /// Gets the number of yaw deltas in the yaw control object.
        /// </summary>
        public int NumberOfDeltas => (int)native.num_deltas;

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_control_init_from_binary_file(
            ref YawControlNative ctrl, int fd);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_control_init_from_binary_file_in_memory(
            ref YawControlNative ctrl, byte[] buf, UIntPtr nbytes);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_control_init_from_buffer(
            ref YawControlNative ctrl, IntPtr buf, UIntPtr nbytes);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_control_init_empty(ref YawControlNative ctrl);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sb_yaw_control_destroy(ref YawControlNative ctrl);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte sb_yaw_control_is_empty(ref YawControlNative ctrl);

        /// <summary>
        /// Initializes a new empty yaw control object.
        /// </summary>
        public YawControl()
        {
            SkybrushError error = sb_yaw_control_init_empty(ref native);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to initialize empty yaw control object");
            }
        }

        /// <summary>
        /// Initializes a yaw control object from a Skybrush binary file loaded in memory.
        /// </summary>
        /// <param name="buffer">The buffer holding the loaded Skybrush file in binary format.</param>
        public YawControl(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            SkybrushError error = sb_yaw_control_init_from_binary_file_in_memory(
                ref native, buffer, (UIntPtr)buffer.Length);
            
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to initialize yaw control from buffer");
            }
        }

        /// <summary>
        /// Gets whether the yaw control object is empty.
        /// </summary>
        public bool IsEmpty()
        {
            ThrowIfDisposed();
            return sb_yaw_control_is_empty(ref native) != 0;
        }

        /// <summary>
        /// Disposes the yaw control object and releases all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                sb_yaw_control_destroy(ref native);
                disposed = true;
            }
        }

        ~YawControl()
        {
            Dispose(false);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        internal ref YawControlNative GetNative()
        {
            ThrowIfDisposed();
            return ref native;
        }
    }
}
