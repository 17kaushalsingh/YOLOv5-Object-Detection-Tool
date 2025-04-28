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

            // Initialize the UI Components
            InitializeUI();

            // Register event handler for form load
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void InitializeUI()
        {
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

        private void Form1_Load(object sender, EventArgs e)
        {

            // Set form properties
            this.Text = "YOLOv5 Object Detection Tool";
            this.Size = new System.Drawing.Size(1366, 768);
            this.Font = regularFont;
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
                // Construct the path to the output image using the guaranteed output directory
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
                if (!_detectionService.CanStartDetection(selectWeightsFileComboBox, selectLabelsFileComboBox, projectNameTextBox.Text))
                {
                    return;
                }

                // Show wait cursor
                this.Cursor = Cursors.WaitCursor;
                startServerButton.Enabled = false;
                startServerButton.Text = "Starting...";
                Application.DoEvents();

                // Start the detection server
                string errorMessage;
                bool success = _detectionService.StartServer(
                    selectWeightsFileComboBox.SelectedItem.ToString(),
                    selectLabelsFileComboBox.SelectedItem.ToString(),
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

                // Disable the start detection button during processing
                startDetectionButton.Enabled = false;

                if (isFolder)
                {
                    // Process folder detection
                    string errorMessage;
                    bool success = _detectionService.DetectFolder(selectedPath, out errorMessage);

                    if (success)
                    {
                        // Detection started successfully
                        _detectionCompleted = true;
                        
                        // Add a small delay to allow the detection server to process and save the image
                        Task.Delay(1000).ContinueWith(t =>
                        {
                            this.Invoke(new Action(() =>
                            {
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
                    }
                }
                else
                {
                    // Single image detection
                    string errorMessage;
                    bool success = _detectionService.DetectImage(selectedPath, out errorMessage);
                    
                    if (success)
                    {
                        // Detection started successfully
                        _detectionCompleted = true;
                        
                        // Add a small delay to allow the detection server to process and save the image
                        Task.Delay(1000).ContinueWith(t =>
                        {
                            this.Invoke(new Action(() =>
                            {
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
    }
}
