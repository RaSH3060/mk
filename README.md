# Universal Game Trainer

A complete, ready-to-compile C# Windows Forms application that serves as a universal trainer for games using the dinput8.dll library (a DirectInput proxy DLL for input manipulation).

## Features

### Multi-level Pointer Reading
- Reads memory via multi-level pointers (e.g., 'MK10.exe'+033C8A98 with offsets 8, 130, 108, 78, 90, 120, F20)
- Reads values every 10 milliseconds (configurable)
- Triggers macros when pointer values match user-defined triggers
- Supports complex pointer chains for accessing game data

### Advanced Macro System
- Records user input including keyboard presses and gamepad inputs
- Saves and loads macro configurations to JSON file
- Executes macros with precise timing control
- Adjustable delays (default 280ms for delay after trigger, 260ms for block duration)
- Supports multiple action types:
  - Keyboard key presses and releases
  - Gamepad button presses
  - Blocking selected keys during macro execution

### Input Manipulation
- Blocks specific keys during macro execution (user-selectable)
- Simulates key presses using dinput8.dll library
- Prevents user input interference during macro playback
- Supports both keyboard and gamepad input manipulation

### Dual Language Support
- English and Russian interface
- Language toggle in settings
- All UI elements are properly translated

### Configuration Management
- Saves all settings to "TrainerSettings.json" file
- Preserves user preferences between sessions
- Configurable process name detection
- Adjustable timing parameters

### User Interface
- Fixed 800x600 interface with tabbed layout
- Multiple tabs for different pointer/macro configurations
- Real-time status indicators
- Emoji-based interface (no images required)
- Responsive design with proper error handling

## Usage Instructions

### Installation
1. Place the dinput8.dll library next to your game executable
2. Launch the trainer and click "🔍 Attach" to select the game process
3. Configure each tab with:
   - Module name (e.g., "MK10.exe")
   - Base offset (hexadecimal)
   - Comma-separated offsets (hexadecimal)
   - Trigger value to activate the macro
   - Read interval, block duration, and delay settings
4. Select keys/buttons to block or simulate
5. Enable the tab and start the macro recording if needed

## Technical Details

### Memory Access
- Uses Windows API functions for process access (OpenProcess, ReadProcessMemory)
- Implements safe memory reading with proper error handling
- Handles multi-level pointer dereferencing

### Input Manipulation
- Communicates with dinput8.dll via shared memory ("Local\WinData_Input_Feedback")
- Overrides input using the SharedInputBuffer structure
- Supports both keyboard (DIK codes) and gamepad input manipulation

### Shared Memory Integration
- Integrates with the provided dinput8.dll proxy system
- Communicates through shared memory for input manipulation
- Uses proper locking mechanisms to prevent conflicts

### Threading Model
- Separate threads for each enabled tab's memory reading
- Proper thread synchronization to prevent race conditions
- Graceful shutdown of all background processes

## Requirements

- Windows 10 or later
- .NET 6.0 Runtime
- Administrative privileges (recommended for memory access)
- Any game that uses DirectInput and supports dinput8.dll

## Safety Notes

- This trainer is designed for single-player use only
- Use responsibly and avoid online multiplayer modes
- Some antivirus software may flag the application due to memory access
- The trainer does not modify game files permanently

## Compilation

The project uses .NET 6.0 Windows Forms and can be compiled with:
```
dotnet build
```

Required NuGet packages:
- System.Text.Json
- Microsoft.VisualBasic (for Interaction.InputBox)

## Supported Games

This trainer is designed as a universal tool that works with any game that uses DirectInput and supports the dinput8.dll proxy library approach.