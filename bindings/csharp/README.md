# Skybrush C# Bindings - Yaw Control

This directory contains C# bindings for creating yaw control in Skybrush drone shows (.skyb files).

## Overview

The C# bindings allow you to:
- **Create yaw control data programmatically** for your .skyb show files
- Add yaw control blocks to existing show files
- Load and read yaw control data from Skybrush binary files
- Query yaw values at specific timestamps
- Calculate yaw rates
- Iterate through yaw setpoints

## Quick Start - Adding Yaw Control to Your Show

If you already know how to create .skyb files and just want to add yaw control, here's the essentials:

### Step 1: Create Yaw Control Data

```csharp
using Skybrush;

// Create a yaw control builder
var yawBuilder = new YawControlBuilder();

// Set initial yaw offset (starting angle in degrees)
yawBuilder.YawOffsetDegrees = 0;

// Add yaw commands (all times in milliseconds)
yawBuilder.HoldYaw(2000);                    // Hold at 0° for 2 seconds
yawBuilder.RotateClockwise(1000, 90);        // Rotate 90° clockwise in 1 second
yawBuilder.HoldYaw(2000);                    // Hold at 90° for 2 seconds
yawBuilder.RotateCounterClockwise(2000, 180); // Rotate 180° CCW in 2 seconds
yawBuilder.Spin360(4000, clockwise: true);   // Full 360° spin in 4 seconds

// Build the binary data
byte[] yawData = yawBuilder.Build();
```

### Step 2: Add to Your .skyb File

```csharp
using System.IO;
using Skybrush;

// In your existing show file creation code:
using (var fs = new FileStream("myshow.skyb", FileMode.Create))
using (var writer = new BinaryWriter(fs))
{
    // Write file header
    SkybrushBinaryWriter.WriteFileHeader(writer);
    
    // Write your existing blocks (trajectory, lights, etc.)
    SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
    SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);
    
    // Add yaw control block
    SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
}
```

That's it! Your .skyb file now includes yaw control.

## Files You Need

For **creating** yaw control (most common use case):
- `YawControlBuilder.cs` - Build yaw control data
- `SkybrushBinaryWriter.cs` - Write .skyb file blocks
- `YawCreationExample.cs` - Complete examples

For **reading** yaw control (if needed):
- `SkybrushError.cs` - Error handling
- `YawControl.cs` - Read yaw data
- `YawPlayer.cs` - Query yaw values
- `Example.cs` - Reading examples

## Requirements

