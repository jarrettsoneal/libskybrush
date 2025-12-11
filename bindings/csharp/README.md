# Skybrush C# Bindings - Complete Show Creation

This directory contains C# bindings for creating complete Skybrush drone shows (.skyb files) with full control over all features.

## Documentation Files

- **BlockFormationGuide.cs** - 📖 **Detailed guide on how blocks are formed and written** (NEW)
  - Step-by-step block creation process
  - Binary structure of each block type
  - Hex output examples with explanations
  - Little-endian encoding details
  - Complete file formation walkthrough
  
- **SkybrushFileFormat.cs** - Complete .skyb format reference (all block types, specs)
- **CompleteLaunchExample.cs** - Working examples (pre-show, staggered launch, post-show)
- **IMPLEMENTATION_SUMMARY.md** - Feature summary and requirements addressed
- **README.md** - This file (usage guide)
- **QUICKSTART.md** - Quick reference for yaw control

## Overview

The C# bindings allow you to:
- **Create complete .skyb show files** with all block types
- **Control launch timing** - stagger drone launches, add pre-show delays
- **Add ground lighting** - show LED colors before takeoff and after landing
- **Build yaw control** - spins, rotations, auto-yaw mode
- **Create trajectories** - 3D flight paths with precise timing
- **Add metadata** - comments, show info, drone IDs
- **Optional: Read existing files** - query and analyze .skyb files

## How Blocks Are Formed and Written

**See BlockFormationGuide.cs for complete documentation.**

Quick overview of block formation:

1. **Prepare data** - Build block-specific bytes
2. **Get length** - Count bytes (max 65535)
3. **Write type** - 1 byte block type identifier
4. **Write length** - 2 bytes (uint16, little-endian)
5. **Write data** - The actual block content

Example:
```csharp
// Comment block formation
string text = "Test";
byte[] data = Encoding.UTF8.GetBytes(text);  // [0x54, 0x65, 0x73, 0x74]
writer.Write((byte)3);        // Type = 3 (Comment)
writer.Write((ushort)4);      // Length = 4 bytes (little-endian: 0x04, 0x00)
writer.Write(data);           // Data = UTF-8 "Test"

// Result: 03 04 00 54 65 73 74
```

**All blocks use little-endian encoding for multi-byte integers.**

## Quick Start - Complete Show File

```csharp
using Skybrush;
using System.IO;

// Create show file
using (var writer = new BinaryWriter(File.Create("myshow.skyb")))
{
    // 1. File header
    SkybrushBinaryWriter.WriteFileHeader(writer);
    
    // 2. Metadata
    SkybrushBinaryWriter.WriteCommentBlock(writer, "My Show Title");
    
    // 3. Trajectory (your flight path)
    SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
    
    // 4. Light program (LED colors)
    SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);
    
    // 5. Yaw control (rotation)
    var yaw = new YawControlBuilder();
    yaw.Spin360(4000);
    SkybrushBinaryWriter.WriteYawControlBlock(writer, yaw.Build());
}
```

## Launch Timing and Ground Lighting

### Pre-Show Ground Lighting

**Show LED colors on the ground before takeoff:**

1. **Light Program** starts at t=0 with your desired colors
2. **Trajectory** holds at ground level (Z=0) for the pre-show duration
3. **Timing**: Lights run while drone waits on ground

```csharp
// Trajectory: Hold on ground for 5 seconds, then takeoff
var traj = new TrajectoryBuilder();
traj.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
traj.HoldPosition(5000);  // 5 second pre-show hold
traj.AppendLine(new Vector3WithYaw(0, 0, 5000, 0), 5000); // Takeoff

// Light program (conceptual):
// t=0:     Set RED    (ground)
// t=2000:  Fade GREEN (ground)
// t=5000:  Set WHITE  (takeoff starts)
// t=10000: Flight colors...
```

### Staggered Launch (Launch Drones at Different Times)

**Method 1: Different Hold Durations**

Each drone's .skyb file has a different initial hold period:

```csharp
// Drone 1: Launch immediately
var traj1 = new TrajectoryBuilder();
traj1.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
traj1.AppendLine(new Vector3WithYaw(0, 0, 5, 0), 5000); // Immediate takeoff

// Drone 2: Launch 2 seconds later
var traj2 = new TrajectoryBuilder();
traj2.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
traj2.HoldPosition(2000);  // Wait 2 seconds
traj2.AppendLine(new Vector3WithYaw(0, 0, 5, 0), 5000); // Then takeoff

// Drone 3: Launch 4 seconds later
var traj3 = new TrajectoryBuilder();
traj3.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
traj3.HoldPosition(4000);  // Wait 4 seconds
traj3.AppendLine(new Vector3WithYaw(0, 0, 5, 0), 5000); // Then takeoff
```

