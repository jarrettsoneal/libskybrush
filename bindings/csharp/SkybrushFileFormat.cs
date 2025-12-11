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
    /// Complete reference for the Skybrush binary (.skyb) file format.
    /// This document describes all block types and options available for creating drone shows.
    /// 
    /// Note: This class contains embedded documentation as a string constant for easy reference.
    /// For production use, consider extracting to separate documentation files if needed.
    /// </summary>
    public static class SkybrushFileFormat
    {
        /// <summary>
        /// Complete documentation of the Skybrush .skyb file format.
        /// Access this string to get comprehensive format documentation programmatically.
        /// 
        /// Topics covered:
        /// - File structure (header + blocks)
        /// - All 5 block types with binary format specs
        /// - Launch timing strategies
        /// - Ground lighting techniques
        /// - Data type reference
        /// - Binary encoding details
        /// </summary>
        public const string Documentation = @"
# Skybrush .skyb File Format - Complete Reference

## File Structure

A .skyb file consists of:
1. **File Header** (5-6 bytes)
2. **Blocks** (variable length, any order, any number)

### File Header Format

**Version 1** (5 bytes):
- Bytes 0-3: Magic string 'skyb' (0x73 0x6B 0x79 0x62)
- Byte 4: Version number (0x01)

**Version 2** (6 bytes):
- Bytes 0-3: Magic string 'skyb' (0x73 0x6B 0x79 0x62)
- Byte 4: Version number (0x02)
- Byte 5: Feature flags
  - Bit 0: CRC32 checksum present (0x01)
  - Bits 1-7: Reserved (0x00)

### Block Format

Each block has:
- Byte 0: Block type (see below)
- Bytes 1-2: Block length in bytes (uint16, little-endian, max 65535)
- Bytes 3+: Block data (format depends on block type)

## Block Types

### 1. Trajectory Block (Type 0x01)

Defines the 3D flight path of a drone.

**Header** (9 bytes):
- Byte 0: Scale and flags
  - Bits 0-6: Scale factor (1-127)
  - Bit 7: Use yaw flag (0x80 if yaw is included)
- Bytes 1-2: Start X coordinate (int16, scaled millimeters)
- Bytes 3-4: Start Y coordinate (int16, scaled millimeters)  
- Bytes 5-6: Start Z coordinate (int16, scaled millimeters)
- Bytes 7-8: Start yaw angle (int16, decidegrees) - only if use_yaw flag is set

**Segments** (variable length):
Each segment describes movement over time. Format depends on segment type.

**Linear segment** (most common):
- Byte 0: Type flags (see trajectory segment format flags)
- Bytes 1-2: Duration (uint16, milliseconds)
- Optional coordinate changes (2 bytes each, int16):
  - If X changes: X delta
  - If Y changes: Y delta
  - If Z changes: Z delta
  - If yaw changes: Yaw delta

**Use Cases**:
- Define flight paths
- Control takeoff and landing
- Coordinate multi-drone formations

### 2. Light Program Block (Type 0x02)

Defines LED colors and pyrotechnic channels over time.

**Format**: Bytecode program (complex, see light program documentation)

**Common operations**:
- Set RGB color
- Fade between colors
- Control pyro channels (for effects like fireworks)
- Wait commands
- Loop commands

**Use Cases**:
- Pre-show ground lighting (before takeoff)
- In-flight LED colors and patterns
- Post-show ground lighting (after landing)
- Pyrotechnic effect timing

### 3. Comment Block (Type 0x03)

Contains UTF-8 text metadata.

**Format**: 
- Raw UTF-8 encoded string (no null terminator needed)

**Use Cases**:
- Show title, author, version
- Drone ID or configuration notes
- License information
- Debug information

### 4. Return-to-Home (RTH) Plan Block (Type 0x04)

Defines emergency return-to-home behavior.

**Header** (variable):
- Byte 0: Scale and flags
- Following bytes: RTH waypoints and actions

**RTH Actions**:
- Land in place
- Go to target keeping altitude
- Go to target with altitude change

**Use Cases**:
- Emergency landing procedures
- Collective RTH for entire fleet
- Fail-safe behaviors
- End-of-show coordinated landing

### 5. Yaw Control Block (Type 0x05)

Defines drone rotation around vertical axis.

**Header** (3 bytes):
- Byte 0: Flags
  - Bit 0: Auto-yaw mode (0x01)
  - Bits 1-7: Reserved (0x00)
- Bytes 1-2: Yaw offset (int16, decidegrees)

**Deltas** (4 bytes each):
- Bytes 0-1: Duration (uint16, milliseconds)
- Bytes 2-3: Yaw change (int16, decidegrees)
  - Positive = clockwise rotation
  - Negative = counter-clockwise rotation

**Auto-Yaw Mode**:
When enabled, drone automatically points in direction of travel.
Manual yaw deltas are added as offsets to auto-yaw.

**Use Cases**:
- Spins and rotations during show
- Pointing drone in specific direction
- Camera orientation control
- Synchronized rotation effects

## Launch Timing and Ground Lighting

### Pre-Show Ground Lighting

To show lights on the ground before takeoff:

1. **Light Program**: Start with color commands at t=0
2. **Trajectory**: Delay takeoff using hold-position segment
3. **Timing**: Light program runs before drone lifts off

Example:
```
Light Program:
  t=0ms: Set color RED
  t=2000ms: Fade to GREEN
  t=4000ms: Set color BLUE

Trajectory:
  t=0-5000ms: Hold at ground (Z=0)
  t=5000ms: Start takeoff
```

### Staggered Launch (Launch Drones One at a Time)

To launch drones at different times:

1. Each drone has trajectory starting at different time offset
2. Or use hold-position segments of different durations
3. Light programs can run independently

**Method 1: Trajectory Time Offsets**
- Drone 1: Takeoff at t=0ms
- Drone 2: Takeoff at t=2000ms (hold for 2s first)
- Drone 3: Takeoff at t=4000ms (hold for 4s first)

**Method 2: Programmatic Delay**
- Each .skyb file is identical
- Launch command sent at different wall-clock times
- Drone internal timer starts when launch command received

### Post-Show Ground Lighting

To continue showing lights after landing:

1. **Trajectory**: Include landing sequence
2. **Light Program**: Continue beyond landing time
3. **Timing**: Lights continue after Z reaches ground

Example:
```
Trajectory:
  t=0-30000ms: Flight path
  t=30000-32000ms: Landing descent
  t=32000ms+: On ground (Z=0)

Light Program:
  t=0-32000ms: Flight colors
  t=32000-37000ms: Landing sequence colors
  t=37000ms+: Final color or fade out
```

## Complete Show File Example Structure

A typical multi-drone show .skyb file contains:

1. **File Header** (v2 with feature flags)
2. **Comment Block** - Show metadata
3. **Trajectory Block** - Flight path with launch delay
4. **Light Program Block** - Pre-show, flight, and post-show lighting
5. **Yaw Control Block** - Rotation choreography
6. **RTH Plan Block** - Emergency procedures

## Data Type Reference

**Coordinate Units**:
- X, Y, Z: millimeters (in code) or scaled decidegrees (in binary)
- Scale factor: Divider for compression (1-127)
- Example: scale=10, coordinate range: ±3276.7 meters

**Time Units**:
- Milliseconds (uint16): 0-65535 ms (~65 seconds per segment)
- For longer durations: Use multiple segments
- Total show time: Sum of all segments

**Angle Units**:
- Decidegrees (int16): 1 decidegree = 0.1 degrees
- Range: ±3276.7 degrees (±32767 decidegrees)
- Example: 900 decidegrees = 90 degrees

**Color Format**:
- RGB: 3 bytes (R, G, B), values 0-255
- Example: RED = (255, 0, 0), WHITE = (255, 255, 255)

## Binary Encoding

All multi-byte integers use **little-endian** encoding:
- uint16: Low byte first, high byte second
- int16: Low byte first, high byte second (two's complement)

Example: 1000 (0x03E8) as uint16 = [0xE8, 0x03]
";
    }
}
