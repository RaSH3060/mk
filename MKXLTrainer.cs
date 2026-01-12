using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.MemoryMappedFiles;

namespace MKXLTrainer
{
    public partial class MainForm : Form
    {
        #region Native Methods and Structures
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys key);

        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;
        public const uint INPUT_HARDWARE = 2;

        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_HWHEEL = 0x1000;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_SCANCODE = 0x0008;

        private const int PROCESS_WM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        #endregion

        #region Variables and Settings
        private Process targetProcess = null;
        private IntPtr processHandle = IntPtr.Zero;
        private bool isReadingPointer = false;
        private bool isBlockingInput = false;
        private bool isMacroRunning = false;
        private bool isMacroRecording = false;
        private Thread pointerReadThread = null;
        private Thread inputBlockThread = null;
        private Thread macroThread = null;
        private List<MacroAction> recordedMacro = new List<MacroAction>();
        private List<MacroAction> currentMacro = new List<MacroAction>();
        private readonly object macroLock = new object();
        private Dictionary<string, Keys> blockedKeys = new Dictionary<string, Keys>();
        private List<GamepadButton> blockedGamepadButtons = new List<GamepadButton>();
        private readonly object inputBlockLock = new object();
        
        // Settings
        private int pointerReadInterval = 10; // ms
        private int inputBlockDelay = 260; // ms (adjustable)
        private int macroStartDelay = 280; // ms (adjustable)
        private string processName = "MK10.exe";
        private string trainerTitle = "MKXL Trainer";
        private bool languageIsRussian = true;
        
        // Pointer settings
        private string baseAddressStr = "033C8A98";
        private List<int> offsets = new List<int> { 8, 0x130, 0x108, 0x78, 0x90, 0x120, 0xF20 };
        
        // UI Components
        private TabControl mainTabControl;
        private TabPage pointersTab;
        private TabPage macrosTab;
        private TabPage settingsTab;
        
        // Pointers Tab Controls
        private TextBox baseAddressTextBox;
        private TextBox offsetsTextBox;
        private TextBox pointerResultTextBox;
        private Button startStopPointerBtn;
        private Label statusLabel;
        
        // Macros Tab Controls
        private ListBox macroListBox;
        private Button startStopMacroBtn;
        private Button recordMacroBtn;
        private Button stopRecordMacroBtn;
        private Button clearMacroBtn;
        private Button saveMacroBtn;
        private Button loadMacroBtn;
        private CheckBox macroEnabledCheckBox;
        private NumericUpDown macroDelayNumeric;
        
        // Settings Tab Controls
        private TextBox processNameTextBox;
        private NumericUpDown readIntervalNumeric;
        private NumericUpDown blockDelayNumeric;
        private NumericUpDown macroDelayStartNumeric;
        private ComboBox languageComboBox;
        private TextBox blockedKeysTextBox;
        private TextBox blockedGamepadTextBox;
        private Button saveSettingsBtn;
        #endregion

        #region Enums
        public enum MacroActionType
        {
            KeyDown,
            KeyUp,
            MouseClick,
            MouseMove,
            Delay,
            GamepadButtonDown,
            GamepadButtonUp
        }

        public enum GamepadButton
        {
            A, B, X, Y, LB, RB, LT, RT, Up, Down, Left, Right, Start, Back, LS, RS
        }
        #endregion

        #region Data Classes
        public class MacroAction
        {
            public MacroActionType Type { get; set; }
            public Keys Key { get; set; }
            public GamepadButton GamepadBtn { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int DelayMs { get; set; }
            
            public MacroAction(MacroActionType type, Keys key = Keys.None, GamepadButton gamepadBtn = GamepadButton.A, int x = 0, int y = 0, int delayMs = 0)
            {
                Type = type;
                Key = key;
                GamepadBtn = gamepadBtn;
                X = x;
                Y = y;
                DelayMs = delayMs;
            }
        }
        #endregion

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            UpdateLanguage();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.Text = trainerTitle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            InitializeTabs();
            SetupPointerTab();
            SetupMacrosTab();
            SetupSettingsTab();
        }

        private void InitializeTabs()
        {
            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;

            pointersTab = new TabPage();
            macrosTab = new TabPage();
            settingsTab = new TabPage();

            mainTabControl.TabPages.Add(pointersTab);
            mainTabControl.TabPages.Add(macrosTab);
            mainTabControl.TabPages.Add(settingsTab);

            this.Controls.Add(mainTabControl);
        }