- .NET Framework 4.6.1 or higher / .NET Core 2.0 or higher / .NET 5.0 or higher
- **No native library required** for creating yaw control (pure C#)
- libskybrush native library only needed for reading/querying existing .skyb files

## Detailed Examples

### Example 1: Simple Yaw Sequence

```csharp
using Skybrush;

var yawBuilder = new YawControlBuilder();
yawBuilder.YawOffsetDegrees = 0;

// Define a simple sequence
yawBuilder.HoldYaw(2000);              // Hold for 2s
yawBuilder.RotateClockwise(1000, 90);  // Turn 90° in 1s
yawBuilder.HoldYaw(2000);              // Hold for 2s
yawBuilder.RotateClockwise(1000, 90);  // Turn another 90°

byte[] yawData = yawBuilder.Build();
Console.WriteLine($"Yaw data: {yawData.Length} bytes");
```

### Example 2: Spin Choreography

```csharp
var yawBuilder = new YawControlBuilder();
yawBuilder.YawOffsetDegrees = 0;

// Create a spinning choreography
yawBuilder.Spin360(4000, clockwise: true);   // Slow spin
yawBuilder.HoldYaw(1000);                     // Brief pause
yawBuilder.Spin360(2000, clockwise: false);  // Fast reverse spin
yawBuilder.AddDelta(1000, 720);              // Double spin!

byte[] yawData = yawBuilder.Build();
```

### Example 3: Auto-Yaw Mode

```csharp
var yawBuilder = new YawControlBuilder();

// Enable auto-yaw (drone orients along flight path automatically)
yawBuilder.AutoYaw = true;
yawBuilder.YawOffsetDegrees = 0;

// You can still add manual adjustments on top of auto-yaw
yawBuilder.HoldYaw(2000);        // Auto-orient for 2s
yawBuilder.AddDelta(1000, 45);   // Add 45° offset
yawBuilder.HoldYaw(2000);        // Hold offset
yawBuilder.AddDelta(1000, -45);  // Remove offset

byte[] yawData = yawBuilder.Build();
```

### Example 4: Complete .skyb File Creation

```csharp
using System.IO;
using Skybrush;

// Create yaw control
var yawBuilder = new YawControlBuilder();
yawBuilder.YawOffsetDegrees = 0;
yawBuilder.RotateClockwise(2000, 180);
yawBuilder.HoldYaw(1000);
byte[] yawData = yawBuilder.Build();

// Write complete show file
using (var fs = new FileStream("myshow.skyb", FileMode.Create))
using (var writer = new BinaryWriter(fs))
{
    // File header
    SkybrushBinaryWriter.WriteFileHeader(writer);
    
    // Optional comment
    SkybrushBinaryWriter.WriteCommentBlock(writer, "My awesome show with yaw!");
    
    // Your trajectory data (you already know how to create this)
    byte[] trajectoryData = GetYourTrajectoryData();
    SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
    
    // Your light program data
    byte[] lightData = GetYourLightProgramData();
    SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);
    
    // Yaw control data
    SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
}

Console.WriteLine("Show file created with yaw control!");
```

## YawControlBuilder API Reference

### Properties

- `bool AutoYaw` - Enable/disable auto-yaw mode (drone orients along flight path)
- `float YawOffsetDegrees` - Starting yaw angle in degrees
- `int Count` - Number of yaw deltas
- `uint TotalDurationMs` - Total duration in milliseconds

### Methods

- `AddDelta(ushort durationMs, float yawChangeDeg)` - Add a yaw change
  - `durationMs`: How long the change takes (0-65535 milliseconds)
  - `yawChangeDeg`: How much to rotate (positive = clockwise, negative = counter-clockwise)

- `HoldYaw(ushort durationMs)` - Hold current yaw (no rotation)

- `RotateClockwise(ushort durationMs, float angleDeg)` - Rotate clockwise

- `RotateCounterClockwise(ushort durationMs, float angleDeg)` - Rotate counter-clockwise

- `Spin360(ushort durationMs, bool clockwise = true)` - Full 360° rotation

- `byte[] Build()` - Build the binary yaw control data

- `Clear()` - Clear all deltas and reset to initial state

## Understanding Yaw Control

### What is Yaw?

Yaw is the rotation of the drone around its vertical axis (like a compass heading):
- **0°** = North (or your reference direction)
- **90°** = East (clockwise)
- **180°** = South
- **270°** = West (or -90° counter-clockwise)

### Yaw Deltas

Yaw control in Skybrush uses "deltas" - changes in yaw over time:
- Each delta specifies a duration and a yaw change
- Deltas are executed sequentially
- Positive values rotate clockwise, negative rotate counter-clockwise

Example:
```csharp
// Start at 0°, rotate 90° clockwise in 2 seconds, then hold for 3 seconds
yawBuilder.YawOffsetDegrees = 0;        // Starting angle
yawBuilder.AddDelta(2000, 90);          // Rotate to 90° over 2s
yawBuilder.AddDelta(3000, 0);           // Hold at 90° for 3s
```

### Auto-Yaw Mode

When `AutoYaw = true`, the drone automatically points in the direction of travel.
Manual yaw deltas are added on top of this automatic orientation.

## Binary Format Reference

For advanced users, the yaw control binary format is:

**Header (3 bytes):**
- Byte 0: Flags (bit 0 = auto_yaw)
- Bytes 1-2: Yaw offset in decidegrees (int16, little-endian)

**Delta (4 bytes each):**
- Bytes 0-1: Duration in milliseconds (uint16, little-endian)
- Bytes 2-3: Yaw change in decidegrees (int16, little-endian)

Note: 1 decidegree = 0.1 degrees

## Reading Yaw Control (Advanced)

If you need to read and query yaw values from existing .skyb files:

```csharp
using Skybrush;
using System;
using System.IO;

// Requires libskybrush native library!

// Load a Skybrush binary file
byte[] fileData = File.ReadAllBytes("show.skyb");

// Create a yaw control object from the file
using (var yawControl = new YawControl(fileData))
{
    Console.WriteLine($"Auto Yaw: {yawControl.AutoYaw}");
    Console.WriteLine($"Yaw Offset: {yawControl.YawOffsetDegrees}°");
    
    // Create a player to query yaw values
    using (var player = new YawPlayer(yawControl))
    {
        // Query yaw at specific time
        float yaw = player.GetYawAt(5.0f);
        Console.WriteLine($"Yaw at 5s: {yaw}°");
        
        // Query yaw rate
        float yawRate = player.GetYawRateAt(5.0f);
        Console.WriteLine($"Yaw rate at 5s: {yawRate}°/s");
    }
}
```

## Building and Running Examples

### Create Yaw Control Files

```bash
# Compile the creation example (no native library needed)
dotnet build YawCreationExample.cs YawControlBuilder.cs SkybrushBinaryWriter.cs

# Run it
dotnet run YawCreationExample.cs

# This will create: show_with_yaw.skyb, complex_yaw_show.skyb, auto_yaw_show.skyb
```

### Read Existing Yaw Control Files

```bash
# Requires libskybrush native library to be installed/built first
dotnet build SkybrushExample.csproj

# Run with a .skyb file
dotnet run --project SkybrushExample.csproj path/to/your/show.skyb
```

## Summary

**To add yaw control to your shows:**

1. Copy `YawControlBuilder.cs` and `SkybrushBinaryWriter.cs` to your project
2. Create yaw sequences with `YawControlBuilder`
3. Add the yaw block to your .skyb file with `SkybrushBinaryWriter.WriteYawControlBlock()`

**Key concepts:**
- Yaw deltas define rotation changes over time
- Positive angles = clockwise, negative = counter-clockwise
- Auto-yaw mode makes drones point along their flight path
- Each delta is max 65535 milliseconds (split longer durations into multiple deltas)

See `YawCreationExample.cs` for complete working examples!

## License

The C# bindings are part of libskybrush and are licensed under the GNU General Public License v3.0 or later. See the LICENSE.txt file for details.

