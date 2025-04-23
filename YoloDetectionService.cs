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
    public class YoloDetectionService
    {
        private readonly string _assetsPath, _scriptPath, _pythonPath;
        private Process _serverProcess;
        private bool _isServerRunning = false;
        private bool _isProcessingDetection = false;
        private string _tempDirectory = null;
        private bool _portableEnvironmentAvailable = false;

        public YoloDetectionService(string basePath)
        {
            _assetsPath = Path.Combine(basePath, "Models");
            _scriptPath = Path.Combine(basePath, "detect_trt_server.py");
            
            // Check for portable Python environment
            string portablePythonPath = Path.Combine(basePath, "yolov5_env", "python.exe");
            
            // Log the path we're checking
            MessageBox.Show($"Checking for Python at: {portablePythonPath}", "Python Path Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Check if portable Python environment exists
            if (File.Exists(portablePythonPath))
            {
                _pythonPath = portablePythonPath;
                _portableEnvironmentAvailable = true;
                MessageBox.Show("Portable Python environment found and will be used.", "Python Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // No fallback - portable environment is required
                _pythonPath = portablePythonPath; // Still set the path even though it doesn't exist
                _portableEnvironmentAvailable = false;
                MessageBox.Show("ERROR: Portable Python environment not found at: " + portablePythonPath, "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Check if the directory exists but python.exe is missing
                string envDir = Path.Combine(basePath, "yolov5_env");
                if (Directory.Exists(envDir))
                {
                    string dirContents = GetDirectoryContents(envDir);
                    MessageBox.Show($"yolov5_env directory exists but python.exe not found. Directory contents:\n\n{dirContents}", 
                        "Python Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            
            // Create a temporary directory for file operations
            _tempDirectory = Path.Combine(basePath, "Temp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public string GetAssetsPath() => _assetsPath;
        public string GetScriptPath() => _scriptPath;
        public string GetPythonPath() => _pythonPath;
        public bool IsServerRunning => _isServerRunning;
        public bool IsPortableEnvironmentAvailable => _portableEnvironmentAvailable;

        // Returns true if detect_trt_server.py and models are available
        public bool VerifyDependencies(out string errorMessage)
        {
            errorMessage = string.Empty;

            // First verify portable Python environment
            if (!_portableEnvironmentAvailable)
            {
                errorMessage = $"Portable Python environment not found at: {_pythonPath}\nPlease ensure the yolov5_env folder is included with the application.";
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
        /// Gets directory contents as a string for display in MessageBox
        /// </summary>
        private string GetDirectoryContents(string directoryPath, int maxFiles = 20)
        {
            StringBuilder sb = new StringBuilder();
            
            if (!Directory.Exists(directoryPath))
            {
                return $"Directory not found: {directoryPath}";
            }

            try
            {
                // List files in the root directory
                string[] files = Directory.GetFiles(directoryPath);
                sb.AppendLine($"Files in {directoryPath} ({Math.Min(files.Length, maxFiles)} of {files.Length} shown):");
                
                foreach (var file in files.Take(maxFiles))
                {
                    sb.AppendLine($"- {Path.GetFileName(file)}");
                }
                
                if (files.Length > maxFiles)
                {
                    sb.AppendLine($"... and {files.Length - maxFiles} more files");
                }
                
                // List subdirectories
                string[] dirs = Directory.GetDirectories(directoryPath);
                sb.AppendLine($"\nSubdirectories in {directoryPath}:");
                
                foreach (var dir in dirs)
                {
                    string dirName = Path.GetFileName(dir);
                    bool hasPython = File.Exists(Path.Combine(dir, "python.exe"));
                    sb.AppendLine($"- {dirName}" + (hasPython ? " (contains python.exe)" : ""));
                }
                
                // Check Scripts directory specifically if it exists
                string scriptsDir = Path.Combine(directoryPath, "Scripts");
                if (Directory.Exists(scriptsDir))
                {
                    sb.AppendLine($"\nContents of Scripts directory:");
                    string[] scriptFiles = Directory.GetFiles(scriptsDir).Take(maxFiles).ToArray();
                    
                    foreach (var file in scriptFiles)
                    {
                        sb.AppendLine($"- Scripts/{Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error listing directory: {ex.Message}");
            }
            
            return sb.ToString();
        }

        // Start detection server
        public bool StartServer(
            string engineFile,
            string yamlFile,
            string horizontalResolution,
            string verticalResolution,
            string confidenceThreshold,
            string iouThreshold,
            bool hideLabels,
            bool hideConfidence,
            string projectName,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (_isServerRunning)
                {
                    errorMessage = "Server is already running.";
                    return false;
                }
                
                // Verify portable environment is available
                if (!_portableEnvironmentAvailable)
                {
                    errorMessage = "Portable Python environment is required but not available.";
                    return false;
                }
                
                // Double-check that Python exists - using only verified paths
                if (!File.Exists(_pythonPath))
                {
                    errorMessage = $"Python executable not found at {_pythonPath}";
                    MessageBox.Show($"Python executable not found at: {_pythonPath}", "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                // Double-check that the script exists
                if (!File.Exists(_scriptPath))
                {
                    errorMessage = $"Detection script not found at {_scriptPath}";
                    MessageBox.Show($"Detection script not found at: {_scriptPath}", "Script Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Build the command parameters for starting the server
                string enginePath = Path.Combine(_assetsPath, engineFile);
                string labelsPath = Path.Combine(_assetsPath, yamlFile);
                
                // Normalize paths for Python
                enginePath = enginePath.Replace('\\', '/');
                labelsPath = labelsPath.Replace('\\', '/');
                
                // Create the output directory if it doesn't exist
                string detectionsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detections");
                string projectDir = Path.Combine(detectionsDir, projectName);
                
                if (!Directory.Exists(detectionsDir))
                {
                    try 
                    { 
                        Directory.CreateDirectory(detectionsDir);
                        MessageBox.Show($"Created Detections directory: {detectionsDir}", "Directory Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating Detections directory: {ex.Message}", "Directory Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                
                if (!Directory.Exists(projectDir))
                {
                    try 
                    { 
                        Directory.CreateDirectory(projectDir);
                        MessageBox.Show($"Created project directory: {projectDir}", "Directory Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating project directory: {ex.Message}", "Directory Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                
                // Create the command for the server using the portable Python executable
                string serverCommand = $"\"{_pythonPath}\" \"{_scriptPath.Replace('\\', '/')}\" --engine \"{enginePath}\" --labels \"{labelsPath}\" --input_shape 1,3,{horizontalResolution},{verticalResolution} --output_shape 1,100800,15 --conf_thresh {confidenceThreshold} --nms_thresh {iouThreshold} --output_dir \"{projectName}\"";
                
                // Add optional parameters
                if (hideLabels)
                    serverCommand += " --hide_labels";
                
                if (hideConfidence)
                    serverCommand += " --hide_conf";

                // Debug logging - log the exact command being executed
                MessageBox.Show($"Executing command:\n{serverCommand}", "Server Command", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Configure process start info to hide the window
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k {serverCommand}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true, // Hide the window
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

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
        
        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            _isServerRunning = false;
        }
        
        private void ServerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Process output data if needed
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"Server output: {e.Data}");
                
                // Check for common error indicators in output
                if (e.Data.Contains("Error:") || e.Data.Contains("Exception:") || 
                    e.Data.Contains("error:") || e.Data.Contains("Failed to"))
                {
                    // Don't show MessageBox for every server output - it would be too many dialogs
                    Console.WriteLine($"Detected error in server output: {e.Data}");
                }
                
                // Check if server is ready for commands or detection completed
                if (e.Data.Contains(">"))
                {
                    if (_isProcessingDetection)
                    {
                        Console.WriteLine("Detection processing completed.");
                        _isProcessingDetection = false; // Detections completed
                    }
                }
                
                // Look for confirmation that detection is starting
                if (e.Data.Contains("Processing") || e.Data.Contains("started"))
                {
                    Console.WriteLine("Detection process started.");
                }
            }
        }
        
        private void ServerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Process error data if needed
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"Server error: {e.Data}");
            }
        }

        // Stop the detection server
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

        // Send command to detect a single image
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
                
                // Make sure the temp directory exists
                if (!Directory.Exists(_tempDirectory))
                {
                    Directory.CreateDirectory(_tempDirectory);
                }
                
                // Copy the file to temp location preserving original filename
                File.Copy(imagePath, tempFilePath, true);
                
                // Convert to forward slashes for Python
                string simplePath = tempFilePath.Replace('\\', '/');
                
                // Send command with path containing original filename
                string command = $"--image {simplePath}";
                Console.WriteLine($"Sending command to server: {command}");
                _serverProcess.StandardInput.WriteLine(command);
                _serverProcess.StandardInput.Flush();
                
                // Add a small delay to allow command to be processed
                Thread.Sleep(1000);
                
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error sending image detection command: {ex.Message}";
                Console.WriteLine($"Exception in DetectImage: {ex}");
                return false;
            }
        }

        // Send command to detect all images in a folder
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
                    Console.WriteLine($"Sending command to server: {command}");
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
                Console.WriteLine($"Exception in DetectFolder: {ex}");
                return false;
            }
        }

        // Cleanup resources on disposal
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
                    Console.WriteLine($"Error cleaning up temp directory: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}