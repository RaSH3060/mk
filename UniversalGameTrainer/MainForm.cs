using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UniversalGameTrainer
{
    public partial class MainForm : Form
    {
        private TabControl tabControl;
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private Process attachedProcess;
        private readonly List<TabPageData> tabPageDataList = new List<TabPageData>();
        private readonly Dictionary<string, Dictionary<string, string>> languageStrings = new Dictionary<string, Dictionary<string, string>>();
        private string currentLanguage = "EN";
        private Settings settings = new Settings();

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

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

        public MainForm()
        {
            InitializeComponent();
            InitializeLanguageStrings();
            LoadSettings();
            UpdateLanguage();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Universal Game Trainer";

            // Dark theme
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Menu Strip
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem(GetLocalizedString("File"));
            var attachItem = new ToolStripMenuItem("🔍 " + GetLocalizedString("Attach")) { Tag = "attach" };
            var settingsItem = new ToolStripMenuItem("⚙️ " + GetLocalizedString("Settings")) { Tag = "settings" };
            var languageItem = new ToolStripMenuItem("🌐 " + GetLocalizedString("Language")) { Tag = "language" };
            var saveItem = new ToolStripMenuItem("💾 " + GetLocalizedString("Save")) { Tag = "save" };
            var loadItem = new ToolStripMenuItem("📂 " + GetLocalizedString("Load")) { Tag = "load" };

            attachItem.Click += MenuItem_Click;
            settingsItem.Click += MenuItem_Click;
            languageItem.Click += MenuItem_Click;
            saveItem.Click += MenuItem_Click;
            loadItem.Click += MenuItem_Click;

            fileMenu.DropDownItems.Add(attachItem);
            fileMenu.DropDownItems.Add(settingsItem);
            fileMenu.DropDownItems.Add(languageItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(saveItem);
            fileMenu.DropDownItems.Add(loadItem);

            var tabMenu = new ToolStripMenuItem(GetLocalizedString("Tabs"));
            var addTabItem = new ToolStripMenuItem("➕ " + GetLocalizedString("AddTab")) { Tag = "add_tab" };
            var removeTabItem = new ToolStripMenuItem("❌ " + GetLocalizedString("RemoveTab")) { Tag = "remove_tab" };

            addTabItem.Click += MenuItem_Click;
            removeTabItem.Click += MenuItem_Click;

            tabMenu.DropDownItems.Add(addTabItem);
            tabMenu.DropDownItems.Add(removeTabItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(tabMenu);
            this.Controls.Add(menuStrip);

            // Tab Control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal,
               SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(120, 30)
            };
            
            tabControl.ControlAdded += TabControl_ControlAdded;
            this.Controls.Add(tabControl);

            // Status Strip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("✅ " + GetLocalizedString("Ready"));
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);

            // Add initial tab
            AddNewTab();
        }

        private void TabControl_ControlAdded(object sender, ControlEventArgs e)
        {
            // Update tab headers to reflect enabled state
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                var tabPageData = tabPageDataList.FirstOrDefault(t => t.TabPage == tabPage);
                if (tabPageData != null)
                {
                    tabPage.Text = (tabPageData.IsEnabled ? "✅ " : "❌ ") + tabPageData.Name;
                }
            }
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            var tag = menuItem?.Tag?.ToString();

            switch (tag)
            {
                case "attach":
                    ShowAttachDialog();
                    break;
                case "settings":
                    ShowSettingsDialog();
                    break;
                case "language":
                    ToggleLanguage();
                    break;
                case "save":
                    SaveSettings();
                    break;
                case "load":
                    LoadSettings();
                    UpdateAllTabControls();
                    break;
                case "add_tab":
                    AddNewTab();
                    break;
                case "remove_tab":
                    RemoveCurrentTab();
                    break;
            }
        }

        private void AddNewTab()
        {
            var tabPage = new TabPage($"Tab {tabControl.TabCount + 1}");
            tabPage.BackColor = Color.FromArgb(55, 55, 60);
            
            var tabPageData = new TabPageData
            {
                Name = $"Tab {tabControl.TabCount + 1}",
                IsEnabled = true,
                TabPage = tabPage
            };

            var tabContent = new TabContentControl(tabPageData, currentLanguage, languageStrings);
            tabContent.Dock = DockStyle.Fill;
            tabPage.Controls.Add(tabContent);

            tabControl.TabPages.Add(tabPage);
            tabPageDataList.Add(tabPageData);

            // Update tab header
            tabPage.Text = (tabPageData.IsEnabled ? "✅ " : "❌ ") + tabPageData.Name;
        }

        private void RemoveCurrentTab()
        {
            if (tabControl.TabPages.Count <= 1) return; // Keep at least one tab
            
            var currentIndex = tabControl.SelectedIndex;
            if (currentIndex >= 0 && currentIndex < tabPageDataList.Count)
            {
                var tabPageData = tabPageDataList[currentIndex];
                
                // Stop monitoring if running
                tabPageData.StopMonitoring();
                
                tabControl.TabPages.RemoveAt(currentIndex);
                tabPageDataList.RemoveAt(currentIndex);
            }
        }

        private void UpdateAllTabControls()
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                if (i < tabPageDataList.Count)
                {
                    var tabPageData = tabPageDataList[i];
                    var existingControl = tabControl.TabPages[i].Controls[0] as TabContentControl;
                    
                    if (existingControl != null)
                    {
                        // Update the control with new settings
                        existingControl.UpdateSettings(tabPageData, currentLanguage, languageStrings);
                        tabControl.TabPages[i].Text = (tabPageData.IsEnabled ? "✅ " : "❌ ") + tabPageData.Name;
                    }
                }
            }
        }

        private void ShowAttachDialog()
        {
            var dialog = new AttachProcessDialog(currentLanguage, languageStrings);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var exeName = dialog.SelectedExeName;
                AttachToProcess(exeName);
            }
        }

        private void AttachToProcess(string exeName)
        {
            try
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName));
                if (processes.Length > 0)
                {
                    attachedProcess = processes[0];
                    statusLabel.Text = $"✅ {GetLocalizedString("Attached")} ({exeName})";
                    
                    // Start monitoring for all enabled tabs
                    foreach (var tabPageData in tabPageDataList.Where(t => t.IsEnabled))
                    {
                        tabPageData.StartMonitoring(attachedProcess);
                    }
                }
                else
                {
                    MessageBox.Show($"{GetLocalizedString("ProcessNotFound")}: {exeName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = $"❌ {GetLocalizedString("ProcessNotFound")}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{GetLocalizedString("AttachError")}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = $"❌ {GetLocalizedString("AttachError")}";
            }
        }

        private void ShowSettingsDialog()
        {
            var settingsDialog = new SettingsForm(settings, currentLanguage, languageStrings);
            if (settingsDialog.ShowDialog() == DialogResult.OK)
            {
                settings = settingsDialog.Settings;
            }
        }

        private void ToggleLanguage()
        {
            currentLanguage = currentLanguage == "EN" ? "RU" : "EN";
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            // Update menu items
            ((ToolStripMenuItem)menuStrip.Items[0]).Text = GetLocalizedString("File");
            ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip.Items[0]).DropDownItems[0]).Text = "🔍 " + GetLocalizedString("Attach");
            ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip.Items[0]).DropDownItems[1]).Text = "⚙️ " + GetLocalizedString("Settings");
            ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip.Items[0]).DropDownItems[2]).Text = "🌐 " + GetLocalizedString("Language");
            ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip.Items[0]).DropDownItems[4]).Text = "💾 " + GetLocalizedString("Save");
            ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip.Items[0]).DropDownItems[5]).Text = "📂 " + GetLocalizedString("Load");

            ((ToolStripMenuItem)menuStrip.Items[1]).Text = GetLocalizedString("Tabs");
            ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip.Items[1]).DropDownItems[0]).Text = "➕ " + GetLocalizedString("AddTab");
            ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip.Items[1]).DropDownItems[1]).Text = "❌ " + GetLocalizedString("RemoveTab");

            statusLabel.Text = statusLabel.Text.Contains("✅") ? "✅ " + GetLocalizedString("Ready") : "❌ " + GetLocalizedString("NotReady");

            // Update all tab controls
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                var tabContent = tabPage.Controls[0] as TabContentControl;
                if (tabContent != null)
                {
                    var tabPageData = tabPageDataList.FirstOrDefault(t => t.TabPage == tabPage);
                    if (tabPageData != null)
                    {
                        tabContent.UpdateLanguage(currentLanguage, languageStrings);
                    }
                }
            }
        }

        private void InitializeLanguageStrings()
        {
            languageStrings = LocalizedStrings.GetStringDictionary();
        }

        private string GetLocalizedString(string key)
        {
            if (languageStrings.ContainsKey(key) && languageStrings[key].ContainsKey(currentLanguage))
            {
                return languageStrings[key][currentLanguage];
            }
            return key; // fallback to key if translation not found
        }

        private void SaveSettings()
        {
            try
            {
                settings.TabPageDataList = tabPageDataList.Select(t => t.Clone()).ToList();
                settings.Language = currentLanguage;
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText("TrainerSettings.json", json);
                
                statusLabel.Text = "✅ Settings saved";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "❌ Error saving settings";
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists("TrainerSettings.json"))
                {
                    var json = File.ReadAllText("TrainerSettings.json");
                    var loadedSettings = JsonSerializer.Deserialize<Settings>(json);
                    
                    if (loadedSettings != null)
                    {
                        settings = loadedSettings;
                        currentLanguage = settings.Language ?? "EN";
                        
                        // Clear existing tabs
                        tabControl.TabPages.Clear();
                        tabPageDataList.Clear();
                        
                        // Recreate tabs from loaded data
                        foreach (var tabPageData in settings.TabPageDataList)
                        {
                            var tabPage = new TabPage(tabPageData.Name);
                            tabPage.BackColor = Color.FromArgb(55, 55, 60);
                            
                            var tabContent = new TabContentControl(tabPageData, currentLanguage, languageStrings);
                            tabContent.Dock = DockStyle.Fill;
                            tabPage.Controls.Add(tabContent);

                            tabControl.TabPages.Add(tabPage);
                            tabPageDataList.Add(tabPageData);
                            
                            // Update tab header
                            tabPage.Text = (tabPageData.IsEnabled ? "✅ " : "❌ ") + tabPageData.Name;
                        }
                        
                        if (tabControl.TabPages.Count == 0)
                        {
                            AddNewTab(); // Add default tab if none were loaded
                        }
                        
                        statusLabel.Text = "✅ Settings loaded";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "❌ Error loading settings";
                AddNewTab(); // Ensure we have at least one tab
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stop all monitoring
                foreach (var tabPageData in tabPageDataList)
                {
                    tabPageData.StopMonitoring();
                }
                
                // Close process handle if open
                if (attachedProcess != null)
                {
                    attachedProcess.Dispose();
                }
                
                menuStrip?.Dispose();
                statusStrip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}