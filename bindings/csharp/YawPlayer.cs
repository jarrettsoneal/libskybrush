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
    /// Structure describing a single yaw setpoint.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct YawSetpoint
    {
        /// <summary>The timestamp when the setpoint should start, in seconds</summary>
        public float StartTimeSec;

        /// <summary>The timestamp when the setpoint should start, in milliseconds</summary>
        public uint StartTimeMsec;

        /// <summary>The net duration of the setpoint, in seconds</summary>
        public float DurationSec;

        /// <summary>The net duration of the setpoint, in milliseconds</summary>
        public ushort DurationMsec;

        /// <summary>The timestamp when the setpoint should end, in seconds</summary>
        public float EndTimeSec;

        /// <summary>The timestamp when the setpoint should end, in milliseconds</summary>
        public uint EndTimeMsec;

        /// <summary>The starting yaw of the setpoint, in 1/10th of degrees</summary>
        public int StartYawDdeg;

        /// <summary>The starting yaw of the setpoint, in degrees</summary>
        public float StartYawDeg;

        /// <summary>The amount of yaw change during the setpoint, in 1/10th of degrees</summary>
        public short YawChangeDdeg;

        /// <summary>The amount of yaw change during the setpoint, in degrees</summary>
        public float YawChangeDeg;

        /// <summary>The ending yaw of the setpoint, in 1/10th of degrees</summary>
        public int EndYawDdeg;

        /// <summary>The ending yaw of the setpoint, in degrees</summary>
        public float EndYawDeg;
    }

    /// <summary>
    /// Internal structure for the yaw player current setpoint.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct YawPlayerCurrentSetpoint
    {
        public UIntPtr start;
        public UIntPtr length;
        public YawSetpoint data;
    }

    /// <summary>
    /// Internal native structure for the yaw player.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct YawPlayerNative
    {
        public IntPtr ctrl;
        public YawPlayerCurrentSetpoint current_setpoint;
    }

    /// <summary>
    /// Structure representing a yaw control player that allows querying the
    /// yaw and yaw rate along the yaw control curve.
    /// </summary>
    public class YawPlayer : IDisposable
    {
        private const string LibName = "skybrush";
        private YawPlayerNative native;
        private YawControl control;
        private bool disposed = false;

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_player_init(
            ref YawPlayerNative player, ref YawControlNative ctrl);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sb_yaw_player_destroy(ref YawPlayerNative player);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_player_build_next_setpoint(
            ref YawPlayerNative player);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sb_yaw_player_get_current_setpoint(
            ref YawPlayerNative player);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_player_get_yaw_at(
            ref YawPlayerNative player, float t, out float result);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_player_get_yaw_rate_at(
            ref YawPlayerNative player, float t, out float result);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern SkybrushError sb_yaw_player_get_total_duration_msec(
            ref YawPlayerNative player, out uint duration);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte sb_yaw_player_has_more_setpoints(
            ref YawPlayerNative player);

        /// <summary>
        /// Initializes a new yaw player for the given yaw control object.
        /// </summary>
        /// <param name="control">The yaw control object to play.</param>
        public YawPlayer(YawControl control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            this.control = control;
            SkybrushError error = sb_yaw_player_init(ref native, ref control.GetNative());
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to initialize yaw player");
            }
        }

        /// <summary>
        /// Builds and advances to the next setpoint in the yaw control curve.
        /// </summary>
        public void BuildNextSetpoint()
        {
            ThrowIfDisposed();
            SkybrushError error = sb_yaw_player_build_next_setpoint(ref native);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to build next setpoint");
            }
        }

        /// <summary>
        /// Gets the current setpoint.
        /// </summary>
        public YawSetpoint GetCurrentSetpoint()
        {
            ThrowIfDisposed();
            IntPtr ptr = sb_yaw_player_get_current_setpoint(ref native);
            if (ptr == IntPtr.Zero)
            {
                throw new InvalidOperationException("No current setpoint available");
            }
            return Marshal.PtrToStructure<YawSetpoint>(ptr);
        }

        /// <summary>
        /// Gets the yaw at the specified time.
        /// </summary>
        /// <param name="time">The time in seconds.</param>
        /// <returns>The yaw value in degrees.</returns>
        public float GetYawAt(float time)
        {
            ThrowIfDisposed();
            SkybrushError error = sb_yaw_player_get_yaw_at(ref native, time, out float result);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to get yaw at time");
            }
            return result;
        }

        /// <summary>
        /// Gets the yaw rate at the specified time.
        /// </summary>
        /// <param name="time">The time in seconds.</param>
        /// <returns>The yaw rate in degrees per second.</returns>
        public float GetYawRateAt(float time)
        {
            ThrowIfDisposed();
            SkybrushError error = sb_yaw_player_get_yaw_rate_at(ref native, time, out float result);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to get yaw rate at time");
            }
            return result;
        }

        /// <summary>
        /// Gets the total duration of the yaw control curve in milliseconds.
        /// </summary>
        public uint GetTotalDurationMsec()
        {
            ThrowIfDisposed();
            SkybrushError error = sb_yaw_player_get_total_duration_msec(ref native, out uint duration);
            if (error != SkybrushError.Success)
            {
                throw new SkybrushException(error, "Failed to get total duration");
            }
            return duration;
        }

        /// <summary>
        /// Checks if there are more setpoints to process.
        /// </summary>
        public bool HasMoreSetpoints()
        {
            ThrowIfDisposed();
            return sb_yaw_player_has_more_setpoints(ref native) != 0;
        }

        /// <summary>
        /// Disposes the yaw player and releases all resources.
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
                sb_yaw_player_destroy(ref native);
                disposed = true;
                
                if (disposing)
                {
                    control = null;
                }
            }
        }

        ~YawPlayer()
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
