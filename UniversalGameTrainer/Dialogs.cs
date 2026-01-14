using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UniversalGameTrainer
{
    public partial class AttachProcessDialog : Form
    {
        private TextBox exeNameTextBox;
        private Button okButton;
        private Button cancelButton;
        private ListBox processListBox;
        private readonly Dictionary<string, Dictionary<string, string>> languageStrings;
        private string currentLanguage;
        
        public string SelectedExeName { get; private set; } = "";

        public AttachProcessDialog(string language, Dictionary<string, Dictionary<string, string>> langStrings)
        {
            currentLanguage = language;
            languageStrings = langStrings ?? LocalizedStrings.GetStringDictionary();
            InitializeComponent();
            LoadProcesses();
        }

        private void InitializeComponent()
        {
            this.Text = "🔍 " + GetLocalizedString("AttachToProcess");
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var exeNameLabel = new Label
            {
                Text = GetLocalizedString("ExeName") + ":",
                Location = new Point(10, 20),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            exeNameTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(360, 22),
                Text = "game.exe"
            };

            var processListLabel = new Label
            {
                Text = GetLocalizedString("AvailableProcesses") + ":",
                Location = new Point(10, 70),
                Size = new Size(150, 20),
                ForeColor = Color.White
            };

            processListBox = new ListBox
            {
                Location = new Point(10, 90),
                Size = new Size(360, 120),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White
            };
            processListBox.SelectedIndexChanged += ProcessListBox_SelectedIndexChanged;

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(210, 220),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White,
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = GetLocalizedString("Cancel"),
                Location = new Point(305, 220),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] {
                exeNameLabel, exeNameTextBox,
                processListLabel, processListBox,
                okButton, cancelButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadProcesses()
        {
            processListBox.Items.Clear();
            var processes = Process.GetProcesses();
            var uniqueExeNames = new HashSet<string>();

            foreach (var process in processes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(process.ProcessName))
                    {
                        var exeName = process.ProcessName + ".exe";
                        uniqueExeNames.Add(exeName);
                    }
                }
                catch
                {
                    // Some processes might not be accessible
                }
            }

            foreach (var exeName in uniqueExeNames.OrderBy(x => x))
            {
                processListBox.Items.Add(exeName);
            }
        }

        private void ProcessListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (processListBox.SelectedItem != null)
            {
                exeNameTextBox.Text = processListBox.SelectedItem.ToString();
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            SelectedExeName = exeNameTextBox.Text;
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

    public partial class SettingsForm : Form
    {
        private Settings settings;
        private TextBox lastExeTextBox;
        private ComboBox languageComboBox;
        private Button okButton;
        private Button cancelButton;
        private readonly Dictionary<string, Dictionary<string, string>> languageStrings;
        private string currentLanguage;

        public Settings Settings { get { return settings; } }

        public SettingsForm(Settings currentSettings, string language, Dictionary<string, Dictionary<string, string>> langStrings)
        {
            settings = currentSettings;
            currentLanguage = language;
            languageStrings = langStrings ?? LocalizedStrings.GetStringDictionary();
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "⚙️ " + GetLocalizedString("Settings");
            this.Size = new Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var lastExeLabel = new Label
            {
                Text = GetLocalizedString("LastExeName") + ":",
                Location = new Point(10, 20),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            lastExeTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(360, 22)
            };

            var languageLabel = new Label
            {
                Text = GetLocalizedString("Language") + ":",
                Location = new Point(10, 70),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            languageComboBox = new ComboBox
            {
                Location = new Point(10, 90),
                Size = new Size(150, 22),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            languageComboBox.Items.Add("English");
            languageComboBox.Items.Add("Русский");

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(230, 120),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White,
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = GetLocalizedString("Cancel"),
                Location = new Point(315, 120),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(65, 65, 70),
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] {
                lastExeLabel, lastExeTextBox,
                languageLabel, languageComboBox,
                okButton, cancelButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadSettings()
        {
            lastExeTextBox.Text = settings.LastExeName;
            languageComboBox.SelectedIndex = settings.Language == "RU" ? 1 : 0;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            settings.LastExeName = lastExeTextBox.Text;
            settings.Language = languageComboBox.SelectedIndex == 1 ? "RU" : "EN";
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
}