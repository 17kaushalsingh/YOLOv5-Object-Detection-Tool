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
        private readonly string _modelsPath, _scriptPath;
        private string _pythonPath;
        private Process _serverProcess;
        private bool _isServerRunning = false;
        private bool _isProcessingDetection = false;
        private string _tempDirectory = null;

        public YoloDetectionService(string basePath)
        {
            _modelsPath = Path.Combine(basePath, "Models");
            _scriptPath = Path.Combine(basePath, "server.py");

            basePath = Path.GetFullPath(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            _pythonPath = Path.Combine(basePath, "yolov5_env", "python.exe");

            _tempDirectory = Path.Combine(basePath, "Temp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public string GetModelsPath() => _modelsPath;
        public bool IsServerRunning => _isServerRunning;

        public bool StartServer(string engineFile, string yamlFile, bool enableGpu, string horizontalResolution, string verticalResolution, string confidenceThreshold, string iouThreshold, string projectName, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (_isServerRunning)
                {
                    errorMessage = "Server is already running.";
                    return false;
                }

                // Check if project directory already exists
                string detectionsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detections");
                string projectDir = Path.Combine(detectionsDir, projectName);
                
                if (Directory.Exists(projectDir))
                {
                    errorMessage = $"Project '{projectName}' already exists. Please choose a different project name to ensure unique results.";
                    return false;
                }

                string enginePath = Path.Combine(_modelsPath, engineFile);
                string labelsPath = Path.Combine(_modelsPath, yamlFile);
                string scriptPathNormalized = _scriptPath.Replace('\\', '/');
                string enginePathNormalized = enginePath.Replace('\\', '/');
                string labelsPathNormalized = labelsPath.Replace('\\', '/');

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"{scriptPathNormalized} " +
                              $"--model \"{enginePathNormalized}\" " +
                              $"--labels \"{labelsPathNormalized}\" " +
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
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                // Wait for the server to initialize
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
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Process server command prompt indicator - indicates server is ready for next command
                if (e.Data.Contains(">"))
                {
                    if (_isProcessingDetection)
                    {
                        _isProcessingDetection = false;
                    }
                }
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