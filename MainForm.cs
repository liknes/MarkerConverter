namespace MarkerConverter
{
    public partial class MainForm : Form
    {
        private ComboBox colorFilterComboBox;
        private CheckBox includeNotesCheckBox;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Form settings
            this.Text = "Marker Converter";
            this.Size = new Size(600, 400);

            // Create color filter combo box
            colorFilterComboBox = new ComboBox
            {
                Location = new Point(270, 10),
                Size = new Size(150, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            colorFilterComboBox.Items.AddRange(new string[] {
                "All Colors",
                "Blue Only",
                "Cyan Only",
                "Green Only",
                "Yellow Only",
                "Red Only",
                "Pink Only",
                "Purple Only",
                "Fuchsia Only",
                "Rose Only",
                "Lavender Only",
                "Sky Only",
                "Mint Only",
                "Lemon Only",
                "Sand Only",
                "Cocoa Only",
                "Cream Only"
            });
            colorFilterComboBox.SelectedIndex = 0; // Default to "All Colors"

            // Create other controls
            var selectFileButton = new Button
            {
                Text = "Select Marker File",
                Location = new Point(10, 10),
                Size = new Size(120, 30)
            };

            var convertButton = new Button
            {
                Text = "Convert",
                Location = new Point(140, 10),
                Size = new Size(120, 30),
                Enabled = false
            };

            var outputTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 50),
                Size = new Size(560, 300),
                ReadOnly = true
            };

            // Add checkbox for including notes
            includeNotesCheckBox = new CheckBox
            {
                Text = "Include Notes",
                Location = new Point(430, 13),
                Size = new Size(120, 20),
                Checked = false
            };

            // Add event handlers
            selectFileButton.Click += SelectFile_Click;
            convertButton.Click += Convert_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] { 
                selectFileButton, 
                convertButton, 
                colorFilterComboBox,
                includeNotesCheckBox,
                outputTextBox 
            });
        }

        private void SelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "EDL files (*.edl)|*.edl";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var convertButton = Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Convert");
                    if (convertButton != null)
                        convertButton.Enabled = true;
                    
                    // Store the selected file path
                    Tag = openFileDialog.FileName;
                }
            }
        }

        private void Convert_Click(object sender, EventArgs e)
        {
            if (Tag == null) return;

            try
            {
                var filePath = Tag.ToString();
                var markers = ConvertMarkers(filePath);
                
                var outputTextBox = Controls.OfType<TextBox>().FirstOrDefault();
                if (outputTextBox != null)
                {
                    outputTextBox.Text = string.Join(Environment.NewLine, markers);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting markers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<string> ConvertMarkers(string filePath)
        {
            var markers = new List<string>();
            var lines = File.ReadAllLines(filePath);

            // Skip the first 3 lines (header)
            for (int i = 3; i < lines.Length - 1; i++)
            {
                var currentLine = lines[i].Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    continue;
                }

                // This must be a marker line with timestamp
                if (char.IsDigit(currentLine[0]))
                {
                    // Get the metadata line (next line)
                    var nextLine = lines[i + 1].Trim();
                    
                    // Split notes from metadata if they exist
                    string notes = null;
                    string metadataLine = nextLine;
                    
                    if (!nextLine.StartsWith(" |"))
                    {
                        var parts = nextLine.Split(new[] { '|' }, 2);
                        if (parts.Length == 2)
                        {
                            notes = parts[0].Trim();
                            metadataLine = "|" + parts[1];
                        }
                    }

                    // Parse time
                    var timeparts = currentLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (timeparts.Length < 5) continue;

                    var timestamp = timeparts[4].Split(':');
                    if (timestamp.Length < 3) continue;

                    // Adjust hour (subtract 1)
                    int hour = int.Parse(timestamp[0]) - 1;
                    var minutes = timestamp[1];
                    var seconds = timestamp[2];
                    var adjustedTime = $"{hour:D2}:{minutes}:{seconds}";

                    // Parse metadata
                    var metaParts = metadataLine.Split('|')
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s.Trim().Split(new[] { ':' }, 2))
                        .Where(parts => parts.Length == 2)
                        .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    if (metaParts.TryGetValue("M", out string markerText) && 
                        metaParts.TryGetValue("C", out string color))
                    {
                        bool includeMarker = colorFilterComboBox.SelectedItem.ToString() switch
                        {
                            "All Colors" => true,
                            "Blue Only" => color == "ResolveColorBlue",
                            "Cyan Only" => color == "ResolveColorCyan",
                            "Green Only" => color == "ResolveColorGreen",
                            "Yellow Only" => color == "ResolveColorYellow",
                            "Red Only" => color == "ResolveColorRed",
                            "Pink Only" => color == "ResolveColorPink",
                            "Purple Only" => color == "ResolveColorPurple",
                            "Fuchsia Only" => color == "ResolveColorFuchsia",
                            "Rose Only" => color == "ResolveColorRose",
                            "Lavender Only" => color == "ResolveColorLavender",
                            "Sky Only" => color == "ResolveColorSky",
                            "Mint Only" => color == "ResolveColorMint",
                            "Lemon Only" => color == "ResolveColorLemon",
                            "Sand Only" => color == "ResolveColorSand",
                            "Cocoa Only" => color == "ResolveColorCocoa",
                            "Cream Only" => color == "ResolveColorCream",
                            _ => false
                        };

                        if (includeMarker)
                        {
                            var markerLine = $"[{adjustedTime}] {markerText}";
                            if (!string.IsNullOrEmpty(notes) && includeNotesCheckBox.Checked)
                            {
                                markerLine += $" - {notes}";
                            }
                            markers.Add(markerLine);
                        }
                    }
                }
            }

            return markers;
        }
    }
}