        private void SetupPointerTab()
        {
            pointersTab.Text = languageIsRussian ? "Указатели" : "Pointers";
            
            // Base address
            var baseAddrLabel = new Label();
            baseAddrLabel.Text = languageIsRussian ? "Базовый адрес:" : "Base Address:";
            baseAddrLabel.Location = new Point(20, 20);
            baseAddrLabel.AutoSize = true;
            pointersTab.Controls.Add(baseAddrLabel);
            
            baseAddressTextBox = new TextBox();
            baseAddressTextBox.Location = new Point(120, 20);
            baseAddressTextBox.Size = new Size(100, 20);
            baseAddressTextBox.Text = baseAddressStr;
            pointersTab.Controls.Add(baseAddressTextBox);
            
            // Offsets
            var offsetsLabel = new Label();
            offsetsLabel.Text = languageIsRussian ? "Смещения (через запятую):" : "Offsets (comma separated):";
            offsetsLabel.Location = new Point(20, 50);
            offsetsLabel.AutoSize = true;
            pointersTab.Controls.Add(offsetsLabel);
            
            offsetsTextBox = new TextBox();
            offsetsTextBox.Location = new Point(20, 70);
            offsetsTextBox.Size = new Size(200, 60);
            offsetsTextBox.Multiline = true;
            offsetsTextBox.ScrollBars = ScrollBars.Vertical;
            offsetsTextBox.Text = string.Join(", ", offsets.Select(o => "0x" + o.ToString("X")));
            pointersTab.Controls.Add(offsetsTextBox);
            
            // Result display
            var resultLabel = new Label();
            resultLabel.Text = languageIsRussian ? "Результат чтения:" : "Read Result:";
            resultLabel.Location = new Point(20, 140);
            resultLabel.AutoSize = true;
            pointersTab.Controls.Add(resultLabel);
            
            pointerResultTextBox = new TextBox();
            pointerResultTextBox.Location = new Point(20, 160);
            pointerResultTextBox.Size = new Size(200, 200);
            pointerResultTextBox.Multiline = true;
            pointerResultTextBox.ReadOnly = true;
            pointerResultTextBox.ScrollBars = ScrollBars.Vertical;
            pointersTab.Controls.Add(pointerResultTextBox);
            
            // Control buttons
            startStopPointerBtn = new Button();
            startStopPointerBtn.Location = new Point(20, 370);
            startStopPointerBtn.Size = new Size(100, 30);
            startStopPointerBtn.Text = languageIsRussian ? "Запустить" : "Start";
            startStopPointerBtn.Click += StartStopPointerBtn_Click;
            pointersTab.Controls.Add(startStopPointerBtn);
            
            statusLabel = new Label();
            statusLabel.Location = new Point(130, 375);
            statusLabel.Size = new Size(200, 20);
            statusLabel.Text = languageIsRussian ? "Статус: Остановлен" : "Status: Stopped";
            pointersTab.Controls.Add(statusLabel);
        }

        private void SetupMacrosTab()
        {
            macrosTab.Text = languageIsRussian ? "Макросы" : "Macros";
            
            // Macro list
            var macroListLabel = new Label();
            macroListLabel.Text = languageIsRussian ? "Список действий макроса:" : "Macro Actions List:";
            macroListLabel.Location = new Point(20, 20);
            macroListLabel.AutoSize = true;
            macrosTab.Controls.Add(macroListLabel);
            
            macroListBox = new ListBox();
            macroListBox.Location = new Point(20, 40);
            macroListBox.Size = new Size(300, 200);
            macrosTab.Controls.Add(macroListBox);
            
            // Control buttons
            recordMacroBtn = new Button();
            recordMacroBtn.Location = new Point(20, 250);
            recordMacroBtn.Size = new Size(100, 30);
            recordMacroBtn.Text = languageIsRussian ? "Запись" : "Record";
            recordMacroBtn.Click += RecordMacroBtn_Click;
            macrosTab.Controls.Add(recordMacroBtn);
            
            stopRecordMacroBtn = new Button();
            stopRecordMacroBtn.Location = new Point(130, 250);
            stopRecordMacroBtn.Size = new Size(100, 30);
            stopRecordMacroBtn.Text = languageIsRussian ? "Стоп" : "Stop";
            stopRecordMacroBtn.Enabled = false;
            stopRecordMacroBtn.Click += StopRecordMacroBtn_Click;
            macrosTab.Controls.Add(stopRecordMacroBtn);
            
            startStopMacroBtn = new Button();
            startStopMacroBtn.Location = new Point(240, 250);
            startStopMacroBtn.Size = new Size(100, 30);
            startStopMacroBtn.Text = languageIsRussian ? "Выполнить" : "Execute";
            startStopMacroBtn.Click += StartStopMacroBtn_Click;
            macrosTab.Controls.Add(startStopMacroBtn);
            
            clearMacroBtn = new Button();
            clearMacroBtn.Location = new Point(20, 290);
            clearMacroBtn.Size = new Size(100, 30);
            clearMacroBtn.Text = languageIsRussian ? "Очистить" : "Clear";
            clearMacroBtn.Click += ClearMacroBtn_Click;
            macrosTab.Controls.Add(clearMacroBtn);
            
            saveMacroBtn = new Button();
            saveMacroBtn.Location = new Point(130, 290);
            saveMacroBtn.Size = new Size(100, 30);
            saveMacroBtn.Text = languageIsRussian ? "Сохранить" : "Save";
            saveMacroBtn.Click += SaveMacroBtn_Click;
            macrosTab.Controls.Add(saveMacroBtn);
            
            loadMacroBtn = new Button();
            loadMacroBtn.Location = new Point(240, 290);
            loadMacroBtn.Size = new Size(100, 30);
            loadMacroBtn.Text = languageIsRussian ? "Загрузить" : "Load";
            loadMacroBtn.Click += LoadMacroBtn_Click;
            macrosTab.Controls.Add(loadMacroBtn);
            
            // Options
            macroEnabledCheckBox = new CheckBox();
            macroEnabledCheckBox.Location = new Point(20, 330);
            macroEnabledCheckBox.Size = new Size(200, 20);
            macroEnabledCheckBox.Text = languageIsRussian ? "Включить макрос" : "Enable Macro";
            macrosTab.Controls.Add(macroEnabledCheckBox);
            
            var macroDelayLabel = new Label();
            macroDelayLabel.Text = languageIsRussian ? "Задержка старта (мс):" : "Start Delay (ms):";
            macroDelayLabel.Location = new Point(20, 360);
            macroDelayLabel.AutoSize = true;
            macrosTab.Controls.Add(macroDelayLabel);
            
            macroDelayNumeric = new NumericUpDown();
            macroDelayNumeric.Location = new Point(150, 360);
            macroDelayNumeric.Size = new Size(100, 20);
            macroDelayNumeric.Minimum = 0;
            macroDelayNumeric.Maximum = 5000;
            macroDelayNumeric.Value = macroStartDelay;
            macrosTab.Controls.Add(macroDelayNumeric);
        }

