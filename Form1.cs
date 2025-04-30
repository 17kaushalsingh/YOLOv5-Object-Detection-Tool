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
    /// <summary>
    /// Main form for the YOLOv5 Object Detection Tool.
    /// This class provides a graphical user interface for interacting with the YOLOv5 detection engine.
    /// It allows users to:
    /// - Select model weights and configuration files
    /// - Configure detection parameters like confidence threshold and resolution
    /// - Process single images or entire folders
    /// - View and navigate through detection results
    /// - Start and stop the detection server
    /// </summary>
    public partial class Form1 : Form
    {
        // 1. Detection Configuration Items: Weights and Labels Files, Source Image Selection
        Label selectWeightsFileLabel, selectLabelsFileLabel;
        ComboBox selectWeightsFileComboBox, selectLabelsFileComboBox;
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
        private System.Windows.Forms.Timer _serverReadyTimer; // Timer to check server readiness

        /// <summary>
        /// Initializes a new instance of the main form for YOLOv5 object detection.
        /// Sets up the Python environment, initializes the detection service,
        /// creates UI components, and registers event handlers.
        /// </summary>
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

            // Initialize server ready timer
            _serverReadyTimer = new System.Windows.Forms.Timer();
            _serverReadyTimer.Interval = 100; // Check every 100ms
            _serverReadyTimer.Tick += ServerReadyTimer_Tick;

            // Initialize the UI Components
            InitializeUI();

            // Register event handler for form load
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        /// <summary>
        /// Initializes all UI components for the form.
        /// Creates and configures the detection configuration panel, parameters panel,
        /// server control buttons, and image display panels.
        /// </summary>
        private void InitializeUI()
        {
            // Create detection configuration group
            detectionConfigGroupBox = YoloApplicationUI.CreateGroupBox(this, "Detection Configuration", new Point(20, 20), new Size(400, 150), boldFont);
            YoloApplicationUI.InitializeDetectionConfigControls(detectionConfigGroupBox, regularFont, ref selectWeightsFileLabel, ref selectLabelsFileLabel, ref selectWeightsFileComboBox, ref selectLabelsFileComboBox, ref selectImageButton, ref selectFolderButton, selectImageButton_Click, selectFolderButton_Click);

            // Create detection parameters group
            detectionParametersGroupBox = YoloApplicationUI.CreateGroupBox(this, "Detection Parameters", new Point(20, 180), new Size(400, 150), boldFont);
            YoloApplicationUI.InitializeDetectionParametersControls(detectionParametersGroupBox, regularFont, ref imageResolutionLabel, ref confidenceThresholdLabel, ref iouThresholdLabel, ref projectNameLabel, ref imageResolutionHorizontalTextBox, ref imageResolutionVerticalTextBox, ref confidenceThresholdTextBox, ref iouThresholdTextBox, ref projectNameTextBox);

            // Create server control buttons
            YoloApplicationUI.CreateServerControlButtons(this, ref startServerButton, ref quitServerButton, ref startDetectionButton, startServerButton_Click, quitServerButton_Click, startDetectionButton_Click);

            // Create image panel group
            imagePanelGroupBox = YoloApplicationUI.CreateGroupBox(this, "Image Panel", new Point(440, 20), new Size(890, 590), boldFont);
            YoloApplicationUI.InitializeImagePanelControls(imagePanelGroupBox, boldFont, regularFont, ref inputImageLabel, ref outputImageLabel, ref inputPictureBox, ref outputPictureBox, ref previousButton, ref nextButton, previousButton_Click, nextButton_Click);
        }

        /// <summary>
        /// Handles the form's Load event.
        /// Sets the form title, size, and font.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
        private void Form1_Load(object sender, EventArgs e)
        {

            // Set form properties
            this.Text = "YOLOv5 Object Detection Tool";
            this.Size = new System.Drawing.Size(1366, 768);
            this.Font = regularFont;
        }

        /// <summary>
        /// Handles the form's FormClosing event.
        /// Cleans up resources, stops the detection server if running,
        /// and ensures proper shutdown of the detection service.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Form closing event arguments</param>
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

        /// <summary>
        /// Handles the Select Image button click event.
        /// Opens a file dialog for selecting an image, loads the selected image,
        /// and updates the UI accordingly.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
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

        /// <summary>
        /// Handles the Select Folder button click event.
        /// Opens a folder browse dialog, loads all images from the selected folder,
        /// displays the first image, and sets up navigation for browsing through images.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
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

        /// <summary>
        /// Loads all supported image files from the specified folder.
        /// Adds found images to the _imageFiles list for navigation and processing.
        /// </summary>
        /// <param name="folderPath">Path to the folder containing images to load</param>
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

        /// <summary>
        /// Updates the enabled state of the navigation buttons (previous/next)
        /// based on the current image index and the total number of images.
        /// </summary>
        private void UpdateNavigationButtons()
        {
            previousButton.Enabled = _imageFiles.Count > 1 && _currentImageIndex > 0;
            nextButton.Enabled = _imageFiles.Count > 1 && _currentImageIndex < _imageFiles.Count - 1;
        }

        /// <summary>
        /// Handles the Previous button click event.
        /// Navigates to the previous image in the collection and updates both
        /// input and output displays if detection has been completed.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
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

        /// <summary>
        /// Handles the Next button click event.
        /// Navigates to the next image in the collection and updates both
        /// input and output displays if detection has been completed.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
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

        /// <summary>
        /// Loads and displays an input image in the input picture box.
        /// Also updates the output picture box with the same image if detection
        /// hasn't been run yet.
        /// </summary>
        /// <param name="imagePath">Path to the image file to load and display</param>
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

        /// <summary>
        /// Loads and displays the corresponding output image (with detections)
        /// for the given input image path.
        /// </summary>
        /// <param name="inputImagePath">Path to the original input image</param>
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

        private void ServerReadyTimer_Tick(object sender, EventArgs e)
        {
            // First check if server is still running
            if (!_detectionService.IsServerRunning)
            {
                _serverReadyTimer.Stop();
                ResetServerButtonState();
                MessageBox.Show("Server process stopped unexpectedly.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Then check if server is ready
            if (_detectionService.IsServerReady)
            {
                _serverReadyTimer.Stop();
                startDetectionButton.Enabled = true;
                startServerButton.Text = "Server Running";
            }
        }

        /// <summary>
        /// Handles the Start Server button click event.
        /// Validates model selection, starts the YOLOv5 detection server,
        /// and updates UI state accordingly.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
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
                startDetectionButton.Enabled = false; // Keep detection button disabled
                Application.DoEvents();

                // Start the detection server
                string errorMessage;
                bool success = _detectionService.StartServer(
                    selectWeightsFileComboBox.SelectedItem.ToString(),
                    selectLabelsFileComboBox.SelectedItem.ToString(),
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
                    quitServerButton.Enabled = true;

                    // Lock configuration controls
                    EnableConfigControls(false);

                    // Set the output directory for loading images
                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    _outputDirectory = Path.Combine(basePath, "Detections", projectNameTextBox.Text);

                    // Start checking for server readiness
                    _serverReadyTimer.Start();
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

        /// <summary>
        /// Resets the Start Server button to its initial state.
        /// Used when the server fails to start or encounters an error.
        /// </summary>
        private void ResetServerButtonState()
        {
            startServerButton.Enabled = true;
            startServerButton.Text = "Start Server";
            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Handles the Stop Server button click event.
        /// Stops the YOLOv5 detection server and updates UI state accordingly.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
        private void quitServerButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Show wait cursor
                this.Cursor = Cursors.WaitCursor;
                quitServerButton.Enabled = false;
                quitServerButton.Text = "Stopping...";
                startDetectionButton.Enabled = false; // Disable detection button
                _serverReadyTimer.Stop(); // Stop checking for server readiness
                Application.DoEvents();

                // Store the project name before stopping server
                string projectName = projectNameTextBox.Text;

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

                    // Unlock configuration controls
                    EnableConfigControls(true);

                    // Show results location message if detection was completed
                    if (_detectionCompleted)
                    {
                        string resultsPath = Path.Combine("Detections", projectName);
                        MessageBox.Show(
                            $"Detection results have been saved in:\n{resultsPath}\n\nResults include:\n- Detected images with bounding boxes\n- CSV file with detection details",
                            "Detection Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
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

        /// <summary>
        /// Enables or disables the configuration controls based on server state.
        /// Locks controls when the server is running to prevent changing parameters.
        /// </summary>
        /// <param name="enabled">True to enable controls, False to disable</param>
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
        }

        /// <summary>
        /// Handles the Start Detection button click event.
        /// Sends the selected image or folder for processing by the YOLOv5 detection server.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
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
