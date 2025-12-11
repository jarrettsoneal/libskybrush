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
using System.IO;
using System.Text;

namespace Skybrush
{
    /// <summary>
    /// Known block types in the Skybrush binary file format.
    /// </summary>
    public enum SkybrushBlockType : byte
    {
        /// <summary>Invalid block type</summary>
        None = 0,
        /// <summary>Block that contains a trajectory</summary>
        Trajectory = 1,
        /// <summary>Block that contains a light program</summary>
        LightProgram = 2,
        /// <summary>Comment block that contains arbitrary text</summary>
        Comment = 3,
        /// <summary>Block that contains a return-to-home plan</summary>
        RthPlan = 4,
        /// <summary>Block that contains yaw control setpoints</summary>
        YawControl = 5
    }

    /// <summary>
    /// Helper class for writing Skybrush binary (.skyb) file blocks.
    /// Use this to add yaw control (or other blocks) to your existing .skyb file creation code.
    /// </summary>
    public static class SkybrushBinaryWriter
    {
        /// <summary>
        /// Writes a complete Skybrush binary file header.
        /// Call this at the start of your .skyb file.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="version">File format version (1 or 2, default is 2)</param>
        public static void WriteFileHeader(BinaryWriter writer, byte version = 2)
        {
            if (version != 1 && version != 2)
            {
                throw new ArgumentException("Version must be 1 or 2", nameof(version));
            }

            // Write magic header "skyb"
            writer.Write(Encoding.ASCII.GetBytes("skyb"));
            
            // Write version
            writer.Write(version);
            
            // Write feature bits (version 2 only)
            if (version == 2)
            {
                byte features = 0; // No CRC32 for simplicity
                writer.Write(features);
            }
        }

        /// <summary>
        /// Writes a block to the Skybrush binary file.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="blockType">The type of block to write</param>
        /// <param name="data">The block data</param>
        public static void WriteBlock(BinaryWriter writer, SkybrushBlockType blockType, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length > ushort.MaxValue)
            {
                throw new ArgumentException($"Block data too large: {data.Length} bytes (max {ushort.MaxValue})", nameof(data));
            }

            // Write block type
            writer.Write((byte)blockType);
            
            // Write block length (2 bytes, little-endian)
            writer.Write((ushort)data.Length);
            
            // Write block data
            writer.Write(data);
        }

        /// <summary>
        /// Writes a yaw control block to the Skybrush binary file.
        /// This is a convenience method that calls WriteBlock with the YawControl block type.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="yawControlData">The yaw control data (from YawControlBuilder.Build())</param>
        public static void WriteYawControlBlock(BinaryWriter writer, byte[] yawControlData)
        {
            WriteBlock(writer, SkybrushBlockType.YawControl, yawControlData);
        }

        /// <summary>
        /// Writes a comment block to the Skybrush binary file.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="comment">The comment text</param>
        public static void WriteCommentBlock(BinaryWriter writer, string comment)
        {
            byte[] data = Encoding.UTF8.GetBytes(comment ?? "");
            WriteBlock(writer, SkybrushBlockType.Comment, data);
        }

        /// <summary>
        /// Writes a trajectory block to the Skybrush binary file.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="trajectoryData">The trajectory data</param>
        public static void WriteTrajectoryBlock(BinaryWriter writer, byte[] trajectoryData)
        {
            WriteBlock(writer, SkybrushBlockType.Trajectory, trajectoryData);
        }

        /// <summary>
        /// Writes a light program block to the Skybrush binary file.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="lightProgramData">The light program data</param>
        public static void WriteLightProgramBlock(BinaryWriter writer, byte[] lightProgramData)
        {
            WriteBlock(writer, SkybrushBlockType.LightProgram, lightProgramData);
        }
    }
}