        private void SetupSettingsTab()
        {
            settingsTab.Text = languageIsRussian ? "Настройки" : "Settings";
            
            // Process name
            var procNameLabel = new Label();
            procNameLabel.Text = languageIsRussian ? "Имя процесса:" : "Process Name:";
            procNameLabel.Location = new Point(20, 20);
            procNameLabel.AutoSize = true;
            settingsTab.Controls.Add(procNameLabel);
            
            processNameTextBox = new TextBox();
            processNameTextBox.Location = new Point(120, 20);
            processNameTextBox.Size = new Size(100, 20);
            processNameTextBox.Text = processName;
            settingsTab.Controls.Add(processNameTextBox);
            
            // Read interval
            var readIntervalLabel = new Label();
            readIntervalLabel.Text = languageIsRussian ? "Интервал чтения (мс):" : "Read Interval (ms):";
            readIntervalLabel.Location = new Point(20, 50);
            readIntervalLabel.AutoSize = true;
            settingsTab.Controls.Add(readIntervalLabel);
            
            readIntervalNumeric = new NumericUpDown();
            readIntervalNumeric.Location = new Point(150, 50);
            readIntervalNumeric.Size = new Size(100, 20);
            readIntervalNumeric.Minimum = 1;
            readIntervalNumeric.Maximum = 1000;
            readIntervalNumeric.Value = pointerReadInterval;
            settingsTab.Controls.Add(readIntervalNumeric);
            
            // Block delay
            var blockDelayLabel = new Label();
            blockDelayLabel.Text = languageIsRussian ? "Задержка блокировки (мс):" : "Block Delay (ms):";
            blockDelayLabel.Location = new Point(20, 80);
            blockDelayLabel.AutoSize = true;
            settingsTab.Controls.Add(blockDelayLabel);
            
            blockDelayNumeric = new NumericUpDown();
            blockDelayNumeric.Location = new Point(150, 80);
            blockDelayNumeric.Size = new Size(100, 20);
            blockDelayNumeric.Minimum = 0;
            blockDelayNumeric.Maximum = 5000;
            blockDelayNumeric.Value = inputBlockDelay;
            settingsTab.Controls.Add(blockDelayNumeric);
            
            // Macro start delay
            var macroDelayStartLabel = new Label();
            macroDelayStartLabel.Text = languageIsRussian ? "Задержка старта макроса (мс):" : "Macro Start Delay (ms):";
            macroDelayStartLabel.Location = new Point(20, 110);
            macroDelayStartLabel.AutoSize = true;
            settingsTab.Controls.Add(macroDelayStartLabel);
            
            macroDelayStartNumeric = new NumericUpDown();
            macroDelayStartNumeric.Location = new Point(180, 110);
            macroDelayStartNumeric.Size = new Size(100, 20);
            macroDelayStartNumeric.Minimum = 0;
            macroDelayStartNumeric.Maximum = 5000;
            macroDelayStartNumeric.Value = macroStartDelay;
            settingsTab.Controls.Add(macroDelayStartNumeric);
            
            // Language
            var langLabel = new Label();
            langLabel.Text = languageIsRussian ? "Язык:" : "Language:";
            langLabel.Location = new Point(20, 140);
            langLabel.AutoSize = true;
            settingsTab.Controls.Add(langLabel);
            
            languageComboBox = new ComboBox();
            languageComboBox.Location = new Point(80, 140);
            languageComboBox.Size = new Size(100, 20);
            languageComboBox.Items.Add("Русский");
            languageComboBox.Items.Add("English");
            languageComboBox.SelectedIndex = languageIsRussian ? 0 : 1;
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            settingsTab.Controls.Add(languageComboBox);
            
            // Blocked keys
            var blockedKeysLabel = new Label();
            blockedKeysLabel.Text = languageIsRussian ? "Блокируемые клавиши (через запятую):" : "Blocked Keys (comma separated):";
            blockedKeysLabel.Location = new Point(20, 170);
            blockedKeysLabel.AutoSize = true;
            settingsTab.Controls.Add(blockedKeysLabel);
            
            blockedKeysTextBox = new TextBox();
            blockedKeysTextBox.Location = new Point(20, 190);
            blockedKeysTextBox.Size = new Size(200, 60);
            blockedKeysTextBox.Multiline = true;
            blockedKeysTextBox.ScrollBars = ScrollBars.Vertical;
            settingsTab.Controls.Add(blockedKeysTextBox);
            
            // Blocked gamepad buttons
            var blockedGamepadLabel = new Label();
            blockedGamepadLabel.Text = languageIsRussian ? "Блокируемые кнопки геймпада:" : "Blocked Gamepad Buttons:";
            blockedGamepadLabel.Location = new Point(20, 260);
            blockedGamepadLabel.AutoSize = true;
            settingsTab.Controls.Add(blockedGamepadLabel);
            
            blockedGamepadTextBox = new TextBox();
            blockedGamepadTextBox.Location = new Point(20, 280);
            blockedGamepadTextBox.Size = new Size(200, 60);
            blockedGamepadTextBox.Multiline = true;
            blockedGamepadTextBox.ScrollBars = ScrollBars.Vertical;
            settingsTab.Controls.Add(blockedGamepadTextBox);
            
            // Save button
            saveSettingsBtn = new Button();
            saveSettingsBtn.Location = new Point(20, 350);
            saveSettingsBtn.Size = new Size(100, 30);
            saveSettingsBtn.Text = languageIsRussian ? "Сохранить настройки" : "Save Settings";
            saveSettingsBtn.Click += SaveSettingsBtn_Click;
            settingsTab.Controls.Add(saveSettingsBtn);
        }

