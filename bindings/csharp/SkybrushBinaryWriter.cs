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
    /// Each block type serves a specific purpose in defining the drone show.
    /// </summary>
    public enum SkybrushBlockType : byte
    {
        /// <summary>Invalid block type</summary>
        None = 0,
        
        /// <summary>
        /// Trajectory block - Defines the 3D flight path of the drone.
        /// Required for all flying drones.
        /// </summary>
        Trajectory = 1,
        
        /// <summary>
        /// Light program block - Defines LED colors and effects over time.
        /// Controls RGB LEDs and pyrotechnic channels.
        /// Use for pre-show ground lighting, flight effects, and post-show lighting.
        /// </summary>
        LightProgram = 2,
        
        /// <summary>
        /// Comment block - Contains UTF-8 text metadata.
        /// Use for show title, author, version, drone ID, or any documentation.
        /// </summary>
        Comment = 3,
        
        /// <summary>
        /// Return-to-home (RTH) plan block - Defines emergency landing procedures.
        /// Specifies where and how the drone should land in case of issues.
        /// Optional but recommended for safety.
        /// </summary>
        RthPlan = 4,
        
        /// <summary>
        /// Yaw control block - Defines rotation around vertical axis.
        /// Controls which direction the drone faces during the show.
        /// Supports spins, rotations, and auto-yaw (pointing in direction of travel).
        /// </summary>
        YawControl = 5
    }

    /// <summary>
    /// Helper class for writing Skybrush binary (.skyb) files.
    /// Provides methods to create complete drone show files with all features.
    /// 
    /// Typical show file structure:
    /// 1. File header (WriteFileHeader)
    /// 2. Comment block with metadata (WriteCommentBlock)
    /// 3. Trajectory block (WriteTrajectoryBlock)
    /// 4. Light program block (WriteLightProgramBlock)
    /// 5. Yaw control block (WriteYawControlBlock)
    /// 6. RTH plan block (WriteRthPlanBlock) - optional
    /// 
    /// For launch timing and ground lighting, see SkybrushFileFormat documentation.
    /// </summary>
    public static class SkybrushBinaryWriter
    {
        /// <summary>
        /// Writes a complete Skybrush binary file header.
        /// Call this at the start of your .skyb file before writing any blocks.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="version">File format version (1 or 2). Version 2 is recommended. Default is 2.</param>
        /// <param name="features">Feature flags for version 2 files. Usually 0 for no CRC32. Default is 0.</param>
        public static void WriteFileHeader(BinaryWriter writer, byte version = 2, byte features = 0)
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

            // Block length is stored as uint16 in the .skyb format (2 bytes, max 65535)
            if (data.Length > ushort.MaxValue)
            {
                throw new ArgumentException(
                    $"Block data too large: {data.Length} bytes (max {ushort.MaxValue} due to .skyb format)", 
                    nameof(data));
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
        /// 
        /// Yaw control defines how the drone rotates around its vertical axis.
        /// Use this for spins, rotations, and orientation control during the show.
        /// 
        /// This is a convenience method that calls WriteBlock with the YawControl block type.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="yawControlData">The yaw control data (from YawControlBuilder.Build())</param>
        /// <example>
        /// <code>
        /// var yawBuilder = new YawControlBuilder();
        /// yawBuilder.Spin360(4000, clockwise: true);
        /// byte[] yawData = yawBuilder.Build();
        /// SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
        /// </code>
        /// </example>
        public static void WriteYawControlBlock(BinaryWriter writer, byte[] yawControlData)
        {
            WriteBlock(writer, SkybrushBlockType.YawControl, yawControlData);
        }

        /// <summary>
        /// Writes a comment block to the Skybrush binary file.
        /// 
        /// Comments store metadata like show title, author, version, drone ID, etc.
        /// Multiple comment blocks can be added to a single file.
        /// Text is stored as UTF-8.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="comment">The comment text (UTF-8)</param>
        /// <example>
        /// <code>
        /// SkybrushBinaryWriter.WriteCommentBlock(writer, "Show: Fireworks 2025");
        /// SkybrushBinaryWriter.WriteCommentBlock(writer, "Drone ID: D001");
        /// </code>
        /// </example>
        public static void WriteCommentBlock(BinaryWriter writer, string comment)
        {
            byte[] data = Encoding.UTF8.GetBytes(comment ?? "");
            WriteBlock(writer, SkybrushBlockType.Comment, data);
        }

        /// <summary>
        /// Writes a trajectory block to the Skybrush binary file.
        /// 
        /// Trajectories define the 3D flight path of the drone.
        /// This is a required block for any flying drone.
        /// 
        /// For staggered launches (launching drones at different times):
        /// - Add a hold-position segment at ground level for the desired delay
        /// - Each drone can have a different delay duration
        /// - Light programs can run during the hold period (pre-show ground lighting)
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="trajectoryData">The trajectory data (from TrajectoryBuilder or your own builder)</param>
        /// <example>
        /// <code>
        /// // Drone 1: Immediate takeoff
        /// var traj1 = new TrajectoryBuilder();
        /// traj1.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
        /// traj1.AppendLine(new Vector3WithYaw(0, 0, 5, 0), 5000); // Takeoff
        /// 
        /// // Drone 2: Launch 2 seconds later
        /// var traj2 = new TrajectoryBuilder();
        /// traj2.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
        /// traj2.HoldPosition(2000);  // Wait 2 seconds on ground
        /// traj2.AppendLine(new Vector3WithYaw(0, 0, 5, 0), 5000); // Takeoff
        /// </code>
        /// </example>
        public static void WriteTrajectoryBlock(BinaryWriter writer, byte[] trajectoryData)
        {
            WriteBlock(writer, SkybrushBlockType.Trajectory, trajectoryData);
        }

        /// <summary>
        /// Writes a light program block to the Skybrush binary file.
        /// 
        /// Light programs control RGB LED colors and pyrotechnic effects over time.
        /// 
        /// For pre-show ground lighting:
        /// - Start light program at t=0 with desired colors
        /// - Light program runs while drone is on ground (before trajectory takeoff)
        /// 
        /// For post-show ground lighting:
        /// - Continue light program beyond landing time
        /// - Lights can fade out or show final pattern after drone lands
        /// 
        /// For pyrotechnics:
        /// - Use pyro channel commands in the light program
        /// - Coordinate with trajectory altitude and timing
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="lightProgramData">The light program data (bytecode format)</param>
        /// <example>
        /// <code>
        /// // Example conceptual structure (actual bytecode creation requires light program builder):
        /// // t=0ms: Set RED (pre-show ground lighting)
        /// // t=2000ms: Fade to GREEN
        /// // t=5000ms: Start flight colors (trajectory begins here)
        /// // ... flight colors ...
        /// // t=30000ms: Landing colors
        /// // t=35000ms: Fade out (post-show ground lighting)
        /// </code>
        /// </example>
        public static void WriteLightProgramBlock(BinaryWriter writer, byte[] lightProgramData)
        {
            WriteBlock(writer, SkybrushBlockType.LightProgram, lightProgramData);
        }

        /// <summary>
        /// Writes a return-to-home (RTH) plan block to the Skybrush binary file.
        /// 
        /// RTH plans define emergency landing procedures.
        /// Specifies where the drone should go and how it should land if issues occur.
        /// 
        /// Optional but recommended for safety in multi-drone shows.
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="rthPlanData">The RTH plan data</param>
        /// <example>
        /// <code>
        /// // RTH plan typically includes:
        /// // - Home position coordinates
        /// // - Landing pattern (direct descent, go-to-home-then-land, etc.)
        /// // - Altitude constraints
        /// // - Timing parameters
        /// </code>
        /// </example>
        public static void WriteRthPlanBlock(BinaryWriter writer, byte[] rthPlanData)
        {
            WriteBlock(writer, SkybrushBlockType.RthPlan, rthPlanData);
        }
    }
}
