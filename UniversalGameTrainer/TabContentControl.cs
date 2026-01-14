using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace UniversalGameTrainer
{
    public partial class TabContentControl : UserControl
    {
        private TabPageData tabPageData;
        private GroupBox pointerGroupBox;
        private GroupBox macroGroupBox;
        private GroupBox blockingGroupBox;
        private TextBox moduleNameTextBox;
        private TextBox baseOffsetTextBox;
        private TextBox offsetsTextBox;
        private TextBox triggerValueTextBox;
        private TextBox readIntervalTextBox;
        private TextBox blockDurationTextBox;
        private TextBox delayAfterTriggerTextBox;
        private ListBox keysToBlockListBox;
        private Button addKeyButton;
        private Button removeKeyButton;
        private CheckBox macroEnabledCheckBox;
        private Button recordMacroButton;
        private Button playMacroButton;
        private Button editMacroButton;
        private Button saveConfigButton;
        private Button loadConfigButton;
        private CheckBox isEnabledCheckBox;
        private TextBox tabNameTextBox;
        private readonly Dictionary<string, Dictionary<string, string>> languageStrings;
        private string currentLanguage;

        public TabContentControl(TabPageData data, string language, Dictionary<string, Dictionary<string, string>> langStrings)
        {
            tabPageData = data;
            currentLanguage = language;
            languageStrings = langStrings;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(760, 480);
            this.BackColor = Color.FromArgb(55, 55, 60);

            // Enabled checkbox and tab name
            isEnabledCheckBox = new CheckBox
            {
                Text = "Enabled",
                Location = new Point(10, 10),
                Size = new Size(100, 20),
                Checked = tabPageData.IsEnabled,
                BackColor = Color.FromArgb(55, 55, 60),
                ForeColor = Color.White
            };
            isEnabledCheckBox.CheckedChanged += IsEnabledCheckBox_CheckedChanged;

            tabNameTextBox = new TextBox
            {
                Location = new Point(120, 8),
                Size = new Size(200, 22),
                Text = tabPageData.Name
            };
            tabNameTextBox.TextChanged += TabNameTextBox_TextChanged;

            // Pointer GroupBox
            pointerGroupBox = new GroupBox
            {
                Text = "🔍 Pointer Configuration",
                Location = new Point(10, 40),
                Size = new Size(350, 180),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var moduleNameLabel = new Label
            {
                Text = "Module:",
                Location = new Point(10, 25),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };
            moduleNameTextBox = new TextBox
            {
                Location = new Point(90, 23),
                Size = new Size(250, 22),
                Text = tabPageData.ModuleName
            };

            var baseOffsetLabel = new Label
            {
                Text = "Base Offset:",
                Location = new Point(10, 55),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };
            baseOffsetTextBox = new TextBox
            {
                Location = new Point(90, 53),
                Size = new Size(250, 22),
                Text = tabPageData.BaseOffset
            };

            var offsetsLabel = new Label
            {
                Text = "Offsets:",
                Location = new Point(10, 85),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };
            offsetsTextBox = new TextBox
            {
                Location = new Point(90, 83),
                Size = new Size(250, 22),
                Text = tabPageData.Offsets
            };

            var triggerValueLabel = new Label
            {
                Text = "Trigger Value:",
                Location = new Point(10, 115),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };
            triggerValueTextBox = new TextBox
            {
                Location = new Point(90, 113),
                Size = new Size(250, 22),
                Text = tabPageData.TriggerValue.ToString()
            };

            var readIntervalLabel = new Label
            {
                Text = "Read Interval (ms):",
                Location = new Point(10, 145),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };
            readIntervalTextBox = new TextBox
            {
                Location = new Point(130, 143),
                Size = new Size(210, 22),
                Text = tabPageData.ReadIntervalMs.ToString()
            };

            pointerGroupBox.Controls.AddRange(new Control[] {
                moduleNameLabel, moduleNameTextBox,
                baseOffsetLabel, baseOffsetTextBox,
                offsetsLabel, offsetsTextBox,
                triggerValueLabel, triggerValueTextBox,
                readIntervalLabel, readIntervalTextBox
            });

            // Blocking GroupBox
            blockingGroupBox = new GroupBox
            {
                Text = "🚫 Key Blocking",
                Location = new Point(10, 230),
                Size = new Size(350, 200),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var blockDurationLabel = new Label
            {
                Text = "Block Duration (ms):",
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };
            blockDurationTextBox = new TextBox
            {
                Location = new Point(130, 23),
                Size = new Size(210, 22),
                Text = tabPageData.BlockDurationMs.ToString()
            };

            var keysToBlockLabel = new Label
            {
                Text = "Keys to Block:",
                Location = new Point(10, 55),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            keysToBlockListBox = new ListBox
            {
                Location = new Point(10, 75),
                Size = new Size(200, 110),
                SelectionMode = SelectionMode.MultiExtended
            };
            foreach (var key in tabPageData.KeysToBlock)
            {
                keysToBlockListBox.Items.Add(key);
            }

            addKeyButton = new Button
            {
                Text = "➕ Add",
                Location = new Point(220, 75),
                Size = new Size(60, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            addKeyButton.Click += AddKeyButton_Click;

            removeKeyButton = new Button
            {
                Text = "❌ Remove",
                Location = new Point(220, 115),
                Size = new Size(60, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            removeKeyButton.Click += RemoveKeyButton_Click;

            blockingGroupBox.Controls.AddRange(new Control[] {
                blockDurationLabel, blockDurationTextBox,
                keysToBlockLabel, keysToBlockListBox,
                addKeyButton, removeKeyButton
            });

            // Macro GroupBox
            macroGroupBox = new GroupBox
            {
                Text = "🎬 Macro Configuration",
                Location = new Point(370, 40),
                Size = new Size(380, 390),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var delayAfterTriggerLabel = new Label
            {
                Text = "Delay After Trigger (ms):",
                Location = new Point(10, 25),
                Size = new Size(140, 20),
                ForeColor = Color.White
            };
            delayAfterTriggerTextBox = new TextBox
            {
                Location = new Point(150, 23),
                Size = new Size(220, 22),
                Text = tabPageData.DelayAfterTriggerMs.ToString()
            };

            macroEnabledCheckBox = new CheckBox
            {
                Text = "Enable Macro",
                Location = new Point(10, 55),
                Size = new Size(120, 20),
                Checked = tabPageData.IsMacroEnabled,
                BackColor = Color.FromArgb(55, 55, 60),
                ForeColor = Color.White
            };
            macroEnabledCheckBox.CheckedChanged += MacroEnabledCheckBox_CheckedChanged;

            recordMacroButton = new Button
            {
                Text = "🔴 Record Macro",
                Location = new Point(10, 85),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            recordMacroButton.Click += RecordMacroButton_Click;

            playMacroButton = new Button
            {
                Text = "▶️ Play Macro",
                Location = new Point(130, 85),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            playMacroButton.Click += PlayMacroButton_Click;

            editMacroButton = new Button
            {
                Text = "📝 Edit Macro",
                Location = new Point(240, 85),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            editMacroButton.Click += EditMacroButton_Click;

            var macroActionsLabel = new Label
            {
                Text = "Macro Actions:",
                Location = new Point(10, 125),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            var macroActionsListBox = new ListBox
            {
                Location = new Point(10, 145),
                Size = new Size(360, 150)
            };
            foreach (var action in tabPageData.MacroActions)
            {
                macroActionsListBox.Items.Add($"{action.ActionType}: {action.KeyName} for {action.DurationMs}ms");
            }

            saveConfigButton = new Button
            {
                Text = "💾 Save Config",
                Location = new Point(10, 310),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            saveConfigButton.Click += SaveConfigButton_Click;

            loadConfigButton = new Button
            {
                Text = "📂 Load Config",
                Location = new Point(130, 310),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            loadConfigButton.Click += LoadConfigButton_Click;

            macroGroupBox.Controls.AddRange(new Control[] {
                delayAfterTriggerLabel, delayAfterTriggerTextBox,
                macroEnabledCheckBox,
                recordMacroButton, playMacroButton, editMacroButton,
                macroActionsLabel, macroActionsListBox,
                saveConfigButton, loadConfigButton
            });

            this.Controls.AddRange(new Control[] {
                isEnabledCheckBox, tabNameTextBox,
                pointerGroupBox, blockingGroupBox, macroGroupBox
            });

            UpdateLanguage(currentLanguage, languageStrings);
        }

        public void UpdateLanguage(string language, Dictionary<string, Dictionary<string, string>> langStrings)
        {
            currentLanguage = language;
            languageStrings = langStrings;

            isEnabledCheckBox.Text = GetLocalizedString("Enabled");
            pointerGroupBox.Text = "🔍 " + GetLocalizedString("PointerConfiguration");
            blockingGroupBox.Text = "🚫 " + GetLocalizedString("KeyBlocking");
            macroGroupBox.Text = "🎬 " + GetLocalizedString("MacroConfiguration");
            
            // Update labels in pointer group
            var pointerControls = pointerGroupBox.Controls;
            foreach (Control c in pointerControls)
            {
                if (c is Label label)
                {
                    switch (label.Text)
                    {
                        case "Module:":
                            label.Text = GetLocalizedString("Module");
                            break;
                        case "Base Offset:":
                            label.Text = GetLocalizedString("BaseOffset");
                            break;
                        case "Offsets:":
                            label.Text = GetLocalizedString("Offsets");
                            break;
                        case "Trigger Value:":
                            label.Text = GetLocalizedString("TriggerValue");
                            break;
                        case "Read Interval (ms):":
                            label.Text = GetLocalizedString("ReadIntervalMs");
                            break;
                    }
                }
            }

            // Update labels in blocking group
            var blockingControls = blockingGroupBox.Controls;
            foreach (Control c in blockingControls)
            {
                if (c is Label label)
                {
                    switch (label.Text)
                    {
                        case "Block Duration (ms):":
                            label.Text = GetLocalizedString("BlockDurationMs");
                            break;
                        case "Keys to Block:":
                            label.Text = GetLocalizedString("KeysToBlock");
                            break;
                    }
                }
                else if (c is Button btn)
                {
                    switch (btn.Text)
                    {
                        case "➕ Add":
                            btn.Text = "➕ " + GetLocalizedString("Add");
                            break;
                        case "❌ Remove":
                            btn.Text = "❌ " + GetLocalizedString("Remove");
                            break;
                    }
                }
            }

            // Update labels in macro group
            var macroControls = macroGroupBox.Controls;
            foreach (Control c in macroControls)
            {
                if (c is Label label)
                {
                    switch (label.Text)
                    {
                        case "Delay After Trigger (ms):":
                            label.Text = GetLocalizedString("DelayAfterTriggerMs");
                            break;
                    }
                }
                else if (c is CheckBox cb)
                {
                    if (cb == macroEnabledCheckBox)
                    {
                        cb.Text = GetLocalizedString("EnableMacro");
                    }
                }
                else if (c is Button btn)
                {
                    switch (btn.Text)
                    {
                        case "🔴 Record Macro":
                            btn.Text = "🔴 " + GetLocalizedString("RecordMacro");
                            break;
                        case "▶️ Play Macro":
                            btn.Text = "▶️ " + GetLocalizedString("PlayMacro");
                            break;
                        case "📝 Edit Macro":
                            btn.Text = "📝 " + GetLocalizedString("EditMacro");
                            break;
                        case "💾 Save Config":
                            btn.Text = "💾 " + GetLocalizedString("SaveConfig");
                            break;
                        case "📂 Load Config":
                            btn.Text = "📂 " + GetLocalizedString("LoadConfig");
                            break;
                    }
                }
            }
        }

        public void UpdateSettings(TabPageData data, string language, Dictionary<string, Dictionary<string, string>> langStrings)
        {
            tabPageData = data;
            currentLanguage = language;
            languageStrings = langStrings;
            LoadData();
        }

        private void LoadData()
        {
            if (moduleNameTextBox != null) moduleNameTextBox.Text = tabPageData.ModuleName;
            if (baseOffsetTextBox != null) baseOffsetTextBox.Text = tabPageData.BaseOffset;
            if (offsetsTextBox != null) offsetsTextBox.Text = tabPageData.Offsets;
            if (triggerValueTextBox != null) triggerValueTextBox.Text = tabPageData.TriggerValue.ToString();
            if (readIntervalTextBox != null) readIntervalTextBox.Text = tabPageData.ReadIntervalMs.ToString();
            if (blockDurationTextBox != null) blockDurationTextBox.Text = tabPageData.BlockDurationMs.ToString();
            if (delayAfterTriggerTextBox != null) delayAfterTriggerTextBox.Text = tabPageData.DelayAfterTriggerMs.ToString();
            if (isEnabledCheckBox != null) isEnabledCheckBox.Checked = tabPageData.IsEnabled;
            if (tabNameTextBox != null) tabNameTextBox.Text = tabPageData.Name;
            if (macroEnabledCheckBox != null) macroEnabledCheckBox.Checked = tabPageData.IsMacroEnabled;

            if (keysToBlockListBox != null)
            {
                keysToBlockListBox.Items.Clear();
                foreach (var key in tabPageData.KeysToBlock)
                {
                    keysToBlockListBox.Items.Add(key);
                }
            }
        }

        private void IsEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            tabPageData.IsEnabled = isEnabledCheckBox.Checked;
        }

        private void TabNameTextBox_TextChanged(object sender, EventArgs e)
        {
            tabPageData.Name = tabNameTextBox.Text;
        }

        private void AddKeyButton_Click(object sender, EventArgs e)
        {
            // In a real implementation, this would open a dialog to select keys
            var key = Prompt.ShowDialog("Enter key to block:", "Add Key");
            if (!string.IsNullOrEmpty(key))
            {
                keysToBlockListBox.Items.Add(key);
                tabPageData.KeysToBlock.Add(key);
            }
        }

        private void RemoveKeyButton_Click(object sender, EventArgs e)
        {
            var selectedIndices = keysToBlockListBox.SelectedIndices.Cast<int>().ToList();
            for (int i = selectedIndices.Count - 1; i >= 0; i--)
            {
                tabPageData.KeysToBlock.RemoveAt(selectedIndices[i]);
                keysToBlockListBox.Items.RemoveAt(selectedIndices[i]);
            }
        }

        private void MacroEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            tabPageData.IsMacroEnabled = macroEnabledCheckBox.Checked;
        }

        private void RecordMacroButton_Click(object sender, EventArgs e)
        {
            // Placeholder for macro recording functionality
            MessageBox.Show(GetLocalizedString("RecordMacroPlaceholder"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PlayMacroButton_Click(object sender, EventArgs e)
        {
            // Placeholder for macro playing functionality
            MessageBox.Show(GetLocalizedString("PlayMacroPlaceholder"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditMacroButton_Click(object sender, EventArgs e)
        {
            // Placeholder for macro editing functionality
            MessageBox.Show(GetLocalizedString("EditMacroPlaceholder"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveConfigButton_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        private void LoadConfigButton_Click(object sender, EventArgs e)
        {
            LoadConfiguration();
        }

        private void SaveConfiguration()
        {
            // Save current UI values to tabPageData
            tabPageData.ModuleName = moduleNameTextBox.Text;
            tabPageData.BaseOffset = baseOffsetTextBox.Text;
            tabPageData.Offsets = offsetsTextBox.Text;
            tabPageData.TriggerValue = int.Parse(triggerValueTextBox.Text);
            tabPageData.ReadIntervalMs = int.Parse(readIntervalTextBox.Text);
            tabPageData.BlockDurationMs = int.Parse(blockDurationTextBox.Text);
            tabPageData.DelayAfterTriggerMs = int.Parse(delayAfterTriggerTextBox.Text);
            tabPageData.IsEnabled = isEnabledCheckBox.Checked;
            tabPageData.Name = tabNameTextBox.Text;
            tabPageData.IsMacroEnabled = macroEnabledCheckBox.Checked;

            // Save keys to block
            tabPageData.KeysToBlock.Clear();
            foreach (string key in keysToBlockListBox.Items)
            {
                tabPageData.KeysToBlock.Add(key);
            }

            MessageBox.Show(GetLocalizedString("ConfigurationSaved"), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadConfiguration()
        {
            // Load values from tabPageData to UI
            moduleNameTextBox.Text = tabPageData.ModuleName;
            baseOffsetTextBox.Text = tabPageData.BaseOffset;
            offsetsTextBox.Text = tabPageData.Offsets;
            triggerValueTextBox.Text = tabPageData.TriggerValue.ToString();
            readIntervalTextBox.Text = tabPageData.ReadIntervalMs.ToString();
            blockDurationTextBox.Text = tabPageData.BlockDurationMs.ToString();
            delayAfterTriggerTextBox.Text = tabPageData.DelayAfterTriggerMs.ToString();
            isEnabledCheckBox.Checked = tabPageData.IsEnabled;
            tabNameTextBox.Text = tabPageData.Name;
            macroEnabledCheckBox.Checked = tabPageData.IsMacroEnabled;

            keysToBlockListBox.Items.Clear();
            foreach (var key in tabPageData.KeysToBlock)
            {
                keysToBlockListBox.Items.Add(key);
            }

            MessageBox.Show(GetLocalizedString("ConfigurationLoaded"), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GetLocalizedString(string key)
        {
            if (languageStrings.ContainsKey(key) && languageStrings[key].ContainsKey(currentLanguage))
            {
                return languageStrings[key][currentLanguage];
            }
            return key; // fallback to key if translation not found
        }
    }

    // Helper class for showing prompt dialogs
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            Label textLabel = new Label() { Left = 10, Top = 10, Text = text, ForeColor = Color.White, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 10, Top = 30, Width = 260, BackColor = Color.FromArgb(65, 65, 70), ForeColor = Color.White };
            Button confirmation = new Button() { Text = "OK", Left = 190, Width = 80, Top = 60, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(65, 65, 70), ForeColor = Color.White };

            prompt.Controls.AddRange(new Control[] { textLabel, textBox, confirmation });
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}