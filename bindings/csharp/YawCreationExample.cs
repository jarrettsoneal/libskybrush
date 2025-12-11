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

namespace SkybrushYawExample
{
    /// <summary>
    /// Example demonstrating how to create a .skyb file with yaw control.
    /// This shows the key part you need - adding yaw control to your existing show files.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string outputFile = args.Length > 0 ? args[0] : "show_with_yaw.skyb";

            Console.WriteLine("Skybrush Yaw Control - File Creation Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            // Example 1: Simple yaw control sequence
            CreateSimpleYawExample(outputFile);

            // Example 2: Complex choreography with spins
            CreateComplexYawExample("complex_yaw_show.skyb");

            // Example 3: Auto-yaw mode
            CreateAutoYawExample("auto_yaw_show.skyb");

            Console.WriteLine();
            Console.WriteLine("Examples complete! Check the generated .skyb files.");
        }

        /// <summary>
        /// Example 1: Create a simple .skyb file with basic yaw control
        /// </summary>
        static void CreateSimpleYawExample(string filename)
        {
            Console.WriteLine($"Creating simple yaw control example: {filename}");
            
            // Create a yaw control builder
            var yawBuilder = new YawControlBuilder();
            
            // Set initial yaw offset to 0 degrees (facing north)
            yawBuilder.YawOffsetDegrees = 0;

            // Define the yaw sequence:
            // 1. Hold at 0° for 2 seconds
            yawBuilder.HoldYaw(2000);
            
            // 2. Rotate 90° clockwise over 1 second
            yawBuilder.RotateClockwise(1000, 90);
            
            // 3. Hold at 90° for 2 seconds
            yawBuilder.HoldYaw(2000);
            
            // 4. Rotate 180° counter-clockwise over 2 seconds
            yawBuilder.RotateCounterClockwise(2000, 180);
            
            // 5. Hold at -90° for 1 second
            yawBuilder.HoldYaw(1000);
            
            // 6. Rotate back to 0° (90° clockwise) over 1 second
            yawBuilder.RotateClockwise(1000, 90);

            Console.WriteLine($"  Total yaw deltas: {yawBuilder.Count}");
            Console.WriteLine($"  Total duration: {yawBuilder.TotalDurationMs} ms");

            // Build the yaw control data
            byte[] yawData = yawBuilder.Build();
            Console.WriteLine($"  Yaw data size: {yawData.Length} bytes");

            // Write to .skyb file
            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                // Write file header
                SkybrushBinaryWriter.WriteFileHeader(writer);

                // Add a comment block (optional)
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Simple yaw control example");

                // TODO: Add your trajectory block here
                // SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);

                // TODO: Add your light program block here
                // SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);

                // Add the yaw control block
                SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
            }

            Console.WriteLine($"  File written: {filename}");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 2: Complex choreography with multiple spins
        /// </summary>
        static void CreateComplexYawExample(string filename)
        {
            Console.WriteLine($"Creating complex yaw choreography: {filename}");
            
            var yawBuilder = new YawControlBuilder();
            yawBuilder.YawOffsetDegrees = 0;

            // Choreography:
            // 1. Slow 360° spin (4 seconds)
            yawBuilder.Spin360(4000, clockwise: true);
            
            // 2. Quick double spin (2 seconds for 720°)
            yawBuilder.AddDelta(2000, 720);
            
            // 3. Hold position
            yawBuilder.HoldYaw(1000);
            
            // 4. Counter-clockwise spin
            yawBuilder.Spin360(3000, clockwise: false);
            
            // 5. Series of quick 90° turns
            for (int i = 0; i < 4; i++)
            {
                yawBuilder.RotateClockwise(500, 90);
                yawBuilder.HoldYaw(500);
            }

            Console.WriteLine($"  Total yaw deltas: {yawBuilder.Count}");
            Console.WriteLine($"  Total duration: {yawBuilder.TotalDurationMs} ms");

            byte[] yawData = yawBuilder.Build();

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                SkybrushBinaryWriter.WriteFileHeader(writer);
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Complex yaw choreography with spins");
                SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
            }

            Console.WriteLine($"  File written: {filename}");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 3: Using auto-yaw mode
        /// In auto-yaw mode, the drone automatically orients along its flight path
        /// </summary>
        static void CreateAutoYawExample(string filename)
        {
            Console.WriteLine($"Creating auto-yaw example: {filename}");
            
            var yawBuilder = new YawControlBuilder();
            
            // Enable auto-yaw mode
            yawBuilder.AutoYaw = true;
            yawBuilder.YawOffsetDegrees = 0;

            // In auto-yaw mode, you can still add manual yaw adjustments
            // The drone will orient along path + these adjustments
            
            // Example: slight rotation adjustments during auto-yaw
            yawBuilder.HoldYaw(2000);           // Auto-orient for 2s
            yawBuilder.AddDelta(1000, 45);      // Add 45° offset
            yawBuilder.HoldYaw(2000);           // Hold that offset
            yawBuilder.AddDelta(1000, -45);     // Remove offset
            yawBuilder.HoldYaw(2000);           // Back to pure auto-yaw

            Console.WriteLine($"  Auto-yaw enabled: {yawBuilder.AutoYaw}");
            Console.WriteLine($"  Total yaw deltas: {yawBuilder.Count}");
            Console.WriteLine($"  Total duration: {yawBuilder.TotalDurationMs} ms");

            byte[] yawData = yawBuilder.Build();

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                SkybrushBinaryWriter.WriteFileHeader(writer);
                SkybrushBinaryWriter.WriteCommentBlock(writer, "Auto-yaw mode example");
                SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
            }

            Console.WriteLine($"  File written: {filename}");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 4: How to integrate yaw control into your existing show file creation code
        /// </summary>
        static void IntegrationExample()
        {
            // This is pseudocode showing how to add yaw to your existing code:
            /*
            
            // Your existing show creation code:
            byte[] trajectoryData = CreateYourTrajectory();
            byte[] lightData = CreateYourLightProgram();
            
            // NEW: Create yaw control
            var yawBuilder = new YawControlBuilder();
            yawBuilder.YawOffsetDegrees = 0;
            yawBuilder.RotateClockwise(1000, 90);
            yawBuilder.HoldYaw(2000);
            // ... add more yaw commands as needed
            byte[] yawData = yawBuilder.Build();
            
            // Write the complete .skyb file
            using (var fs = new FileStream("myshow.skyb", FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // Write header
                SkybrushBinaryWriter.WriteFileHeader(writer);
                
                // Write your existing blocks
                SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
                SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);
                
                // NEW: Write yaw control block
                SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
            }
            
            */
        }
    }
}
