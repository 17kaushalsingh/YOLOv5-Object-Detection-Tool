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
        private readonly string _assetsPath, _scriptPath;
        private string _pythonPath;
        private Process _serverProcess;
        private bool _isServerRunning = false;
        private bool _isProcessingDetection = false;
        private string _tempDirectory = null;
        private bool _portableEnvironmentAvailable = false;

        public YoloDetectionService(string basePath)
        {
            _assetsPath = Path.Combine(basePath, "Models");
            _scriptPath = Path.Combine(basePath, "detect_trt_server.py");
            
            basePath = Path.GetFullPath(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            _pythonPath = Path.Combine(basePath, "yolov5_env", "python.exe");
            
            CheckAndSetPortableEnvironment();
            
            MessageBox.Show($"Python path set to: {_pythonPath}\n" +
                           $"Python.exe exists: {File.Exists(_pythonPath)}\n" +
                           $"Base directory: {basePath}\n" +
                           $"yolov5_env directory exists: {Directory.Exists(Path.Combine(basePath, "yolov5_env"))}\n" + 
                           $"PortableEnvironmentAvailable: {_portableEnvironmentAvailable}", 
                           "Debug: YoloDetectionService Constructor");
            
            _tempDirectory = Path.Combine(basePath, "Temp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        private void CheckAndSetPortableEnvironment()
        {
            bool pythonExists = File.Exists(_pythonPath);
            
            if (!pythonExists)
            {
                string altPath1 = _pythonPath.Replace('\\', '/');
                string altPath2 = Path.GetFullPath(_pythonPath);
                
                pythonExists = File.Exists(altPath1) || File.Exists(altPath2);
                
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
            
            _portableEnvironmentAvailable = pythonExists;
        }

        public string GetAssetsPath() => _assetsPath;
        public string GetScriptPath() => _scriptPath;
        public string GetPythonPath() => _pythonPath;

        public bool IsServerRunning => _isServerRunning;
        public bool IsPortableEnvironmentAvailable => _portableEnvironmentAvailable;

        public bool VerifyDependencies(out string errorMessage)
        {
            errorMessage = string.Empty;

            MessageBox.Show($"DEBUG VerifyDependencies: Checking portable environment flag: {_portableEnvironmentAvailable}");
            
            if (!_portableEnvironmentAvailable)
            {
                errorMessage = $"Portable Python environment not found at: {_pythonPath}\nPlease ensure the yolov5_env folder is included with the application.";
                
                MessageBox.Show("ERROR: Portable Python environment not found at: " + _pythonPath, "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
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

            if (!Directory.Exists(_assetsPath))
            {
                errorMessage = "Models directory not found. Please ensure the models are included.";
                return false;
            }

            if (!File.Exists(_scriptPath))
            {
                errorMessage = "detect_trt_server.py file not found. Please ensure it is included in the project.";
                return false;
            }

            bool modelsExist = Directory.GetFiles(_assetsPath, "*.pt").Length > 0 ||
                              Directory.GetFiles(_assetsPath, "*.engine").Length > 0;
            if (!modelsExist)
            {
                errorMessage = "No model files found in the Models directory. Please add model files.";
                return false;
            }

            bool yamlExists =  Directory.GetFiles(_assetsPath, "*.yml").Length > 0 ||
                               Directory.GetFiles(_assetsPath, "*.yaml").Length > 0;
            if (!yamlExists)
            {
                errorMessage = "No YAML data files found in the Models directory. Please add YAML files.";
                return false;
            }

            return true;
        }

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
                
                if (!VerifyDependencies(out string dependencyError))
                {
                    errorMessage = dependencyError;
                    return false;
                }

                if (!File.Exists(_pythonPath))
                {
                    errorMessage = $"Python executable not found at: {_pythonPath}. Cannot start server.";
                    MessageBox.Show(errorMessage, "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                MessageBox.Show($"DEBUG StartServer: Using Python at: {_pythonPath}");

                string enginePath = Path.Combine(_assetsPath, engineFile);
                string labelsPath = Path.Combine(_assetsPath, yamlFile);
                
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
                
                string scriptPathNormalized = _scriptPath.Replace('\\', '/');
                string enginePathNormalized = enginePath.Replace('\\', '/');
                string labelsPathNormalized = labelsPath.Replace('\\', '/');
                
                MessageBox.Show($"Script: {scriptPathNormalized}\nEngine: {enginePathNormalized}\nLabels: {labelsPathNormalized}", "Normalized Paths Debug");
                
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
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                
                MessageBox.Show($"Full command: {startInfo.FileName} {startInfo.Arguments}", "Command Debug");
                
                MessageBox.Show($"Working directory: {AppDomain.CurrentDomain.BaseDirectory}", "Working Directory Debug");
                
                bool pythonTestPassed = RunPythonTest();
                if (!pythonTestPassed)
                {
                    errorMessage = "Python test failed. The environment may be misconfigured.";
                    MessageBox.Show(errorMessage, "Python Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                _serverProcess = new Process { StartInfo = startInfo };
                _serverProcess.EnableRaisingEvents = true;
                _serverProcess.Exited += ServerProcess_Exited;
                _serverProcess.OutputDataReceived += ServerProcess_OutputDataReceived;
                _serverProcess.ErrorDataReceived += ServerProcess_ErrorDataReceived;
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();
                
                Thread.Sleep(2000);
                
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

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            _isServerRunning = false;
        }

        private void ServerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                MessageBox.Show($"Server Output: {e.Data}", "Server Output Debug");
                
                if (e.Data.Contains(">"))
                {
                    if (_isProcessingDetection)
                    {
                        _isProcessingDetection = false;
                    }
                }
            }
        }

        private void ServerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                MessageBox.Show($"Server Error: {e.Data}", "Server Error Debug");
                
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

        private bool RunPythonTest()
        {
            try
            {
                
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
                    
                    MessageBox.Show($"Python Test Output:\n{output.ToString()}", "Python Test Results");
                    
                    if (!string.IsNullOrEmpty(error.ToString()))
                    {
                        MessageBox.Show($"Python Test Errors:\n{error.ToString()}", "Python Test Errors");
                        return false;
                    }
                    
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
                _isServerRunning = false;
                
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error stopping server: {ex.Message}";
                return false;
            }
        }

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

        public void Cleanup()
        {
            try
            {
                if (_isServerRunning && _serverProcess != null && !_serverProcess.HasExited)
                {
                    StopServer(out _);
                }
                
                try
                {
                    if (Directory.Exists(_tempDirectory))
                    {
                        Directory.Delete(_tempDirectory, true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to clean up temporary directory: {ex.Message}", "Cleanup Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during application cleanup: {ex.Message}", "Cleanup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}