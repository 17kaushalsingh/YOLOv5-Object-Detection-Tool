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
    /// Service that interfaces with a Python-based YOLOv5 object detection system.
    /// Handles starting and stopping the detection server, sending detection commands,
    /// and managing the lifecycle of the Python process.
    /// </summary>
    public class YoloDetectionService
    {
        // Path information for assets, Python script, and Python executable
        private readonly string _assetsPath, _scriptPath;
        private string _pythonPath;  // Removed readonly to allow updating the path if needed
        // Process reference to the running Python server
        private Process _serverProcess;
        // Status flags for tracking server and detection state
        private bool _isServerRunning = false;
        private bool _isProcessingDetection = false;
        // Temporary directory for file operations
        private string _tempDirectory = null;
        // Flag indicating if the portable Python environment is available
        private bool _portableEnvironmentAvailable = false;

        /// <summary>
        /// Initializes a new instance of the YoloDetectionService class.
        /// Sets up paths and creates necessary directories.
        /// </summary>
        /// <param name="basePath">Base directory path where the application is running</param>
        public YoloDetectionService(string basePath)
        {
            // Define the paths for the assets, python script, python executables, etc.
            _assetsPath = Path.Combine(basePath, "Models");
            _scriptPath = Path.Combine(basePath, "detect_trt_server.py");
            
            // Ensure proper path normalization for Windows
            basePath = Path.GetFullPath(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            _pythonPath = Path.Combine(basePath, "yolov5_env", "python.exe");
            
            // Immediately check and set _portableEnvironmentAvailable flag
            CheckAndSetPortableEnvironment();
            
            // Debug logging - display the absolute path and check if it exists
            MessageBox.Show($"Python path set to: {_pythonPath}\n" +
                           $"Python.exe exists: {File.Exists(_pythonPath)}\n" +
                           $"Base directory: {basePath}\n" +
                           $"yolov5_env directory exists: {Directory.Exists(Path.Combine(basePath, "yolov5_env"))}\n" + 
                           $"PortableEnvironmentAvailable: {_portableEnvironmentAvailable}", 
                           "Debug: YoloDetectionService Constructor");
            
            // Create a temporary directory for file operations
            _tempDirectory = Path.Combine(basePath, "Temp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        /// <summary>
        /// Check and set the _portableEnvironmentAvailable flag by checking if python.exe exists
        /// </summary>
        private void CheckAndSetPortableEnvironment()
        {
            // Try multiple approaches to check if the file exists
            bool pythonExists = File.Exists(_pythonPath);
            
            // If standard check fails, try alternate path format
            if (!pythonExists)
            {
                // Try alternative path formats
                string altPath1 = _pythonPath.Replace('\\', '/');
                string altPath2 = Path.GetFullPath(_pythonPath);
                
                pythonExists = File.Exists(altPath1) || File.Exists(altPath2);
                
                // If we confirmed existence using alternate paths, update _pythonPath
                if (pythonExists)
                {
                    if (File.Exists(altPath1))
                    {
                        _pythonPath = altPath1;
                    }
                    else if (File.Exists(altPath2))
                    {
                        _pythonPath = altPath2;
                    }
                }
            }
            
            // Set the flag based on whether we found python.exe
            _portableEnvironmentAvailable = pythonExists;
        }

        // Getters for the paths
        public string GetAssetsPath() => _assetsPath;
        public string GetScriptPath() => _scriptPath;
        public string GetPythonPath() => _pythonPath;

        // Getters for the server status
        public bool IsServerRunning => _isServerRunning;
        public bool IsPortableEnvironmentAvailable => _portableEnvironmentAvailable;

        /// <summary>
        /// Verifies that all required dependencies for YOLOv5 detection are available.
        /// </summary>
        /// <param name="errorMessage">Out parameter containing error details if verification fails</param>
        /// <returns>True if all dependencies are available, false otherwise</returns>
        public bool VerifyDependencies(out string errorMessage)
        {
            errorMessage = string.Empty;

            // First verify portable Python environment - use the flag we already set
            MessageBox.Show($"DEBUG VerifyDependencies: Checking portable environment flag: {_portableEnvironmentAvailable}");
            
            if (!_portableEnvironmentAvailable)
            {
                errorMessage = $"Portable Python environment not found at: {_pythonPath}\nPlease ensure the yolov5_env folder is included with the application.";
                
                MessageBox.Show("ERROR: Portable Python environment not found at: " + _pythonPath, "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Check if the directory exists but python.exe is missing
                string envDir = Path.GetDirectoryName(_pythonPath);
                MessageBox.Show($"DEBUG VerifyDependencies: Checking directory: {envDir}");
                MessageBox.Show($"DEBUG VerifyDependencies: Directory exists: {Directory.Exists(envDir)}");
                
                if (Directory.Exists(envDir))
                {
                    MessageBox.Show($"yolov5_env directory exists but python.exe not found.", 
                        "Python Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                return false;
            }

            // Verify Assets directory exists
            if (!Directory.Exists(_assetsPath))
            {
                errorMessage = "Models directory not found. Please ensure the models are included.";
                return false;
            }

            // Verify detect_trt_server.py script
            if (!File.Exists(_scriptPath))
            {
                errorMessage = "detect_trt_server.py file not found. Please ensure it is included in the project.";
                return false;
            }

            // Verify model files
            bool modelsExist = Directory.GetFiles(_assetsPath, "*.pt").Length > 0 ||
                              Directory.GetFiles(_assetsPath, "*.engine").Length > 0;
            if (!modelsExist)
            {
                errorMessage = "No model files found in the Models directory. Please add model files.";
                return false;
            }

            // Verify YAML files
            bool yamlExists =  Directory.GetFiles(_assetsPath, "*.yml").Length > 0 ||
                               Directory.GetFiles(_assetsPath, "*.yaml").Length > 0;
            if (!yamlExists)
            {
                errorMessage = "No YAML data files found in the Models directory. Please add YAML files.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts the YOLOv5 detection server with the specified parameters.
        /// </summary>
        /// <param name="engineFile">YOLO model file (.pt or .engine)</param>
        /// <param name="yamlFile">YAML configuration file</param>
        /// <param name="horizontalResolution">Horizontal resolution for input images</param>
        /// <param name="verticalResolution">Vertical resolution for input images</param>
        /// <param name="confidenceThreshold">Confidence threshold for detections</param>
        /// <param name="iouThreshold">IoU threshold for non-maximum suppression</param>
        /// <param name="hideLabels">Whether to hide labels in output</param>
        /// <param name="hideConfidence">Whether to hide confidence scores in output</param>
        /// <param name="projectName">Project name for saving outputs</param>
        /// <param name="errorMessage">Out parameter containing error details if server fails to start</param>
        /// <returns>True if server started successfully, false otherwise</returns>
        public bool StartServer(string engineFile, string yamlFile, string horizontalResolution, string verticalResolution, string confidenceThreshold, string iouThreshold, string projectName, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (_isServerRunning)
                {
                    errorMessage = "Server is already running.";
                    return false;
                }
                
                // Verify dependencies first - this includes Python environment check
                if (!VerifyDependencies(out string dependencyError))
                {
                    errorMessage = dependencyError;
                    return false;
                }

                // Additional check for python.exe - verify it exists one more time
                if (!File.Exists(_pythonPath))
                {
                    errorMessage = $"Python executable not found at: {_pythonPath}. Cannot start server.";
                    MessageBox.Show(errorMessage, "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                // Log the Python path being used
                MessageBox.Show($"DEBUG StartServer: Using Python at: {_pythonPath}");

                // Build the command parameters for starting the server
                string enginePath = Path.Combine(_assetsPath, engineFile);
                string labelsPath = Path.Combine(_assetsPath, yamlFile);
                
                // Check if the specific model and labels files exist
                if (!File.Exists(enginePath))
                {
                    errorMessage = $"Model file not found: {enginePath}";
                    MessageBox.Show($"Model file not found: {engineFile}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                if (!File.Exists(labelsPath))
                {
                    errorMessage = $"Labels file not found: {labelsPath}";
                    MessageBox.Show($"Labels file not found: {yamlFile}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                // Create the command for the server using the portable Python executable
                // Fix up all path separators to use forward slashes for Python
                string scriptPathNormalized = _scriptPath.Replace('\\', '/');
                string enginePathNormalized = enginePath.Replace('\\', '/');
                string labelsPathNormalized = labelsPath.Replace('\\', '/');
                
                // Show the normalized paths for debugging
                MessageBox.Show($"Script: {scriptPathNormalized}\nEngine: {enginePathNormalized}\nLabels: {labelsPathNormalized}", "Normalized Paths Debug");
                
                // Configure process start info to run Python directly instead of through cmd
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"{scriptPathNormalized} " +
                              $"--engine \"{enginePathNormalized}\" " +
                              $"--labels \"{labelsPathNormalized}\" " +
                              $"--input_shape 1,3,{horizontalResolution},{verticalResolution} " +
                              $"--output_shape 1,100800,15 " +
                              $"--conf_thresh {confidenceThreshold} " +
                              $"--nms_thresh {iouThreshold} " +
                              $"--output_dir \"{projectName}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true, // Hide the window
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                
                // Show the full command being executed for debugging
                MessageBox.Show($"Full command: {startInfo.FileName} {startInfo.Arguments}", "Command Debug");
                
                // Display the working directory
                MessageBox.Show($"Working directory: {AppDomain.CurrentDomain.BaseDirectory}", "Working Directory Debug");
                
                // First run a simple test to verify Python is working properly
                bool pythonTestPassed = RunPythonTest();
                if (!pythonTestPassed)
                {
                    errorMessage = "Python test failed. The environment may be misconfigured.";
                    MessageBox.Show(errorMessage, "Python Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Start the server process
                _serverProcess = new Process { StartInfo = startInfo };
                _serverProcess.EnableRaisingEvents = true;
                _serverProcess.Exited += ServerProcess_Exited;
                _serverProcess.OutputDataReceived += ServerProcess_OutputDataReceived;
                _serverProcess.ErrorDataReceived += ServerProcess_ErrorDataReceived;
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();
                
                // Wait a moment to allow the server to initialize
                Thread.Sleep(2000);
                
                // Check if the server created the expected directories
                string detectionsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detections");
                string projectDir = Path.Combine(detectionsDir, projectName);
                
                MessageBox.Show($"Checking for Detections directory: {detectionsDir}\nExists: {Directory.Exists(detectionsDir)}\n\nChecking for project directory: {projectDir}\nExists: {Directory.Exists(projectDir)}", "Directory Check Debug");
                
                _isServerRunning = true;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error starting server: {ex.Message}";
                MessageBox.Show($"Exception in StartServer: {ex}", "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Event handler for when the server process exits.
        /// </summary>
        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            _isServerRunning = false;
        }
        
        /// <summary>
        /// Event handler for data received from the server's standard output.
        /// Processes output data to track detection status.
        /// </summary>
        private void ServerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Process output data if needed
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Log all server output for debugging
                MessageBox.Show($"Server Output: {e.Data}", "Server Output Debug");
                
                // Check if server is ready for commands or detection completed
                if (e.Data.Contains(">"))
                {
                    if (_isProcessingDetection)
                    {
                        _isProcessingDetection = false; // Detections completed
                    }
                }
            }
        }
        
        /// <summary>
        /// Event handler for data received from the server's standard error.
        /// </summary>
        private void ServerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Log all error output for debugging
            if (!string.IsNullOrEmpty(e.Data))
            {
                MessageBox.Show($"Server Error: {e.Data}", "Server Error Debug");
                
                // Check for specific Python errors
                if (e.Data.Contains("ImportError") || e.Data.Contains("ModuleNotFoundError"))
                {
                    string errorMessage = $"Python module import error: {e.Data}";
                    MessageBox.Show(errorMessage, "Python Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Data.Contains("SyntaxError") || e.Data.Contains("IndentationError"))
                {
                    string errorMessage = $"Python syntax error: {e.Data}";
                    MessageBox.Show(errorMessage, "Python Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Data.Contains("FileNotFoundError") || e.Data.Contains("PermissionError"))
                {
                    string errorMessage = $"Python file access error: {e.Data}";
                    MessageBox.Show(errorMessage, "Python File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Data.Contains("RuntimeError") || e.Data.Contains("ValueError"))
                {
                    string errorMessage = $"Python runtime error: {e.Data}";
                    MessageBox.Show(errorMessage, "Python Runtime Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Data.Contains("Traceback") || e.Data.Contains("Exception"))
                {
                    string errorMessage = $"Python exception: {e.Data}";
                    MessageBox.Show(errorMessage, "Python Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Run a simple Python test to verify the environment is working properly.
        /// </summary>
        /// <returns>True if the test passes, false if it fails.</returns>
        private bool RunPythonTest()
        {
            try
            {
                // Create a simple Python test script to verify imports
                string testScript = @"
import sys
import os
print('Python version:', sys.version)
print('Python executable:', sys.executable)
print('Current directory:', os.getcwd())
print('PYTHONPATH:', os.environ.get('PYTHONPATH', ''))

# Try importing required modules
try:
    import numpy
    print('numpy version:', numpy.__version__)
except ImportError as e:
    print('Error importing numpy:', e)

try:
    import cv2
    print('cv2 version:', cv2.__version__)
except ImportError as e:
    print('Error importing cv2:', e)

try:
    import tensorrt
    print('tensorrt version:', tensorrt.__version__)
except ImportError as e:
    print('Error importing tensorrt:', e)

try:
    import pycuda.driver
    print('pycuda version available')
except ImportError as e:
    print('Error importing pycuda:', e)
";
                string tempTestFilePath = Path.Combine(Path.GetTempPath(), "python_test.py");
                File.WriteAllText(tempTestFilePath, testScript);
                
                // Create process to run the test
                ProcessStartInfo testStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{tempTestFilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                
                // Run the test
                using (Process testProcess = new Process { StartInfo = testStartInfo })
                {
                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();
                    
                    testProcess.OutputDataReceived += (sender, e) => {
                        if (!string.IsNullOrEmpty(e.Data))
                            output.AppendLine(e.Data);
                    };
                    
                    testProcess.ErrorDataReceived += (sender, e) => {
                        if (!string.IsNullOrEmpty(e.Data))
                            error.AppendLine(e.Data);
                    };
                    
                    testProcess.Start();
                    testProcess.BeginOutputReadLine();
                    testProcess.BeginErrorReadLine();
                    testProcess.WaitForExit();
                    
                    // Display the test results
                    MessageBox.Show($"Python Test Output:\n{output.ToString()}", "Python Test Results");
                    
                    if (!string.IsNullOrEmpty(error.ToString()))
                    {
                        MessageBox.Show($"Python Test Errors:\n{error.ToString()}", "Python Test Errors");
                        return false;
                    }
                    
                    // Clean up the temp file
                    try { File.Delete(tempTestFilePath); } catch { }
                    
                    return testProcess.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Python test failed with exception: {ex.Message}", "Python Test Exception");
                return false;
            }
        }

        /// <summary>
        /// Stops the YOLOv5 detection server if it's running.
        /// </summary>
        /// <param name="errorMessage">Out parameter containing error details if server fails to stop</param>
        /// <returns>True if server stopped successfully or wasn't running, false otherwise</returns>
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

                // Send the quit command to the server
                _serverProcess.StandardInput.WriteLine("quit");
                _serverProcess.StandardInput.Flush();
                
                // Wait a bit for the server to process the command
                if (!_serverProcess.WaitForExit(5000))
                {
                    _serverProcess.Kill();
                }
                
                // Clean up event handlers
                _serverProcess.OutputDataReceived -= ServerProcess_OutputDataReceived;
                _serverProcess.ErrorDataReceived -= ServerProcess_ErrorDataReceived;
                _serverProcess.Exited -= ServerProcess_Exited;
                
                _serverProcess.Dispose();
                _serverProcess = null;
                _isServerRunning = false;
                
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error stopping server: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Sends a command to detect objects in a single image.
        /// </summary>
        /// <param name="imagePath">Path to the image file to detect</param>
        /// <param name="errorMessage">Out parameter containing error details if detection fails</param>
        /// <returns>True if detection command was sent successfully, false otherwise</returns>
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

                // Make sure the image path exists
                if (!File.Exists(imagePath))
                {
                    errorMessage = $"Image file not found: {imagePath}";
                    return false;
                }

                // Set processing flag
                _isProcessingDetection = true;

                // Extract just the original filename (without path)
                string originalFilename = Path.GetFileName(imagePath);
                
                // Create temp file with the same filename to preserve original name
                string tempFilePath = Path.Combine(_tempDirectory, originalFilename);
                
                // Copy the file to temp location preserving original filename
                File.Copy(imagePath, tempFilePath, true);
                
                // Convert to forward slashes for Python
                string simplePath = tempFilePath.Replace('\\', '/');
                
                // Send command with path containing original filename
                string command = $"--image {simplePath}";
                _serverProcess.StandardInput.WriteLine(command);
                _serverProcess.StandardInput.Flush();
                
                // Add a small delay to allow command to be processed
                Thread.Sleep(1000);
                
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error sending image detection command: {ex.Message}";
                MessageBox.Show($"Exception in DetectImage: {ex}", "Image Detection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Sends a command to detect objects in all images in a folder.
        /// </summary>
        /// <param name="folderPath">Path to the folder containing images to detect</param>
        /// <param name="errorMessage">Out parameter containing error details if detection fails</param>
        /// <returns>True if detection command was sent successfully, false otherwise</returns>
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

                // Make sure the folder path exists
                if (!Directory.Exists(folderPath))
                {
                    errorMessage = $"Folder not found: {folderPath}";
                    return false;
                }

                // Set processing flag
                _isProcessingDetection = true;
                
                // Create a temp folder
                string tempFolderPath = Path.Combine(_tempDirectory, "temp_folder");
                
                // Clear and recreate temp folder
                if (Directory.Exists(tempFolderPath))
                {
                    Directory.Delete(tempFolderPath, true);
                }
                Directory.CreateDirectory(tempFolderPath);
                
                // Copy image files to temp folder, preserving original filenames
                string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
                int filesCopied = 0;
                
                foreach (string extension in imageExtensions)
                {
                    foreach (string file in Directory.GetFiles(folderPath, extension))
                    {
                        // Copy with original filename preserved
                        string filename = Path.GetFileName(file);
                        string destFile = Path.Combine(tempFolderPath, filename);
                        File.Copy(file, destFile);
                        filesCopied++;
                    }
                }
                
                if (filesCopied > 0)
                {
                    // Convert to forward slashes for Python
                    string simplePath = tempFolderPath.Replace('\\', '/');
                    
                    // Send command with simple path
                    string command = $"--folder {simplePath}";
                    _serverProcess.StandardInput.WriteLine(command);
                    _serverProcess.StandardInput.Flush();
                    
                    // Add a small delay to allow command to be processed
                    Thread.Sleep(1000);
                    
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
                MessageBox.Show($"Exception in DetectFolder: {ex}", "Folder Detection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Cleans up resources used by the service.
        /// Stops the server if running and deletes temporary directories.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (_isServerRunning && _serverProcess != null && !_serverProcess.HasExited)
                {
                    StopServer(out _);
                }
                
                // Clean up temp directory
                try
                {
                    if (Directory.Exists(_tempDirectory))
                    {
                        Directory.Delete(_tempDirectory, true);
                    }
                }
                catch (Exception ex)
                {
                    // Show message about temp directory cleanup failure
                    MessageBox.Show($"Failed to clean up temporary directory: {ex.Message}", "Cleanup Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // Show message about general cleanup failure
                MessageBox.Show($"Error during application cleanup: {ex.Message}", "Cleanup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}