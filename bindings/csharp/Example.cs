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

namespace SkybrushExample
{
    /// <summary>
    /// Example program demonstrating how to use the Skybrush C# bindings to control yaw in a show.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SkybrushExample <path-to-skybrush-file.skyb>");
                Console.WriteLine();
                Console.WriteLine("This example demonstrates yaw control in Skybrush shows using C#.");
                return;
            }

            string filePath = args[0];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }

            try
            {
                // Load the Skybrush binary file
                byte[] fileData = File.ReadAllBytes(filePath);
                Console.WriteLine($"Loaded file: {filePath} ({fileData.Length} bytes)");
                Console.WriteLine();

                // Create a yaw control object from the file
                using (var yawControl = new YawControl(fileData))
                {
                    Console.WriteLine("=== Yaw Control Information ===");
                    Console.WriteLine($"Auto Yaw Mode: {yawControl.AutoYaw}");
                    Console.WriteLine($"Yaw Offset: {yawControl.YawOffsetDegrees:F2}°");
                    Console.WriteLine($"Number of Deltas: {yawControl.NumberOfDeltas}");
                    Console.WriteLine($"Is Empty: {yawControl.IsEmpty()}");
                    Console.WriteLine();

                    if (yawControl.IsEmpty())
                    {
                        Console.WriteLine("The yaw control data is empty. No yaw information available.");
                        return;
                    }

                    // Create a player to query yaw values
                    using (var player = new YawPlayer(yawControl))
                    {
                        // Get and display total duration
                        uint durationMs = player.GetTotalDurationMsec();
                        float durationSec = durationMs / 1000.0f;
                        Console.WriteLine($"Total Duration: {durationMs} ms ({durationSec:F2} seconds)");
                        Console.WriteLine();

                        // Display yaw setpoints
                        Console.WriteLine("=== Yaw Setpoints ===");
                        int setpointCount = 0;
                        while (player.HasMoreSetpoints() && setpointCount < 10) // Limit to first 10 for brevity
                        {
                            player.BuildNextSetpoint();
                            YawSetpoint setpoint = player.GetCurrentSetpoint();
                            
                            Console.WriteLine($"Setpoint #{setpointCount + 1}:");
                            Console.WriteLine($"  Time Range: {setpoint.StartTimeSec:F3}s - {setpoint.EndTimeSec:F3}s");
                            Console.WriteLine($"  Duration: {setpoint.DurationSec:F3}s");
                            Console.WriteLine($"  Yaw Range: {setpoint.StartYawDeg:F2}° → {setpoint.EndYawDeg:F2}°");
                            Console.WriteLine($"  Yaw Change: {setpoint.YawChangeDeg:F2}°");
                            Console.WriteLine();
                            
                            setpointCount++;
                        }

                        if (player.HasMoreSetpoints())
                        {
                            Console.WriteLine("... (additional setpoints omitted)");
                            Console.WriteLine();
                        }

                        // Sample yaw values at regular intervals
                        Console.WriteLine("=== Yaw Sampling ===");
                        float sampleInterval = Math.Max(0.5f, durationSec / 20); // Sample ~20 times
                        Console.WriteLine($"Sampling yaw every {sampleInterval:F2} seconds:");
                        Console.WriteLine();
                        Console.WriteLine("  Time (s)  |  Yaw (°)  |  Rate (°/s)");
                        Console.WriteLine("------------|-----------|-------------");

                        for (float t = 0; t <= durationSec; t += sampleInterval)
                        {
                            try
                            {
                                float yaw = player.GetYawAt(t);
                                float yawRate = player.GetYawRateAt(t);
                                Console.WriteLine($"  {t,8:F2}  |  {yaw,7:F2}  |  {yawRate,9:F2}");
                            }
                            catch (SkybrushException ex)
                            {
                                Console.WriteLine($"  {t,8:F2}  |  Error: {ex.ErrorCode}");
                            }
                        }

                        Console.WriteLine();
                        Console.WriteLine("=== Example Complete ===");
                    }
                }
            }
            catch (SkybrushException ex)
            {
                Console.WriteLine($"Skybrush Error: {ex.Message}");
                Console.WriteLine($"Error Code: {ex.ErrorCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
