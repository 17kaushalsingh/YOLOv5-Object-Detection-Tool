namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Provides UI component creation and management for the YOLO Object Detection application.
    /// This utility class encapsulates the creation and configuration of various UI elements
    /// including controls, group boxes, buttons, and image display panels.
    /// 
    /// The class follows a modular design pattern where each method is responsible for 
    /// initializing a specific group of related UI components, making the main form code
    /// cleaner and more maintainable.
    /// </summary>
    public static class YoloApplicationUI
    {
        /// <summary>
        /// Creates a GroupBox with the specified text, location, and size.
        /// </summary>
        /// <param name="parent">The parent form where the GroupBox will be added</param>
        /// <param name="text">The text to display in the GroupBox header</param>
        /// <param name="location">The location (coordinates) where the GroupBox should be positioned</param>
        /// <param name="size">The size (width and height) of the GroupBox</param>
        /// <param name="boldFont">The font to use for the GroupBox title</param>
        /// <returns>A configured GroupBox control added to the parent form</returns>
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
        /// Creates and configures labels, comboboxes, buttons, and checkboxes for configuring
        /// the YOLO detection process, including model selection and input sources.
        /// </summary>
        /// <param name="parentGroup">The parent GroupBox where controls will be added</param>
        /// <param name="regularFont">Font to use for controls</param>
        /// <param name="selectWeightsFileLabel">Reference to the weights file label</param>
        /// <param name="selectLabelsFileLabel">Reference to the labels file label</param>
        /// <param name="selectWeightsFileComboBox">Reference to the weights file selection combobox</param>
        /// <param name="selectLabelsFileComboBox">Reference to the labels file selection combobox</param>
        /// <param name="selectImageButton">Reference to the image selection button</param>
        /// <param name="selectFolderButton">Reference to the folder selection button</param>
        /// <param name="enableGpuCheckBox">Reference to the GPU enable checkbox</param>
        /// <param name="selectImageButtonClickHandler">Event handler for image selection button</param>
        /// <param name="selectFolderButtonClickHandler">Event handler for folder selection button</param>
        public static void InitializeDetectionConfigControls(GroupBox parentGroup, Font regularFont, ref Label selectWeightsFileLabel, ref Label selectLabelsFileLabel, ref ComboBox selectWeightsFileComboBox, ref ComboBox selectLabelsFileComboBox, ref Button selectImageButton, ref Button selectFolderButton, ref CheckBox enableGpuCheckBox, EventHandler selectImageButtonClickHandler, EventHandler selectFolderButtonClickHandler)
        {
            // Weights File Label
            selectWeightsFileLabel = new Label
            {
                Text = "Select Weights File",
                Location = new Point(20, 30),
                Size = new Size(150, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(selectWeightsFileLabel);

            // Weights File ComboBox - Preconfigured with common model options
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
            selectWeightsFileComboBox.SelectedIndex = 2; // Default to fp32 engine
            parentGroup.Controls.Add(selectWeightsFileComboBox);

            // Labels File Label
            selectLabelsFileLabel = new Label
            {
                Text = "Select Labels File",
                Location = new Point(20, 60),
                Size = new Size(150, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(selectLabelsFileLabel);

            // Labels File ComboBox - Preconfigured with the YAML file
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

            // Select Image Button - For selecting a single image for detection
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
            
            // Select Folder Button - For selecting a folder of images for batch detection
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

            // Enable GPU CheckBox - Toggle GPU acceleration
            enableGpuCheckBox = new CheckBox
            {
                Text = "Enable GPU",
                Location = new Point(20, 130),
                Size = new Size(150, 20),
                Checked = true, // Default to GPU enabled
                Font = regularFont
            };
            parentGroup.Controls.Add(enableGpuCheckBox);
        }

        /// <summary>
        /// Initializes detection parameters controls within the specified parent group.
        /// Creates and configures input fields for detection parameters such as image resolution,
        /// confidence threshold, and IOU threshold.
        /// </summary>
        /// <param name="parentGroup">The parent GroupBox where controls will be added</param>
        /// <param name="regularFont">Font to use for controls</param>
        /// <param name="imageResolutionLabel">Reference to the image resolution label</param>
        /// <param name="confidenceThresholdLabel">Reference to the confidence threshold label</param>
        /// <param name="iouThresholdLabel">Reference to the IOU threshold label</param>
        /// <param name="projectNameLabel">Reference to the project name label</param>
        /// <param name="imageResolutionHorizontalTextBox">Reference to the horizontal resolution textbox</param>
        /// <param name="imageResolutionVerticalTextBox">Reference to the vertical resolution textbox</param>
        /// <param name="confidenceThresholdTextBox">Reference to the confidence threshold textbox</param>
        /// <param name="iouThresholdTextBox">Reference to the IOU threshold textbox</param>
        /// <param name="projectNameTextBox">Reference to the project name textbox</param>
        public static void InitializeDetectionParametersControls(GroupBox parentGroup, Font regularFont, ref Label imageResolutionLabel, ref Label confidenceThresholdLabel, ref Label iouThresholdLabel, ref Label projectNameLabel, ref TextBox imageResolutionHorizontalTextBox, ref TextBox imageResolutionVerticalTextBox, ref TextBox confidenceThresholdTextBox, ref TextBox iouThresholdTextBox, ref TextBox projectNameTextBox)
        {
            // Image Resolution Label
            imageResolutionLabel = new Label
            {
                Text = "Image Resolution",
                Location = new Point(20, 30),
                Size = new Size(120, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(imageResolutionLabel);

            // Resolution Separator Label - "×" symbol between width and height
            Label resolutionSeparatorLabel = new Label
            {
                Text = "×",
                Location = new Point(245, 33),
                Size = new Size(15, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(resolutionSeparatorLabel);

            // Confidence Threshold Label - minimum confidence score for detections
            confidenceThresholdLabel = new Label
            {
                Text = "Confidence Threshold",
                Location = new Point(20, 60),
                Size = new Size(150, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(confidenceThresholdLabel);

            // IOU Threshold Label - intersection over union threshold for NMS
            iouThresholdLabel = new Label
            {
                Text = "IOU Threshold",
                Location = new Point(20, 90),
                Size = new Size(120, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(iouThresholdLabel);

            // Project Name Label - for organizing detection results
            projectNameLabel = new Label
            {
                Text = "Project Name",
                Location = new Point(20, 120),
                Size = new Size(100, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(projectNameLabel);

            // Horizontal Resolution TextBox - width dimension for input resizing
            imageResolutionHorizontalTextBox = new TextBox
            {
                Text = "1280", // Default width for YOLOv5x
                Location = new Point(200, 30),
                Size = new Size(40, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(imageResolutionHorizontalTextBox);

            // Vertical Resolution TextBox - height dimension for input resizing
            imageResolutionVerticalTextBox = new TextBox
            {
                Text = "1280", // Default height for YOLOv5x
                Location = new Point(265, 30),
                Size = new Size(40, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(imageResolutionVerticalTextBox);

            // Confidence Threshold TextBox - default 0.25 (25%)
            confidenceThresholdTextBox = new TextBox
            {
                Text = "0.25",
                Location = new Point(200, 60),
                Size = new Size(40, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(confidenceThresholdTextBox);

            // IOU Threshold TextBox - default 0.45 (45%)
            iouThresholdTextBox = new TextBox
            {
                Text = "0.45",
                Location = new Point(200, 90),
                Size = new Size(40, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(iouThresholdTextBox);

            // Project Name TextBox - for results storage
            projectNameTextBox = new TextBox
            {
                Text = "PETRIS_Test_Data",
                Location = new Point(200, 120),
                Size = new Size(180, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(projectNameTextBox);
        }

        /// <summary>
        /// Creates server control buttons on the form for starting/stopping the detection server
        /// and initiating the detection process.
        /// </summary>
        /// <param name="parent">The parent form where the buttons will be added</param>
        /// <param name="startServerButton">Reference to the start server button</param>
        /// <param name="quitServerButton">Reference to the quit server button</param>
        /// <param name="startDetectionButton">Reference to the start detection button</param>
        /// <param name="startServerButtonClickHandler">Event handler for start server button</param>
        /// <param name="quitServerButtonClickHandler">Event handler for quit server button</param>
        /// <param name="startDetectionButtonClickHandler">Event handler for start detection button</param>
        public static void CreateServerControlButtons(Form parent, ref Button startServerButton, ref Button quitServerButton, ref Button startDetectionButton, EventHandler startServerButtonClickHandler, EventHandler quitServerButtonClickHandler, EventHandler startDetectionButtonClickHandler)
        {
            // Start Server Button - Green, left-positioned
            startServerButton = new Button
            {
                Text = "Start Server",
                Location = new Point(20, 390),
                Size = new Size(190, 40),
                Font = new Font("Arial", 14, FontStyle.Bold),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
            };
            startServerButton.FlatAppearance.BorderSize = 1;
            startServerButton.FlatAppearance.BorderColor = Color.Black;
            startServerButton.Click += startServerButtonClickHandler;
            parent.Controls.Add(startServerButton);
            
            // Stop Server Button - Red, right-positioned, initially disabled
            quitServerButton = new Button
            {
                Text = "Stop Server",
                Location = new Point(230, 390),
                Size = new Size(190, 40),
                Font = new Font("Arial", 14, FontStyle.Bold),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Enabled = false // Disabled until server is started
            };
            quitServerButton.FlatAppearance.BorderSize = 1;
            quitServerButton.FlatAppearance.BorderColor = Color.Black;
            quitServerButton.Click += quitServerButtonClickHandler;
            parent.Controls.Add(quitServerButton);
            
            // Start Detection Button - Blue, full width, initially disabled
            startDetectionButton = new Button
            {
                Text = "► Start Detection",
                Location = new Point(20, 450),
                Size = new Size(400, 40),
                Font = new Font("Arial", 18, FontStyle.Bold),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Enabled = false // Disabled until server is started
            };
            startDetectionButton.FlatAppearance.BorderSize = 1;
            startDetectionButton.FlatAppearance.BorderColor = Color.Black;
            startDetectionButton.Click += startDetectionButtonClickHandler;
            parent.Controls.Add(startDetectionButton);
        }

        /// <summary>
        /// Initializes image panel controls within the specified parent group.
        /// Creates and configures picture boxes for displaying input and output images,
        /// as well as navigation buttons for browsing through multiple images.
        /// </summary>
        /// <param name="parentGroup">The parent GroupBox where controls will be added</param>
        /// <param name="boldFont">Font for header labels</param>
        /// <param name="regularFont">Font for buttons</param>
        /// <param name="inputImageLabel">Reference to the input image label</param>
        /// <param name="outputImageLabel">Reference to the output image label</param>
        /// <param name="inputPictureBox">Reference to the input image picture box</param>
        /// <param name="outputPictureBox">Reference to the output image picture box</param>
        /// <param name="previousButton">Reference to the previous image button</param>
        /// <param name="nextButton">Reference to the next image button</param>
        /// <param name="previousButtonClickHandler">Event handler for previous button</param>
        /// <param name="nextButtonClickHandler">Event handler for next button</param>
        public static void InitializeImagePanelControls(GroupBox parentGroup, Font boldFont, Font regularFont, ref Label inputImageLabel, ref Label outputImageLabel, ref PictureBox inputPictureBox, ref PictureBox outputPictureBox, ref Button previousButton, ref Button nextButton, EventHandler previousButtonClickHandler, EventHandler nextButtonClickHandler)
        {
            // Input Image Label - Title for the left picture box
            inputImageLabel = new Label
            {
                Text = "Input Image",
                Location = new Point(20, 25),
                Size = new Size(300, 20),
                Font = boldFont
            };
            parentGroup.Controls.Add(inputImageLabel);

            // Input PictureBox - Displays the original image
            inputPictureBox = new PictureBox
            {
                Location = new Point(20, 50),
                Size = new Size(410, 500),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom, // Maintain aspect ratio
                BackColor = Color.WhiteSmoke
            };
            parentGroup.Controls.Add(inputPictureBox);

            // Output Image Label - Title for the right picture box
            outputImageLabel = new Label
            {
                Text = "Output Image (Showing Input - Run Detection)",
                Location = new Point(450, 25),
                Size = new Size(400, 20),
                Font = boldFont
            };
            parentGroup.Controls.Add(outputImageLabel);

            // Output PictureBox - Displays the processed image with detections
            outputPictureBox = new PictureBox
            {
                Location = new Point(450, 50),
                Size = new Size(410, 500),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom, // Maintain aspect ratio
                BackColor = Color.WhiteSmoke
            };
            parentGroup.Controls.Add(outputPictureBox);
            
            // Previous Button - Navigation for batch processing, initially disabled
            previousButton = new Button
            {
                Text = "◄ Previous",
                Location = new Point(20, 560),
                Size = new Size(200, 30),
                Font = regularFont,
                Enabled = false // Disabled until multiple images are loaded
            };
            previousButton.Click += previousButtonClickHandler;
            parentGroup.Controls.Add(previousButton);
            
            // Next Button - Navigation for batch processing, initially disabled
            nextButton = new Button
            {
                Text = "Next ►",
                Location = new Point(230, 560),
                Size = new Size(200, 30),
                Font = regularFont,
                Enabled = false // Disabled until multiple images are loaded
            };
            nextButton.Click += nextButtonClickHandler;
            parentGroup.Controls.Add(nextButton);
        }
    }
}