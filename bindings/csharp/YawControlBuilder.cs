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
using System.Collections.Generic;
using System.IO;

namespace Skybrush
{
    /// <summary>
    /// Represents a yaw delta - a change in yaw over a specified duration.
    /// </summary>
    public struct YawDelta
    {
        /// <summary>Duration of this yaw change in milliseconds</summary>
        public ushort DurationMs;
        
        /// <summary>Amount of yaw change in degrees (can be negative for counter-clockwise rotation)</summary>
        public float YawChangeDeg;

        public YawDelta(ushort durationMs, float yawChangeDeg)
        {
            DurationMs = durationMs;
            YawChangeDeg = yawChangeDeg;
        }
    }

    /// <summary>
    /// Builder class for creating yaw control data programmatically.
    /// Use this to add yaw control to your Skybrush show files.
    /// </summary>
    public class YawControlBuilder
    {
        private readonly List<YawDelta> deltas = new List<YawDelta>();
        private bool autoYaw = false;
        private float yawOffsetDeg = 0;

        /// <summary>
        /// Gets or sets whether auto-yaw mode is enabled.
        /// In auto-yaw mode, the drone automatically orients itself along its flight path.
        /// </summary>
        public bool AutoYaw
        {
            get => autoYaw;
            set => autoYaw = value;
        }

        /// <summary>
        /// Gets or sets the initial yaw offset in degrees.
        /// This is the starting yaw angle for the entire yaw control sequence.
        /// </summary>
        public float YawOffsetDegrees
        {
            get => yawOffsetDeg;
            set => yawOffsetDeg = value;
        }

        /// <summary>
        /// Adds a yaw change delta to the yaw control sequence.
        /// </summary>
        /// <param name="durationMs">How long this yaw change takes, in milliseconds (max 65535)</param>
        /// <param name="yawChangeDeg">How much the yaw changes in degrees (positive = clockwise, negative = counter-clockwise)</param>
        public void AddDelta(ushort durationMs, float yawChangeDeg)
        {
            deltas.Add(new YawDelta(durationMs, yawChangeDeg));
        }

        /// <summary>
        /// Holds the current yaw (no change) for the specified duration.
        /// </summary>
        /// <param name="durationMs">Duration to hold yaw in milliseconds</param>
        public void HoldYaw(ushort durationMs)
        {
            AddDelta(durationMs, 0);
        }

        /// <summary>
        /// Rotates the drone clockwise by the specified angle.
        /// </summary>
        /// <param name="durationMs">Duration of the rotation in milliseconds</param>
        /// <param name="angleDeg">Angle to rotate in degrees (positive values)</param>
        public void RotateClockwise(ushort durationMs, float angleDeg)
        {
            AddDelta(durationMs, Math.Abs(angleDeg));
        }

        /// <summary>
        /// Rotates the drone counter-clockwise by the specified angle.
        /// </summary>
        /// <param name="durationMs">Duration of the rotation in milliseconds</param>
        /// <param name="angleDeg">Angle to rotate in degrees (positive values)</param>
        public void RotateCounterClockwise(ushort durationMs, float angleDeg)
        {
            AddDelta(durationMs, -Math.Abs(angleDeg));
        }

        /// <summary>
        /// Performs a full 360-degree rotation.
        /// </summary>
        /// <param name="durationMs">Duration of the full rotation in milliseconds</param>
        /// <param name="clockwise">True for clockwise, false for counter-clockwise</param>
        public void Spin360(ushort durationMs, bool clockwise = true)
        {
            AddDelta(durationMs, clockwise ? 360 : -360);
        }

        /// <summary>
        /// Gets the total number of yaw deltas.
        /// </summary>
        public int Count => deltas.Count;

        /// <summary>
        /// Gets the total duration of all yaw deltas in milliseconds.
        /// </summary>
        public uint TotalDurationMs
        {
            get
            {
                uint total = 0;
                foreach (var delta in deltas)
                {
                    total += delta.DurationMs;
                }
                return total;
            }
        }

        /// <summary>
        /// Builds the binary data for the yaw control block in Skybrush format.
        /// This creates the raw bytes that can be written to a .skyb file.
        /// </summary>
        /// <returns>Byte array containing the yaw control data</returns>
        public byte[] Build()
        {
            // Calculate total size: 3-byte header + 4 bytes per delta
            int size = 3 + (deltas.Count * 4);
            byte[] data = new byte[size];

            using (var ms = new MemoryStream(data))
            using (var writer = new BinaryWriter(ms))
            {
                // Write header
                byte flags = (byte)(autoYaw ? 0x01 : 0x00);
                writer.Write(flags);

                // Write yaw offset in decidegrees (1/10th of a degree)
                short yawOffsetDdeg = (short)Math.Round(yawOffsetDeg * 10);
                writer.Write(yawOffsetDdeg);

                // Write deltas
                foreach (var delta in deltas)
                {
                    writer.Write(delta.DurationMs);
                    
                    // Convert yaw change from degrees to decidegrees
                    short yawChangeDdeg = (short)Math.Round(delta.YawChangeDeg * 10);
                    writer.Write(yawChangeDdeg);
                }
            }

            return data;
        }

        /// <summary>
        /// Clears all yaw deltas and resets to initial state.
        /// </summary>
        public void Clear()
        {
            deltas.Clear();
            autoYaw = false;
            yawOffsetDeg = 0;
        }
    }
}
