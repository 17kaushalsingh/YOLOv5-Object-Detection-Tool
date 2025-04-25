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
        // 1. Detection Configuration Items: Weights and Labels Files, Source Image Selection, Hardware Acceleration Settings
        Label selectWeightsFileLabel, selectLabelsFileLabel;
        ComboBox selectWeightsFileComboBox, selectLabelsFileComboBox;
        CheckBox enableGpuCheckBox;
        Button selectImageButton, selectFolderButton;
        GroupBox detectionConfigGroupBox;

        // 2. Detection Parameters Items: Image Resolution, Confidence and IOU, Project Name
        Label imageResolutionLabel, confidenceThresholdLabel, iouThresholdLabel, projectNameLabel;
        TextBox imageResolutionHorizontalTextBox, imageResolutionVerticalTextBox, confidenceThresholdTextBox, iouThresholdTextBox, projectNameTextBox;
        GroupBox detectionParametersGroupBox;

        // 3. Server Controls: Start Server, Stop Server, Start Detection
        Button startServerButton, quitServerButton, startDetectionButton;

        // 4. Image Panel Controls: Input and Output Image Display
        Label inputImageLabel, outputImageLabel;
        PictureBox inputPictureBox, outputPictureBox;
        Image inputImage, outputImage;
        Button previousButton, nextButton;
        GroupBox imagePanelGroupBox;
        string selectedPath;
        bool isFolder = false;
        private List<string> _imageFiles = new List<string>();
        private int _currentImageIndex = 0;

        // 5. Common Font for Controls
        Font regularFont = new Font("Arial", 9, FontStyle.Regular);
        Font boldFont = new Font("Arial", 9, FontStyle.Bold);

        // 6. Yolov5 Detection Service to manage communication between the UI and the python based detection server
        private readonly YoloDetectionService _detectionService;

        private bool _detectionCompleted = false; // Flag to track if detection has been completed
        private string _outputDirectory = string.Empty; // Store the output directory for detected images

        public Form1()
        {
            InitializeComponent();
            
            try
            {
                this.Load += Form1_Load; // Register event handler for form load and load the UI

                // Extract yolov5_env.tar.gz if needed before initializing the detection service
                bool extractionSuccess = YoloInitialSetup.ExtractEnvironmentIfNeeded();
                if (!extractionSuccess)
                {
                    MessageBox.Show("Failed to set up the Python environment. The application will now close.",
                        "Environment Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                    return;
                }

                // Initialize the detection service
                _detectionService = new YoloDetectionService(AppDomain.CurrentDomain.BaseDirectory);

                this.FormClosing += Form1_FormClosing; // Register event handler for form closing

                // Initialize image files list and current index
                _imageFiles = new List<string>();
                _currentImageIndex = -1;

                // Initialize detections directory
                try
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string detectionsDirectory = Path.Combine(baseDirectory, "Detections");
                    
                    // Create the Detections directory if it doesn't exist
                    if (!Directory.Exists(detectionsDirectory))
                    {
                        Directory.CreateDirectory(detectionsDirectory);
                        MessageBox.Show($"Created Detections directory at: {detectionsDirectory}", "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    // Initialize the output directory with a timestamp
                    InitializeOutputDirectory();
                        
                    // Update navigation buttons
                    UpdateNavigationButtons();
                    
                    MessageBox.Show("Form1 initialization completed successfully", "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    // Create default directories if possible
                    try
                    {
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        _outputDirectory = Path.Combine(baseDirectory, "Detections");
                        if (!Directory.Exists(_outputDirectory))
                        {
                            Directory.CreateDirectory(_outputDirectory);
                        }
                    }
                    catch {}
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error during initialization: {ex.Message}\n{ex.StackTrace}", 
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Set form properties
                this.Text = "YOLOv5 Object Detection Tool";
                this.Size = new System.Drawing.Size(1366, 768);
                this.Font = regularFont;

                // Create detection configuration group
                detectionConfigGroupBox = YoloApplicationUI.CreateGroupBox(this, "Detection Configuration", new Point(20, 20), new Size(400, 190), boldFont);
                YoloApplicationUI.InitializeDetectionConfigControls(detectionConfigGroupBox, regularFont, ref selectWeightsFileLabel, ref selectLabelsFileLabel, ref selectWeightsFileComboBox, ref selectLabelsFileComboBox, ref selectImageButton, ref selectFolderButton, ref enableGpuCheckBox, selectImageButton_Click, selectFolderButton_Click);

                // Create detection parameters group
                detectionParametersGroupBox = YoloApplicationUI.CreateGroupBox(this, "Detection Parameters", new Point(20, 220), new Size(400, 150), boldFont);
                YoloApplicationUI.InitializeDetectionParametersControls(detectionParametersGroupBox, regularFont, ref imageResolutionLabel, ref confidenceThresholdLabel, ref iouThresholdLabel, ref projectNameLabel, ref imageResolutionHorizontalTextBox, ref imageResolutionVerticalTextBox, ref confidenceThresholdTextBox, ref iouThresholdTextBox, ref projectNameTextBox);

                // Create server control buttons
                YoloApplicationUI.CreateServerControlButtons(this, ref startServerButton, ref quitServerButton, ref startDetectionButton, startServerButton_Click, quitServerButton_Click, startDetectionButton_Click);

                // Create image panel group
                imagePanelGroupBox = YoloApplicationUI.CreateGroupBox(this, "Image Panel", new Point(440, 20), new Size(890, 590), boldFont);
                YoloApplicationUI.InitializeImagePanelControls(imagePanelGroupBox, boldFont, regularFont, ref inputImageLabel, ref outputImageLabel, ref inputPictureBox, ref outputPictureBox, ref previousButton, ref nextButton, previousButton_Click, nextButton_Click);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading form: {ex.Message}", "Form Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void selectImageButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // Configure dialog settings
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Update path variable
                        selectedPath = openFileDialog.FileName;
                        
                        // Verify file exists
                        if (!File.Exists(selectedPath))
                        {
                            MessageBox.Show($"Selected file not found: {selectedPath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        isFolder = false;

                        // Initialize _imageFiles if needed
                        if (_imageFiles == null)
                        {
                            _imageFiles = new List<string>();
                        }

                        // Reset navigation
                        _imageFiles.Clear();
                        _imageFiles.Add(selectedPath);
                        _currentImageIndex = 0;

                        // Update navigation buttons
                        UpdateNavigationButtons();

                        // Load and display the selected image
                        LoadAndDisplayInputImage(selectedPath);

                        // Reset detection completed flag
                        _detectionCompleted = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error selecting image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void selectFolderButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Update path variable
                        selectedPath = folderDialog.SelectedPath;
                        
                        // Verify directory exists
                        if (!Directory.Exists(selectedPath))
                        {
                            MessageBox.Show($"Selected folder not found: {selectedPath}", "Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        isFolder = true;
                        
                        // Load all image files from the folder
                        LoadImagesFromFolder(selectedPath);
                        
                        // Initialize _imageFiles if needed
                        if (_imageFiles == null)
                        {
                            _imageFiles = new List<string>();
                        }
                        
                        // Update input image label
                        if (inputImageLabel != null)
                        {
                            inputImageLabel.Text = $"Input Folder: {Path.GetFileName(selectedPath)}";
                        }

                        // Display the first image if any
                        if (_imageFiles.Count > 0)
                        {
                            _currentImageIndex = 0;
                            LoadAndDisplayInputImage(_imageFiles[_currentImageIndex]);
                        }
                        else
                        {
                            MessageBox.Show("No image files found in the selected folder.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            
                            // Clear picture boxes if no images found
                            if (inputPictureBox != null)
                            {
                                inputPictureBox.Image = null;
                            }
                            if (outputPictureBox != null)
                            {
                                outputPictureBox.Image = null;
                            }
                        }

                        // Reset detection completed flag
                        _detectionCompleted = false;

                        // Update navigation buttons
                        UpdateNavigationButtons();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error selecting folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadImagesFromFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show($"Invalid folder path: {folderPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ensure the list is initialized
            if (_imageFiles == null)
            {
                _imageFiles = new List<string>();
            }

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
            // Ensure the buttons exist
            if (previousButton == null || nextButton == null)
            {
                return;
            }

            // Ensure _imageFiles is initialized
            if (_imageFiles == null)
            {
                _imageFiles = new List<string>();
            }

            // Update button states
            if (_imageFiles.Count > 1 && _currentImageIndex > 0) 
                previousButton.Enabled = true;
            else 
                previousButton.Enabled = false;

            if (_imageFiles.Count > 1 && _currentImageIndex < _imageFiles.Count - 1) 
                nextButton.Enabled = true;
            else 
                nextButton.Enabled = false;
        }
        
        private void previousButton_Click(object sender, EventArgs e)
        {
            // Validate _imageFiles and _currentImageIndex
            if (_imageFiles == null || _imageFiles.Count == 0 || _currentImageIndex <= 0)
            {
                return;
            }

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
        
        private void nextButton_Click(object sender, EventArgs e)
        {
            // Validate _imageFiles and _currentImageIndex
            if (_imageFiles == null || _imageFiles.Count == 0 || _currentImageIndex >= _imageFiles.Count - 1)
            {
                return;
            }

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

        private void LoadAndDisplayInputImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("Warning: Attempted to load a null or empty image path", "Debug Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (inputImage != null) // Dispose existing images if any
            {
                inputImage.Dispose();
            }

            try
            {
                // Load the new image
                inputImage = Image.FromFile(imagePath);

                // Display input image in input picture box
                if (inputPictureBox != null)
                {
                    inputPictureBox.Image = inputImage;
                    inputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    inputPictureBox.Refresh();
                }

                // If detection hasn't been completed, show input image in output box too
                if (!_detectionCompleted || outputImage == null)
                {
                    if (outputPictureBox != null && inputImage != null)
                    {
                        outputPictureBox.Image = inputImage;
                        outputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        outputPictureBox.Refresh();
                        if (outputImageLabel != null)
                        {
                            outputImageLabel.Text = "Output Image (Showing Input - Run Detection)";
                        }
                    }
                }
                
                // Update input image label with filename
                if (inputImageLabel != null)
                {
                    inputImageLabel.Text = $"Input Image: {Path.GetFileName(imagePath)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadAndDisplayOutputImage(string inputImagePath)
        {
            if (string.IsNullOrEmpty(inputImagePath))
            {
                MessageBox.Show("Warning: Attempted to load output for a null or empty image path", "Debug Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                
                // Use direct expected output path
                string outputImagePath = Path.Combine(_outputDirectory, filename);
                
                // Check if the output image exists
                if (File.Exists(outputImagePath))
                {
                    // Load the output image
                    outputImage = Image.FromFile(outputImagePath);
                    
                    // Display the output image
                    if (outputPictureBox != null)
                    {
                        outputPictureBox.Image = outputImage;
                        outputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        outputPictureBox.Refresh();
                    }
                    
                    // Update the output image label
                    if (outputImageLabel != null)
                    {
                        outputImageLabel.Text = $"Output Image: {filename}";
                    }
                }
                else
                {
                    // If output image doesn't exist, show input image instead
                    if (outputPictureBox != null && inputImage != null)
                    {
                        outputPictureBox.Image = inputImage;
                        outputPictureBox.Refresh();
                    }
                    
                    if (outputImageLabel != null)
                    {
                        outputImageLabel.Text = "Output Image Not Found";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading output image: {ex.Message}", "Output Image Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                if (outputPictureBox != null && inputImage != null)
                {
                    outputPictureBox.Image = inputImage;
                    outputPictureBox.Refresh();
                }
                
                if (outputImageLabel != null)
                {
                    outputImageLabel.Text = "Error Loading Output Image";
                }
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
                string weightsFile = selectWeightsFileComboBox.SelectedItem.ToString();
                weightsFile = Path.Combine(_detectionService.GetModelsPath(), weightsFile);
                string labelsFile = selectLabelsFileComboBox.SelectedItem.ToString();
                labelsFile = Path.Combine(_detectionService.GetModelsPath(), labelsFile);

                if (!File.Exists(weightsFile))
                {
                    MessageBox.Show($"Model file not found: {weightsFile}\nPlease ensure the model file is in the Models directory.", 
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetServerButtonState();
                    return;
                }
                
                if (!File.Exists(labelsFile))
                {
                    MessageBox.Show($"Labels file not found: {labelsFile}\nPlease ensure the labels file is in the Models directory.", 
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetServerButtonState();
                    return;
                }
                
                // Start the detection server
                string errorMessage;
                bool success = _detectionService.StartServer(
                    weightsFile,
                    labelsFile,
                    enableGpuCheckBox.Checked,
                    imageResolutionHorizontalTextBox.Text,
                    imageResolutionVerticalTextBox.Text,
                    confidenceThresholdTextBox.Text,
                    iouThresholdTextBox.Text,
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
                }
                else
                {
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
                
                // Validate _detectionService
                if (_detectionService == null)
                {
                    MessageBox.Show("Detection service not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Ensure the output directory exists
                if (string.IsNullOrEmpty(_outputDirectory) || !Directory.Exists(_outputDirectory))
                {
                    try
                    {
                        InitializeOutputDirectory();
                        if (string.IsNullOrEmpty(_outputDirectory) || !Directory.Exists(_outputDirectory))
                        {
                            MessageBox.Show("Failed to create output directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating output directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                
                // Disable the start detection button during processing
                startDetectionButton.Enabled = false;
                this.Cursor = Cursors.WaitCursor;
                
                if (isFolder)
                {
                    // Verify folder exists
                    if (!Directory.Exists(selectedPath))
                    {
                        MessageBox.Show($"Selected folder not found: {selectedPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Cursor = Cursors.Default;
                        startDetectionButton.Enabled = true;
                        return;
                    }
                    
                    // Process folder detection
                    string errorMessage = string.Empty;
                    bool success = _detectionService.DetectFolder(selectedPath, out errorMessage);
                    
                    if (success)
                    {
                        // Detection started successfully
                        _detectionCompleted = true;
                        
                        // Add a short delay to allow the Python server to process the images
                        Thread.Sleep(500); // Wait 0.5 seconds
                        
                        // Load the output image if we have valid image files
                        if (_imageFiles != null && _imageFiles.Count > 0 && _currentImageIndex >= 0 && _currentImageIndex < _imageFiles.Count)
                        {
                            LoadAndDisplayOutputImage(_imageFiles[_currentImageIndex]);
                        }
                        else
                        {
                            MessageBox.Show("No images to display after detection.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else 
                    {
                        // Detection failed
                        MessageBox.Show(errorMessage, "Folder Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // Verify file exists
                    if (!File.Exists(selectedPath))
                    {
                        MessageBox.Show($"Selected image not found: {selectedPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Cursor = Cursors.Default;
                        startDetectionButton.Enabled = true;
                        return;
                    }
                    
                    // Single image detection
                    string errorMessage = string.Empty;
                    bool success = _detectionService.DetectImage(selectedPath, out errorMessage);
                    
                    if (success)
                    {
                        // Detection started successfully
                        _detectionCompleted = true;
                        
                        // Add a short delay to allow the Python server to process the image
                        Thread.Sleep(500); // Wait 0.5 seconds
                        
                        // Load the output image if we have valid image files
                        if (_imageFiles != null && _imageFiles.Count > 0 && _currentImageIndex >= 0 && _currentImageIndex < _imageFiles.Count)
                        {
                            LoadAndDisplayOutputImage(_imageFiles[_currentImageIndex]);
                        }
                        else
                        {
                            MessageBox.Show("No image to display after detection.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else 
                    {
                        // Detection failed
                        MessageBox.Show(errorMessage, "Image Detection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception in startDetectionButton_Click: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restore cursor and button state
                this.Cursor = Cursors.Default;
                if (startDetectionButton != null)
                {
                    startDetectionButton.Enabled = true;
                    startDetectionButton.Text = "► Start Detection";
                }
            }
        }

        private void InitializeOutputDirectory()
        {
            try
            {
                // Create a timestamp for the project
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string detectionsDirectory = Path.Combine(baseDirectory, "Detections");
                
                // Create the detections directory if it doesn't exist
                if (!Directory.Exists(detectionsDirectory))
                {
                    Directory.CreateDirectory(detectionsDirectory);
                }
                
                // Create project folder with timestamp
                _outputDirectory = Path.Combine(detectionsDirectory, $"Project_{timestamp}");
                
                // Create the project directory if it doesn't exist
                if (!Directory.Exists(_outputDirectory))
                {
                    Directory.CreateDirectory(_outputDirectory);
                    MessageBox.Show($"Created output directory at: {_outputDirectory}", "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating output directory: {ex.Message}", "Directory Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Set a default output directory if there's an error
                _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detections");
            }
        }
    }
}
