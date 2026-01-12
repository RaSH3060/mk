using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace UniversalGameTrainer
{
    public partial class MainForm : Form
    {
        // WinAPI imports
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMappingW(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenFileMappingW(uint dwDesiredAccess, bool bInheritHandle, [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        // Memory constants
        const int PROCESS_WM_READ = 0x0010;
        const uint PAGE_READWRITE = 0x04;
        const uint FILE_MAP_ALL_ACCESS = 0x001f0000;

        // Shared memory structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SharedInputBuffer
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool bIsActive;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] keyboard;
            public int lX, lY, lZ;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] buttons;
            public int lRX, lRY, lRZ;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public int[] sliders;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] povs;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] joyButtons;
        }

        // Language dictionary
        private Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>
        {
            { "Attach", new Dictionary<string, string> { { "EN", "🔍 Attach" }, { "RU", "🔍 Прикрепить" } } },
            { "Settings", new Dictionary<string, string> { { "EN", "⚙️ Settings" }, { "RU", "⚙️ Настройки" } } },
            { "Language", new Dictionary<string, string> { { "EN", "🌐 Language" }, { "RU", "🌐 Язык" } } },
            { "Save", new Dictionary<string, string> { { "EN", "💾 Save" }, { "RU", "💾 Сохранить" } } },
            { "Load", new Dictionary<string, string> { { "EN", "📁 Load" }, { "RU", "📁 Загрузить" } } },
            { "AddTab", new Dictionary<string, string> { { "EN", "➕ Add Tab" }, { "RU", "➕ Добавить вкладку" } } },
            { "RemoveTab", new Dictionary<string, string> { { "EN", "❌ Remove Tab" }, { "RU", "❌ Удалить вкладку" } } },
            { "Enabled", new Dictionary<string, string> { { "EN", "Enabled" }, { "RU", "Включено" } } },
            { "ModuleName", new Dictionary<string, string> { { "EN", "Module Name" }, { "RU", "Имя модуля" } } },
            { "BaseOffset", new Dictionary<string, string> { { "EN", "Base Offset" }, { "RU", "Базовое смещение" } } },
            { "Offsets", new Dictionary<string, string> { { "EN", "Offsets" }, { "RU", "Смещения" } } },
            { "TriggerValue", new Dictionary<string, string> { { "EN", "Trigger Value" }, { "RU", "Значение триггера" } } },
            { "ReadInterval", new Dictionary<string, string> { { "EN", "Read Interval (ms)" }, { "RU", "Интервал чтения (мс)" } } },
            { "BlockDuration", new Dictionary<string, string> { { "EN", "Block Duration (ms)" }, { "RU", "Длительность блокировки (мс)" } } },
            { "DelayAfterTrigger", new Dictionary<string, string> { { "EN", "Delay After Trigger (ms)" }, { "RU", "Задержка после триггера (мс)" } } },
            { "BlockKeys", new Dictionary<string, string> { { "EN", "Block Keys" }, { "RU", "Блокировать клавиши" } } },
            { "SimulateKeys", new Dictionary<string, string> { { "EN", "Simulate Keys" }, { "RU", "Симулировать клавиши" } } },
            { "RecordMacro", new Dictionary<string, string> { { "EN", "🎬 Record Macro" }, { "RU", "🎬 Записать макрос" } } },
            { "PlayMacro", new Dictionary<string, string> { { "EN", "▶️ Play Macro" }, { "RU", "▶️ Воспроизвести макрос" } } },
            { "StopMacro", new Dictionary<string, string> { { "EN", "⏹️ Stop Macro" }, { "RU", "⏹️ Остановить макрос" } } },
            { "Active", new Dictionary<string, string> { { "EN", "✅ Active" }, { "RU", "✅ Активно" } } },
            { "Inactive", new Dictionary<string, string> { { "EN", "❌ Inactive" }, { "RU", "❌ Неактивно" } }
        };

        private string currentLanguage = "EN";
        private IntPtr sharedMemoryHandle = IntPtr.Zero;
        private IntPtr sharedMemoryPtr = IntPtr.Zero;
        private Process attachedProcess = null;
        private Dictionary<int, Thread> memoryReadingThreads = new Dictionary<int, Thread>();
        private List<TabPage> tabPages = new List<TabPage>();

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
            LoadSettings();
        }

        private void SetupUI()
        {
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Universal Game Trainer";

            // Dark theme
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            // Menu strip
            var menuStrip = new MenuStrip();
            var toolStripAttach = new ToolStripMenuItem(GetText("Attach"));
            var toolStripSettings = new ToolStripMenuItem(GetText("Settings"));
            var toolStripLanguage = new ToolStripMenuItem(GetText("Language"));
            var toolStripSave = new ToolStripMenuItem(GetText("Save"));
            var toolStripLoad = new ToolStripMenuItem(GetText("Load"));
            var toolStripAddTab = new ToolStripMenuItem(GetText("AddTab"));
            var toolStripRemoveTab = new ToolStripMenuItem(GetText("RemoveTab"));

            toolStripAttach.Click += (s, e) => AttachToProcess();
            toolStripSettings.Click += (s, e) => OpenSettings();
            toolStripLanguage.Click += (s, e) => ToggleLanguage();
            toolStripSave.Click += (s, e) => SaveSettings();
            toolStripLoad.Click += (s, e) => LoadSettings();
            toolStripAddTab.Click += (s, e) => AddNewTab();
            toolStripRemoveTab.Click += (s, e) => RemoveCurrentTab();

            menuStrip.Items.AddRange(new ToolStripItem[] {
                toolStripAttach, toolStripSettings, toolStripLanguage, 
                toolStripSave, toolStripLoad, toolStripAddTab, toolStripRemoveTab
            });

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Tab control
            var tabControl = new TabControl();
            tabControl.Name = "mainTabControl";
            tabControl.Dock = DockStyle.Fill;
            tabControl.Appearance = TabAppearance.FlatButtons;
            tabControl.ItemSize = new Size(100, 30);
            tabControl.SizeMode = TabSizeMode.Fixed;

            // Status strip
            var statusStrip = new StatusStrip();
            var toolStripStatusLabel = new ToolStripStatusLabel("❌ Inactive");
            var toolStripLanguageLabel = new ToolStripStatusLabel($"🌐 {currentLanguage}");
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel, toolStripLanguageLabel });
            this.Controls.Add(statusStrip);

            this.Controls.Add(tabControl);
            
            // Add initial tab
            AddNewTab();
        }

        private string GetText(string key)
        {
            if (translations.ContainsKey(key) && translations[key].ContainsKey(currentLanguage))
                return translations[key][currentLanguage];
            return key;
        }

        private void AddNewTab()
        {
            var tabPage = new TabPage($"Tab {tabPages.Count + 1}");
            tabPage.BackColor = Color.FromArgb(40, 40, 40);
            tabPage.ForeColor = Color.White;

            // Create controls for the tab
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(10);

            // Enabled checkbox
            var enabledCheckBox = new CheckBox();
            enabledCheckBox.Text = GetText("Enabled");
            enabledCheckBox.Checked = true;
            enabledCheckBox.Location = new Point(10, 10);
            enabledCheckBox.AutoSize = true;

            // Module name
            var moduleNameLabel = new Label();
            moduleNameLabel.Text = GetText("ModuleName");
            moduleNameLabel.Location = new Point(10, 40);
            moduleNameLabel.AutoSize = true;

            var moduleNameTextBox = new TextBox();
            moduleNameTextBox.Text = "game.exe";
            moduleNameTextBox.Location = new Point(150, 40);
            moduleNameTextBox.Width = 150;

            // Base offset
            var baseOffsetLabel = new Label();
            baseOffsetLabel.Text = GetText("BaseOffset");
            baseOffsetLabel.Location = new Point(10, 70);
            baseOffsetLabel.AutoSize = true;

            var baseOffsetTextBox = new TextBox();
            baseOffsetTextBox.Text = "00000000";
            baseOffsetTextBox.Location = new Point(150, 70);
            baseOffsetTextBox.Width = 150;

            // Offsets
            var offsetsLabel = new Label();
            offsetsLabel.Text = GetText("Offsets");
            offsetsLabel.Location = new Point(10, 100);
            offsetsLabel.AutoSize = true;

            var offsetsTextBox = new TextBox();
            offsetsTextBox.Text = "0,0,0";
            offsetsTextBox.Location = new Point(150, 100);
            offsetsTextBox.Width = 150;

            // Trigger value
            var triggerValueLabel = new Label();
            triggerValueLabel.Text = GetText("TriggerValue");
            triggerValueLabel.Location = new Point(10, 130);
            triggerValueLabel.AutoSize = true;

            var triggerValueTextBox = new TextBox();
            triggerValueTextBox.Text = "0";
            triggerValueTextBox.Location = new Point(150, 130);
            triggerValueTextBox.Width = 150;

            // Read interval
            var readIntervalLabel = new Label();
            readIntervalLabel.Text = GetText("ReadInterval");
            readIntervalLabel.Location = new Point(10, 160);
            readIntervalLabel.AutoSize = true;

            var readIntervalTextBox = new TextBox();
            readIntervalTextBox.Text = "10";
            readIntervalTextBox.Location = new Point(150, 160);
            readIntervalTextBox.Width = 150;

            // Block duration
            var blockDurationLabel = new Label();
            blockDurationLabel.Text = GetText("BlockDuration");
            blockDurationLabel.Location = new Point(10, 190);
            blockDurationLabel.AutoSize = true;

            var blockDurationTextBox = new TextBox();
            blockDurationTextBox.Text = "260";
            blockDurationTextBox.Location = new Point(150, 190);
            blockDurationTextBox.Width = 150;

            // Delay after trigger
            var delayAfterTriggerLabel = new Label();
            delayAfterTriggerLabel.Text = GetText("DelayAfterTrigger");
            delayAfterTriggerLabel.Location = new Point(10, 220);
            delayAfterTriggerLabel.AutoSize = true;

            var delayAfterTriggerTextBox = new TextBox();
            delayAfterTriggerTextBox.Text = "280";
            delayAfterTriggerTextBox.Location = new Point(150, 220);
            delayAfterTriggerTextBox.Width = 150;

            // Block keys
            var blockKeysLabel = new Label();
            blockKeysLabel.Text = GetText("BlockKeys");
            blockKeysLabel.Location = new Point(10, 250);
            blockKeysLabel.AutoSize = true;

            var blockKeysListBox = new ListBox();
            blockKeysListBox.Location = new Point(150, 250);
            blockKeysListBox.Size = new Size(150, 80);
            blockKeysListBox.SelectionMode = SelectionMode.MultiExtended;

            // Simulate keys
            var simulateKeysLabel = new Label();
            simulateKeysLabel.Text = GetText("SimulateKeys");
            simulateKeysLabel.Location = new Point(320, 40);
            simulateKeysLabel.AutoSize = true;

            var simulateKeysListBox = new ListBox();
            simulateKeysListBox.Location = new Point(460, 40);
            simulateKeysListBox.Size = new Size(150, 80);
            simulateKeysListBox.SelectionMode = SelectionMode.MultiExtended;

            // Macro recording
            var recordMacroButton = new Button();
            recordMacroButton.Text = GetText("RecordMacro");
            recordMacroButton.Location = new Point(320, 130);
            recordMacroButton.Size = new Size(120, 30);
            recordMacroButton.BackColor = Color.FromArgb(60, 60, 60);
            recordMacroButton.ForeColor = Color.White;

            var playMacroButton = new Button();
            playMacroButton.Text = GetText("PlayMacro");
            playMacroButton.Location = new Point(450, 130);
            playMacroButton.Size = new Size(120, 30);
            playMacroButton.BackColor = Color.FromArgb(60, 60, 60);
            playMacroButton.ForeColor = Color.White;

            var stopMacroButton = new Button();
            stopMacroButton.Text = GetText("StopMacro");
            stopMacroButton.Location = new Point(580, 130);
            stopMacroButton.Size = new Size(120, 30);
            stopMacroButton.BackColor = Color.FromArgb(60, 60, 60);
            stopMacroButton.ForeColor = Color.White;

            // Add controls to panel
            panel.Controls.AddRange(new Control[] {
                enabledCheckBox,
                moduleNameLabel, moduleNameTextBox,
                baseOffsetLabel, baseOffsetTextBox,
                offsetsLabel, offsetsTextBox,
                triggerValueLabel, triggerValueTextBox,
                readIntervalLabel, readIntervalTextBox,
                blockDurationLabel, blockDurationTextBox,
                delayAfterTriggerLabel, delayAfterTriggerTextBox,
                blockKeysLabel, blockKeysListBox,
                simulateKeysLabel, simulateKeysListBox,
                recordMacroButton, playMacroButton, stopMacroButton
            });

            tabPage.Controls.Add(panel);
            
            // Store references in tag for later access
            tabPage.Tag = new TabConfig
            {
                EnabledCheckBox = enabledCheckBox,
                ModuleNameTextBox = moduleNameTextBox,
                BaseOffsetTextBox = baseOffsetTextBox,
                OffsetsTextBox = offsetsTextBox,
                TriggerValueTextBox = triggerValueTextBox,
                ReadIntervalTextBox = readIntervalTextBox,
                BlockDurationTextBox = blockDurationTextBox,
                DelayAfterTriggerTextBox = delayAfterTriggerTextBox,
                BlockKeysListBox = blockKeysListBox,
                SimulateKeysListBox = simulateKeysListBox,
                RecordMacroButton = recordMacroButton,
                PlayMacroButton = playMacroButton,
                StopMacroButton = stopMacroButton
            };

            var tabControl = this.Controls.Find("mainTabControl", true).FirstOrDefault() as TabControl;
            tabControl.TabPages.Add(tabPage);
            tabPages.Add(tabPage);
        }

        private void RemoveCurrentTab()
        {
            var tabControl = this.Controls.Find("mainTabControl", true).FirstOrDefault() as TabControl;
            if (tabControl.SelectedIndex >= 0 && tabControl.TabCount > 1)
            {
                var tabPage = tabControl.SelectedTab;
                tabControl.TabPages.Remove(tabPage);
                tabPages.Remove(tabPage);
                
                // Update tab names
                for (int i = 0; i < tabControl.TabPages.Count; i++)
                {
                    tabControl.TabPages[i].Text = $"Tab {i + 1}";
                }
            }
        }

        private void AttachToProcess()
        {
            if (attachedProcess != null && !attachedProcess.HasExited)
            {
                MessageBox.Show("Already attached to a process!");
                return;
            }

            var processName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter process name (e.g., game.exe)", 
                "Attach to Process", 
                "game.exe");

            if (string.IsNullOrEmpty(processName)) return;

            try
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
                if (processes.Length > 0)
                {
                    attachedProcess = processes[0];
                    
                    // Initialize shared memory
                    InitializeSharedMemory();
                    
                    // Start monitoring threads for enabled tabs
                    StartMonitoringForAllTabs();
                    
                    UpdateStatusBar("✅ Active");
                }
                else
                {
                    MessageBox.Show($"Process '{processName}' not found!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error attaching to process: {ex.Message}");
            }
        }

        private void InitializeSharedMemory()
        {
            // Try to open existing shared memory or create new one
            sharedMemoryHandle = OpenFileMappingW(FILE_MAP_ALL_ACCESS, false, "Local\\WinData_Input_Feedback");
            if (sharedMemoryHandle == IntPtr.Zero)
            {
                // Create new mapping
                sharedMemoryHandle = CreateFileMappingW(
                    new IntPtr(-1), // Use system page file
                    IntPtr.Zero,
                    PAGE_READWRITE,
                    0,
                    (uint)Marshal.SizeOf(typeof(SharedInputBuffer)),
                    "Local\\WinData_Input_Feedback"
                );
            }

            if (sharedMemoryHandle != IntPtr.Zero)
            {
                sharedMemoryPtr = MapViewOfFile(sharedMemoryHandle, FILE_MAP_ALL_ACCESS, 0, 0, 0);
            }
        }

        private void StartMonitoringForAllTabs()
        {
            var tabControl = this.Controls.Find("mainTabControl", true).FirstOrDefault() as TabControl;
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                var tabPage = tabControl.TabPages[i];
                var config = tabPage.Tag as TabConfig;
                
                if (config.EnabledCheckBox.Checked)
                {
                    if (!memoryReadingThreads.ContainsKey(i))
                    {
                        var thread = new Thread(() => MonitorMemoryForTab(i));
                        thread.IsBackground = true;
                        thread.Start();
                        memoryReadingThreads[i] = thread;
                    }
                }
            }
        }

        private void MonitorMemoryForTab(int tabIndex)
        {
            var tabControl = this.Controls.Find("mainTabControl", true).FirstOrDefault() as TabControl;
            var tabPage = tabControl.TabPages[tabIndex];
            var config = tabPage.Tag as TabConfig;

            while (attachedProcess != null && !attachedProcess.HasExited && config.EnabledCheckBox.Checked)
            {
                try
                {
                    var readInterval = int.Parse(config.ReadIntervalTextBox.Text);
                    var triggerValue = int.Parse(config.TriggerValueTextBox.Text);
                    
                    var currentValue = ReadMultiLevelPointer(
                        attachedProcess.Handle,
                        config.ModuleNameTextBox.Text,
                        config.BaseOffsetTextBox.Text,
                        config.OffsetsTextBox.Text
                    );

                    if (currentValue == triggerValue)
                    {
                        ExecuteMacroForTab(config);
                    }

                    Thread.Sleep(readInterval);
                }
                catch (Exception ex)
                {
                    // Log error but continue monitoring
                    Console.WriteLine($"Error in memory monitoring thread {tabIndex}: {ex.Message}");
                    Thread.Sleep(1000); // Wait before retrying
                }
            }
        }

        private int ReadMultiLevelPointer(IntPtr processHandle, string moduleName, string baseOffset, string offsetsStr)
        {
            try
            {
                // Get module base address
                var modules = attachedProcess.Modules.Cast<ProcessModule>().ToList();
                var targetModule = modules.FirstOrDefault(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
                if (targetModule == null) return 0;

                var baseAddr = targetModule.BaseAddress;
                var parsedBaseOffset = Convert.ToInt32(baseOffset, 16);
                var currentAddr = IntPtr.Add(baseAddr, parsedBaseOffset);

                // Parse offsets
                var offsets = offsetsStr.Split(',')
                    .Select(offset => Convert.ToInt32(offset.Trim(), 16))
                    .ToArray();

                // Follow the pointer chain
                foreach (var offset in offsets.Take(offsets.Length - 1))
                {
                    var buffer = new byte[IntPtr.Size];
                    if (!ReadProcessMemory(processHandle, currentAddr + offset, buffer, buffer.Length, out _))
                        return 0;

                    if (IntPtr.Size == 8)
                    {
                        currentAddr = new IntPtr(BitConverter.ToInt64(buffer, 0));
                    }
                    else
                    {
                        currentAddr = new IntPtr(BitConverter.ToInt32(buffer, 0));
                    }
                }

                // Read final value
                var finalOffset = offsets.LastOrDefault();
                var valueBuffer = new byte[4];
                if (!ReadProcessMemory(processHandle, currentAddr + finalOffset, valueBuffer, valueBuffer.Length, out _))
                    return 0;

                return BitConverter.ToInt32(valueBuffer, 0);
            }
            catch
            {
                return 0;
            }
        }

        private void ExecuteMacroForTab(TabConfig config)
        {
            var delayAfterTrigger = int.Parse(config.DelayAfterTriggerTextBox.Text);
            var blockDuration = int.Parse(config.BlockDurationTextBox.Text);

            // Delay before executing macro
            Thread.Sleep(delayAfterTrigger);

            // Execute macro based on configuration
            if (config.BlockKeysListBox.SelectedItems.Count > 0)
            {
                // Block selected keys for the specified duration
                BlockKeysForDuration(config.BlockKeysListBox.SelectedItems, blockDuration);
            }
            else if (config.SimulateKeysListBox.SelectedItems.Count > 0)
            {
                // Simulate selected keys for the specified duration
                SimulateKeysForDuration(config.SimulateKeysListBox.SelectedItems, blockDuration);
            }
        }

        private void BlockKeysForDuration(IList selectedItems, int durationMs)
        {
            if (sharedMemoryPtr != IntPtr.Zero)
            {
                var buffer = Marshal.PtrToStructure<SharedInputBuffer>(sharedMemoryPtr);
                buffer.bIsActive = true;

                // Block selected keys (set to 0x00 - released)
                foreach (var item in selectedItems)
                {
                    var keyStr = item.ToString();
                    if (int.TryParse(keyStr, out int keyCode) && keyCode >= 0 && keyCode < 256)
                    {
                        buffer.keyboard[keyCode] = 0x00; // Released
                    }
                }

                Marshal.StructureToPtr(buffer, sharedMemoryPtr, false);

                // Keep blocking for the duration
                Thread.Sleep(durationMs);

                // Release the block
                buffer.bIsActive = false;
                Marshal.StructureToPtr(buffer, sharedMemoryPtr, false);
            }
        }

        private void SimulateKeysForDuration(IList selectedItems, int durationMs)
        {
            if (sharedMemoryPtr != IntPtr.Zero)
            {
                var buffer = Marshal.PtrToStructure<SharedInputBuffer>(sharedMemoryPtr);
                buffer.bIsActive = true;

                // Press selected keys (set to 0x80 - pressed)
                foreach (var item in selectedItems)
                {
                    var keyStr = item.ToString();
                    if (int.TryParse(keyStr, out int keyCode) && keyCode >= 0 && keyCode < 256)
                    {
                        buffer.keyboard[keyCode] = 0x80; // Pressed
                    }
                }

                Marshal.StructureToPtr(buffer, sharedMemoryPtr, false);

                // Keep pressed for the duration
                Thread.Sleep(durationMs);

                // Release the keys
                foreach (var item in selectedItems)
                {
                    var keyStr = item.ToString();
                    if (int.TryParse(keyStr, out int keyCode) && keyCode >= 0 && keyCode < 256)
                    {
                        buffer.keyboard[keyCode] = 0x00; // Released
                    }
                }

                Marshal.StructureToPtr(buffer, sharedMemoryPtr, false);
            }
        }

        private void OpenSettings()
        {
            // For now, just show a simple language selection
            var form = new Form();
            form.Size = new Size(300, 150);
            form.Text = "Settings";
            form.StartPosition = FormStartPosition.CenterParent;
            form.BackColor = Color.FromArgb(40, 40, 40);
            form.ForeColor = Color.White;

            var languageLabel = new Label();
            languageLabel.Text = "Language:";
            languageLabel.Location = new Point(20, 20);
            languageLabel.AutoSize = true;

            var languageComboBox = new ComboBox();
            languageComboBox.Items.Add("EN");
            languageComboBox.Items.Add("RU");
            languageComboBox.SelectedItem = currentLanguage;
            languageComboBox.Location = new Point(100, 20);
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

            var saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new Point(100, 60);
            saveButton.Size = new Size(80, 30);
            saveButton.Click += (s, e) => {
                currentLanguage = languageComboBox.SelectedItem.ToString();
                UpdateUITexts();
                form.Close();
            };

            form.Controls.AddRange(new Control[] { languageLabel, languageComboBox, saveButton });
            form.ShowDialog();
        }

        private void ToggleLanguage()
        {
            currentLanguage = currentLanguage == "EN" ? "RU" : "EN";
            UpdateUITexts();
        }

        private void UpdateUITexts()
        {
            var menuStrip = this.MainMenuStrip;
            if (menuStrip.Items.Count > 0)
            {
                menuStrip.Items[0].Text = GetText("Attach");
                menuStrip.Items[1].Text = GetText("Settings");
                menuStrip.Items[2].Text = GetText("Language");
                menuStrip.Items[3].Text = GetText("Save");
                menuStrip.Items[4].Text = GetText("Load");
                menuStrip.Items[5].Text = GetText("AddTab");
                menuStrip.Items[6].Text = GetText("RemoveTab");
            }

            // Update status bar language indicator
            var statusStrip = this.Controls.OfType<StatusStrip>().FirstOrDefault();
            if (statusStrip?.Items.Count > 1)
            {
                statusStrip.Items[1].Text = $"🌐 {currentLanguage}";
            }
        }

        private void SaveSettings()
        {
            var settings = new TrainerSettings
            {
                Language = currentLanguage,
                ProcessName = attachedProcess?.ProcessName ?? "",
                Tabs = new List<TabSettings>()
            };

            var tabControl = this.Controls.Find("mainTabControl", true).FirstOrDefault() as TabControl;
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                var config = tabPage.Tag as TabConfig;
                settings.Tabs.Add(new TabSettings
                {
                    Enabled = config.EnabledCheckBox.Checked,
                    ModuleName = config.ModuleNameTextBox.Text,
                    BaseOffset = config.BaseOffsetTextBox.Text,
                    Offsets = config.OffsetsTextBox.Text,
                    TriggerValue = config.TriggerValueTextBox.Text,
                    ReadInterval = config.ReadIntervalTextBox.Text,
                    BlockDuration = config.BlockDurationTextBox.Text,
                    DelayAfterTrigger = config.DelayAfterTriggerTextBox.Text,
                    BlockKeys = string.Join(",", config.BlockKeysListBox.Items.Cast<object>().Select(item => item.ToString())),
                    SimulateKeys = string.Join(",", config.SimulateKeysListBox.Items.Cast<object>().Select(item => item.ToString()))
                });
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("TrainerSettings.json", json);
            
            MessageBox.Show("Settings saved successfully!");
        }

        private void LoadSettings()
        {
            if (!File.Exists("TrainerSettings.json")) return;

            try
            {
                var json = File.ReadAllText("TrainerSettings.json");
                var settings = JsonSerializer.Deserialize<TrainerSettings>(json);

                currentLanguage = settings.Language;
                UpdateUITexts();

                // Clear existing tabs except the first one
                var tabControl = this.Controls.Find("mainTabControl", true).FirstOrDefault() as TabControl;
                while (tabControl.TabPages.Count > 1)
                {
                    tabControl.TabPages.RemoveAt(tabControl.TabPages.Count - 1);
                }

                // Load tabs
                foreach (var tabSetting in settings.Tabs)
                {
                    AddNewTab();
                    var tabPage = tabControl.TabPages[tabControl.TabPages.Count - 1];
                    var config = tabPage.Tag as TabConfig;

                    config.EnabledCheckBox.Checked = tabSetting.Enabled;
                    config.ModuleNameTextBox.Text = tabSetting.ModuleName;
                    config.BaseOffsetTextBox.Text = tabSetting.BaseOffset;
                    config.OffsetsTextBox.Text = tabSetting.Offsets;
                    config.TriggerValueTextBox.Text = tabSetting.TriggerValue;
                    config.ReadIntervalTextBox.Text = tabSetting.ReadInterval;
                    config.BlockDurationTextBox.Text = tabSetting.BlockDuration;
                    config.DelayAfterTriggerTextBox.Text = tabSetting.DelayAfterTrigger;

                    if (!string.IsNullOrEmpty(tabSetting.BlockKeys))
                    {
                        foreach (var key in tabSetting.BlockKeys.Split(','))
                        {
                            if (!string.IsNullOrWhiteSpace(key))
                                config.BlockKeysListBox.Items.Add(key);
                        }
                    }

                    if (!string.IsNullOrEmpty(tabSetting.SimulateKeys))
                    {
                        foreach (var key in tabSetting.SimulateKeys.Split(','))
                        {
                            if (!string.IsNullOrWhiteSpace(key))
                                config.SimulateKeysListBox.Items.Add(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}");
            }
        }

        private void UpdateStatusBar(string status)
        {
            var statusStrip = this.Controls.OfType<StatusStrip>().FirstOrDefault();
            if (statusStrip?.Items.Count > 0)
            {
                statusStrip.Items[0].Text = status;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up threads
            foreach (var thread in memoryReadingThreads.Values)
            {
                if (thread.IsAlive)
                    thread.Interrupt();
            }
            memoryReadingThreads.Clear();

            // Clean up shared memory
            if (sharedMemoryPtr != IntPtr.Zero)
            {
                UnmapViewOfFile(sharedMemoryPtr);
                sharedMemoryPtr = IntPtr.Zero;
            }

            if (sharedMemoryHandle != IntPtr.Zero)
            {
                CloseHandle(sharedMemoryHandle);
                sharedMemoryHandle = IntPtr.Zero;
            }

            base.OnFormClosing(e);
        }

        // Configuration classes
        public class TabConfig
        {
            public CheckBox EnabledCheckBox { get; set; }
            public TextBox ModuleNameTextBox { get; set; }
            public TextBox BaseOffsetTextBox { get; set; }
            public TextBox OffsetsTextBox { get; set; }
            public TextBox TriggerValueTextBox { get; set; }
            public TextBox ReadIntervalTextBox { get; set; }
            public TextBox BlockDurationTextBox { get; set; }
            public TextBox DelayAfterTriggerTextBox { get; set; }
            public ListBox BlockKeysListBox { get; set; }
            public ListBox SimulateKeysListBox { get; set; }
            public Button RecordMacroButton { get; set; }
            public Button PlayMacroButton { get; set; }
            public Button StopMacroButton { get; set; }
        }

        public class TrainerSettings
        {
            public string Language { get; set; }
            public string ProcessName { get; set; }
            public List<TabSettings> Tabs { get; set; }
        }

        public class TabSettings
        {
            public bool Enabled { get; set; }
            public string ModuleName { get; set; }
            public string BaseOffset { get; set; }
            public string Offsets { get; set; }
            public string TriggerValue { get; set; }
            public string ReadInterval { get; set; }
            public string BlockDuration { get; set; }
            public string DelayAfterTrigger { get; set; }
            public string BlockKeys { get; set; }
            public string SimulateKeys { get; set; }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}