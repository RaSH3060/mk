using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace UniversalGameTrainer
{
    // Settings class to store all application settings
    public class Settings
    {
        public string Language { get; set; } = "EN";
        public List<TabPageData> TabPageDataList { get; set; } = new List<TabPageData>();
        public string LastExeName { get; set; } = "game.exe";
    }

    // Data structure for each tab
    public class TabPageData
    {
        public string Name { get; set; } = "New Tab";
        public bool IsEnabled { get; set; } = true;
        public string ModuleName { get; set; } = "game.exe";
        public string BaseOffset { get; set; } = "00000000";
        public string Offsets { get; set; } = "00,00,00";
        public int TriggerValue { get; set; } = 0;
        public int ReadIntervalMs { get; set; } = 10;
        public int BlockDurationMs { get; set; } = 260;
        public int DelayAfterTriggerMs { get; set; } = 280;
        public List<string> KeysToBlock { get; set; } = new List<string>();
        public List<MacroAction> MacroActions { get; set; } = new List<MacroAction>();
        public bool IsMacroRecording { get; set; } = false;
        public bool IsMacroEnabled { get; set; } = false;
        
        // Runtime properties
        public TabPage TabPage { get; set; }
        public Thread MonitorThread { get; set; }
        public bool IsMonitoring { get; set; } = false;
        public Process AttachedProcess { get; set; }

        public TabPageData Clone()
        {
            return new TabPageData
            {
                Name = this.Name,
                IsEnabled = this.IsEnabled,
                ModuleName = this.ModuleName,
                BaseOffset = this.BaseOffset,
                Offsets = this.Offsets,
                TriggerValue = this.TriggerValue,
                ReadIntervalMs = this.ReadIntervalMs,
                BlockDurationMs = this.BlockDurationMs,
                DelayAfterTriggerMs = this.DelayAfterTriggerMs,
                KeysToBlock = new List<string>(this.KeysToBlock),
                MacroActions = new List<MacroAction>(this.MacroActions),
                IsMacroEnabled = this.IsMacroEnabled,
                TabPage = this.TabPage // This will need to be reassigned when loading
            };
        }

        public void StartMonitoring(Process process)
        {
            if (IsMonitoring || !IsEnabled) return;

            AttachedProcess = process;
            IsMonitoring = true;
            MonitorThread = new Thread(MonitorMemoryLoop);
            MonitorThread.IsBackground = true;
            MonitorThread.Start();
        }

        public void StopMonitoring()
        {
            IsMonitoring = false;
            MonitorThread?.Join(100); // Wait up to 100ms for thread to finish
        }

        private void MonitorMemoryLoop()
        {
            var processHandle = IntPtr.Zero;
            try
            {
                var processId = AttachedProcess.Id;
                const int PROCESS_VM_READ = 0x0010;
                processHandle = OpenProcess(PROCESS_VM_READ, false, processId);

                if (processHandle == IntPtr.Zero)
                {
                    IsMonitoring = false;
                    return;
                }

                while (IsMonitoring)
                {
                    try
                    {
                        var currentValue = ReadMultiLevelPointer(processHandle, ModuleName, BaseOffset, Offsets);
                        if (currentValue == TriggerValue)
                        {
                            // Trigger macro
                            ExecuteMacro();
                        }
                    }
                    catch
                    {
                        // Ignore errors during memory reading
                    }

                    Thread.Sleep(ReadIntervalMs);
                }
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        private int ReadMultiLevelPointer(IntPtr processHandle, string moduleName, string baseOffsetStr, string offsetsStr)
        {
            // Get module base address
            IntPtr moduleBase = GetModuleBaseAddress(AttachedProcess, moduleName);
            if (moduleBase == IntPtr.Zero) return 0;

            // Parse base offset
            if (!int.TryParse(baseOffsetStr.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out int baseOffset))
                return 0;

            IntPtr currentAddress = IntPtr.Add(moduleBase, baseOffset);

            // Parse and apply offsets
            var offsets = offsetsStr.Split(',');
            foreach (var offsetStr in offsets)
            {
                if (int.TryParse(offsetStr.Trim().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out int offset))
                {
                    byte[] buffer = new byte[8]; // Read 8 bytes for 64-bit pointer
                    int bytesRead = 0;
                    if (!ReadProcessMemory(processHandle, currentAddress, buffer, buffer.Length, ref bytesRead))
                        return 0;

                    // Interpret as pointer (little-endian)
                    long ptrValue = BitConverter.ToInt64(buffer, 0);
                    currentAddress = new IntPtr(ptrValue);
                    currentAddress = IntPtr.Add(currentAddress, offset);
                }
            }

            // Read final value
            byte[] valueBuffer = new byte[4]; // Assuming int32 value
            int valueBytesRead = 0;
            if (!ReadProcessMemory(processHandle, currentAddress, valueBuffer, valueBuffer.Length, ref valueBytesRead))
                return 0;

            return BitConverter.ToInt32(valueBuffer, 0);
        }

        private IntPtr GetModuleBaseAddress(Process process, string moduleName)
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
            return IntPtr.Zero;
        }

        private void ExecuteMacro()
        {
            // Create input manager to interact with dinput8.dll shared memory
            var inputManager = new InputManager();
            if (!inputManager.Initialize())
            {
                // Could not connect to shared memory
                return;
            }

            try
            {
                // Activate input override
                inputManager.SetInputActive(true);

                // Apply key blocking if enabled
                if (KeysToBlock.Count > 0)
                {
                    // Block the specified keys for the configured duration
                    foreach (var key in KeysToBlock)
                    {
                        if (int.TryParse(key, out int keyCode))
                        {
                            inputManager.BlockKey(keyCode);
                        }
                    }
                    
                    // Hold for the configured duration
                    System.Threading.Thread.Sleep(BlockDurationMs);
                }

                // Execute macro actions if enabled
                if (IsMacroEnabled && MacroActions.Count > 0)
                {
                    // Delay before executing macro
                    System.Threading.Thread.Sleep(DelayAfterTriggerMs);
                    
                    foreach (var action in MacroActions)
                    {
                        switch (action.ActionType.ToLower())
                        {
                            case "keypress":
                                if (int.TryParse(action.KeyName, out int keyPressCode))
                                {
                                    inputManager.SimulateKeyPress(keyPressCode, action.DurationMs);
                                }
                                break;
                            case "gamepadpress":
                                if (int.TryParse(action.KeyName, out int buttonIndex))
                                {
                                    inputManager.SimulateGamepadPress(buttonIndex, action.DurationMs);
                                }
                                break;
                            case "setaxis":
                                if (int.TryParse(action.KeyName, out int axisIndex))
                                {
                                    short value = (short)action.Value;
                                    inputManager.SetGamepadAxis(axisIndex, value);
                                }
                                break;
                            case "delay":
                                System.Threading.Thread.Sleep(action.DurationMs);
                                break;
                        }
                    }
                }
            }
            finally
            {
                // Deactivate input override and reset
                inputManager.SetInputActive(false);
                inputManager.Cleanup();
            }
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
    }

    // Macro action data structure
    public class MacroAction
    {
        public string ActionType { get; set; } // "KeyPress", "KeyHold", "Delay", etc.
        public string KeyName { get; set; }
        public int DurationMs { get; set; }
        public int Value { get; set; } // Additional value for axes or other parameters
    }
}