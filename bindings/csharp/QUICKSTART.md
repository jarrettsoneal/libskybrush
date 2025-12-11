# Yaw Control in C# - Quick Reference

## What You Need

To add yaw control to your .skyb show files in C#, you need just **2 files**:
1. `YawControlBuilder.cs` - Creates yaw control data
2. `SkybrushBinaryWriter.cs` - Writes .skyb file blocks

**No native library required** for creating yaw control!

## Basic Usage

### Step 1: Build Yaw Control Data

```csharp
using Skybrush;

var yawBuilder = new YawControlBuilder();

// Set starting angle
yawBuilder.YawOffsetDegrees = 0;  // Start facing north

// Add yaw commands
yawBuilder.RotateClockwise(2000, 90);        // Rotate 90° in 2 seconds
yawBuilder.HoldYaw(1000);                     // Hold for 1 second
yawBuilder.RotateCounterClockwise(3000, 180);// Rotate back 180° in 3 seconds
yawBuilder.Spin360(4000, clockwise: true);   // Full spin in 4 seconds

// Build the binary data
byte[] yawData = yawBuilder.Build();
```

### Step 2: Add to Your .skyb File

```csharp
using System.IO;
using Skybrush;

using (var fs = new FileStream("myshow.skyb", FileMode.Create))
using (var writer = new BinaryWriter(fs))
{
    // Write header
    SkybrushBinaryWriter.WriteFileHeader(writer);
    
    // Write your existing blocks
    SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
    SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);
    
    // Add yaw control
    SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
}
```

## Common Yaw Commands

```csharp
// Hold current yaw for specified time
yawBuilder.HoldYaw(2000);  // 2 seconds

// Rotate clockwise
yawBuilder.RotateClockwise(1000, 45);  // 45° in 1 second

// Rotate counter-clockwise
yawBuilder.RotateCounterClockwise(2000, 90);  // 90° in 2 seconds

// Full 360° spin
yawBuilder.Spin360(4000, clockwise: true);  // 4 second spin

// Custom rotation (positive = CW, negative = CCW)
yawBuilder.AddDelta(3000, 270);   // 270° CW in 3 seconds
yawBuilder.AddDelta(2000, -180);  // 180° CCW in 2 seconds
```

## Auto-Yaw Mode

```csharp
var yawBuilder = new YawControlBuilder();

// Enable auto-yaw (drone follows flight path direction)
yawBuilder.AutoYaw = true;

// You can still add manual adjustments on top
yawBuilder.HoldYaw(2000);        // Auto-orient
yawBuilder.AddDelta(1000, 45);   // Add 45° offset
yawBuilder.HoldYaw(2000);        // Hold offset
yawBuilder.AddDelta(1000, -45);  // Remove offset
```

## Complete Example

```csharp
using System;
using System.IO;
using Skybrush;

class Program
{
    static void Main()
    {
        // Create yaw control
        var yawBuilder = new YawControlBuilder();
        yawBuilder.YawOffsetDegrees = 0;
        
        // Choreography: hold, spin, hold, reverse spin
        yawBuilder.HoldYaw(2000);
        yawBuilder.Spin360(4000, clockwise: true);
        yawBuilder.HoldYaw(1000);
        yawBuilder.Spin360(3000, clockwise: false);
        
        Console.WriteLine($"Created {yawBuilder.Count} yaw deltas");
        Console.WriteLine($"Total duration: {yawBuilder.TotalDurationMs} ms");
        
        // Build binary data
        byte[] yawData = yawBuilder.Build();
        
        // Write to .skyb file
        using (var fs = new FileStream("show.skyb", FileMode.Create))
        using (var writer = new BinaryWriter(fs))
        {
            SkybrushBinaryWriter.WriteFileHeader(writer);
            SkybrushBinaryWriter.WriteCommentBlock(writer, "My show with yaw!");
            
            // TODO: Add your trajectory and lights
            // SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
            // SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightData);
            
            SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
        }
        
        Console.WriteLine("Show file created!");
    }
}
```

## Tips

- **Yaw angles**: 0° = North, 90° = East, 180° = South, 270° = West (-90°)
- **Time limits**: Each delta can be max 65,535 ms (~65 seconds)
- **For longer holds**: Chain multiple HoldYaw() calls
- **Smooth rotations**: Use smaller angle changes with shorter durations
- **Testing**: Build with `dotnet build`, run with `dotnet run`

## Verified

This implementation has been tested and verified to:
✅ Generate correct binary format
✅ Create valid .skyb files
✅ Be readable by the native libskybrush library
✅ Work on Linux, macOS, and Windows with .NET

See `YawCreationExample.cs` for more examples!
