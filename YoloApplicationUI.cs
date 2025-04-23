namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public static class YoloApplicationUI
    {
        /// <summary>
        /// Creates a GroupBox with the specified text, location, and size.
        /// </summary>
        public static GroupBox CreateGroupBox(Form parent, string text, Point location, Size size, Font boldFont)
        {
            var groupBox = new GroupBox
            {
                Text = text,
                Location = location,
                Size = size,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            groupBox.Font = boldFont;
            parent.Controls.Add(groupBox);
            return groupBox;
        }

        /// <summary>
        /// Initializes detection configuration controls within the specified parent group.
        /// </summary>
        public static void InitializeDetectionConfigControls(GroupBox parentGroup, Font regularFont, ref Label selectWeightsFileLabel, ref Label selectLabelsFileLabel, ref ComboBox selectWeightsFileComboBox, ref ComboBox selectLabelsFileComboBox, ref Button selectImageButton, ref Button selectFolderButton, ref CheckBox enableGpuCheckBox, EventHandler selectImageButtonClickHandler, EventHandler selectFolderButtonClickHandler)
        {
            // Weights File Label
            selectWeightsFileLabel = new Label
            {
                Text = "Select Weights File:",
                Location = new Point(20, 30),
                Size = new Size(150, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(selectWeightsFileLabel);

            // Weights File ComboBox
            selectWeightsFileComboBox = new ComboBox
            {
                Location = new Point(180, 30),
                Size = new Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = regularFont
            };
            selectWeightsFileComboBox.Items.AddRange(new object[] {
                "petris_yolov5x.pt",
                "petris_yolov5x_fp16.engine",
                "petris_yolov5x_fp32.engine"
            });
            selectWeightsFileComboBox.SelectedIndex = 2;
            parentGroup.Controls.Add(selectWeightsFileComboBox);

            // Labels File Label
            selectLabelsFileLabel = new Label
            {
                Text = "Select Labels File:",
                Location = new Point(20, 60),
                Size = new Size(150, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(selectLabelsFileLabel);

            // Labels File ComboBox
            selectLabelsFileComboBox = new ComboBox
            {
                Location = new Point(180, 60),
                Size = new Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = regularFont
            };
            selectLabelsFileComboBox.Items.AddRange(new object[] {
                "petris_data.yaml"
            });
            selectLabelsFileComboBox.SelectedIndex = 0;
            parentGroup.Controls.Add(selectLabelsFileComboBox);

            // Select Image Button
            selectImageButton = new Button
            {
                Text = "Select Image",
                Location = new Point(20, 90),
                Size = new Size(175, 30),
                BackColor = Color.LightGray,
                Font = regularFont
            };
            selectImageButton.Click += selectImageButtonClickHandler;
            parentGroup.Controls.Add(selectImageButton);
            
            // Select Folder Button
            selectFolderButton = new Button
            {
                Text = "Select Folder",
                Location = new Point(205, 90),
                Size = new Size(175, 30),
                BackColor = Color.LightGray,
                Font = regularFont
            };
            selectFolderButton.Click += selectFolderButtonClickHandler;
            parentGroup.Controls.Add(selectFolderButton);

            // Enable GPU CheckBox
            enableGpuCheckBox = new CheckBox
            {
                Text = "Enable GPU",
                Location = new Point(20, 130),
                Size = new Size(150, 20),
                Checked = true,
                Font = regularFont
            };
            parentGroup.Controls.Add(enableGpuCheckBox);
        }

        /// <summary>
        /// Initializes detection parameters controls within the specified parent group.
        /// </summary>
        public static void InitializeDetectionParametersControls(GroupBox parentGroup, Font regularFont, ref Label imageResolutionLabel, ref Label confidenceThresholdLabel, ref Label iouThresholdLabel, ref TextBox imageResolutionHorizontalTextBox, ref TextBox imageResolutionVerticalTextBox, ref TextBox confidenceThresholdTextBox, ref TextBox iouThresholdTextBox)
        {
            // Image Resolution Label
            imageResolutionLabel = new Label
            {
                Text = "Image Resolution:",
                Location = new Point(20, 30),
                Size = new Size(120, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(imageResolutionLabel);

            // Horizontal Resolution TextBox
            imageResolutionHorizontalTextBox = new TextBox
            {
                Text = "1280",
                Location = new Point(150, 30),
                Size = new Size(50, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(imageResolutionHorizontalTextBox);

            // Resolution Separator Label
            Label resolutionSeparatorLabel = new Label
            {
                Text = "×",
                Location = new Point(205, 33),
                Size = new Size(15, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(resolutionSeparatorLabel);

            // Vertical Resolution TextBox
            imageResolutionVerticalTextBox = new TextBox
            {
                Text = "1280",
                Location = new Point(225, 30),
                Size = new Size(50, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(imageResolutionVerticalTextBox);

            // Confidence Threshold Label
            confidenceThresholdLabel = new Label
            {
                Text = "Confidence Threshold:",
                Location = new Point(20, 60),
                Size = new Size(120, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(confidenceThresholdLabel);

            // Confidence Threshold TextBox
            confidenceThresholdTextBox = new TextBox
            {
                Text = "0.25",
                Location = new Point(150, 60),
                Size = new Size(60, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(confidenceThresholdTextBox);

            // IOU Threshold Label
            iouThresholdLabel = new Label
            {
                Text = "IOU Threshold:",
                Location = new Point(20, 90),
                Size = new Size(120, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(iouThresholdLabel);

            // IOU Threshold TextBox
            iouThresholdTextBox = new TextBox
            {
                Text = "0.45",
                Location = new Point(150, 90),
                Size = new Size(60, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(iouThresholdTextBox);
        }

        /// <summary>
        /// Initializes logging configuration controls within the specified parent group.
        /// </summary>
        public static void InitializeLoggingConfigurationControls(GroupBox parentGroup, Font regularFont, ref Label projectNameLabel, ref TextBox projectNameTextBox, ref CheckBox hideLabelCheckBox, ref CheckBox hideConfidenceCheckBox)
        {
            // Project Name Label
            projectNameLabel = new Label
            {
                Text = "Project Name:",
                Location = new Point(20, 30),
                Size = new Size(100, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(projectNameLabel);

            // Project Name TextBox
            projectNameTextBox = new TextBox
            {
                Text = "PETRIS_Test_Data",
                Location = new Point(130, 30),
                Size = new Size(250, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(projectNameTextBox);

            // Hide Labels CheckBox
            hideLabelCheckBox = new CheckBox
            {
                Text = "Hide Labels",
                Location = new Point(20, 60),
                Size = new Size(100, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(hideLabelCheckBox);

            // Hide Confidence CheckBox
            hideConfidenceCheckBox = new CheckBox
            {
                Text = "Hide Confidence",
                Location = new Point(130, 60),
                Size = new Size(120, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(hideConfidenceCheckBox);
        }
        
        /// <summary>
        /// Creates server control buttons on the form.
        /// </summary>
        public static void CreateServerControlButtons(Form parent, ref Button startServerButton, ref Button quitServerButton, ref Button startDetectionButton, EventHandler startServerButtonClickHandler, EventHandler quitServerButtonClickHandler, EventHandler startDetectionButtonClickHandler)
        {
            // Start Server Button
            startServerButton = new Button
            {
                Text = "Start Server",
                Location = new Point(20, 480),
                Size = new Size(190, 40),
                Font = new Font("Arial", 14, FontStyle.Bold),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
            };
            startServerButton.FlatAppearance.BorderSize = 1;
            startServerButton.FlatAppearance.BorderColor = Color.Black;
            startServerButton.Click += startServerButtonClickHandler;
            parent.Controls.Add(startServerButton);
            
            // Stop Server Button
            quitServerButton = new Button
            {
                Text = "Stop Server",
                Location = new Point(230, 480),
                Size = new Size(190, 40),
                Font = new Font("Arial", 14, FontStyle.Bold),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            quitServerButton.FlatAppearance.BorderSize = 1;
            quitServerButton.FlatAppearance.BorderColor = Color.Black;
            quitServerButton.Click += quitServerButtonClickHandler;
            parent.Controls.Add(quitServerButton);
            
            // Start Detection Button
            startDetectionButton = new Button
            {
                Text = "► Start Detection",
                Location = new Point(20, 530),
                Size = new Size(400, 40),
                Font = new Font("Arial", 18, FontStyle.Bold),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            startDetectionButton.FlatAppearance.BorderSize = 1;
            startDetectionButton.FlatAppearance.BorderColor = Color.Black;
            startDetectionButton.Click += startDetectionButtonClickHandler;
            parent.Controls.Add(startDetectionButton);
        }

        /// <summary>
        /// Initializes image panel controls within the specified parent group.
        /// </summary>
        public static void InitializeImagePanelControls(GroupBox parentGroup, Font boldFont, Font regularFont, ref Label inputImageLabel, ref Label outputImageLabel, ref PictureBox inputPictureBox, ref PictureBox outputPictureBox, ref Button previousButton, ref Button nextButton, EventHandler previousButtonClickHandler, EventHandler nextButtonClickHandler)
        {
            // Input Image Label
            inputImageLabel = new Label
            {
                Text = "Input Image",
                Location = new Point(20, 25),
                Size = new Size(300, 20),
                Font = boldFont
            };
            parentGroup.Controls.Add(inputImageLabel);

            // Input PictureBox
            inputPictureBox = new PictureBox
            {
                Location = new Point(20, 50),
                Size = new Size(410, 500),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };
            parentGroup.Controls.Add(inputPictureBox);

            // Output Image Label
            outputImageLabel = new Label
            {
                Text = "Output Image (Showing Input - Run Detection)",
                Location = new Point(450, 25),
                Size = new Size(400, 20),
                Font = boldFont
            };
            parentGroup.Controls.Add(outputImageLabel);

            // Output PictureBox
            outputPictureBox = new PictureBox
            {
                Location = new Point(450, 50),
                Size = new Size(410, 500),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };
            parentGroup.Controls.Add(outputPictureBox);
            
            // Previous Button
            previousButton = new Button
            {
                Text = "◄ Previous",
                Location = new Point(20, 560),
                Size = new Size(200, 30),
                Font = regularFont,
                Enabled = false
            };
            previousButton.Click += previousButtonClickHandler;
            parentGroup.Controls.Add(previousButton);
            
            // Next Button
            nextButton = new Button
            {
                Text = "Next ►",
                Location = new Point(230, 560),
                Size = new Size(200, 30),
                Font = regularFont,
                Enabled = false
            };
            nextButton.Click += nextButtonClickHandler;
            parentGroup.Controls.Add(nextButton);
        }
    }
}