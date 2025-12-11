# Skybrush C# Bindings

This directory contains C# bindings for the libskybrush library, enabling .NET developers to control yaw in Skybrush drone shows.

## Overview

The C# bindings provide a managed wrapper around the native libskybrush C library, allowing you to:
- Load yaw control data from Skybrush binary files
- Query yaw values at specific timestamps
- Calculate yaw rates
- Iterate through yaw setpoints

## Requirements

- .NET Framework 4.6.1 or higher / .NET Core 2.0 or higher / .NET 5.0 or higher
- libskybrush native library (libskybrush.so on Linux, libskybrush.dylib on macOS, skybrush.dll on Windows)

## Installation

### Building the Native Library

First, build the native libskybrush library:

```bash
cd /path/to/libskybrush
mkdir build
cd build
cmake ..
make
```

### Using the C# Bindings

1. Copy the C# source files to your project:
   - `SkybrushError.cs`
   - `YawControl.cs`
   - `YawPlayer.cs`

2. Ensure the native library is in your system's library path or in the same directory as your executable.

## Usage Examples

### Basic Usage

```csharp
using Skybrush;
using System;
using System.IO;

class Program
{
    static void Main()
    {
        // Load a Skybrush binary file
        byte[] fileData = File.ReadAllBytes("show.skyb");
        
        // Create a yaw control object from the file
        using (var yawControl = new YawControl(fileData))
        {
            Console.WriteLine($"Auto Yaw: {yawControl.AutoYaw}");
            Console.WriteLine($"Yaw Offset: {yawControl.YawOffsetDegrees}°");
            Console.WriteLine($"Number of Deltas: {yawControl.NumberOfDeltas}");
            
            // Create a player to query yaw values
            using (var player = new YawPlayer(yawControl))
            {
                // Get total duration
                uint durationMs = player.GetTotalDurationMsec();
                Console.WriteLine($"Total Duration: {durationMs} ms");
                
                // Query yaw at specific time (e.g., 5 seconds)
                float yaw = player.GetYawAt(5.0f);
                Console.WriteLine($"Yaw at 5s: {yaw}°");
                
                // Query yaw rate at specific time
                float yawRate = player.GetYawRateAt(5.0f);
                Console.WriteLine($"Yaw rate at 5s: {yawRate}°/s");
            }
        }
    }
}
```

### Iterating Through Setpoints

```csharp
using Skybrush;
using System;

class Program
{
    static void Main()
    {
        byte[] fileData = File.ReadAllBytes("show.skyb");
        
        using (var yawControl = new YawControl(fileData))
        using (var player = new YawPlayer(yawControl))
        {
            // Iterate through all setpoints
            while (player.HasMoreSetpoints())
            {
                player.BuildNextSetpoint();
                YawSetpoint setpoint = player.GetCurrentSetpoint();
                
                Console.WriteLine($"Setpoint:");
                Console.WriteLine($"  Start Time: {setpoint.StartTimeSec}s");
                Console.WriteLine($"  Duration: {setpoint.DurationSec}s");
                Console.WriteLine($"  Start Yaw: {setpoint.StartYawDeg}°");
                Console.WriteLine($"  End Yaw: {setpoint.EndYawDeg}°");
                Console.WriteLine($"  Yaw Change: {setpoint.YawChangeDeg}°");
            }
        }
    }
}
```

### Sampling Yaw Over Time

```csharp
using Skybrush;
using System;

class Program
{
    static void Main()
    {
        byte[] fileData = File.ReadAllBytes("show.skyb");
        
        using (var yawControl = new YawControl(fileData))
        using (var player = new YawPlayer(yawControl))
        {
            uint durationMs = player.GetTotalDurationMsec();
            float durationSec = durationMs / 1000.0f;
            
            // Sample yaw every 0.1 seconds
            for (float t = 0; t <= durationSec; t += 0.1f)
            {
                try
                {
                    float yaw = player.GetYawAt(t);
                    float yawRate = player.GetYawRateAt(t);
                    Console.WriteLine($"Time: {t:F1}s, Yaw: {yaw:F2}°, Rate: {yawRate:F2}°/s");
                }
                catch (SkybrushException ex)
                {
                    Console.WriteLine($"Error at time {t}: {ex.Message}");
                }
            }
        }
    }
}
```

