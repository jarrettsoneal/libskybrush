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
    /// Comprehensive guide on how .skyb file blocks are formed and written.
    /// This document explains the step-by-step process of creating each block type.
    /// </summary>
    public static class BlockFormationGuide
    {
        public const string Documentation = @"
# How .skyb File Blocks Are Formed and Written

## Overview

A .skyb file consists of:
1. **File Header** - Identifies the file format and version
2. **Blocks** - Sequential data chunks, each with its own type and data

Each block is independently formed and written. The order matters for some blocks (header first), but blocks can generally appear in any order after the header.

## Step-by-Step File Creation Process

### Step 1: Create File and Writer

```csharp
using System.IO;

// Create a file stream
using (var fileStream = File.Create(""myshow.skyb""))
// Create a binary writer
using (var writer = new BinaryWriter(fileStream))
{
    // File creation steps go here
}
```

The `BinaryWriter` handles writing data types and manages byte-level operations.

### Step 2: Write File Header

**What it does:** Identifies this as a .skyb file and specifies the version.

**Binary structure:**
```
Byte 0-3:  Magic string ""skyb"" (0x73 0x6B 0x79 0x62)
Byte 4:    Version (0x01 or 0x02)
Byte 5:    Feature flags (version 2 only)
```

**Formation process:**

```csharp
// Version 1 (5 bytes total)
SkybrushBinaryWriter.WriteFileHeader(writer, version: 1);

// Version 2 (6 bytes total) - Recommended
SkybrushBinaryWriter.WriteFileHeader(writer, version: 2, features: 0);
```

**Internal implementation:**
1. Convert string ""skyb"" to ASCII bytes: [0x73, 0x6B, 0x79, 0x62]
2. Write those 4 bytes
3. Write version byte
4. If version 2, write features byte

**Hex output example (version 2):**
```
73 6B 79 62 02 00
^^^^^^^^^^^^^^^  ^^  ^^
magic ""skyb""   v2  features=0
```

### Step 3: Write Comment Blocks (Optional but Recommended)

**What it does:** Stores text metadata like show title, drone ID, author, etc.

**Binary structure:**
```
Byte 0:    Block type = 0x03 (Comment)
Byte 1-2:  Block length (uint16, little-endian)
Byte 3+:   UTF-8 encoded text (no null terminator)
```

**Formation process:**

```csharp
string comment = ""Show Title: Fireworks 2025"";

// Step 3a: Convert text to UTF-8 bytes
byte[] textBytes = Encoding.UTF8.GetBytes(comment);
// Result: [0x53, 0x68, 0x6F, 0x77, ...] (UTF-8 bytes)

// Step 3b: Write the block
SkybrushBinaryWriter.WriteCommentBlock(writer, comment);
```

**Internal implementation:**
1. Convert string to UTF-8 bytes
2. Get byte count (length)
3. Write block type: 0x03
4. Write length as uint16 (little-endian): length % 256, length / 256
5. Write the UTF-8 bytes

**Hex output example:**
```
Comment: ""Test""
03 04 00 54 65 73 74
^^ ^^^^^ ^^^^^^^^^^^
|  |     UTF-8 ""Test""
|  length=4 (0x0004)
type=3
```

**Multiple comments:**
You can write multiple comment blocks. Each is independent:

```csharp
SkybrushBinaryWriter.WriteCommentBlock(writer, ""Show Title: Demo"");
SkybrushBinaryWriter.WriteCommentBlock(writer, ""Drone ID: D001"");
SkybrushBinaryWriter.WriteCommentBlock(writer, ""Author: John"");
```

### Step 4: Write Trajectory Block

**What it does:** Defines the 3D flight path of the drone.

**Binary structure:**
```
Byte 0:      Block type = 0x01 (Trajectory)
Byte 1-2:    Block length (uint16, little-endian)
Byte 3:      Header byte 0: Scale (bits 0-6) + Use Yaw flag (bit 7)
Byte 4-5:    Start X (int16, scaled millimeters)
Byte 6-7:    Start Y (int16, scaled millimeters)
Byte 8-9:    Start Z (int16, scaled millimeters)
Byte 10-11:  Start Yaw (int16, decidegrees) - if use_yaw flag set
Byte 12+:    Trajectory segments (variable format)
```

**Formation process:**

```csharp
// Step 4a: Create trajectory builder
var trajBuilder = new TrajectoryBuilder(scale: 10, useYaw: true);

// Step 4b: Set starting position (in millimeters and degrees)
trajBuilder.SetStartPosition(new Vector3WithYaw(
    x: 0,      // 0 mm (origin)
    y: 0,      // 0 mm
    z: 0,      // 0 mm (ground)
    yaw: 0     // 0 degrees (facing north)
));

// Step 4c: Add movement segments
trajBuilder.HoldPosition(3000);  // Hold for 3 seconds
trajBuilder.AppendLine(
    target: new Vector3WithYaw(0, 0, 5000, 0),  // Rise to 5m
    durationMs: 5000                             // Over 5 seconds
);

// Step 4d: Get binary data
// Note: Actual implementation would extract from builder
byte[] trajectoryData = GetTrajectoryBinaryData(trajBuilder);

// Step 4e: Write the block
SkybrushBinaryWriter.WriteTrajectoryBlock(writer, trajectoryData);
```

**Header formation (first 9+ bytes of data):**

1. **Scale byte**: Combine scale and yaw flag
   - Scale: 10 = 0x0A
   - Use yaw: true = bit 7 = 0x80
   - Result: 0x0A | 0x80 = 0x8A

2. **Coordinates**: Convert millimeters to scaled units
   - Formula: scaled = (mm / scale) rounded to int16
   - X=0mm, scale=10: 0/10 = 0 → int16: 0x0000
   - Y=0mm, scale=10: 0/10 = 0 → int16: 0x0000
   - Z=0mm, scale=10: 0/10 = 0 → int16: 0x0000

3. **Yaw**: Convert degrees to decidegrees
   - 0 degrees = 0 * 10 = 0 decidegrees → int16: 0x0000

**Hex output example (header only):**
```
Trajectory header with scale=10, start at origin, yaw enabled:
01 09 00 8A 00 00 00 00 00 00 00 00
^^ ^^^^^ ^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^
|  |     |  |     |     |     start yaw=0
|  |     |  |     |     start Z=0
|  |     |  |     start Y=0
|  |     |  start X=0
|  |     scale=10 + yaw flag
|  length=9 (just header, no segments)
type=1
```

**Segment encoding** (after header):
Each segment encodes duration and coordinate changes. Format varies by segment type (hold, line, bezier, etc.).

### Step 5: Write Light Program Block

**What it does:** Defines LED colors and pyrotechnic effects over time.

**Binary structure:**
```
Byte 0:    Block type = 0x02 (Light Program)
Byte 1-2:  Block length (uint16, little-endian)
Byte 3+:   Bytecode program (complex format)
```

**Formation process:**

```csharp
// Light programs use a bytecode format
// Each instruction is one or more bytes

// Example conceptual structure:
// SetColor(RED):    [opcode, R, G, B]
// Wait(1000ms):     [opcode, duration_low, duration_high]
// FadeToColor(GREEN, 2000ms): [opcode, R, G, B, duration_low, duration_high]

byte[] lightProgramData = BuildLightProgram();
SkybrushBinaryWriter.WriteLightProgramBlock(writer, lightProgramData);
```

**Common bytecode operations:**
- Set RGB color
- Fade between colors
- Wait for duration
- Control pyro channels
- Loop instructions

**Hex output example:**
```
Light program: Set RED, wait 1000ms
02 07 00 [bytecode: 7 bytes of operations]
^^ ^^^^^ 
|  length=7
type=2
```

### Step 6: Write Yaw Control Block

**What it does:** Defines how the drone rotates around its vertical axis.

**Binary structure:**
```
Byte 0:    Block type = 0x05 (Yaw Control)
Byte 1-2:  Block length (uint16, little-endian)
Byte 3:    Flags (bit 0 = auto-yaw)
Byte 4-5:  Yaw offset (int16, decidegrees)
Byte 6+:   Deltas (4 bytes each)
```

**Formation process:**

```csharp
// Step 6a: Create yaw control builder
var yawBuilder = new YawControlBuilder();

// Step 6b: Set initial parameters
yawBuilder.YawOffsetDegrees = 0;    // Start at 0 degrees
yawBuilder.AutoYaw = false;         // Manual control

// Step 6c: Add yaw commands
yawBuilder.HoldYaw(2000);                      // Hold for 2s
yawBuilder.RotateClockwise(1000, 90);          // Rotate 90° in 1s
yawBuilder.Spin360(4000, clockwise: true);     // Full spin in 4s

// Step 6d: Build binary data
byte[] yawData = yawBuilder.Build();

// Step 6e: Write the block
SkybrushBinaryWriter.WriteYawControlBlock(writer, yawData);
```

**Data formation (inside Build()):**

1. **Header (3 bytes):**
   ```csharp
   byte flags = autoYaw ? (byte)0x01 : (byte)0x00;
   short offsetDdeg = (short)(yawOffsetDeg * 10);  // Convert to decidegrees
   
   // Write: flags (1 byte), offset (2 bytes little-endian)
   ```

2. **Each Delta (4 bytes):**
   ```csharp
   foreach (var delta in deltas) {
       ushort duration = delta.DurationMs;           // milliseconds
       short yawChange = (short)(delta.YawChangeDeg * 10);  // decidegrees
       
       // Write: duration (2 bytes), yawChange (2 bytes), both little-endian
   }
   ```

**Hex output example:**
```
Yaw control: offset=0°, hold 2s, rotate 90° in 1s
05 0B 00 00 00 00 D0 07 00 00 E8 03 84 03
^^ ^^^^^ ^^ ^^^^^ ^^^^^^^^^ ^^^^^^^^^
|  |     |  |     |         delta 2: 1000ms, +900 decidegrees (90°)
|  |     |  |     delta 1: 2000ms, 0 decidegrees (hold)
|  |     |  offset=0 decidegrees
|  |     flags=0 (no auto-yaw)
|  length=11
type=5
```

**Breakdown of little-endian encoding:**
- 2000ms = 0x07D0 → bytes: [0xD0, 0x07]
- 0 decidegrees = 0x0000 → bytes: [0x00, 0x00]
- 1000ms = 0x03E8 → bytes: [0xE8, 0x03]
- 900 decidegrees = 0x0384 → bytes: [0x84, 0x03]

### Step 7: Write RTH Plan Block (Optional)

**What it does:** Defines emergency return-to-home behavior.

**Binary structure:**
```
Byte 0:    Block type = 0x04 (RTH Plan)
Byte 1-2:  Block length (uint16, little-endian)
Byte 3+:   RTH plan data (format varies)
```

**Formation process:**

```csharp
// RTH plan defines emergency landing procedures
byte[] rthPlanData = BuildRthPlan();
SkybrushBinaryWriter.WriteRthPlanBlock(writer, rthPlanData);
```

**Hex output example:**
```
RTH plan: 15 bytes of data
04 0F 00 [15 bytes of RTH plan data]
^^ ^^^^^
|  length=15
type=4
```

## Complete File Example

**C# Code:**
```csharp
using (var writer = new BinaryWriter(File.Create(""show.skyb""))) {
    // 1. Header
    SkybrushBinaryWriter.WriteFileHeader(writer, version: 2);
    
    // 2. Comment
    SkybrushBinaryWriter.WriteCommentBlock(writer, ""Test Show"");
    
    // 3. Trajectory
    var traj = BuildTrajectory();
    SkybrushBinaryWriter.WriteTrajectoryBlock(writer, traj);
    
    // 4. Yaw
    var yaw = new YawControlBuilder();
    yaw.Spin360(4000);
    SkybrushBinaryWriter.WriteYawControlBlock(writer, yaw.Build());
}
```

**Resulting Binary (hex):**
```
73 6B 79 62 02 00                 # Header: ""skyb"" v2 features=0
03 09 00 54 65 73 74 20 53 68... # Comment block (type 3)
01 0C 00 0A 00 00 00 00 00 00... # Trajectory block (type 1)
05 07 00 00 00 00 FA 0F 68 0E    # Yaw block (type 5)
```

## Block Formation Summary

### Generic Block Write Process

**All blocks follow this pattern:**

1. **Prepare data** - Build the block-specific data bytes
2. **Get length** - Count the data bytes (max 65535)
3. **Write type** - 1 byte identifying block type
4. **Write length** - 2 bytes (uint16, little-endian)
5. **Write data** - The actual block content

**Helper method (internal):**
```csharp
public static void WriteBlock(BinaryWriter writer, SkybrushBlockType type, byte[] data)
{
    writer.Write((byte)type);              // 1 byte: block type
    writer.Write((ushort)data.Length);     // 2 bytes: length (little-endian)
    writer.Write(data);                    // N bytes: data
}
```

### Block Type Values

| Type | Value | Name          | Purpose                    |
|------|-------|---------------|----------------------------|
| 1    | 0x01  | Trajectory    | Flight path                |
| 2    | 0x02  | Light Program | LED colors & pyro effects  |
| 3    | 0x03  | Comment       | Text metadata              |
| 4    | 0x04  | RTH Plan      | Emergency procedures       |
| 5    | 0x05  | Yaw Control   | Rotation control           |

### Little-Endian Encoding

**All multi-byte integers use little-endian (least significant byte first):**

Examples:
- 1000 (0x03E8) → [0xE8, 0x03]
- 65535 (0xFFFF) → [0xFF, 0xFF]
- 256 (0x0100) → [0x00, 0x01]
- -1 (0xFFFF as int16) → [0xFF, 0xFF]

**C# BinaryWriter automatically uses little-endian for:**
- `Write(ushort)` - unsigned 16-bit
- `Write(short)` - signed 16-bit
- `Write(uint)` - unsigned 32-bit
- `Write(int)` - signed 32-bit

## Tips for Block Formation

1. **Order matters for header** - Always write file header first
2. **Order doesn't matter for blocks** - Blocks can be in any order after header
3. **Multiple blocks of same type allowed** - e.g., multiple comments
4. **Size limits** - Each block max 65535 bytes (uint16 limit)
5. **Validation** - Check data size before writing
6. **Encoding** - Always use little-endian for multi-byte integers
7. **Text encoding** - Use UTF-8 for comment blocks

## Debugging Block Formation

**To verify block formation:**

1. **Check file size** - Should match sum of header + all blocks
2. **Hex dump** - Use hex editor to verify bytes
3. **Parse with native library** - Verify .skyb file is valid
4. **Check block boundaries** - Type + length + data for each block

**Example hex dump analysis:**
```
Offset  Hex                                          ASCII
------  -------------------------------------------  -----
00000000  73 6B 79 62 02 00 03 09 00 54 65 73 74  skyb.....Test
                           ^^block type 3 (comment)
                              ^^^^^ length=9
                                    ^^^^^^... ""Test...""
```
";
    }
}
