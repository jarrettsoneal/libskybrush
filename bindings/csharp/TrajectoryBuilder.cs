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
    /// A 3D vector with yaw component for trajectory points.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3WithYaw
    {
        /// <summary>The X coordinate</summary>
        public float X;
        /// <summary>The Y coordinate</summary>
        public float Y;
        /// <summary>The Z coordinate</summary>
        public float Z;
        /// <summary>The yaw angle in degrees</summary>
        public float Yaw;

        public Vector3WithYaw(float x, float y, float z, float yaw = 0)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
        }
    }

    /// <summary>
    /// Flags for trajectory creation.
    /// </summary>
    [Flags]
    public enum TrajectoryFlags : byte
    {
        /// <summary>No flags</summary>
        None = 0,
        /// <summary>Use yaw in the trajectory</summary>
        UseYaw = 1
    }

    /// <summary>
    /// Internal structure for buffer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct BufferNative
    {
        public IntPtr stor_begin;
        public IntPtr end;
        public IntPtr stor_end;
        public byte owned;
    }

    /// <summary>
    /// Internal structure for trajectory builder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TrajectoryBuilderNative
    {
        public BufferNative buffer;
        public Vector3WithYaw last_position;
        public byte scale;
    }

    /// <summary>
    /// Builder class for creating drone trajectories programmatically.
    /// Trajectories define the path a drone follows during a show.
    /// </summary>
    public class TrajectoryBuilder : IDisposable
    {
        private const string LibName = "skybrush";
        private TrajectoryBuilderNative native;
        private bool disposed = false;

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_trajectory_builder_init(
            ref TrajectoryBuilderNative builder, byte scale, byte flags);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sb_trajectory_builder_destroy(
            ref TrajectoryBuilderNative builder);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_trajectory_builder_set_start_position(
            ref TrajectoryBuilderNative builder, Vector3WithYaw start);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_trajectory_builder_append_line(
            ref TrajectoryBuilderNative builder, Vector3WithYaw target, uint duration_msec);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_trajectory_builder_hold_position_for(
            ref TrajectoryBuilderNative builder, uint duration_msec);

        /// <summary>
        /// Initializes a new trajectory builder.
        /// </summary>
        /// <param name="scale">The scale of the trajectory (1-127). Higher values allow larger coordinate ranges but lower precision.</param>
        /// <param name="useYaw">Whether to include yaw control in the trajectory.</param>
        public TrajectoryBuilder(byte scale = 10, bool useYaw = false)
        {
            if (scale == 0 || scale > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be between 1 and 127");
            }

            byte flags = useYaw ? (byte)TrajectoryFlags.UseYaw : (byte)TrajectoryFlags.None;
            SkybrushError error = sb_trajectory_builder_init(ref native, scale, flags);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to initialize trajectory builder");
            }
        }

        /// <summary>
        /// Sets the starting position of the trajectory.
        /// This must be called before adding any segments.
        /// </summary>
        /// <param name="position">The starting position.</param>
        public void SetStartPosition(Vector3WithYaw position)
        {
            ThrowIfDisposed();
            SkybrushError error = sb_trajectory_builder_set_start_position(ref native, position);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to set start position");
            }
        }

        /// <summary>
        /// Appends a straight line segment to the trajectory.
        /// </summary>
        /// <param name="target">The target position to move to.</param>
        /// <param name="durationMs">The duration of the movement in milliseconds.</param>
        public void AppendLine(Vector3WithYaw target, uint durationMs)
        {
            ThrowIfDisposed();
            SkybrushError error = sb_trajectory_builder_append_line(ref native, target, durationMs);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to append line segment");
            }
        }

        /// <summary>
        /// Holds the current position for the specified duration.
        /// </summary>
        /// <param name="durationMs">The duration to hold position in milliseconds.</param>
        public void HoldPosition(uint durationMs)
        {
            ThrowIfDisposed();
            SkybrushError error = sb_trajectory_builder_hold_position_for(ref native, durationMs);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to hold position");
            }
        }

        /// <summary>
        /// Gets the raw buffer data for this trajectory.
        /// Used internally when creating .skyb files.
        /// </summary>
        internal BufferNative GetBuffer()
        {
            ThrowIfDisposed();
            return native.buffer;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                sb_trajectory_builder_destroy(ref native);
                disposed = true;
            }
        }

        ~TrajectoryBuilder()
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
    }
}
