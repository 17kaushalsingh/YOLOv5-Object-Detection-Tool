using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    public class YoloDetectionService
    {
        private readonly string _assetsPath, _scriptPath;
        private Process _serverProcess;
        private bool _isServerRunning = false;
        private bool _isProcessingDetection = false;
        private string _tempDirectory = null;

        public YoloDetectionService(string basePath)
        {
            _assetsPath = Path.Combine(basePath, "Models");
            _scriptPath = Path.Combine(basePath, "detect_trt_server.py");
            
            // Create a temporary directory for file operations
            _tempDirectory = Path.Combine(basePath, "Temp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public string GetAssetsPath() => _assetsPath;
        public string GetScriptPath() => _scriptPath;
        public bool IsServerRunning => _isServerRunning;

        // Returns true if detect_trt_server.py and models are available
        public bool VerifyDependencies(out string errorMessage)
        {
            errorMessage = string.Empty;

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

        // Start detection server
        public bool StartServer(
            string pythonPath,
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

                // Build the command parameters for starting the server
                string enginePath = Path.Combine(_assetsPath, engineFile);
                string labelsPath = Path.Combine(_assetsPath, yamlFile);
                
                // Normalize paths for Python
                enginePath = enginePath.Replace('\\', '/');
                labelsPath = labelsPath.Replace('\\', '/');
                
                // Create the complete command with conda environment activation
                string condaActivateCmd = $"call activate yolov5";
                string serverCommand = $"{pythonPath} \"{_scriptPath.Replace('\\', '/')}\" --engine \"{enginePath}\" --labels \"{labelsPath}\" --input_shape 1,3,{horizontalResolution},{verticalResolution} --output_shape 1,100800,15 --conf_thresh {confidenceThreshold} --nms_thresh {iouThreshold} --output_dir \"{projectName}\"";
                
                // Add optional parameters
                if (hideLabels)
                    serverCommand += " --hide_labels";
                
                if (hideConfidence)
                    serverCommand += " --hide_conf";
                
                string completeCommand = $"{condaActivateCmd} && {serverCommand}";

                // Debug logging - log the exact command being executed
                Console.WriteLine($"Executing command: {completeCommand}");
                
                // Configure process start info to hide the window
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k {completeCommand}",
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
                Console.WriteLine($"Exception in StartServer: {ex}");
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