### Error Handling

```csharp
using Skybrush;
using System;

class Program
{
    static void Main()
    {
        try
        {
            byte[] fileData = File.ReadAllBytes("show.skyb");
            using (var yawControl = new YawControl(fileData))
            {
                if (yawControl.IsEmpty())
                {
                    Console.WriteLine("Yaw control data is empty");
                    return;
                }
                
                using (var player = new YawPlayer(yawControl))
                {
                    float yaw = player.GetYawAt(10.0f);
                    Console.WriteLine($"Yaw at 10s: {yaw}°");
                }
            }
        }
        catch (SkybrushException ex)
        {
            Console.WriteLine($"Skybrush error: {ex.Message} (Code: {ex.ErrorCode})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

## API Reference

### YawControl Class

Represents yaw control data from a Skybrush show file.

**Properties:**
- `bool AutoYaw` - Gets whether auto yaw mode is in use
- `float YawOffsetDegrees` - Gets the yaw offset in degrees
- `int NumberOfDeltas` - Gets the number of yaw deltas

**Methods:**
- `YawControl()` - Creates an empty yaw control object
- `YawControl(byte[] buffer)` - Creates a yaw control object from a binary buffer
- `bool IsEmpty()` - Checks if the yaw control object is empty
- `void Dispose()` - Releases native resources

### YawPlayer Class

Allows querying yaw values and rates along a yaw control curve.

**Methods:**
- `YawPlayer(YawControl control)` - Creates a player for the given yaw control
- `void BuildNextSetpoint()` - Advances to the next setpoint
- `YawSetpoint GetCurrentSetpoint()` - Gets the current setpoint
- `float GetYawAt(float time)` - Gets yaw in degrees at the specified time
- `float GetYawRateAt(float time)` - Gets yaw rate in degrees/second at the specified time
- `uint GetTotalDurationMsec()` - Gets the total duration in milliseconds
- `bool HasMoreSetpoints()` - Checks if there are more setpoints
- `void Dispose()` - Releases native resources

### YawSetpoint Struct

Represents a single yaw setpoint in the trajectory.

**Fields:**
- `float StartTimeSec` - Start time in seconds
- `uint StartTimeMsec` - Start time in milliseconds
- `float DurationSec` - Duration in seconds
- `ushort DurationMsec` - Duration in milliseconds
- `float EndTimeSec` - End time in seconds
- `uint EndTimeMsec` - End time in milliseconds
- `float StartYawDeg` - Starting yaw in degrees
- `float EndYawDeg` - Ending yaw in degrees
- `float YawChangeDeg` - Yaw change in degrees

### SkybrushError Enum

Error codes returned by the library.

- `Success` - No error
- `NoMemory` - Not enough memory
- `InvalidValue` - Invalid value
- `ReadError` - Error while reading
- `NotFound` - File or resource not found
- And more...

### SkybrushException Class

Exception thrown when Skybrush operations fail.

**Properties:**
- `SkybrushError ErrorCode` - The error code

## Platform Notes

### Linux
Ensure `libskybrush.so` is in `/usr/lib`, `/usr/local/lib`, or set `LD_LIBRARY_PATH`:
```bash
export LD_LIBRARY_PATH=/path/to/libskybrush/build:$LD_LIBRARY_PATH
```

### macOS
Ensure `libskybrush.dylib` is in `/usr/lib`, `/usr/local/lib`, or set `DYLD_LIBRARY_PATH`:
```bash
export DYLD_LIBRARY_PATH=/path/to/libskybrush/build:$DYLD_LIBRARY_PATH
```

### Windows
Place `skybrush.dll` in the same directory as your executable or in a directory in your `PATH`.

## License

The C# bindings are part of libskybrush and are licensed under the GNU General Public License v3.0 or later. See the LICENSE.txt file for details.
