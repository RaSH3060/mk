# Universal Game Trainer

A comprehensive game trainer application that works with the dinput8.dll library to provide input manipulation capabilities for various games.

## Features

- 🔍 **Multi-level pointer reading**: Supports complex pointer chains like `'MK10.exe'+033C8A98` with configurable offsets
- 🎮 **Input manipulation**: Blocks/simulates keyboard and gamepad inputs via dinput8.dll shared memory
- 🎬 **Advanced macro system**: Record, edit, and play back complex input sequences
- 🌐 **Dual language support**: English and Russian with easy toggle
- 💾 **Persistent settings**: Saves all configurations to JSON file
- 📁 **Tabbed interface**: Multiple independent configurations running simultaneously
- ⚙️ **Configurable timing**: Adjustable read intervals, block durations, and delays

## Architecture

The application is organized into multiple files for maintainability:

- `Program.cs` - Application entry point
- `MainForm.cs` - Main application window and UI logic
- `Models.cs` - Data models and core logic
- `TabContentControl.cs` - Individual tab configuration UI
- `Dialogs.cs` - Modal dialogs (attach, settings)
- `InputManipulation.cs` - DirectInput shared memory interaction
- `LocalizedStrings.cs` - Translation strings
- `UniversalGameTrainer.csproj` - Project definition

## Usage

1. Place `dinput8.dll` next to the target game executable
2. Launch the trainer and attach to the game process
3. Configure pointer values and offsets for your target game
4. Set trigger values and macro actions
5. Enable the tab and activate the trainer

## Requirements

- Windows OS
- .NET 6.0 Runtime
- Target game with DirectInput support

## How It Works

The trainer connects to the dinput8.dll shared memory (`Local\WinData_Input_Feedback`) to intercept and manipulate input between the game and hardware. When configured pointers reach their trigger values, the trainer can block specific inputs or simulate new ones according to the configured macro.

## Configuration

Each tab supports:
- Base module name and offset
- Multi-level pointer offsets
- Trigger value detection
- Key blocking with configurable duration
- Macro recording and playback
- Per-tab enable/disable toggle

Settings are saved to `TrainerSettings.json` in the application directory.