using System;
using System.Runtime.InteropServices;

namespace UniversalGameTrainer
{
    // Structure that matches the shared memory structure expected by dinput8.dll
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SharedInputBuffer
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool bIsActive;           // Set to true to enable override

        // Keyboard: 256 bytes, 0x80 for pressed, 0x00 for released
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] keyboard;

        // Mouse: relative movement and buttons
        public int lX;                 // X-axis movement
        public int lY;                 // Y-axis movement  
        public int lZ;                 // Z-axis movement (wheel)
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] buttons;         // Mouse buttons, 0x80 for pressed

        // Joystick/Gamepad: extended input
        public int lRx;                // X-axis rotation
        public int lRy;                // Y-axis rotation
        public int lRz;                // Z-axis rotation
        public int lVX;                // X-axis velocity
        public int lVY;                // Y-axis velocity
        public int lVZ;                // Z-axis velocity
        public int lVRx;               // X-axis angular velocity
        public int lVRy;               // Y-axis angular velocity
        public int lVRz;               // Z-axis angular velocity
        public int lAX;                // X-axis acceleration
        public int lAY;                // Y-axis acceleration
        public int lAZ;                // Z-axis acceleration
        public int lARx;               // X-axis angular acceleration
        public int lARy;               // Y-axis angular acceleration
        public int lARz;               // Z-axis angular acceleration
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] sliders;          // Two slider controls
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] povs;             // Four POV hat switches
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] joyButtons;      // 128 joystick buttons, 0x80 for pressed
    }

    public class InputManager
    {
        private IntPtr fileMappingHandle = IntPtr.Zero;
        private IntPtr mapViewHandle = IntPtr.Zero;
        private SharedInputBuffer inputBuffer;
        private readonly object bufferLock = new object();

        private const string SHARED_MEMORY_NAME = "Local\\WinData_Input_Feedback";
        private const uint PAGE_READWRITE = 0x04;
        private const uint FILE_MAP_ALL_ACCESS = 0x001F001F;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMappingW(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenFileMappingW(
            uint dwDesiredAccess,
            bool bInheritHandle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        public bool Initialize()
        {
            try
            {
                // Try to open existing shared memory
                fileMappingHandle = OpenFileMappingW(FILE_MAP_ALL_ACCESS, false, SHARED_MEMORY_NAME);
                
                if (fileMappingHandle == IntPtr.Zero)
                {
                    // If it doesn't exist, create it (this is usually done by the DLL)
                    return false;
                }

                // Map the shared memory
                mapViewHandle = MapViewOfFile(fileMappingHandle, FILE_MAP_ALL_ACCESS, 0, 0, (uint)Marshal.SizeOf<SharedInputBuffer>());
                
                if (mapViewHandle == IntPtr.Zero)
                {
                    CloseHandle(fileMappingHandle);
                    fileMappingHandle = IntPtr.Zero;
                    return false;
                }

                // Initialize the input buffer
                inputBuffer = new SharedInputBuffer
                {
                    bIsActive = false,
                    keyboard = new byte[256],
                    buttons = new byte[8],
                    joyButtons = new byte[128],
                    sliders = new int[2],
                    povs = new int[4]
                };

                return true;
            }
            catch
            {
                Cleanup();
                return false;
            }
        }

        public void SetInputActive(bool active)
        {
            lock (bufferLock)
            {
                inputBuffer.bIsActive = active;
                if (active)
                {
                    WriteInputBuffer();
                }
                else
                {
                    // Reset all inputs when deactivating
                    ResetInputBuffer();
                    WriteInputBuffer();
                }
            }
        }

        public void BlockKey(int diKeyCode)
        {
            if (diKeyCode >= 0 && diKeyCode < 256)
            {
                lock (bufferLock)
                {
                    inputBuffer.keyboard[diKeyCode] = 0x00; // Released
                    WriteInputBuffer();
                }
            }
        }

        public void SimulateKeyPress(int diKeyCode, int durationMs)
        {
            // This would be called from a separate thread to simulate a key press
            lock (bufferLock)
            {
                if (diKeyCode >= 0 && diKeyCode < 256)
                {
                    inputBuffer.keyboard[diKeyCode] = 0x80; // Pressed
                    WriteInputBuffer();
                }
            }

            // Wait for the specified duration
            System.Threading.Thread.Sleep(durationMs);

            lock (bufferLock)
            {
                if (diKeyCode >= 0 && diKeyCode < 256)
                {
                    inputBuffer.keyboard[diKeyCode] = 0x00; // Released
                    WriteInputBuffer();
                }
            }
        }

        public void BlockGamepadButton(int buttonIndex)
        {
            if (buttonIndex >= 0 && buttonIndex < 128)
            {
                lock (bufferLock)
                {
                    inputBuffer.joyButtons[buttonIndex] = 0x00; // Released
                    WriteInputBuffer();
                }
            }
        }

        public void SimulateGamepadPress(int buttonIndex, int durationMs)
        {
            lock (bufferLock)
            {
                if (buttonIndex >= 0 && buttonIndex < 128)
                {
                    inputBuffer.joyButtons[buttonIndex] = 0x80; // Pressed
                    WriteInputBuffer();
                }
            }

            // Wait for the specified duration
            System.Threading.Thread.Sleep(durationMs);

            lock (bufferLock)
            {
                if (buttonIndex >= 0 && buttonIndex < 128)
                {
                    inputBuffer.joyButtons[buttonIndex] = 0x00; // Released
                    WriteInputBuffer();
                }
            }
        }

        public void SetGamepadAxis(int axisIndex, short value)
        {
            lock (bufferLock)
            {
                switch (axisIndex)
                {
                    case 0: inputBuffer.lX = value; break; // X-axis
                    case 1: inputBuffer.lY = value; break; // Y-axis
                    case 2: inputBuffer.lZ = value; break; // Z-axis
                    case 3: inputBuffer.lRx = value; break; // X-rotation
                    case 4: inputBuffer.lRy = value; break; // Y-rotation
                    case 5: inputBuffer.lRz = value; break; // Z-rotation
                }
                WriteInputBuffer();
            }
        }

        private void WriteInputBuffer()
        {
            if (mapViewHandle != IntPtr.Zero)
            {
                Marshal.StructureToPtr(inputBuffer, mapViewHandle, false);
            }
        }

        private void ReadInputBuffer()
        {
            if (mapViewHandle != IntPtr.Zero)
            {
                inputBuffer = Marshal.PtrToStructure<SharedInputBuffer>(mapViewHandle);
            }
        }

        private void ResetInputBuffer()
        {
            inputBuffer.bIsActive = false;
            for (int i = 0; i < inputBuffer.keyboard.Length; i++) inputBuffer.keyboard[i] = 0x00;
            for (int i = 0; i < inputBuffer.buttons.Length; i++) inputBuffer.buttons[i] = 0x00;
            for (int i = 0; i < inputBuffer.joyButtons.Length; i++) inputBuffer.joyButtons[i] = 0x00;
            inputBuffer.lX = inputBuffer.lY = inputBuffer.lZ = 0;
            inputBuffer.lRx = inputBuffer.lRy = inputBuffer.lRz = 0;
            for (int i = 0; i < inputBuffer.sliders.Length; i++) inputBuffer.sliders[i] = 0;
            for (int i = 0; i < inputBuffer.povs.Length; i++) inputBuffer.povs[i] = -1;
        }

        public void Cleanup()
        {
            if (mapViewHandle != IntPtr.Zero)
            {
                UnmapViewOfFile(mapViewHandle);
                mapViewHandle = IntPtr.Zero;
            }

            if (fileMappingHandle != IntPtr.Zero)
            {
                CloseHandle(fileMappingHandle);
                fileMappingHandle = IntPtr.Zero;
            }
        }

        ~InputManager()
        {
            Cleanup();
        }
    }
}