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
using System.Threading;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using System.Text;
using Test_Software_AI_Automatic_Cleaning_Machine;

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

            // Extract yolov5_env.tar.gz if needed before initializing the detection service
            bool extractionSuccess = ExtractEnvironmentIfNeeded();
            if (!extractionSuccess)
            {
                MessageBox.Show("Failed to set up the Python environment. The application will now close.",
                    "Environment Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

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
            detectionConfigGroupBox = YoloApplicationUI.CreateGroupBox(this, "Detection Configuration", new Point(20, 20), new Size(400, 190), boldFont);
            YoloApplicationUI.InitializeDetectionConfigControls(detectionConfigGroupBox, regularFont, ref selectWeightsFileLabel, ref selectLabelsFileLabel, ref selectWeightsFileComboBox, ref selectLabelsFileComboBox, ref selectImageButton, ref selectFolderButton, ref enableGpuCheckBox, selectImageButton_Click, selectFolderButton_Click);

            // Create detection parameters group
            detectionParametersGroupBox = YoloApplicationUI.CreateGroupBox(this, "Detection Parameters", new Point(20, 220), new Size(400, 120), boldFont);
            YoloApplicationUI.InitializeDetectionParametersControls(detectionParametersGroupBox, regularFont, ref imageResolutionLabel, ref confidenceThresholdLabel, ref iouThresholdLabel, ref imageResolutionHorizontalTextBox, ref imageResolutionVerticalTextBox, ref confidenceThresholdTextBox, ref iouThresholdTextBox);

            // Create logging configuration group 
            loggingConfigGroupBox = YoloApplicationUI.CreateGroupBox(this, "Logging Configuration", new Point(20, 350), new Size(400, 120), boldFont);
            YoloApplicationUI.InitializeLoggingConfigurationControls(loggingConfigGroupBox, regularFont, ref projectNameLabel, ref projectNameTextBox, ref hideLabelCheckBox, ref hideConfidenceCheckBox);

            // Create server control buttons
            YoloApplicationUI.CreateServerControlButtons(this, ref startServerButton, ref quitServerButton, ref startDetectionButton, startServerButton_Click, quitServerButton_Click, startDetectionButton_Click);

            // Create image panel group
            imagePanelGroupBox = YoloApplicationUI.CreateGroupBox(this, "Image Panel", new Point(440, 20), new Size(890, 590), boldFont);
            YoloApplicationUI.InitializeImagePanelControls(imagePanelGroupBox, boldFont, regularFont, ref inputImageLabel, ref outputImageLabel, ref inputPictureBox, ref outputPictureBox, ref previousButton, ref nextButton, previousButton_Click, nextButton_Click);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Set form properties
            this.Text = "YOLOv5 Object Detection Tool";
            this.Size = new System.Drawing.Size(1366, 768);
            this.Font = regularFont;
            
            // Display Python environment information
            string pythonPath = _detectionService.GetPythonPath();
            bool isPortable = _detectionService.IsPortableEnvironmentAvailable;
            
            // Add diagnostic information
            MessageBox.Show($"Python path: {pythonPath}\n" +
                           $"Python exists: {File.Exists(pythonPath)}\n" +
                           $"Working directory: {AppDomain.CurrentDomain.BaseDirectory}", 
                           "Debug: Form_Load");
            
            if (isPortable)
            {
                MessageBox.Show($"Using portable Python environment: {pythonPath}", "Portable Python Environment");
                this.Text += " (Portable Python)";
            }
            else
            {
                MessageBox.Show($"Using system Python environment: {pythonPath}", "System Python Environment");
                this.Text += " (System Python)";
            }
        }

        private bool SetupRequiredDependencies()
        {
            try
            {
                // Check if portable environment is available
                bool isPortable = _detectionService.IsPortableEnvironmentAvailable;
                MessageBox.Show($"SetupRequiredDependencies - IsPortableEnvironmentAvailable: {isPortable}", "Debug: Dependency Check");
                
                if (!isPortable)
                {
                    MessageBox.Show(
                        "Portable Python environment not found.\n\n" +
                        "This application requires the 'yolov5_env' folder with a portable Python environment.\n\n" +
                        "Please ensure the 'yolov5_env' folder is in the same directory as the application.",
                        "Missing Required Environment",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }

                // Verify all other dependencies are available
                MessageBox.Show("Now calling VerifyDependencies...", "Debug: Dependency Check");
                string errorMessage;
                if (!_detectionService.VerifyDependencies(out errorMessage))
                {
                    MessageBox.Show(errorMessage, "Missing Dependencies", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                MessageBox.Show("All dependencies verified successfully!", "Debug: Dependency Check Success");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up dependencies: {ex.Message}",
                    "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
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
                
                // Log the input image and expected output location
                MessageBox.Show($"Input image: {inputImagePath}\nExpected output: {filename}", "Debug: Output Image Paths");
                
                // Try multiple possible output locations
                List<string> possibleOutputPaths = new List<string>();
                
                // 1. Try the standard output directory
                if (!string.IsNullOrEmpty(_outputDirectory))
                {
                    possibleOutputPaths.Add(Path.Combine(_outputDirectory, filename));
                }
                
                // 2. Try with timestamp-based directories (they might be created by the Python script)
                string detectionsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detections");
                if (Directory.Exists(detectionsDir))
                {
                    // Add all project directories that might contain the image
                    foreach (var dir in Directory.GetDirectories(detectionsDir))
                    {
                        if (dir.Contains(projectNameTextBox.Text) || dir.Contains("results_"))
                        {
                            possibleOutputPaths.Add(Path.Combine(dir, filename));
                        }
                    }
                }
                
                // Display all the paths we're going to check
                string allPaths = string.Join("\n", possibleOutputPaths);
                MessageBox.Show($"Checking these possible output paths:\n{allPaths}", "Debug: Possible Output Paths");
                
                // Try each possible path
                string foundPath = null;
                foreach (string path in possibleOutputPaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }
                
                // If found, display the image
                if (foundPath != null)
                {
                    // Load the output image
                    outputImage = Image.FromFile(foundPath);
                    
                    // Display the output image
                    outputPictureBox.Image = outputImage;
                    outputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    outputPictureBox.Refresh();
                    
                    // Update the output image label
                    outputImageLabel.Text = $"Output Image: {filename}";
                    
                    // Update the output directory for future lookups
                    _outputDirectory = Path.GetDirectoryName(foundPath);
                    MessageBox.Show($"Updated output directory to: {_outputDirectory}", "Output Directory Updated");
                }
                else
                {
                    // If output image doesn't exist, show input image instead
                    outputPictureBox.Image = inputImage;
                    outputPictureBox.Refresh();
                    outputImageLabel.Text = "Output Image Not Found";

                    MessageBox.Show($"Output image not found in any of the expected locations.", "Output Image Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading output image: {ex.Message}", "Output Image Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                
                // Verify Python exists
                string pythonPath = _detectionService.GetPythonPath();
                if (!File.Exists(pythonPath))
                {
                    MessageBox.Show($"Python executable not found: {pythonPath}\nPlease ensure the yolov5_env is properly extracted.", 
                        "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetServerButtonState();
                    return;
                }
                
                // Add more diagnostic logging
                MessageBox.Show($"Starting server with Python: {pythonPath}\n" +
                               $"Python exists: {File.Exists(pythonPath)}\n" +
                               $"Engine path: {enginePath}\n" +
                               $"Labels path: {labelsPath}", 
                               "Debug: startServerButton_Click");
                
                // Start the detection server
                string errorMessage;
                bool success = _detectionService.StartServer(
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
                    
                    // Set the output directory for loading images
                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    _outputDirectory = Path.Combine(basePath, "Detections", projectNameTextBox.Text);
                    
                    // Log the output directory
                    MessageBox.Show($"Output directory set to: {_outputDirectory}", "Output Directory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
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
                
                // Debug information
                MessageBox.Show($"Starting detection for: {selectedPath}\n" +
                               $"Is folder: {isFolder}", 
                               "Debug: startDetectionButton_Click");
                
                // Disable the start detection button during processing
                startDetectionButton.Enabled = false;
                
                if (isFolder)
                {
                    MessageBox.Show($"Attempting to detect folder: {selectedPath}", "Debug: Folder Detection");
                    
                    // Process folder detection
                    string errorMessage;
                    bool success = _detectionService.DetectFolder(selectedPath, out errorMessage);
                    
                    if (success)
                    {
                        // Detection started successfully
                        _detectionCompleted = true;
                        
                        // Update the output directory based on the project name
                        UpdateOutputDirectory();
                        
                        // Update the output image after a small delay to allow processing
                        Task.Delay(1000).ContinueWith(t =>
                        {
                            this.Invoke(new Action(() => {
                                if (_currentImageIndex < _imageFiles.Count)
                                {
                                    LoadAndDisplayOutputImage(_imageFiles[_currentImageIndex]);
                                }
                            }));
                        });
                    }
                    else 
                    {
                        // Detection failed
                        MessageBox.Show(errorMessage, "Folder Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        MessageBox.Show($"Detection failed: {errorMessage}", "Debug: Detection Error");
                    }
                }
                else
                {
                    // Single image detection
                    MessageBox.Show($"Attempting to detect image: {selectedPath}", "Debug: Image Detection");
                    
                    // Process image detection
                    string errorMessage;
                    bool success = _detectionService.DetectImage(selectedPath, out errorMessage);
                    
                    if (success)
                    {
                        // Detection started successfully
                        _detectionCompleted = true;
                        
                        // Update the output directory based on the project name
                        UpdateOutputDirectory();
                        
                        // Update the output image after a small delay to allow processing
                        Task.Delay(1000).ContinueWith(t =>
                        {
                            this.Invoke(new Action(() => {
                                if (_currentImageIndex < _imageFiles.Count)
                                {
                                    LoadAndDisplayOutputImage(_imageFiles[_currentImageIndex]);
                                }
                            }));
                        });
                    }
                    else 
                    {
                        // Detection failed
                        MessageBox.Show(errorMessage, "Image Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        MessageBox.Show($"Detection failed: {errorMessage}", "Debug: Detection Error");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception in startDetectionButton_Click: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restore cursor and button state
                this.Cursor = Cursors.Default;
                startDetectionButton.Enabled = true;
                startDetectionButton.Text = "► Start Detection";
            }
        }

        /// <summary>
        /// Extracts the embedded yolov5_env.tar.gz file if the yolov5_env directory doesn't exist
        /// </summary>
        /// <returns>True if extraction succeeded or the directory already exists, false otherwise</returns>
        private bool ExtractEnvironmentIfNeeded()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string envDirPath = Path.Combine(basePath, "yolov5_env");
                string pythonExePath = Path.Combine(envDirPath, "python.exe");

                // Ensure paths are normalized
                basePath = Path.GetFullPath(basePath);
                envDirPath = Path.GetFullPath(envDirPath);
                pythonExePath = Path.GetFullPath(pythonExePath);

                MessageBox.Show($"Checking for Python at: {pythonExePath}\n" +
                               $"Environment directory: {envDirPath}\n" +
                               $"Directory exists: {Directory.Exists(envDirPath)}\n" +
                               $"Python exists: {File.Exists(pythonExePath)}",
                               "Debug: ExtractEnvironmentIfNeeded");

                // Check if the environment directory already exists
                if (Directory.Exists(envDirPath) && File.Exists(pythonExePath))
                {
                    MessageBox.Show("yolov5_env directory already exists, skipping extraction.", "Environment Found");
                    return true;
                }

                // Create and show a progress form
                Form progressForm = new Form
                {
                    Text = "Extracting Python Environment",
                    Size = new Size(400, 150),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ControlBox = false
                };

                Label statusLabel = new Label
                {
                    Text = "Extracting portable Python environment...\nThis may take a few minutes.",
                    AutoSize = false,
                    Size = new Size(360, 50),
                    Location = new Point(20, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                ProgressBar progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    MarqueeAnimationSpeed = 30,
                    Size = new Size(360, 23),
                    Location = new Point(20, 70)
                };

                progressForm.Controls.Add(statusLabel);
                progressForm.Controls.Add(progressBar);
                
                // Create a background thread for extraction
                bool extractionResult = false;
                Thread extractionThread = new Thread(() =>
                {
                    try
                    {
                        extractionResult = PerformExtraction(basePath, envDirPath, statusLabel);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Extraction error: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        extractionResult = false;
                    }
                    finally
                    {
                        // Close the progress form
                        progressForm.Invoke((System.Windows.Forms.MethodInvoker)delegate
                        {
                            progressForm.Close();
                        });
                    }
                });
                
                extractionThread.Start();
                progressForm.ShowDialog(); // This will block until the form is closed
                extractionThread.Join(); // Wait for the thread to finish

                return extractionResult;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting environment: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool PerformExtraction(string basePath, string envDirPath, Label statusLabel)
        {
            try
            {
                // Update status
                UpdateStatus(statusLabel, "Looking for Python environment package...");
                
                // Normalize paths
                envDirPath = Path.GetFullPath(envDirPath);
                string pythonExePath = Path.Combine(envDirPath, "python.exe");
                
                MessageBox.Show($"Checking for Python at: {pythonExePath}\n" +
                               $"Environment directory: {envDirPath}\n" +
                               $"Directory exists: {Directory.Exists(envDirPath)}\n" +
                               $"Python exists: {File.Exists(pythonExePath)}",
                               "Debug: PerformExtraction");
                
                // First check if the environment already exists
                if (Directory.Exists(envDirPath) && File.Exists(pythonExePath))
                {
                    MessageBox.Show($"Python environment already exists at {envDirPath}. Using existing installation.",
                        "Environment Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                
                MessageBox.Show("yolov5_env directory not found or missing python.exe, attempting to extract from tar.gz file...",
                    "Extraction Needed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Look for the tar.gz file in the application directory
                string tarGzFilePath = Path.Combine(basePath, "yolov5_env.tar.gz");
                
                // Check if the file exists
                if (!File.Exists(tarGzFilePath))
                {
                    MessageBox.Show($"Error: yolov5_env.tar.gz not found at {tarGzFilePath}. Please make sure the file is in the application directory.",
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus(statusLabel, "Error: Python environment package not found. Make sure yolov5_env.tar.gz is in the application directory.");
                    return false;
                }
                
                MessageBox.Show($"Found yolov5_env.tar.gz at: {tarGzFilePath}. Proceeding with extraction.",
                    "Package Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus(statusLabel, "Found Python environment package. Preparing for extraction...");

                try
                {
                    // Extract using SharpCompress
                    UpdateStatus(statusLabel, "Extracting Python environment (this may take a few minutes)...");
                    
                    // Create the target directory for extraction if it doesn't exist
                    if (!Directory.Exists(envDirPath))
                    {
                        Directory.CreateDirectory(envDirPath);
                    }
                    
                    // Extract the tar.gz file directly into the yolov5_env folder
                    ExtractTarGzWithSharpCompress(tarGzFilePath, envDirPath, statusLabel);
                    
                    // Verify extraction was successful - only check the main Python path
                    string pythonPath = Path.Combine(envDirPath, "python.exe");
                    if (File.Exists(pythonPath))
                    {
                        UpdateStatus(statusLabel, "Successfully extracted Python environment.");
                        MessageBox.Show("Successfully extracted yolov5_env.",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    else
                    {
                        MessageBox.Show($"Error: Python executable not found at {pythonPath}. Please check the extraction process.",
                            "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
                        UpdateStatus(statusLabel, "Error: Python environment not properly extracted. Python executable not found.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus(statusLabel, $"Error during extraction: {ex.Message}");
                    MessageBox.Show($"Error during extraction: {ex.Message}",
                        "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(statusLabel, $"Error: {ex.Message}");
                MessageBox.Show($"Error extracting environment: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ExtractTarGzWithSharpCompress(string tarGzFilePath, string destinationPath, Label statusLabel)
        {
            try
            {
                UpdateStatus(statusLabel, "Opening archive...");
                
                // Open the tar.gz file
                using (var tarGzStream = File.OpenRead(tarGzFilePath))
                {
                    // Create a reader for the archive
                    using (var reader = ReaderFactory.Open(tarGzStream, new ReaderOptions { 
                        ArchiveEncoding = new ArchiveEncoding { Default = System.Text.Encoding.UTF8 },
                        LeaveStreamOpen = false
                    }))
                    {
                        // Count total entries for progress reporting (optional)
                        long totalEntries = 0;
                        try
                        {
                            using (var tempArchive = ArchiveFactory.Open(tarGzFilePath, new ReaderOptions { 
                                ArchiveEncoding = new ArchiveEncoding { Default = System.Text.Encoding.UTF8 } 
                            }))
                            {
                                totalEntries = tempArchive.Entries.Count();
                            }
                        }
                        catch (Exception ex)
                        {
                            // If we can't count entries, just proceed without detailed progress
                            UpdateStatus(statusLabel, $"Warning: Could not count entries: {ex.Message}");
                            totalEntries = 1000; // Assume a large number
                        }
                        
                        // Extract each entry
                        long currentEntry = 0;
                        while (reader.MoveToNextEntry())
                        {
                            currentEntry++;
                            
                            try
                            {
                                // Skip entries with null keys
                                if (string.IsNullOrEmpty(reader.Entry.Key))
                                {
                                    UpdateStatus(statusLabel, "Warning: Skipping entry with null key");
                                    continue;
                                }
                                
                                // Report progress periodically
                                if (currentEntry % 100 == 0 || currentEntry == 1)
                                {
                                    double progressPercent = Math.Min(100, (double)currentEntry / totalEntries * 100);
                                    UpdateStatus(statusLabel, $"Extracting files... ({progressPercent:0}%)");
                                }
                                
                                // Normalize the entry key to handle various archive formats
                                string entryKey = reader.Entry.Key.Replace('\\', '/').TrimStart('/');
                                
                                // Handle entry path cleanup - skip any parent directory references
                                if (entryKey.StartsWith("..") || entryKey.Contains("/../") || entryKey.Contains("/./"))
                                {
                                    UpdateStatus(statusLabel, $"Warning: Skipping potentially unsafe path: {entryKey}");
                                    continue;
                                }
                                
                                // Get the full path for the entry
                                string entryPath = Path.Combine(destinationPath, entryKey);
                                
                                // Handle directory entries
                                if (reader.Entry.IsDirectory)
                                {
                                    if (!Directory.Exists(entryPath))
                                    {
                                        Directory.CreateDirectory(entryPath);
                                    }
                                    continue;
                                }
                                
                                // For file entries, create the containing directory if needed
                                string directoryPath = Path.GetDirectoryName(entryPath);
                                if (directoryPath != null && !Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }
                                
                                // Extract the file
                                using (var entryStream = File.Create(entryPath))
                                {
                                    reader.WriteEntryTo(entryStream);
                                }
                                
                                // Set the file's last write time if available
                                if (reader.Entry.LastModifiedTime.HasValue)
                                {
                                    File.SetLastWriteTime(entryPath, reader.Entry.LastModifiedTime.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log the error but continue with the next entry
                                UpdateStatus(statusLabel, $"Warning: Error extracting entry {reader.Entry.Key}: {ex.Message}");
                            }
                        }
                    }
                }
                
                UpdateStatus(statusLabel, "Extraction completed successfully.");
            }
            catch (Exception ex)
            {
                UpdateStatus(statusLabel, $"Error in SharpCompress extraction: {ex.Message}");
                throw; // Re-throw to be handled by caller
            }
        }

        private void UpdateStatus(Label statusLabel, string message)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    statusLabel.Text = message;
                    Application.DoEvents();
                });
            }
            else
            {
                statusLabel.Text = message;
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Updates the output directory based on the project name
        /// </summary>
        private void UpdateOutputDirectory()
        {
            // Set the output directory again as it might have a timestamp
            string detectionsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detections");
            if (Directory.Exists(detectionsDir))
            {
                // Look for directories containing the project name
                string[] matchingDirs = Directory.GetDirectories(detectionsDir)
                    .Where(d => Path.GetFileName(d).Contains(projectNameTextBox.Text))
                    .ToArray();
                    
                // Use the most recently created directory (which would have the latest timestamp)
                if (matchingDirs.Length > 0)
                {
                    _outputDirectory = matchingDirs
                        .OrderByDescending(d => Directory.GetCreationTime(d))
                        .First();
                        
                    MessageBox.Show($"Updated output directory to: {_outputDirectory}", "Output Directory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
