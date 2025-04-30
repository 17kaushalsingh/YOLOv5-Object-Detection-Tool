using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Windows.Forms;

namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    /// <summary>
    /// Service class that manages communication with the Python-based YOLOv5 detection server.
    /// This class handles starting and stopping the Python server process, managing file paths,
    /// and sending detection commands for both single images and folders of images.
    /// </summary>
    public class YoloDetectionService
    {
        private string _modelsPath, _scriptPath, _pythonPath, _detectionsDirectory;
        private Process _serverProcess;
        private bool _isServerRunning = false, _isProcessingDetection = false;
        private bool _isServerReady = false;
        private string _tempDirectory = null;
        private string _currentProjectDir = null; // Store current project directory path

        /// <summary>
        /// Initializes a new instance of the YoloDetectionService with paths configured
        /// relative to the specified base path.
        /// </summary>
        /// <param name="basePath">The base directory path for the application</param>
        public YoloDetectionService(string basePath)
        {
            _modelsPath = Path.Combine(basePath, "Models");
            _modelsPath = _modelsPath.Replace('\\', '/');
            _scriptPath = Path.Combine(basePath, "server.py");
            _scriptPath = _scriptPath.Replace('\\', '/');
            _detectionsDirectory = Path.Combine(basePath, "Detections");
            _detectionsDirectory = _detectionsDirectory.Replace('\\', '/');

            basePath = Path.GetFullPath(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            _pythonPath = Path.Combine(basePath, "yolov5_env", "python.exe");

            _tempDirectory = Path.Combine(basePath, "Temp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the detection server process is running.
        /// </summary>
        public bool IsServerRunning => _isServerRunning;

        /// <summary>
        /// Gets a value indicating whether the detection server is fully initialized and ready for inference.
        /// This checks if the project directory exists, which indicates the server has finished initialization.
        /// </summary>
        public bool IsServerReady
        {
            get
            {
                if (!_isServerRunning || string.IsNullOrEmpty(_currentProjectDir))
                    return false;
                
                // Check if project directory exists, which indicates server is ready
                return Directory.Exists(_currentProjectDir);
            }
        }

        /// <summary>
        /// Validates that all necessary files exist and conditions are met to start detection.
        /// Checks for existence of model weights, labels file, and ensures output directory doesn't exist.
        /// </summary>
        /// <param name="selectWeightsFileComboBox">ComboBox containing selected weights file</param>
        /// <param name="selectLabelsFileComboBox">ComboBox containing selected labels file</param>
        /// <param name="projectName">Name of the project/folder for storing detection results</param>
        /// <returns>True if all conditions are met to start detection, False otherwise</returns>
        public bool CanStartDetection(ComboBox selectWeightsFileComboBox, ComboBox selectLabelsFileComboBox, string projectName)
        {
            // Make sure the user selected a weights file
            if (selectWeightsFileComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a weights file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Make sure the weights file exists
            string weightsFile = Path.Combine(_modelsPath, selectWeightsFileComboBox.SelectedItem.ToString());

            if (!File.Exists(weightsFile))
            {
                MessageBox.Show($"Weights file not found: {weightsFile}\nPlease ensure the weights file is in the Models directory.",
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Make sure the user selected a labels file
            if (selectLabelsFileComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a Labels file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Make sure the labels file exists
            string labelsFile = Path.Combine(_modelsPath, selectLabelsFileComboBox.SelectedItem.ToString());

            if (!File.Exists(labelsFile))
            {
                MessageBox.Show($"Labels file not found: {labelsFile}\nPlease ensure the labels file is in the Models directory.",
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Make sure the output directory does not already exist
            string outputDir = Path.Combine(_detectionsDirectory, projectName);

            if (Directory.Exists(outputDir))
            {
                MessageBox.Show($"Detection directory already exists: {outputDir}\n",
                    "Directory Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts the Python-based YOLOv5 detection server process with the specified parameters.
        /// </summary>
        /// <param name="modelFile">Name of the model weights file</param>
        /// <param name="yamlFile">Name of the YAML file containing class labels</param>
        /// <param name="horizontalResolution">Horizontal resolution for input images</param>
        /// <param name="verticalResolution">Vertical resolution for input images</param>
        /// <param name="confidenceThreshold">Confidence threshold for detections (0-1)</param>
        /// <param name="iouThreshold">IoU threshold for non-maximum suppression (0-1)</param>
        /// <param name="projectName">Name for the output directory</param>
        /// <param name="errorMessage">Output parameter for error messages if server fails to start</param>
        /// <returns>True if server started successfully, False otherwise with error in errorMessage</returns>
        public bool StartServer(string modelFile, string yamlFile, string horizontalResolution, string verticalResolution, string confidenceThreshold, string iouThreshold, string projectName, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (_isServerRunning)
                {
                    errorMessage = "Server is already running.";
                    return false;
                }

                // Set the expected project directory path
                _currentProjectDir = Path.Combine(_detectionsDirectory, projectName);
                
                // If directory already exists, delete it to avoid false readiness detection
                if (Directory.Exists(_currentProjectDir))
                {
                    try
                    {
                        Directory.Delete(_currentProjectDir, true);
                    }
                    catch (Exception ex)
                    {
                        errorMessage = $"Failed to clean up existing project directory: {ex.Message}";
                        return false;
                    }
                }

                string modelPath = Path.Combine(_modelsPath, modelFile);
                modelPath = modelPath.Replace('\\', '/');
                string labelsPath = Path.Combine(_modelsPath, yamlFile);
                labelsPath = labelsPath.Replace('\\', '/');

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"{_scriptPath} " +
                              $"--model \"{modelPath}\" " +
                              $"--labels \"{labelsPath}\" " +
                              $"--input_shape 1,3,{horizontalResolution},{verticalResolution} " +
                              $"--conf_thresh {confidenceThreshold} " +
                              $"--iou_thresh {iouThreshold} " +
                              $"--project_name \"{projectName}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                _serverProcess = new Process { StartInfo = startInfo };
                _serverProcess.EnableRaisingEvents = true;
                _serverProcess.Exited += ServerProcess_Exited;
                _serverProcess.OutputDataReceived += ServerProcess_OutputDataReceived;
                _serverProcess.ErrorDataReceived += ServerProcess_ErrorDataReceived;
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                _isServerRunning = true;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error starting server: {ex.Message}";
                _isServerRunning = false;
                _currentProjectDir = null;
                return false;
            }
        }

        /// <summary>
        /// Event handler for receiving output data from the server process.
        /// Monitors for command prompt indicators to determine when commands are complete.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Data received event arguments</param>
        private void ServerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Process server command prompt indicator - indicates server is ready for next command
                if (e.Data.Contains(">"))
                {
                    _isServerReady = true;
                    if (_isProcessingDetection)
                    {
                        _isProcessingDetection = false;
                    }
                }
                // You might want to log this for debugging
                Debug.WriteLine($"Server output: {e.Data}");
            }
        }

        private void ServerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Log error output for debugging
                Debug.WriteLine($"Server error: {e.Data}");
            }
        }

        /// <summary>
        /// Event handler for when the server process exits.
        /// Updates the server running status to false.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Event arguments</param>
        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            _isServerRunning = false;
            _currentProjectDir = null;
        }

        /// <summary>
        /// Stops the running YOLOv5 detection server process.
        /// Sends a quit command and waits for process to exit gracefully or kills it after timeout.
        /// </summary>
        /// <param name="errorMessage">Output parameter for error messages if server fails to stop</param>
        /// <returns>True if server stopped successfully, False otherwise with error in errorMessage</returns>
        public bool StopServer(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (!_isServerRunning || _serverProcess == null)
                {
                    errorMessage = "Server is not running.";
                    return false;
                }

                _isServerRunning = false;
                _currentProjectDir = null;

                _serverProcess.StandardInput.WriteLine("quit");
                _serverProcess.StandardInput.Flush();

                if (!_serverProcess.WaitForExit(5000))
                {
                    _serverProcess.Kill();
                }

                _serverProcess.OutputDataReceived -= ServerProcess_OutputDataReceived;
                _serverProcess.ErrorDataReceived -= ServerProcess_ErrorDataReceived;
                _serverProcess.Exited -= ServerProcess_Exited;

                _serverProcess.Dispose();
                _serverProcess = null;

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error stopping server: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Sends a command to the server to process a single image with object detection.
        /// Copies the image to a temporary location and sends the path to the server.
        /// </summary>
        /// <param name="imagePath">Path to the image file to process</param>
        /// <param name="errorMessage">Output parameter for error messages if detection fails</param>
        /// <returns>True if command was sent successfully, False otherwise with error in errorMessage</returns>
        public bool DetectImage(string imagePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (!_isServerRunning || _serverProcess == null)
                {
                    errorMessage = "Server is not running. Please start the server first.";
                    return false;
                }

                if (!File.Exists(imagePath))
                {
                    errorMessage = $"Image file not found: {imagePath}";
                    return false;
                }

                _isProcessingDetection = true;

                string originalFilename = Path.GetFileName(imagePath);

                string tempFilePath = Path.Combine(_tempDirectory, originalFilename);

                File.Copy(imagePath, tempFilePath, true);

                string simplePath = tempFilePath.Replace('\\', '/');

                string command = $"--image {simplePath}";
                _serverProcess.StandardInput.WriteLine(command);
                _serverProcess.StandardInput.Flush();

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error sending image detection command: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Sends a command to the server to process a folder of images with object detection.
        /// Copies all images to a temporary folder and sends the folder path to the server.
        /// </summary>
        /// <param name="folderPath">Path to the folder containing images to process</param>
        /// <param name="errorMessage">Output parameter for error messages if detection fails</param>
        /// <returns>True if command was sent successfully, False otherwise with error in errorMessage</returns>
        public bool DetectFolder(string folderPath, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (!_isServerRunning || _serverProcess == null)
                {
                    errorMessage = "Server is not running. Please start the server first.";
                    return false;
                }

                if (!Directory.Exists(folderPath))
                {
                    errorMessage = $"Folder not found: {folderPath}";
                    return false;
                }

                _isProcessingDetection = true;

                string tempFolderPath = Path.Combine(_tempDirectory, "temp_folder");

                if (Directory.Exists(tempFolderPath))
                {
                    Directory.Delete(tempFolderPath, true);
                }
                Directory.CreateDirectory(tempFolderPath);

                string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
                int filesCopied = 0;

                foreach (string extension in imageExtensions)
                {
                    foreach (string file in Directory.GetFiles(folderPath, extension))
                    {
                        string filename = Path.GetFileName(file);
                        string destFile = Path.Combine(tempFolderPath, filename);
                        File.Copy(file, destFile);
                        filesCopied++;
                    }
                }

                if (filesCopied > 0)
                {
                    string simplePath = tempFolderPath.Replace('\\', '/');

                    string command = $"--folder {simplePath}";
                    _serverProcess.StandardInput.WriteLine(command);
                    _serverProcess.StandardInput.Flush();

                    return true;
                }
                else
                {
                    errorMessage = "No image files found in the specified folder.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error sending folder detection command: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Performs cleanup of resources used by the service.
        /// Stops the server if it's running and removes temporary files.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (_isServerRunning && _serverProcess != null && !_serverProcess.HasExited)
                {
                    StopServer(out _);
                }

                _currentProjectDir = null;

                try
                {
                    if (Directory.Exists(_tempDirectory))
                    {
                        Directory.Delete(_tempDirectory, true);
                    }
                }
                catch (Exception)
                {
                    // Silently ignore temp directory cleanup issues
                }
            }
            catch (Exception)
            {
                // Silently ignore cleanup issues
            }
        }
    }
}