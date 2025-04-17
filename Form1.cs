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

namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    // Added new service class for handling YOLOv5 detection operations
    public class YoloDetectionService
    {
        private readonly string _yolov5BasePath;
        private readonly string _modelsPath;

        public YoloDetectionService(string basePath)
        {
            _yolov5BasePath = Path.Combine(basePath, "yolov5");
            _modelsPath = Path.Combine(basePath, "Models");
        }

        public string GetYolov5Path() => _yolov5BasePath;
        public string GetModelsPath() => _modelsPath;

        // Returns true if YOLOv5 and models are available
        public bool VerifyDependencies(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Verify YOLOv5 installation
            if (!Directory.Exists(_yolov5BasePath) || !File.Exists(Path.Combine(_yolov5BasePath, "detect.py")))
            {
                errorMessage = "YOLOv5 detect.py file not found. Please ensure YOLOv5 is properly installed.";
                return false;
            }

            // Verify model files
            bool modelsExist = Directory.GetFiles(_modelsPath, "*.pt").Length > 0 ||
                              Directory.GetFiles(_modelsPath, "*.onnx").Length > 0 ||
                              Directory.GetFiles(_modelsPath, "*.engine").Length > 0;
            if (!modelsExist)
            {
                errorMessage = "No model files found in the Models directory. Please add model files.";
                return false;
            }

            // Verify YAML files
            bool yamlExists = Directory.GetFiles(_modelsPath, "*.yaml").Length > 0;
            if (!yamlExists)
            {
                errorMessage = "No YAML data files found in the Models directory. Please add YAML files.";
                return false;
            }

            return true;
        }

        // Extract embedded resources
        public bool ExtractEmbeddedZipResource(string resourceNameSuffix, string targetDirectory, string resourceDescription, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                // Get the assembly for embedded resources
                var assembly = Assembly.GetExecutingAssembly();

                // Look for the zip resource
                string resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(rn => rn.EndsWith(resourceNameSuffix, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(resourceName))
                {
                    errorMessage = $"{resourceDescription} resource not found. Please add it manually.";
                    return false;
                }

                // Create temporary file for the zip
                string tempZipPath = Path.Combine(Path.GetTempPath(), resourceNameSuffix);

                // Extract zip to temp location
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var fileStream = new FileStream(tempZipPath, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }

                // Extract the zip contents to the target directory
                ZipFile.ExtractToDirectory(tempZipPath, targetDirectory);

                // Clean up temporary file
                File.Delete(tempZipPath);

                Console.WriteLine($"Successfully extracted {resourceDescription} from embedded resources");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error extracting {resourceDescription}: {ex.Message}";
                return false;
            }
        }

        // Build YOLOv5 detection command
        public string BuildDetectionCommand(
            string pythonPath, 
            string sourceImagePath, 
            string weightsFile, 
            string yamlFile, 
            string projectName, 
            string experimentName,
            string horizontalResolution,
            string verticalResolution,
            string confidenceThreshold,
            string iouThreshold,
            bool enableCuda,
            bool enableHalfPrecision,
            bool hideLabels,
            bool hideConfidence,
            string appBasePath)
        {
            // Build the command parameters using a List for better organization
            List<string> command = new List<string>
            {
                pythonPath,
                $"\"{Path.Combine(_yolov5BasePath, "detect.py").Replace("\\", "\\\\")}\"",
                $"--weights \"{Path.Combine(appBasePath, "Models", weightsFile).Replace("\\", "\\\\")}\"",
                $"--data \"{Path.Combine(appBasePath, "Models", yamlFile).Replace("\\", "\\\\")}\"",
                $"--project \"{Path.Combine(appBasePath, projectName).Replace("\\", "\\\\")}\"",
                $"--name {experimentName}",
                $"--imgsz {horizontalResolution} {verticalResolution}",
                $"--conf-thres {confidenceThreshold}",
                $"--iou-thres {iouThreshold}",
                $"--source \"{sourceImagePath.Replace("\\", "\\\\")}\"",
            };

            // Add hardware acceleration options if enabled
            if (enableCuda)
                command.Add("--device 0");
            else
                command.Add("--device cpu");

            if (enableHalfPrecision)
                command.Add("--half");

            // Add display options if enabled
            if (hideLabels)
                command.Add("--hide-labels");

            if (hideConfidence)
                command.Add("--hide-conf");

            // Add mandatory arguments
            command.Add("--save-txt");
            command.Add("--save-csv");
            command.Add("--save-crop");
            command.Add("--exist-ok");

            // Join all arguments into a single command string
            return string.Join(" ", command);
        }

        // Execute YOLOv5 command and return output/error
        public async Task<(bool Success, string Output, string Error)> ExecuteDetectionCommandAsync(string command, string condaEnvName, string workingDirectory)
        {
            // Create the complete command with conda environment activation
            string condaActivateCmd = $"call activate {condaEnvName}";
            string completeCommand = $"{condaActivateCmd} && {command}";

            // Configure process start info
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {completeCommand}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            // Run the detection process
            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                
                // Read output and error streams asynchronously 
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                // Wait for the process to exit
                await Task.Run(() => process.WaitForExit());
                
                // Get the output strings
                string output = await outputTask;
                string error = await errorTask;

                // Return the results as a tuple
                return (process.ExitCode == 0, output, error);
            }
        }
    }

    public partial class Form1 : Form
    {
        //----------------------------------------------------------------------
        // Detection Configuration Panel Controls
        // 1. Detection Configuration Items: Weights and YAML Files, Source Image Selection, Hardware Acceleration Settings (CUDA and Half Precision)
        // 2. Detection Parameters Items: Image Resolution and Threshold Settings (Confidence and IOU)
        // 3. Logging Configuration Items: Project Naming, Output Visualization Options (Hide Labels and Confidence)
        // 4. Image Panel Controls: Input and Output Image Display
        // 5. Common Font for Controls
        // 6. YOLOv5 Base Directory Path and Models Directory Path
        //----------------------------------------------------------------------
        Label selectWeightsFileLabel, selectYamlFileLabel, imageResolutionLabel, confidenceThresholdLabel, iouThresholdLabel, projectNameLabel, experimentNameLabel, inputImageLabel, outputImageLabel;
        ComboBox selectWeightsFileComboBox, selectYamlFileComboBox;
        Button selectImageButton, startDetectionButton;
        string selectedPath;
        CheckBox enableCudaCheckBox, enableHalfPrecisionCheckBox, hideLabelCheckBox, hideConfidenceCheckBox;
        TextBox imageResolutionHorizontalTextBox, imageResolutionVerticalTextBox, confidenceThresholdTextBox, iouThresholdTextBox, projectNameTextBox, experimentNameTextBox;
        PictureBox inputPictureBox, outputPictureBox;
        Image inputImage, outputImage;
        GroupBox imagePanelGroupBox;
        Font regularFont = new Font("Arial", 9, FontStyle.Regular), boldFont = new Font("Arial", 9, FontStyle.Bold);
        private readonly YoloDetectionService _detectionService;

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
                MessageBox.Show("Failed to set up required dependencies. The application will now close.",
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
        }

        private void InitializeUIComponents()
        {
            // Create detection configuration group
            GroupBox detectionConfigGroupBox = CreateGroupBox("Detection Configuration", new Point(20, 20), new Size(400, 190));
            InitializeDetectionConfigControls(detectionConfigGroupBox, regularFont);

            // Create detection parameters group
            GroupBox detectionParametersGroupBox = CreateGroupBox("Detection Parameters", new Point(20, 220), new Size(400, 120));
            InitializeDetectionParametersControls(detectionParametersGroupBox, regularFont);

            // Create logging configuration group 
            GroupBox loggingConfigurationGroupBox = CreateGroupBox("Logging Configuration", new Point(20, 350), new Size(400, 120));
            InitializeLoggingConfigurationControls(loggingConfigurationGroupBox, regularFont);

            // Create image panel group
            imagePanelGroupBox = CreateGroupBox("Image Panel", new Point(440, 20), new Size(890, 590));
            InitializeImagePanelControlsAndStartDetectionButton(imagePanelGroupBox, boldFont);
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
                // 1. Check for YOLOv5 directory
                if (!Directory.Exists(_detectionService.GetYolov5Path()))
                {
                    ExtractResource("yolov5.zip", AppDomain.CurrentDomain.BaseDirectory, "YOLOv5 repository");
                }

                // 2. Check for Models directory
                if (!Directory.Exists(_detectionService.GetModelsPath()))
                {
                    ExtractResource("models.zip", AppDomain.CurrentDomain.BaseDirectory, "Model files");
                }

                // 3. Verify all dependencies are available
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

        private bool ExtractResource(string resourceName, string targetDir, string description)
        {
            string errorMessage;
            if (!_detectionService.ExtractEmbeddedZipResource(resourceName, targetDir, description, out errorMessage))
            {
                MessageBox.Show(errorMessage, "Resource Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
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
                "petris_yolov5x_fp16.onnx",
                "petris_yolov5x_fp32.onnx",
                "petris_yolov5x_fp16.engine",
                "petris_yolov5x_fp32.engine"
            });
            selectWeightsFileComboBox.SelectedIndex = 4;
            parentGroup.Controls.Add(selectWeightsFileComboBox);

            // Data.yaml File Label
            selectYamlFileLabel = new Label
            {
                Text = "Select YAML File:",
                Location = new Point(20, 60),
                Size = new Size(150, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(selectYamlFileLabel);

            // Data.yaml File ComboBox
            selectYamlFileComboBox = new ComboBox
            {
                Location = new Point(180, 60),
                Size = new Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = regularFont
            };
            selectYamlFileComboBox.Items.AddRange(new object[] { "petris_data.yaml" });
            selectYamlFileComboBox.SelectedIndex = 0;
            parentGroup.Controls.Add(selectYamlFileComboBox);

            // Select Image Button
            selectImageButton = new Button
            {
                Text = "Select Image",
                Location = new Point(20, 90),
                Size = new Size(360, 30),
                BackColor = Color.LightGray,
                Font = regularFont
            };
            selectImageButton.Click += selectImageButton_Click;
            parentGroup.Controls.Add(selectImageButton);

            // Enable CUDA CheckBox
            enableCudaCheckBox = new CheckBox
            {
                Text = "Enable CUDA",
                Location = new Point(20, 130),
                Size = new Size(150, 20),
                Checked = true,
                Font = regularFont
            };
            parentGroup.Controls.Add(enableCudaCheckBox);

            // Enable Half Precision CheckBox
            enableHalfPrecisionCheckBox = new CheckBox
            {
                Text = "Enable Half Precision",
                Location = new Point(20, 160),
                Size = new Size(150, 20),
                Checked = true,
                Font = regularFont
            };
            parentGroup.Controls.Add(enableHalfPrecisionCheckBox);
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

            // Experiment Name Label
            experimentNameLabel = new Label
            {
                Text = "Experiment Name:",
                Location = new Point(20, 60),
                Size = new Size(100, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(experimentNameLabel);

            // Experiment Name TextBox
            experimentNameTextBox = new TextBox
            {
                Text = "exp",
                Location = new Point(130, 60),
                Size = new Size(250, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(experimentNameTextBox);

            // Hide Labels CheckBox
            hideLabelCheckBox = new CheckBox
            {
                Text = "Hide Labels",
                Location = new Point(20, 90),
                Size = new Size(100, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(hideLabelCheckBox);

            // Hide Confidence CheckBox
            hideConfidenceCheckBox = new CheckBox
            {
                Text = "Hide Confidence",
                Location = new Point(130, 90),
                Size = new Size(120, 20),
                Font = regularFont
            };
            parentGroup.Controls.Add(hideConfidenceCheckBox);
        }

        private void InitializeImagePanelControlsAndStartDetectionButton(GroupBox parentGroup, Font boldFont)
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
                Size = new Size(300, 20),
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

            // Create and configure the Start Detection button
            startDetectionButton = new Button
            {
                Text = "▶ Start Detection",
                Location = new Point(20, 490),
                Size = new Size(400, 40),
                Font = new Font("Arial", 18, FontStyle.Bold),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
            };
            startDetectionButton.FlatAppearance.BorderSize = 1;
            startDetectionButton.FlatAppearance.BorderColor = Color.Black;
            startDetectionButton.Click += startDetectionButton_Click;
            Controls.Add(startDetectionButton);
        }

        private void selectImageButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // Configure dialog settings
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Update path variable
                    selectedPath = openFileDialog.FileName;

                    // Load and display the selected image
                    LoadAndDisplayInputImage(selectedPath);
                }
            }
        }

        private void LoadAndDisplayInputImage(string imagePath)
        {
            if (inputImage != null) // Dispose existing images if any
            {
                inputImage.Dispose();
            }

            if (outputImage != null)
            {
                outputImage.Dispose();
                outputImage = null;
            }

            try
            {
                // Load the new image
                inputImage = Image.FromFile(imagePath);

                // Display input image in both picture boxes
                inputPictureBox.Image = inputImage;
                outputPictureBox.Image = inputImage;

                // Configure display properties
                inputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                outputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

                // Refresh displays
                inputPictureBox.Refresh();
                outputPictureBox.Refresh();

                // Update output image label
                outputImageLabel.Text = "Output Image (Showing Input - Run Detection)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Start detection flow
        private async void startDetectionButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate input selection
                if (string.IsNullOrEmpty(selectedPath))
                {
                    MessageBox.Show("Please select an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Update UI to show processing state
                UpdateUIForProcessingState(true);

                // Generate detection command
                string detectionCommand = _detectionService.BuildDetectionCommand(
                    "python",
                    selectedPath,
                    selectWeightsFileComboBox.SelectedItem.ToString(),
                    selectYamlFileComboBox.SelectedItem.ToString(),
                    projectNameTextBox.Text, 
                    experimentNameTextBox.Text,
                    imageResolutionHorizontalTextBox.Text,
                    imageResolutionVerticalTextBox.Text,
                    confidenceThresholdTextBox.Text,
                    iouThresholdTextBox.Text,
                    enableCudaCheckBox.Checked,
                    enableHalfPrecisionCheckBox.Checked,
                    hideLabelCheckBox.Checked,
                    hideConfidenceCheckBox.Checked,
                    AppDomain.CurrentDomain.BaseDirectory
                );

                // Run the detection process asynchronously
                var result = await _detectionService.ExecuteDetectionCommandAsync(
                    detectionCommand, 
                    "yolov5", 
                    AppDomain.CurrentDomain.BaseDirectory
                );

                // Check for errors
                if (!result.Success)
                {
                    throw new Exception($"Error running detection:\n{result.Error}\nOutput:\n{result.Output}");
                }

                // Process detection results
                string outputDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    projectNameTextBox.Text,
                    experimentNameTextBox.Text
                );
                
                // Display results
                DisplayDetectionResult(outputDir);
            }
            catch (Exception ex)
            {
                // Handle any errors
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Always reset UI state
                UpdateUIForProcessingState(false);
            }
        }

        private void UpdateUIForProcessingState(bool isProcessing)
        {
            if (isProcessing)
            {
                // Set UI to processing state
                startDetectionButton.Enabled = false;
                startDetectionButton.Text = "Processing...";
                startDetectionButton.BackColor = Color.Gray;
                outputImageLabel.Text = "Output Image (Processing Detection...)";
                outputImageLabel.Update();
                Application.DoEvents();
            }
            else
            {
                // Reset UI state
                startDetectionButton.Enabled = true;
                startDetectionButton.Text = "▶ Start Detection";
                startDetectionButton.BackColor = Color.LightGreen;
                
                // Only reset label if no detection was displayed
                if (outputImage == null)
                {
                    outputImageLabel.Text = "Output Image (Showing Input - Run Detection)";
                }
            }
        }

        private void DisplayDetectionResult(string outputDir)
        {
            if (Directory.Exists(outputDir))
            {
                // Get the input file name and extension
                string inputFileName = Path.GetFileNameWithoutExtension(selectedPath);
                string inputExtension = Path.GetExtension(selectedPath).ToLower();

                // Directly construct the expected output file path with the same extension
                string expectedOutputPath = Path.Combine(outputDir, inputFileName + inputExtension);

                // Check if the file exists
                if (File.Exists(expectedOutputPath))
                {
                    try
                    {
                        // Load and display the output image
                        if (outputImage != null)
                        {
                            outputImage.Dispose();
                        }

                        outputImage = Image.FromFile(expectedOutputPath);
                        outputPictureBox.Image = outputImage;
                        outputPictureBox.Refresh();

                        // Update output label
                        outputImageLabel.Text = $"Output: {Path.GetFileName(expectedOutputPath)}";

                        Console.WriteLine($"Found output image: {expectedOutputPath}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading result image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // No matching image found - show error
                    MessageBox.Show($"No matching output image found for: {Path.GetFileName(selectedPath)}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    outputImageLabel.Text = "Output Image (No matching result found)";
                }
            }
        }
    }
}