**Benefits:**
- All .skyb files can start at same wall-clock time
- Drones take off in sequence automatically
- Can show different ground lighting per drone during wait

**Method 2: External Launch Commands**

Create identical .skyb files, trigger launches at different times externally:
- Same trajectory in all files
- Ground control sends launch command to each drone at staggered times
- Simpler file creation, requires external coordination

### Post-Show Ground Lighting

**Continue showing lights after landing:**

1. **Trajectory** includes landing sequence
2. **Light Program** continues beyond landing time
3. **Timing**: Lights keep running after drone is on ground

```csharp
// Trajectory ends at 20 seconds (landed)
var traj = new TrajectoryBuilder();
traj.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
traj.AppendLine(new Vector3WithYaw(0, 0, 5, 0), 5000);  // Takeoff
traj.HoldPosition(10000);                               // Hover
traj.AppendLine(new Vector3WithYaw(0, 0, 0, 0), 5000);  // Land (at t=20s)

// Light program continues beyond t=20s:
// t=0-5:    Takeoff colors
// t=5-15:   Flight colors  
// t=15-20:  Landing colors
// t=20-25:  Post-show ground pattern (drone landed)
// t=25-30:  Fade out
```

## Complete File Format Documentation

See `SkybrushFileFormat.cs` for detailed documentation of:
- All 5 block types (Trajectory, Light Program, Comment, RTH Plan, Yaw Control)
- Binary format specifications
- Launch timing strategies
- Coordinate and time units
- Examples for every feature

## Files You Need

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

For **creating complete shows** (most common use case):
- `SkybrushBinaryWriter.cs` - Write all .skyb file blocks
- `YawControlBuilder.cs` - Build yaw control data
- `TrajectoryBuilder.cs` - Build trajectory data
- `SkybrushFileFormat.cs` - Complete format documentation
- `CompleteLaunchExample.cs` - Launch timing and ground lighting examples

For **yaw control only**:
- `YawControlBuilder.cs` - Build yaw control data
- `SkybrushBinaryWriter.cs` - Write .skyb file blocks
- `QUICKSTART.md` - Quick reference

For **reading existing files** (optional):
- `YawControl.cs`, `YawPlayer.cs` - Read and query yaw data
- `SkybrushError.cs` - Error handling
- `Example.cs` - Reading examples

## Requirements

- .NET Framework 4.6.1+ / .NET Core 2.0+ / .NET 5.0+
- **No native library required** for creating .skyb files (pure C#)
- libskybrush native library only needed for reading existing files

## All Block Types Supported

### 1. Trajectory Block (Required)
Defines the 3D flight path.

```csharp
var traj = new TrajectoryBuilder(scale: 10);
traj.SetStartPosition(new Vector3WithYaw(0, 0, 0, 0));
traj.HoldPosition(3000);  // Pre-show hold
traj.AppendLine(new Vector3WithYaw(0, 0, 5000, 0), 5000);
byte[] trajData = GetTrajectoryBytes(traj);
SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajData);
```

### 2. Light Program Block
Controls LED colors and pyro effects.

```csharp
// Light program bytecode (complex format)
// Use for pre-show ground lighting, flight effects, post-show lighting
byte[] lightData = CreateLightProgram();
SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);
```

### 3. Comment Block
Metadata and documentation.

```csharp
SkybrushBinaryWriter.WriteCommentBlock(writer, "Show Title: Fireworks 2025");
SkybrushBinaryWriter.WriteCommentBlock(writer, "Drone ID: D001");
SkybrushBinaryWriter.WriteCommentBlock(writer, "Author: John Doe");
```

### 4. RTH (Return-to-Home) Plan Block (Optional)
Emergency landing procedures.

```csharp
byte[] rthData = CreateRthPlan();
SkybrushBinaryWriter.WriteRthPlanBlock(writer, rthData);
```

### 5. Yaw Control Block
Rotation control.

```csharp
var yaw = new YawControlBuilder();
yaw.Spin360(4000);
yaw.HoldYaw(2000);
SkybrushBinaryWriter.WriteYawControlBlock(writer, yaw.Build());
```

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

## Additional Documentation

- **BlockFormationGuide.cs** - Comprehensive guide on how blocks are formed and written to .skyb files
  - Step-by-step block creation process with code examples
  - Binary structure breakdown for each block type
  - Hex output examples with detailed explanations
  - Little-endian encoding reference
  - Complete file formation walkthrough

- **SkybrushFileFormat.cs** - Complete .skyb format specification
  - All 5 block types documented
  - Binary format details
  - Launch timing strategies
  - Data type reference

- **CompleteLaunchExample.cs** - Working examples for launch timing
  - Pre-show ground lighting
  - Staggered launches
  - Post-show ground lighting
  - Complete multi-block show files

## License

The C# bindings are part of libskybrush and are licensed under the GNU General Public License v3.0 or later. See the LICENSE.txt file for details.

