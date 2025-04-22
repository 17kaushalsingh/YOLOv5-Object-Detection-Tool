using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO.Compression;
using System.Threading;

namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    public partial class Form1 : Form
    {
        //----------------------------------------------------------------------
        // Detection Configuration Panel Controls
        // 1. Detection Configuration Items: Weights and Labels Files, Source Image Selection, Hardware Acceleration Settings
        // 2. Detection Parameters Items: Image Resolution and Threshold Settings (Confidence and IOU)
        // 3. Logging Configuration Items: Project Naming, Output Visualization Options (Hide Labels and Confidence)
        // 4. Image Panel Controls: Input and Output Image Display
        // 5. Common Font for Controls
        //----------------------------------------------------------------------
        Label selectWeightsFileLabel, selectLabelsFileLabel, imageResolutionLabel, confidenceThresholdLabel, iouThresholdLabel, projectNameLabel, inputImageLabel, outputImageLabel;
        ComboBox selectWeightsFileComboBox, selectLabelsFileComboBox;
        Button selectImageButton, selectFolderButton, startServerButton, quitServerButton, startDetectionButton, previousButton, nextButton;
        string selectedPath;
        bool isFolder = false;
        CheckBox enableGpuCheckBox, hideLabelCheckBox, hideConfidenceCheckBox;
        TextBox imageResolutionHorizontalTextBox, imageResolutionVerticalTextBox, confidenceThresholdTextBox, iouThresholdTextBox, projectNameTextBox;
        PictureBox inputPictureBox, outputPictureBox;
        Image inputImage, outputImage;
        GroupBox detectionConfigGroupBox, detectionParametersGroupBox, loggingConfigGroupBox, imagePanelGroupBox;
        Font regularFont = new Font("Arial", 9, FontStyle.Regular), boldFont = new Font("Arial", 9, FontStyle.Bold);
        private readonly YoloDetectionService _detectionService;
        private List<string> _imageFiles = new List<string>();
        private int _currentImageIndex = 0;
        
        // Flag to track if detection has been completed
        private bool _detectionCompleted = false;
        // Store the output directory for detected images
        private string _outputDirectory = string.Empty;

        public Form1()
        {
            InitializeComponent();

            // Initialize the detection service
            _detectionService = new YoloDetectionService(AppDomain.CurrentDomain.BaseDirectory);

            //----------------------------------------------------------------------
            // Check and setup required dependencies before continuing
            //----------------------------------------------------------------------
            if (!SetupRequiredDependencies())
            {
                MessageBox.Show("Failed to verify required dependencies. The application will now close.",
                    "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            //----------------------------------------------------------------------
            // Initialize UI components
            //----------------------------------------------------------------------
            InitializeUIComponents();
            
            // Register event handler for form load
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Clean up resources when the form is closing
            if (_detectionService.IsServerRunning)
            {
                string errorMessage;
                if (!_detectionService.StopServer(out errorMessage))
                {
                    MessageBox.Show($"Error stopping server: {errorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            
            _detectionService.Cleanup();
        }

        private void InitializeUIComponents()
        {
            // Create detection configuration group
            detectionConfigGroupBox = CreateGroupBox("Detection Configuration", new Point(20, 20), new Size(400, 190));
            InitializeDetectionConfigControls(detectionConfigGroupBox, regularFont);

            // Create detection parameters group
            detectionParametersGroupBox = CreateGroupBox("Detection Parameters", new Point(20, 220), new Size(400, 120));
            InitializeDetectionParametersControls(detectionParametersGroupBox, regularFont);

            // Create logging configuration group 
            loggingConfigGroupBox = CreateGroupBox("Logging Configuration", new Point(20, 350), new Size(400, 120));
            InitializeLoggingConfigurationControls(loggingConfigGroupBox, regularFont);

            // Create server control buttons
            CreateServerControlButtons();

            // Create image panel group
            imagePanelGroupBox = CreateGroupBox("Image Panel", new Point(440, 20), new Size(890, 590));
            InitializeImagePanelControls(imagePanelGroupBox, boldFont);
        }

        private GroupBox CreateGroupBox(string text, Point location, Size size)
        {
            var groupBox = new GroupBox
            {
                Text = text,
                Location = location,
                Size = size,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            groupBox.Font = boldFont;
            Controls.Add(groupBox);
            return groupBox;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Set form properties
            this.Text = "YOLOv5 Object Detection Tool";
            this.Size = new System.Drawing.Size(1366, 768);
            this.Font = regularFont;
        }

        private bool SetupRequiredDependencies()
        {
            try
            {
                // Verify all dependencies are available
                string errorMessage;
                if (!_detectionService.VerifyDependencies(out errorMessage))
                {
                    MessageBox.Show(errorMessage, "Missing Dependencies", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up dependencies: {ex.Message}",
                    "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void InitializeDetectionConfigControls(GroupBox parentGroup, Font regularFont)
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
            selectImageButton.Click += selectImageButton_Click;
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
            selectFolderButton.Click += selectFolderButton_Click;
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

        private void InitializeDetectionParametersControls(GroupBox parentGroup, Font regularFont)
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

        private void InitializeLoggingConfigurationControls(GroupBox parentGroup, Font regularFont)
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
        
        private void CreateServerControlButtons()
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
            startServerButton.Click += startServerButton_Click;
            Controls.Add(startServerButton);
            
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
            quitServerButton.Click += quitServerButton_Click;
            Controls.Add(quitServerButton);
            
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
            startDetectionButton.Click += startDetectionButton_Click;
            Controls.Add(startDetectionButton);
        }

        private void InitializeImagePanelControls(GroupBox parentGroup, Font boldFont)
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
            previousButton.Click += previousButton_Click;
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
            nextButton.Click += nextButton_Click;
            parentGroup.Controls.Add(nextButton);
        }

        private void selectImageButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // Configure dialog settings
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Update path variable
                    selectedPath = openFileDialog.FileName;
                    isFolder = false;
                    
                    // Reset navigation
                    _imageFiles.Clear();
                    _imageFiles.Add(selectedPath);
                    _currentImageIndex = 0;
                    
                    // Update navigation buttons
                    UpdateNavigationButtons();

                    // Load and display the selected image
                    LoadAndDisplayInputImage(selectedPath);
                    
                    // Update the input image label
                    inputImageLabel.Text = $"Input Image: {Path.GetFileName(selectedPath)}";
                }
            }
        }
        
        private void selectFolderButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // Update path variable
                    selectedPath = folderDialog.SelectedPath;
                    isFolder = true;
                    
                    // Load all image files from the folder
                    LoadImagesFromFolder(selectedPath);
                    
                    // Update input image label
                    inputImageLabel.Text = $"Input Folder: {Path.GetFileName(selectedPath)}";
                    
                    // Display the first image if any
                    if (_imageFiles.Count > 0)
                    {
                        _currentImageIndex = 0;
                        LoadAndDisplayInputImage(_imageFiles[_currentImageIndex]);
                    }
                    else
                    {
                        MessageBox.Show("No image files found in the selected folder.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    
                    // Update navigation buttons
                    UpdateNavigationButtons();
                }
            }
        }
        
        private void LoadImagesFromFolder(string folderPath)
        {
            _imageFiles.Clear();
            
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
            
            try
            {
                foreach (string ext in imageExtensions)
                {
                    string[] files = Directory.GetFiles(folderPath, $"*{ext}");
                    _imageFiles.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading images from folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateNavigationButtons()
        {
            previousButton.Enabled = _imageFiles.Count > 1 && _currentImageIndex > 0;
            nextButton.Enabled = _imageFiles.Count > 1 && _currentImageIndex < _imageFiles.Count - 1;
        }
        
        private void previousButton_Click(object sender, EventArgs e)
        {
            if (_currentImageIndex > 0)
            {
                _currentImageIndex--;
                
                // Load the input image
                LoadAndDisplayInputImage(_imageFiles[_currentImageIndex]);
                
                // If detection has been completed, load corresponding output image
                if (_detectionCompleted)
                {
                    LoadAndDisplayOutputImage(_imageFiles[_currentImageIndex]);
                }
                
                UpdateNavigationButtons();
            }
        }
        
        private void nextButton_Click(object sender, EventArgs e)
        {
            if (_currentImageIndex < _imageFiles.Count - 1)
            {
                _currentImageIndex++;
                
                // Load the input image
                LoadAndDisplayInputImage(_imageFiles[_currentImageIndex]);
                
                // If detection has been completed, load corresponding output image
                if (_detectionCompleted)
                {
                    LoadAndDisplayOutputImage(_imageFiles[_currentImageIndex]);
                }
                
                UpdateNavigationButtons();
            }
        }

        private void LoadAndDisplayInputImage(string imagePath)
        {
            if (inputImage != null) // Dispose existing images if any
            {
                inputImage.Dispose();
            }

            try
            {
                // Load the new image
                inputImage = Image.FromFile(imagePath);

                // Display input image in input picture box
                inputPictureBox.Image = inputImage;
                inputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                inputPictureBox.Refresh();

                // If detection hasn't been completed, show input image in output box too
                if (!_detectionCompleted || outputImage == null)
                {
                    outputPictureBox.Image = inputImage;
                    outputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    outputPictureBox.Refresh();
                    outputImageLabel.Text = "Output Image (Showing Input - Run Detection)";
                }
                
                // Update input image label with filename
                inputImageLabel.Text = $"Input Image: {Path.GetFileName(imagePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // New method to load and display output image
        private void LoadAndDisplayOutputImage(string inputImagePath)
        {
            // Clean up previous output image if it exists
            if (outputImage != null)
            {
                outputImage.Dispose();
                outputImage = null;
            }
            
            try
            {
                // Construct the path to the output image
                string filename = Path.GetFileName(inputImagePath);
                string outputImagePath = Path.Combine(_outputDirectory, filename);
                
                // Check if the output image exists
                if (File.Exists(outputImagePath))
                {
                    // Load the output image
                    outputImage = Image.FromFile(outputImagePath);
                    
                    // Display the output image
                    outputPictureBox.Image = outputImage;
                    outputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    outputPictureBox.Refresh();
                    
                    // Update the output image label
                    outputImageLabel.Text = $"Output Image: {filename}";
                }
                else
                {
                    // If output image doesn't exist, show input image instead
                    outputPictureBox.Image = inputImage;
                    outputPictureBox.Refresh();
                    outputImageLabel.Text = "Output Image Not Found";
                    
                    Console.WriteLine($"Output image not found at: {outputImagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading output image: {ex.Message}");
                outputPictureBox.Image = inputImage;
                outputPictureBox.Refresh();
                outputImageLabel.Text = "Error Loading Output Image";
            }
        }
        
        private void startServerButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (selectWeightsFileComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a weights file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (selectLabelsFileComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a Labels file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Show wait cursor
                this.Cursor = Cursors.WaitCursor;
                startServerButton.Enabled = false;
                startServerButton.Text = "Starting...";
                Application.DoEvents();
                
                // Make sure the models exist
                string engineFile = selectWeightsFileComboBox.SelectedItem.ToString();
                string labelsFile = selectLabelsFileComboBox.SelectedItem.ToString();
                
                // Verify the files actually exist in the Models directory
                string enginePath = Path.Combine(_detectionService.GetAssetsPath(), engineFile);
                string labelsPath = Path.Combine(_detectionService.GetAssetsPath(), labelsFile);
                
                if (!File.Exists(enginePath))
                {
                    MessageBox.Show($"Model file not found: {enginePath}\nPlease ensure the model file is in the Models directory.", 
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetServerButtonState();
                    return;
                }
                
                if (!File.Exists(labelsPath))
                {
                    MessageBox.Show($"Labels file not found: {labelsPath}\nPlease ensure the labels file is in the Models directory.", 
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetServerButtonState();
                    return;
                }
                
                // Start the detection server
                string errorMessage;
                bool success = _detectionService.StartServer(
                    "python",
                    engineFile,
                    labelsFile,
                    imageResolutionHorizontalTextBox.Text,
                    imageResolutionVerticalTextBox.Text,
                    confidenceThresholdTextBox.Text,
                    iouThresholdTextBox.Text,
                    hideLabelCheckBox.Checked,
                    hideConfidenceCheckBox.Checked,
                    projectNameTextBox.Text,
                    out errorMessage
                );
                
                if (success)
                {
                    // Update UI state
                    startServerButton.Enabled = false;
                    startServerButton.Text = "Server Running";
                    quitServerButton.Enabled = true;
                    startDetectionButton.Enabled = true;
                    
                    // Lock configuration controls
                    EnableConfigControls(false);
                    
                    MessageBox.Show("YOLOv5 detection server started successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to start detection server: {errorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetServerButtonState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetServerButtonState();
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        
        private void ResetServerButtonState()
        {
            startServerButton.Enabled = true;
            startServerButton.Text = "Start Server";
            this.Cursor = Cursors.Default;
        }
        
        private void quitServerButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Show wait cursor
                this.Cursor = Cursors.WaitCursor;
                quitServerButton.Enabled = false;
                quitServerButton.Text = "Stopping...";
                Application.DoEvents();
                
                // Stop the detection server
                string errorMessage;
                bool success = _detectionService.StopServer(out errorMessage);
                
                if (success)
                {
                    // Update UI state
                    startServerButton.Enabled = true;
                    startServerButton.Text = "Start Server";
                    quitServerButton.Enabled = false;
                    quitServerButton.Text = "Stop Server";
                    startDetectionButton.Enabled = false;
                    
                    // Unlock configuration controls
                    EnableConfigControls(true);
                    
                    MessageBox.Show("YOLOv5 detection server stopped successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to stop detection server: {errorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    quitServerButton.Enabled = true;
                    quitServerButton.Text = "Stop Server";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                quitServerButton.Enabled = true;
                quitServerButton.Text = "Stop Server";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        
        private void EnableConfigControls(bool enabled)
        {
            // Enable/disable configuration controls
            selectWeightsFileComboBox.Enabled = enabled;
            selectLabelsFileComboBox.Enabled = enabled;
            imageResolutionHorizontalTextBox.Enabled = enabled;
            imageResolutionVerticalTextBox.Enabled = enabled;
            confidenceThresholdTextBox.Enabled = enabled;
            iouThresholdTextBox.Enabled = enabled;
            projectNameTextBox.Enabled = enabled;
            hideLabelCheckBox.Enabled = enabled;
            hideConfidenceCheckBox.Enabled = enabled;
            enableGpuCheckBox.Enabled = enabled;
        }

        private void startDetectionButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate selected path
                if (string.IsNullOrEmpty(selectedPath))
                {
                    MessageBox.Show("Please select an image or folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Show a loading cursor
                this.Cursor = Cursors.WaitCursor;
                
                // Disable detection button to prevent multiple requests
                startDetectionButton.Enabled = false;
                startDetectionButton.Text = "Processing...";
                Application.DoEvents(); // Refresh UI
                
                string errorMessage;
                bool success;
                
                // Log detection starting
                Console.WriteLine($"Starting detection for: {selectedPath}");
                Console.WriteLine($"Is folder: {isFolder}");
                
                // Set output directory
                _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detections", projectNameTextBox.Text);
                
                // Check if we're processing an image or folder
                if (isFolder)
                {
                    // Process folder
                    Console.WriteLine($"Attempting to detect folder: {selectedPath}");
                    success = _detectionService.DetectFolder(selectedPath, out errorMessage);
                    
                    if (success)
                    {
                        // Set detection completed flag
                        _detectionCompleted = true;
                        
                        MessageBox.Show("Folder detection completed. Results saved in the Detections folder.", 
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Load the output image for the current image
                        if (_imageFiles.Count > 0 && _currentImageIndex >= 0 && _currentImageIndex < _imageFiles.Count)
                        {
                            LoadAndDisplayOutputImage(_imageFiles[_currentImageIndex]);
                        }
                    }
                }
                else
                {
                    // Process single image
                    Console.WriteLine($"Attempting to detect image: {selectedPath}");
                    success = _detectionService.DetectImage(selectedPath, out errorMessage);
                    
                    if (success)
                    {
                        // Set detection completed flag
                        _detectionCompleted = true;
                        
                        MessageBox.Show("Image detection completed. Results saved in the Detections folder.", 
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Load the output image
                        LoadAndDisplayOutputImage(selectedPath);
                    }
                }
                
                if (!success)
                {
                    Console.WriteLine($"Detection failed: {errorMessage}");
                    MessageBox.Show($"Failed to start detection: {errorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in startDetectionButton_Click: {ex}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restore cursor and button state
                this.Cursor = Cursors.Default;
                startDetectionButton.Enabled = true;
                startDetectionButton.Text = "► Start Detection";
            }
        }
    }
}