        private void UpdateLanguage()
        {
            pointersTab.Text = languageIsRussian ? "Указатели" : "Pointers";
            macrosTab.Text = languageIsRussian ? "Макросы" : "Macros";
            settingsTab.Text = languageIsRussian ? "Настройки" : "Settings";
            
            // Update Pointers Tab
            var controlsPtr = pointersTab.Controls;
            foreach(Control c in controlsPtr)
            {
                if(c is Label label)
                {
                    if(label.Text.Contains("Базовый адрес") || label.Text.Contains("Base Address"))
                        label.Text = languageIsRussian ? "Базовый адрес:" : "Base Address:";
                    else if(label.Text.Contains("Смещения") || label.Text.Contains("Offsets"))
                        label.Text = languageIsRussian ? "Смещения (через запятую):" : "Offsets (comma separated):";
                    else if(label.Text.Contains("Результат") || label.Text.Contains("Read Result"))
                        label.Text = languageIsRussian ? "Результат чтения:" : "Read Result:";
                }
                else if(c is Button btn)
                {
                    if(btn.Text.Contains("Запустить") || btn.Text.Contains("Start"))
                        btn.Text = languageIsRussian ? "Запустить" : "Start";
                }
                else if(c is Label statLabel && statLabel != statusLabel)
                {
                    if(statLabel.Text.Contains("Статус") || statLabel.Text.Contains("Status"))
                        statLabel.Text = languageIsRussian ? "Статус: Остановлен" : "Status: Stopped";
                }
            }
            
            if(statusLabel != null)
                statusLabel.Text = languageIsRussian ? "Статус: Остановлен" : "Status: Stopped";
            
            if(startStopPointerBtn != null)
                startStopPointerBtn.Text = languageIsRussian ? "Запустить" : "Start";
            
            // Update Macros Tab
            var controlsMacro = macrosTab.Controls;
            foreach(Control c in controlsMacro)
            {
                if(c is Label label)
                {
                    if(label.Text.Contains("Список") || label.Text.Contains("Macro Actions"))
                        label.Text = languageIsRussian ? "Список действий макроса:" : "Macro Actions List:";
                }
                else if(c is Button btn)
                {
                    if(btn.Text.Contains("Запись") || btn.Text.Contains("Record"))
                        btn.Text = languageIsRussian ? "Запись" : "Record";
                    else if(btn.Text.Contains("Стоп") || btn.Text.Contains("Stop"))
                        btn.Text = languageIsRussian ? "Стоп" : "Stop";
                    else if(btn.Text.Contains("Выполнить") || btn.Text.Contains("Execute"))
                        btn.Text = languageIsRussian ? "Выполнить" : "Execute";
                    else if(btn.Text.Contains("Очистить") || btn.Text.Contains("Clear"))
                        btn.Text = languageIsRussian ? "Очистить" : "Clear";
                    else if(btn.Text.Contains("Сохранить") || btn.Text.Contains("Save"))
                        btn.Text = languageIsRussian ? "Сохранить" : "Save";
                    else if(btn.Text.Contains("Загрузить") || btn.Text.Contains("Load"))
                        btn.Text = languageIsRussian ? "Загрузить" : "Load";
                }
                else if(c is CheckBox chk)
                {
                    if(chk.Text.Contains("Включить") || chk.Text.Contains("Enable"))
                        chk.Text = languageIsRussian ? "Включить макрос" : "Enable Macro";
                }
                else if(c is Label label2)
                {
                    if(label2.Text.Contains("Задержка") || label2.Text.Contains("Start Delay"))
                        label2.Text = languageIsRussian ? "Задержка старта (мс):" : "Start Delay (ms):";
                }
            }
            
            // Update Settings Tab
            var controlsSettings = settingsTab.Controls;
            foreach(Control c in controlsSettings)
            {
                if(c is Label label)
                {
                    if(label.Text.Contains("Имя") || label.Text.Contains("Process Name"))
                        label.Text = languageIsRussian ? "Имя процесса:" : "Process Name:";
                    else if(label.Text.Contains("Интервал") || label.Text.Contains("Read Interval"))
                        label.Text = languageIsRussian ? "Интервал чтения (мс):" : "Read Interval (ms):";
                    else if(label.Text.Contains("Задержка блокировки") || label.Text.Contains("Block Delay"))
                        label.Text = languageIsRussian ? "Задержка блокировки (мс):" : "Block Delay (ms):";
                    else if(label.Text.Contains("Задержка старта макроса") || label.Text.Contains("Macro Start Delay"))
                        label.Text = languageIsRussian ? "Задержка старта макроса (мс):" : "Macro Start Delay (ms):";
                    else if(label.Text.Contains("Язык") || label.Text.Contains("Language"))
                        label.Text = languageIsRussian ? "Язык:" : "Language:";
                    else if(label.Text.Contains("Блокируемые клавиши") || label.Text.Contains("Blocked Keys"))
                        label.Text = languageIsRussian ? "Блокируемые клавиши (через запятую):" : "Blocked Keys (comma separated):";
                    else if(label.Text.Contains("Блокируемые кнопки") || label.Text.Contains("Blocked Gamepad"))
                        label.Text = languageIsRussian ? "Блокируемые кнопки геймпада:" : "Blocked Gamepad Buttons:";
                }
                else if(c is Button btn)
                {
                    if(btn.Text.Contains("Сохранить") || btn.Text.Contains("Save Settings"))
                        btn.Text = languageIsRussian ? "Сохранить настройки" : "Save Settings";
                }
            }
            
            this.Text = trainerTitle;
        }

        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            languageIsRussian = languageComboBox.SelectedIndex == 0;
            UpdateLanguage();
        }

        private void StartStopPointerBtn_Click(object sender, EventArgs e)
        {
            if (!isReadingPointer)
            {
                StartPointerReading();
            }
            else
            {
                StopPointerReading();
            }
        }

        private void StartPointerReading()
        {
            try
            {
                targetProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName))[0];
                processHandle = OpenProcess(PROCESS_WM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, targetProcess.Id);
                
                if (processHandle == IntPtr.Zero)
                {
                    MessageBox.Show(languageIsRussian ? "Не удалось получить доступ к процессу!" : "Failed to access process!");
                    return;
                }
                
                isReadingPointer = true;
                startStopPointerBtn.Text = languageIsRussian ? "Остановить" : "Stop";
                statusLabel.Text = languageIsRussian ? "Статус: Читает" : "Status: Reading";
                
                pointerReadThread = new Thread(PointerReadLoop);
                pointerReadThread.IsBackground = true;
                pointerReadThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(languageIsRussian ? $"Ошибка при запуске чтения указателя: {ex.Message}" : $"Error starting pointer reading: {ex.Message}");
            }
        }

        private void StopPointerReading()
        {
            isReadingPointer = false;
            if (pointerReadThread != null && pointerReadThread.IsAlive)
            {
                pointerReadThread.Join(1000);
            }
            
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
                processHandle = IntPtr.Zero;
            }
            
            startStopPointerBtn.Text = languageIsRussian ? "Запустить" : "Start";
            statusLabel.Text = languageIsRussian ? "Статус: Остановлен" : "Status: Stopped";
        }

        private void PointerReadLoop()
        {
            while (isReadingPointer)
            {
                try
                {
                    IntPtr result = ResolveMultiLevelPointer();
                    
                    this.Invoke((MethodInvoker)delegate
                    {
                        pointerResultTextBox.AppendText($"0x{result.ToInt64():X}\r\n");
                        if (pointerResultTextBox.Lines.Length > 50)
                        {
                            string[] lines = pointerResultTextBox.Lines;
                            pointerResultTextBox.Text = string.Join("\r\n", lines.Skip(lines.Length - 50));
                        }
                    });
                    
                    Thread.Sleep(pointerReadInterval);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in pointer read loop: {ex.Message}");
                    break;
                }
            }
        }

        private IntPtr ResolveMultiLevelPointer()
        {
            try
            {
                // Parse base address
                if (!int.TryParse(baseAddressStr.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out int baseAddr))
                {
                    return IntPtr.Zero;
                }
                
                IntPtr currentAddress = IntPtr.Add(targetProcess.MainModule.BaseAddress, baseAddr);
                
                // Apply offsets
                foreach (int offset in offsets)
                {
                    byte[] buffer = new byte[8]; // Read 8 bytes for 64-bit pointer
                    int bytesRead = 0;
                    
                    if (!ReadProcessMemory(processHandle, currentAddress, buffer, 8, ref bytesRead))
                    {
                        return IntPtr.Zero;
                    }
                    
                    long ptrValue = BitConverter.ToInt64(buffer, 0);
                    currentAddress = IntPtr.Add(new IntPtr(ptrValue), offset);
                }

                return currentAddress;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resolving multi-level pointer: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        private void RecordMacroBtn_Click(object sender, EventArgs e)
        {
            if (!isMacroRecording)
            {
                StartMacroRecording();
            }
        }

        private void StartMacroRecording()
        {
            isMacroRecording = true;
            recordMacroBtn.Enabled = false;
            stopRecordMacroBtn.Enabled = true;
            macroListBox.Items.Clear();
            recordedMacro.Clear();

            MessageBox.Show(languageIsRussian ? "Началась запись макроса. Нажмите клавиши/кнопки геймпада для записи." : "Macro recording started. Press keys/gamepad buttons to record.");

            Thread recordThread = new Thread(RecordMacroLoop);
            recordThread.IsBackground = true;
            recordThread.Start();
        }

        private void RecordMacroLoop()
        {
            DateTime startTime = DateTime.Now;

            while (isMacroRecording)
            {
                // Check keyboard input
                for (int i = 0; i < 256; i++)
                {
                    Keys key = (Keys)i;
                    if (GetAsyncKeyState(key) < 0)
                    {
                        // Key is pressed
                        if (!blockedKeys.ContainsKey(key.ToString()))
                        {
                            lock (macroLock)
                            {
                                TimeSpan elapsed = DateTime.Now - startTime;
                                recordedMacro.Add(new MacroAction(MacroActionType.KeyDown, key: key, delayMs: (int)elapsed.TotalMilliseconds));
                                startTime = DateTime.Now;

                                this.Invoke((MethodInvoker)delegate
                                {
                                    macroListBox.Items.Add($"{languageIsRussian ? "Клавиша" : "Key"} {key} - {languageIsRussian ? "нажата" : "pressed"}");
                                    macroListBox.TopIndex = macroListBox.Items.Count - 1;
                                });
                            }

                            // Wait until key is released
                            while (GetAsyncKeyState(key) < 0)
                            {
                                Thread.Sleep(10);
                            }

                            lock (macroLock)
                            {
                                TimeSpan elapsedAfter = DateTime.Now - startTime;
                                recordedMacro.Add(new MacroAction(MacroActionType.KeyUp, key: key, delayMs: (int)elapsedAfter.TotalMilliseconds));
                                startTime = DateTime.Now;

                                this.Invoke((MethodInvoker)delegate
                                {
                                    macroListBox.Items.Add($"{languageIsRussian ? "Клавиша" : "Key"} {key} - {languageIsRussian ? "отпущена" : "released"}");
                                    macroListBox.TopIndex = macroListBox.Items.Count - 1;
                                });
                            }
                        }
                    }
                }

                // Check mouse input
                // Mouse left button
                if (GetAsyncKeyState(Keys.LButton) < 0)
                {
                    lock (macroLock)
                    {
                        TimeSpan elapsed = DateTime.Now - startTime;
                        recordedMacro.Add(new MacroAction(MacroActionType.MouseDown, delayMs: (int)elapsed.TotalMilliseconds));
                        startTime = DateTime.Now;

                        this.Invoke((MethodInvoker)delegate
                        {
                            macroListBox.Items.Add($"{languageIsRussian ? "Левая кнопка мыши" : "Left Mouse Button"} - {languageIsRussian ? "нажата" : "clicked"}");
                            macroListBox.TopIndex = macroListBox.Items.Count - 1;
                        });
                    }

                    // Wait until released
                    while (GetAsyncKeyState(Keys.LButton) < 0)
                    {
                        Thread.Sleep(10);
                    }
                }

                // Mouse right button
                if (GetAsyncKeyState(Keys.RButton) < 0)
                {
                    lock (macroLock)
                    {
                        TimeSpan elapsed = DateTime.Now - startTime;
                        recordedMacro.Add(new MacroAction(MacroActionType.MouseDown, delayMs: (int)elapsed.TotalMilliseconds));
                        startTime = DateTime.Now;

                        this.Invoke((MethodInvoker)delegate
                        {
                            macroListBox.Items.Add($"{languageIsRussian ? "Правая кнопка мыши" : "Right Mouse Button"} - {languageIsRussian ? "нажата" : "clicked"}");
                            macroListBox.TopIndex = macroListBox.Items.Count - 1;
                        });
                    }

                    // Wait until released
                    while (GetAsyncKeyState(Keys.RButton) < 0)
                    {
                        Thread.Sleep(10);
                    }
                }

                Thread.Sleep(10);
            }
        }

        private void StopRecordMacroBtn_Click(object sender, EventArgs e)
        {
            StopMacroRecording();
        }

        private void StopMacroRecording()
        {
            isMacroRecording = false;
            recordMacroBtn.Enabled = true;
            stopRecordMacroBtn.Enabled = false;
        }

        private void StartStopMacroBtn_Click(object sender, EventArgs e)
        {
            if (!isMacroRunning)
            {
                StartMacroExecution();
            }
            else
            {
                StopMacroExecution();
            }
        }

        private void StartMacroExecution()
        {
            if (recordedMacro.Count == 0)
            {
                MessageBox.Show(languageIsRussian ? "Нет записанных действий для выполнения!" : "No recorded actions to execute!");
                return;
            }

            isMacroRunning = true;
            startStopMacroBtn.Text = languageIsRussian ? "Стоп" : "Stop";

            currentMacro = new List<MacroAction>(recordedMacro);

            macroThread = new Thread(ExecuteMacroLoop);
            macroThread.IsBackground = true;
            macroThread.Start();
        }

        private void StopMacroExecution()
        {
            isMacroRunning = false;
            if (macroThread != null && macroThread.IsAlive)
            {
                macroThread.Join(1000);
            }

            startStopMacroBtn.Text = languageIsRussian ? "Выполнить" : "Execute";
        }

        private void ExecuteMacroLoop()
        {
            try
            {
                // Initial delay
                Thread.Sleep((int)macroDelayNumeric.Value);

                lock (macroLock)
                {
                    foreach (var action in currentMacro)
                    {
                        if (!isMacroRunning) break;

                        switch (action.Type)
                        {
                            case MacroActionType.KeyDown:
                                SimulateKeyDown(action.Key);
                                break;
                            case MacroActionType.KeyUp:
                                SimulateKeyUp(action.Key);
                                break;
                            case MacroActionType.MouseDown:
                                SimulateMouseClick(true);
                                break;
                            case MacroActionType.MouseUp:
                                SimulateMouseClick(false);
                                break;
                            case MacroActionType.MouseMove:
                                // Not implemented in basic recording
                                break;
                            case MacroActionType.Delay:
                                Thread.Sleep(action.DelayMs);
                                break;
                            case MacroActionType.GamepadButtonDown:
                                // Gamepad simulation would go through the shared memory system
                                SimulateGamepadButton(action.GamepadBtn, true);
                                break;
                            case MacroActionType.GamepadButtonUp:
                                SimulateGamepadButton(action.GamepadBtn, false);
                                break;
                        }

                        if (action.DelayMs > 0)
                        {
                            Thread.Sleep(action.DelayMs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in macro execution: {ex.Message}");
            }
        }

        private void SimulateKeyDown(Keys key)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.u.ki.wVk = (ushort)key;
            input.u.ki.wScan = 0;
            input.u.ki.dwFlags = 0; // Key down
            input.u.ki.time = 0;
            input.u.ki.dwExtraInfo = IntPtr.Zero;

            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SimulateKeyUp(Keys key)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.u.ki.wVk = (ushort)key;
            input.u.ki.wScan = 0;
            input.u.ki.dwFlags = KEYEVENTF_KEYUP;
            input.u.ki.time = 0;
            input.u.ki.dwExtraInfo = IntPtr.Zero;

            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SimulateMouseClick(bool isDown)
        {
            INPUT input = new INPUT();
            input.type = INPUT_MOUSE;
            
            if (isDown)
            {
                input.u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
            }
            else
            {
                input.u.mi.dwFlags = MOUSEEVENTF_LEFTUP;
            }
            
            input.u.mi.dx = 0;
            input.u.mi.dy = 0;
            input.u.mi.mouseData = 0;
            input.u.mi.time = 0;
            input.u.mi.dwExtraInfo = IntPtr.Zero;

            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SimulateGamepadButton(GamepadButton button, bool isDown)
        {
            // This would interface with the shared memory system in the DLL
            // For now, we just log that the action was triggered
            Debug.WriteLine($"Simulating gamepad button {button} {(isDown ? "down" : "up")}");
        }

        private void ClearMacroBtn_Click(object sender, EventArgs e)
        {
            recordedMacro.Clear();
            macroListBox.Items.Clear();
        }

        private void SaveMacroBtn_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = languageIsRussian ? "Файлы макросов|*.mkm" : "Macro Files|*.mkm";
                dialog.Title = languageIsRussian ? "Сохранить макрос" : "Save Macro";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(dialog.FileName))
                        {
                            foreach (var action in recordedMacro)
                            {
                                writer.WriteLine($"{(int)action.Type},{(int)action.Key},{(int)action.GamepadBtn},{action.X},{action.Y},{action.DelayMs}");
                            }
                        }

                        MessageBox.Show(languageIsRussian ? "Макрос успешно сохранен!" : "Macro saved successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(languageIsRussian ? $"Ошибка при сохранении макроса: {ex.Message}" : $"Error saving macro: {ex.Message}");
                    }
                }
            }
        }

        private void LoadMacroBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = languageIsRussian ? "Файлы макросов|*.mkm" : "Macro Files|*.mkm";
                dialog.Title = languageIsRussian ? "Загрузить макрос" : "Load Macro";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        recordedMacro.Clear();
                        macroListBox.Items.Clear();

                        using (StreamReader reader = new StreamReader(dialog.FileName))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                string[] parts = line.Split(',');
                                if (parts.Length >= 6)
                                {
                                    MacroActionType type = (MacroActionType)int.Parse(parts[0]);
                                    Keys key = (Keys)int.Parse(parts[1]);
                                    GamepadButton gamepadBtn = (GamepadButton)int.Parse(parts[2]);
                                    int x = int.Parse(parts[3]);
                                    int y = int.Parse(parts[4]);
                                    int delay = int.Parse(parts[5]);

                                    var action = new MacroAction(type, key, gamepadBtn, x, y, delay);
                                    recordedMacro.Add(action);

                                    macroListBox.Items.Add(GetActionDescription(action));
                                }
                            }
                        }

                        MessageBox.Show(languageIsRussian ? "Макрос успешно загружен!" : "Macro loaded successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(languageIsRussian ? $"Ошибка при загрузке макроса: {ex.Message}" : $"Error loading macro: {ex.Message}");
                    }
                }
            }
        }

        private string GetActionDescription(MacroAction action)
        {
            switch (action.Type)
            {
                case MacroActionType.KeyDown:
                    return $"{languageIsRussian ? "Клавиша" : "Key"} {action.Key} - {languageIsRussian ? "нажата" : "pressed"}";
                case MacroActionType.KeyUp:
                    return $"{languageIsRussian ? "Клавиша" : "Key"} {action.Key} - {languageIsRussian ? "отпущена" : "released"}";
                case MacroActionType.MouseDown:
                    return $"{languageIsRussian ? "Мышь" : "Mouse"} - {languageIsRussian ? "нажата" : "clicked"}";
                case MacroActionType.MouseUp:
                    return $"{languageIsRussian ? "Мышь" : "Mouse"} - {languageIsRussian ? "отпущена" : "released"}";
                case MacroActionType.Delay:
                    return $"{languageIsRussian ? "Задержка" : "Delay"} {action.DelayMs} {languageIsRussian ? "мс" : "ms"}";
                case MacroActionType.GamepadButtonDown:
                    return $"{languageIsRussian ? "Геймпад" : "Gamepad"} {action.GamepadBtn} - {languageIsRussian ? "нажата" : "pressed"}";
                case MacroActionType.GamepadButtonUp:
                    return $"{languageIsRussian ? "Геймпад" : "Gamepad"} {action.GamepadBtn} - {languageIsRussian ? "отпущена" : "released"}";
                default:
                    return $"{languageIsRussian ? "Действие" : "Action"} {action.Type}";
            }
        }

        private void SaveSettingsBtn_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\MKXLTrainer");

                key.SetValue("ProcessName", processNameTextBox.Text);
                key.SetValue("PointerReadInterval", (int)readIntervalNumeric.Value);
                key.SetValue("InputBlockDelay", (int)blockDelayNumeric.Value);
                key.SetValue("MacroStartDelay", (int)macroDelayStartNumeric.Value);
                key.SetValue("BaseAddress", baseAddressTextBox.Text);
                key.SetValue("Offsets", offsetsTextBox.Text);
                key.SetValue("LanguageIsRussian", languageIsRussian);

                // Save blocked keys
                string blockedKeysStr = string.Join(",", blockedKeys.Keys);
                key.SetValue("BlockedKeys", blockedKeysStr);

                // Save blocked gamepad buttons
                string blockedGamepadStr = string.Join(",", blockedGamepadButtons.Select(b => b.ToString()));
                key.SetValue("BlockedGamepadButtons", blockedGamepadStr);

                key.Close();

                MessageBox.Show(languageIsRussian ? "Настройки успешно сохранены!" : "Settings saved successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(languageIsRussian ? $"Ошибка при сохранении настроек: {ex.Message}" : $"Error saving settings: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\MKXLTrainer");

                if (key != null)
                {
                    processName = key.GetValue("ProcessName", "MK10.exe").ToString();
                    pointerReadInterval = Convert.ToInt32(key.GetValue("PointerReadInterval", 10));
                    inputBlockDelay = Convert.ToInt32(key.GetValue("InputBlockDelay", 260));
                    macroStartDelay = Convert.ToInt32(key.GetValue("MacroStartDelay", 280));
                    baseAddressStr = key.GetValue("BaseAddress", "033C8A98").ToString();
                    string offsetsStr = key.GetValue("Offsets", "").ToString();

                    if (!string.IsNullOrEmpty(offsetsStr))
                    {
                        offsets.Clear();
                        string[] offsetParts = offsetsStr.Split(',');
                        foreach (string part in offsetParts)
                        {
                            if (int.TryParse(part.Trim().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out int offset))
                            {
                                offsets.Add(offset);
                            }
                        }
                    }

                    languageIsRussian = Convert.ToBoolean(key.GetValue("LanguageIsRussian", true));

                    // Load blocked keys
                    string blockedKeysStr = key.GetValue("BlockedKeys", "").ToString();
                    if (!string.IsNullOrEmpty(blockedKeysStr))
                    {
                        string[] keys = blockedKeysStr.Split(',');
                        foreach (string k in keys)
                        {
                            if (Enum.TryParse<Keys>(k.Trim(), out Keys keyVal))
                            {
                                blockedKeys[k.Trim()] = keyVal;
                            }
                        }
                    }

                    // Load blocked gamepad buttons
                    string blockedGamepadStr = key.GetValue("BlockedGamepadButtons", "").ToString();
                    if (!string.IsNullOrEmpty(blockedGamepadStr))
                    {
                        string[] buttons = blockedGamepadStr.Split(',');
                        foreach (string btn in buttons)
                        {
                            if (Enum.TryParse<GamepadButton>(btn.Trim(), out GamepadButton btnVal))
                            {
                                blockedGamepadButtons.Add(btnVal);
                            }
                        }
                    }

                    key.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopPointerReading();
            StopMacroExecution();
            StopMacroRecording();

            SaveSettings();
            base.OnFormClosed(e);
        }
    }

    // Entry point
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}