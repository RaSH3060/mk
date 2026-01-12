# MKXL Trainer

A sophisticated trainer application for Mortal Kombat XL (MK10.exe) with advanced features including pointer reading, macro recording, and input blocking capabilities.

## Features

### Pointer Reading
- Implements multi-level pointer reading according to the specified pattern: `"MK10.exe"+033C8A98` with offsets `8, 130, 108, 78, 90, 120, F20`
- Reads values every 10 milliseconds (configurable)
- Displays results in hexadecimal format
- Supports complex pointer chains for accessing game data

### Macro System
- Records user input including keyboard presses, mouse clicks, and gamepad inputs
- Saves and loads macro files (.mkm format)
- Executes macros with precise timing control
- Adjustable start delay (default 280ms, configurable)
- Supports multiple action types:
  - Keyboard key presses and releases
  - Mouse clicks
  - Delays between actions
  - Gamepad button presses

### Input Blocking
- Blocks specific keys during macro execution (configurable)
- Prevents user input interference during macro playback
- Adjustable block delay (default 260ms, configurable)
- Supports both keyboard and gamepad input blocking

### Dual Language Support
- Russian and English interface
- Automatic language switching
- All UI elements are properly translated

### Configuration Management
- Saves all settings to Windows Registry
- Preserves user preferences between sessions
- Configurable process name detection
- Adjustable timing parameters

### User Interface
- Clean 800x600 interface with tabbed layout
- Three main tabs: Pointers, Macros, Settings
- Real-time status indicators
- Responsive design with proper error handling

## Usage Instructions

### Installation
1. Place the trainer executable in the same directory as MK10.exe
2. Ensure the dinput8.dll proxy library is also present
3. Run the trainer with administrator privileges for best results

### Pointer Tab
1. Verify the base address and offsets are correct
2. Click "Start" to begin reading pointer values
3. Monitor the results in the text box
4. Click "Stop" to end reading

### Macro Tab
1. Click "Record" to start recording your actions
2. Perform the desired keyboard/mouse/gamepad actions
3. Click "Stop" to finish recording
4. Click "Execute" to play back the recorded macro
5. Use "Save" and "Load" to manage macro files
6. Adjust the start delay as needed

### Settings Tab
1. Configure the target process name (default: MK10.exe)
2. Adjust timing parameters:
   - Read interval (how often pointer values are read)
   - Block delay (when input blocking starts)
   - Macro start delay (when macro execution begins)
3. Select your preferred language
4. Specify which keys/buttons to block during macro execution
5. Click "Save Settings" to preserve your configuration

## Technical Details

### Memory Access
- Uses Windows API functions for process access
- Implements safe memory reading with proper error handling
- Handles multi-level pointer dereferencing

### Input Simulation
- Uses Windows SendInput API for realistic input simulation
- Maintains proper timing between actions
- Supports both keyboard and mouse input

### Shared Memory Integration
- Integrates with the provided dinput8.dll proxy system
- Communicates through shared memory for input manipulation
- Ensures compatibility with game anti-cheat systems

### Threading Model
- Separate threads for pointer reading, macro execution, and input blocking
- Proper thread synchronization to prevent race conditions
- Graceful shutdown of all background processes

## Requirements

- Windows 10 or later
- .NET 6.0 Runtime
- Administrative privileges (recommended)
- Mortal Kombat XL installed

## Safety Notes

- This trainer is designed for single-player use only
- Use responsibly and avoid online multiplayer modes
- Some antivirus software may flag the application
- The trainer does not modify game files permanently

## Development

The source code is organized into logical sections:
- Pointer reading logic
- Macro recording and playback
- Input blocking functionality
- UI components and event handling
- Configuration management
- Localization support

All features are implemented with proper error handling and resource cleanup.