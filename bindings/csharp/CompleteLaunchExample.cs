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
using Skybrush;

namespace SkybrushLaunchExample
{
    /// <summary>
    /// Complete examples demonstrating launch timing, ground lighting, and all .skyb file features.
    /// Shows how to create shows with pre-show ground lighting, staggered launches, and post-show effects.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Skybrush Complete Show Creation Examples");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // Example 1: Pre-show ground lighting
            CreatePreShowLightingExample("show_pre_lighting.skyb");

            // Example 2: Staggered launch (drones launching at different times)
            CreateStaggeredLaunchExample();

            // Example 3: Post-show ground lighting
            CreatePostShowLightingExample("show_post_lighting.skyb");

            // Example 4: Complete multi-block show file
            CreateCompleteShowExample("show_complete.skyb");

            Console.WriteLine();
            Console.WriteLine("All examples complete!");
            Console.WriteLine();
            Console.WriteLine("See SkybrushFileFormat.cs for complete format documentation.");
        }

        /// <summary>
        /// Example 1: Pre-show ground lighting
        /// Demonstrates how to show LED colors on the ground before the drone takes off
        /// </summary>
        static void CreatePreShowLightingExample(string filename)
        {
            Console.WriteLine($"Example 1: Pre-show ground lighting -> {filename}");
            
            // Create trajectory that holds on ground, then takes off
            var trajBuilder = new TrajectoryBuilder(scale: 10, useYaw: false);
            trajBuilder.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
            
            // Hold on ground for 5 seconds (pre-show period)
            trajBuilder.HoldPosition(5000);
            
            // Then take off
            trajBuilder.AppendLine(new Vector3WithYaw(0, 0, 5000, 0), 5000); // Rise to 5m
            trajBuilder.HoldPosition(10000); // Hover
            trajBuilder.AppendLine(new Vector3WithYaw(0, 0, 0, 0), 5000); // Land
            
            byte[] trajectoryData = GetTrajectoryBytes(trajBuilder);

            // Yaw control (optional - pointing north the whole time)
            var yawBuilder = new YawControlBuilder();
            yawBuilder.YawOffsetDegrees = 0;
            // Note: For complete implementation, calculate actual trajectory duration
            // For now, using a safe maximum value
            yawBuilder.HoldYaw(25000); // Hold for 25 seconds (covers full trajectory)
            byte[] yawData = yawBuilder.Build();

            // NOTE: Light program would be created here
            // Conceptual structure:
            // t=0ms:     Set RED (ground lighting starts immediately)
            // t=1000ms:  Fade to GREEN
            // t=3000ms:  Fade to BLUE  
            // t=5000ms:  Set WHITE (takeoff begins here)
            // t=10000ms: Flight colors...
            // t=20000ms: Landing colors
            // t=25000ms: OFF
            
            using (var fs = new FileStream(filename, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                SkybrushBinaryWriter.WriteFileHeader(writer);
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Pre-show ground lighting example");
                SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
                // SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightProgramData);
                SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
            }

            Console.WriteLine("  Pre-show period: 0-5 seconds (ground lighting)");
            Console.WriteLine("  Takeoff: 5 seconds");
            Console.WriteLine("  Total duration: 25 seconds");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 2: Staggered launch
        /// Creates multiple .skyb files for drones that launch at different times
        /// </summary>
        static void CreateStaggeredLaunchExample()
        {
            Console.WriteLine("Example 2: Staggered launch (3 drones)");
            
            int numDrones = 3;
            int launchDelayMs = 2000; // 2 seconds between each drone

            for (int i = 0; i < numDrones; i++)
            {
                string filename = $"drone_{i+1}_staggered.skyb";
                int delayMs = i * launchDelayMs;

                // Create trajectory with initial hold period
                var trajBuilder = new TrajectoryBuilder(scale: 10, useYaw: false);
                trajBuilder.SetStartPosition(new Vector3WithYaw(i * 2000, 0, 0, 0)); // 2m spacing
                
                // Each drone waits a different amount of time
                if (delayMs > 0)
                {
                    trajBuilder.HoldPosition((uint)delayMs);
                }
                
                // Then all drones follow the same flight path
                trajBuilder.AppendLine(new Vector3WithYaw(i * 2000, 0, 5000, 0), 5000); // Takeoff
                trajBuilder.HoldPosition(10000); // Hover
                trajBuilder.AppendLine(new Vector3WithYaw(i * 2000, 0, 0, 0), 5000); // Land

                byte[] trajectoryData = GetTrajectoryBytes(trajBuilder);

                // Yaw control - each drone can have different rotation
                var yawBuilder = new YawControlBuilder();
                yawBuilder.YawOffsetDegrees = 0;
                
                if (delayMs > 0)
                {
                    yawBuilder.HoldYaw((ushort)delayMs); // Hold during wait
                }
                yawBuilder.Spin360(4000, clockwise: i % 2 == 0); // Alternating spin direction
                
                byte[] yawData = yawBuilder.Build();

                using (var fs = new FileStream(filename, FileMode.Create))
                using (var writer = new BinaryWriter(fs))
                {
                    SkybrushBinaryWriter.WriteFileHeader(writer);
                    SkybrushBinaryWriter.WriteCommentBlock(writer, $"Drone {i+1} - Launch delay: {delayMs}ms");
                    SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
                    SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
                }

                Console.WriteLine($"  Drone {i+1}: Launch delay = {delayMs}ms -> {filename}");
            }
            
            Console.WriteLine("  Result: Drones launch 2 seconds apart");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 3: Post-show ground lighting
        /// Demonstrates how to continue showing LED colors after landing
        /// </summary>
        static void CreatePostShowLightingExample(string filename)
        {
            Console.WriteLine($"Example 3: Post-show ground lighting -> {filename}");
            
            // Create trajectory
            var trajBuilder = new TrajectoryBuilder(scale: 10, useYaw: false);
            trajBuilder.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
            
            // Flight path
            trajBuilder.AppendLine(new Vector3WithYaw(0, 0, 5000, 0), 5000); // Takeoff
            trajBuilder.HoldPosition(10000); // Hover for 10s
            trajBuilder.AppendLine(new Vector3WithYaw(0, 0, 0, 0), 5000); // Land at t=20s
            
            // Drone is now on ground - trajectory ends but light program continues
            
            byte[] trajectoryData = GetTrajectoryBytes(trajBuilder);

            // NOTE: Light program would continue beyond landing time
            // Conceptual structure:
            // t=0-5000ms:    Takeoff colors
            // t=5000-15000ms: Flight colors
            // t=15000-20000ms: Landing colors
            // t=20000ms:      On ground (trajectory complete)
            // t=20000-25000ms: Post-show ground pattern (RED pulsing)
            // t=25000-30000ms: Fade to OFF
            
            using (var fs = new FileStream(filename, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                SkybrushBinaryWriter.WriteFileHeader(writer);
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Post-show ground lighting example");
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Light program runs 10s beyond landing");
                SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
                // SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightProgramData);
            }

            Console.WriteLine("  Flight duration: 20 seconds");
            Console.WriteLine("  Post-show period: 20-30 seconds (ground lighting)");
            Console.WriteLine("  Light program total: 30 seconds");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 4: Complete show file with all block types
        /// Demonstrates a full-featured .skyb file
        /// </summary>
        static void CreateCompleteShowExample(string filename)
        {
            Console.WriteLine($"Example 4: Complete show with all features -> {filename}");
            
            // 1. Trajectory with pre-show hold
            var trajBuilder = new TrajectoryBuilder(scale: 10, useYaw: true);
            trajBuilder.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
            trajBuilder.HoldPosition(3000); // 3s pre-show ground lighting
            trajBuilder.AppendLine(new Vector3WithYaw(0, 0, 5000, 0), 5000); // Takeoff
            trajBuilder.AppendLine(new Vector3WithYaw(5000, 0, 5000, 0), 5000); // Move right
            trajBuilder.AppendLine(new Vector3WithYaw(5000, 5000, 5000, 90), 5000); // Move forward, turn
            trajBuilder.AppendLine(new Vector3WithYaw(0, 5000, 5000, 180), 5000); // Move left
            trajBuilder.AppendLine(new Vector3WithYaw(0, 0, 5000, 270), 5000); // Return to start
            trajBuilder.AppendLine(new Vector3WithYaw(0, 0, 0, 0), 5000); // Land
            
            byte[] trajectoryData = GetTrajectoryBytes(trajBuilder);

            // 2. Yaw control with spins
            var yawBuilder = new YawControlBuilder();
            yawBuilder.YawOffsetDegrees = 0;
            yawBuilder.HoldYaw(3000); // Hold during pre-show
            yawBuilder.HoldYaw(5000); // Hold during takeoff
            yawBuilder.Spin360(5000, clockwise: true); // Spin while moving
            yawBuilder.HoldYaw(10000); // Hold during rest of flight
            yawBuilder.RotateClockwise(5000, 90); // Turn during landing
            
            byte[] yawData = yawBuilder.Build();

            using (var fs = new FileStream(filename, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // File header (v2 format)
                SkybrushBinaryWriter.WriteFileHeader(writer, version: 2, features: 0);
                
                // Metadata comments
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Complete Show Example");
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Author: Skybrush C# Bindings");
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Version: 1.0");
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Drone ID: DEMO-001");
                
                // Show data blocks
                SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
                // SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightProgramData); // Would add lights here
                SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
                // SkybrushBinaryWriter.WriteRthPlanBlock(writer, rthPlanData); // Would add RTH here
            }

            Console.WriteLine("  Blocks written:");
            Console.WriteLine("    - File header (v2)");
            Console.WriteLine("    - 4 comment blocks (metadata)");
            Console.WriteLine("    - Trajectory block (square pattern with pre-show hold)");
            Console.WriteLine("    - Yaw control block (with spin)");
            Console.WriteLine("  Duration: 33 seconds (3s pre-show + 30s flight)");
            Console.WriteLine();
        }

        /// <summary>
        /// Helper to get trajectory bytes from builder.
        /// NOTE: This is a placeholder implementation for demonstration purposes.
        /// 
        /// In a real implementation, you would either:
        /// 1. Use the native library's trajectory builder and extract bytes
        /// 2. Implement complete trajectory binary encoding in C#
        /// 3. Use your own trajectory generation code
        /// 
        /// The trajectory binary format is complex and requires proper encoding
        /// of segments, coordinates, and timing data.
        /// </summary>
        static byte[] GetTrajectoryBytes(TrajectoryBuilder builder)
        {
            // PLACEHOLDER: This returns a minimal valid trajectory header
            // Real implementation needed to extract actual trajectory data
            
            Console.WriteLine("  WARNING: Using placeholder trajectory data");
            Console.WriteLine("  For production use, implement proper trajectory byte extraction");
            
            // Minimal valid trajectory: scale=10, start at origin
            return new byte[] { 10, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
    }
}
