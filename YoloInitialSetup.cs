using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;

namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    /// <summary>
    /// Handles the initial setup process for YOLO machine learning components.
    /// This class is responsible for verifying and extracting the required dependencies 
    /// including the Python environment, YOLOv5 repository, and model files.
    /// 
    /// The setup process is designed to be self-contained, requiring no user interaction
    /// beyond accepting the extraction process. All necessary files are packaged with
    /// the application and extracted automatically when first needed.
    /// </summary>
    public class YoloInitialSetup
    {
        // Required components for the YOLO system to function:
        // 1. Python yolov5_env and Python yolov5_env/python.exe executable file
        // 2. yolov5 Repository
        // 3. Model Weights and Label Files
        // 4. Python Server Script

        /// <summary>
        /// Main entry point for the setup process. Checks if all required dependencies exist,
        /// and if not, initiates the extraction process.
        /// </summary>
        /// <returns>
        /// True if all dependencies are available (either pre-existing or successfully extracted).
        /// False if any dependency could not be verified or extracted.
        /// </returns>
        public static bool ExtractEnvironmentIfNeeded()
        {
            // Define paths to dependencies
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string envDirectoryPath = Path.Combine(basePath, "yolov5_env");
            string pythonExePath = Path.Combine(envDirectoryPath, "python.exe");
            string yolov5DirectoryPath = Path.Combine(basePath, "yolov5");
            string modelsDirectoryPath = Path.Combine(basePath, "Models");
            string pythonServerScript = Path.Combine(basePath, "server.py");

            // Ensure paths are normalized
            basePath = Path.GetFullPath(basePath);
            envDirectoryPath = Path.GetFullPath(envDirectoryPath);
            pythonExePath = Path.GetFullPath(pythonExePath);
            yolov5DirectoryPath = Path.GetFullPath(yolov5DirectoryPath);
            modelsDirectoryPath = Path.GetFullPath(modelsDirectoryPath);
            pythonServerScript = Path.GetFullPath(pythonServerScript);

            try
            {
                // Check if the dependencies already exist
                string missingDependency = "";
                bool dependenciesExist = VerifyDependencies(envDirectoryPath, pythonExePath, yolov5DirectoryPath, modelsDirectoryPath, pythonServerScript, out missingDependency);

                if (dependenciesExist)
                {
                    return true; // no need to extract dependencies
                }

                MessageBox.Show($"Missing dependency: {missingDependency}\nWill attempt to extract required files.", "Dependency Missing", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Create and show a progress form
                Form progressForm = new Form
                {
                    Text = "Extracting Dependencies",
                    Size = new Size(400, 150),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ControlBox = false
                };

                Label statusLabel = new Label
                {
                    Text = "Extracting dependencies...\nThis may take a few minutes.",
                    AutoSize = false,
                    Size = new Size(360, 50),
                    Location = new Point(20, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                ProgressBar progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    MarqueeAnimationSpeed = 30,
                    Size = new Size(360, 23),
                    Location = new Point(20, 70)
                };

                progressForm.Controls.Add(statusLabel);
                progressForm.Controls.Add(progressBar);

                // Create a background thread for extraction to keep UI responsive
                bool extractionResult = false;
                Thread extractionThread = new Thread(() =>
                {
                    try
                    {
                        // Perform the extraction process on a background thread
                        extractionResult = PerformExtraction(basePath, envDirectoryPath, yolov5DirectoryPath, statusLabel);
                        
                        // After extraction, verify all dependencies
                        string finalMissingDependency = "";
                        bool finalVerification = VerifyDependencies(envDirectoryPath, pythonExePath, yolov5DirectoryPath, modelsDirectoryPath, pythonServerScript, out finalMissingDependency);
                        
                        // If verification fails, report the specific missing dependency
                        if (!finalVerification)
                        {
                            MessageBox.Show($"Extraction completed, but some dependencies are still missing: {finalMissingDependency}",
                                "Incomplete Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            extractionResult = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Extraction error: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        extractionResult = false;
                    }
                    finally
                    {
                        // Close the progress form when done
                        progressForm.Invoke((System.Windows.Forms.MethodInvoker)delegate
                        {
                            progressForm.Close();
                        });
                    }
                });

                extractionThread.Start();
                progressForm.ShowDialog(); // This will block until the form is closed
                extractionThread.Join(); // Wait for the thread to finish

                return extractionResult;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting environment: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Verifies that all required dependencies exist and are properly structured.
        /// </summary>
        /// <param name="envDirectoryPath">Path to the Python environment directory</param>
        /// <param name="pythonExePath">Path to the Python executable</param>
        /// <param name="yolov5DirectoryPath">Path to the YOLOv5 directory</param>
        /// <param name="modelsDirectoryPath">Path to the models directory</param>
        /// <param name="pythonServerScript">Path to the Python server script</param>
        /// <param name="missingDependency">Output parameter with description of missing dependency</param>
        /// <returns>True if all dependencies exist, False otherwise</returns>
        private static bool VerifyDependencies(string envDirectoryPath, string pythonExePath, string yolov5DirectoryPath, 
            string modelsDirectoryPath, string pythonServerScript, out string missingDependency)
        {
            missingDependency = "";
            
            // Check for Python environment directory
            if (!Directory.Exists(envDirectoryPath))
            {
                missingDependency = $"Python environment directory not found at {envDirectoryPath}";
                return false;
            }
            
            // Check for Python executable
            if (!File.Exists(pythonExePath))
            {
                missingDependency = $"Python executable not found at {pythonExePath}";
                return false;
            }
            
            // Check for YOLOv5 directory
            if (!Directory.Exists(yolov5DirectoryPath))
            {
                missingDependency = $"YOLOv5 directory not found at {yolov5DirectoryPath}";
                return false;
            }
            
            // Check for Models directory
            if (!Directory.Exists(modelsDirectoryPath))
            {
                missingDependency = $"Models directory not found at {modelsDirectoryPath}";
                return false;
            }
            
            // Check for model weight files
            bool modelsExist = Directory.GetFiles(modelsDirectoryPath, "*.pt").Length > 0 ||
                              Directory.GetFiles(modelsDirectoryPath, "*.engine").Length > 0;
            if (!modelsExist)
            {
                missingDependency = $"No model weight files (*.pt or *.engine) found in {modelsDirectoryPath}";
                return false;
            }
            
            // Check for YAML configuration files
            bool yamlExists = Directory.GetFiles(modelsDirectoryPath, "*.yml").Length > 0 ||
                               Directory.GetFiles(modelsDirectoryPath, "*.yaml").Length > 0;
            if (!yamlExists)
            {
                missingDependency = $"No YAML configuration files (*.yml or *.yaml) found in {modelsDirectoryPath}";
                return false;
            }
            
            // Check for Python server script
            if (!File.Exists(pythonServerScript))
            {
                missingDependency = $"Python server script not found at {pythonServerScript}";
                return false;
            }
            
            // All dependencies verified
            return true;
        }

        /// <summary>
        /// Orchestrates the extraction process for all required components.
        /// </summary>
        /// <param name="basePath">Base application directory</param>
        /// <param name="envDirPath">Target path for the Python environment</param>
        /// <param name="yolov5DirectoryPath">Target path for the YOLOv5 directory</param>
        /// <param name="statusLabel">Label for status updates</param>
        /// <returns>True if all extractions were successful, False otherwise</returns>
        private static bool PerformExtraction(string basePath, string envDirPath, string yolov5DirectoryPath, Label statusLabel)
        {
            try
            {
                // Step 1: Extract yolov5_env
                if (!ExtractPythonEnvironment(basePath, envDirPath, statusLabel))
                {
                    MessageBox.Show("Failed to extract Python environment. Cannot continue with the setup.", 
                        "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false; // Cannot proceed without Python environment
                }
                
                // Step 2: Extract yolov5 directory
                if (!ExtractYolov5Directory(basePath, yolov5DirectoryPath, statusLabel))
                {
                    MessageBox.Show("Failed to extract YOLOv5 repository. Cannot continue with the setup.", 
                        "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false; // Cannot proceed without yolov5 directory
                }
                
                // Return true only if all extractions were successful
                return true;
            }
            catch (Exception ex)
            {
                UpdateStatus(statusLabel, $"Error during extraction: {ex.Message}");
                MessageBox.Show($"Error during extraction: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Extracts the Python environment from the tar.gz archive.
        /// </summary>
        /// <param name="basePath">Base application directory</param>
        /// <param name="envDirPath">Target path for the Python environment</param>
        /// <param name="statusLabel">Label for status updates</param>
        /// <returns>True if extraction was successful, False otherwise</returns>
        private static bool ExtractPythonEnvironment(string basePath, string envDirPath, Label statusLabel)
        {
            try
            {
                // Normalize paths
                envDirPath = Path.GetFullPath(envDirPath);
                string pythonExePath = Path.Combine(envDirPath, "python.exe");

                // First check if the environment already exists
                if (Directory.Exists(envDirPath) && File.Exists(pythonExePath))
                {
                    return true;
                }

                UpdateStatus(statusLabel, "Extracting Python environment...");

                // Look for the tar.gz file in the application directory
                string tarGzFilePath = Path.Combine(basePath, "yolov5_env.tar.gz");

                // Check if the file exists
                if (!File.Exists(tarGzFilePath))
                {
                    MessageBox.Show($"Error: yolov5_env.tar.gz not found at {tarGzFilePath}. Please make sure the file is in the application directory.",
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    // Create the target directory for extraction if it doesn't exist
                    if (!Directory.Exists(envDirPath))
                    {
                        Directory.CreateDirectory(envDirPath);
                    }

                    // Extract the tar.gz file directly into the yolov5_env folder
                    ExtractTarGzWithSharpCompress(tarGzFilePath, envDirPath, statusLabel);

                    // Verify extraction was successful - check the main Python path
                    string pythonPath = Path.Combine(envDirPath, "python.exe");
                    if (!File.Exists(pythonPath))
                    {
                        MessageBox.Show($"Error: Python executable not found at {pythonPath}. Please check the extraction process.",
                            "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during Python environment extraction: {ex.Message}",
                        "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting Python environment: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Extracts the YOLOv5 repository from the zip archive.
        /// </summary>
        /// <param name="basePath">Base application directory</param>
        /// <param name="yolov5DirectoryPath">Target path for the YOLOv5 directory</param>
        /// <param name="statusLabel">Label for status updates</param>
        /// <returns>True if extraction was successful, False otherwise</returns>
        private static bool ExtractYolov5Directory(string basePath, string yolov5DirectoryPath, Label statusLabel)
        {
            try
            {
                // Check if the yolov5 directory already exists
                if (Directory.Exists(yolov5DirectoryPath))
                {
                    return true;
                }
                
                UpdateStatus(statusLabel, "Extracting YOLOv5 files...");
                
                // Look for the zip file in the application directory
                string zipFilePath = Path.Combine(basePath, "yolov5.zip");
                
                // Check if the file exists
                if (!File.Exists(zipFilePath))
                {
                    MessageBox.Show($"Error: yolov5.zip not found at {zipFilePath}. Please make sure the file is in the application directory.",
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                try
                {
                    // Create the target directory for extraction if it doesn't exist
                    if (!Directory.Exists(yolov5DirectoryPath))
                    {
                        Directory.CreateDirectory(yolov5DirectoryPath);
                    }
                    
                    // Extract the zip file to the yolov5 folder
                    ExtractZipWithSharpCompress(zipFilePath, yolov5DirectoryPath, statusLabel);
                    
                    // Verify extraction was successful - check if directory has files
                    if (!(Directory.Exists(yolov5DirectoryPath) && Directory.GetFiles(yolov5DirectoryPath).Length > 0))
                    {
                        MessageBox.Show($"Error: yolov5 directory appears to be empty after extraction. Please check the extraction process.",
                            "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during yolov5 extraction: {ex.Message}",
                        "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting yolov5 directory: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Extracts a tar.gz archive to a specified destination path.
        /// Handles path normalization, directory creation, and error handling during extraction.
        /// </summary>
        /// <param name="tarGzFilePath">Path to the tar.gz archive</param>
        /// <param name="destinationPath">Destination directory for extracted files</param>
        /// <param name="statusLabel">Label for status updates</param>
        private static void ExtractTarGzWithSharpCompress(string tarGzFilePath, string destinationPath, Label statusLabel)
        {
            try
            {
                // Open the tar.gz file
                using (var tarGzStream = File.OpenRead(tarGzFilePath))
                {
                    // Create a reader for the archive
                    using (var reader = ReaderFactory.Open(tarGzStream, new ReaderOptions
                    {
                        ArchiveEncoding = new ArchiveEncoding { Default = System.Text.Encoding.UTF8 },
                        LeaveStreamOpen = false
                    }))
                    {
                        // No need to count entries for progress reporting
                        while (reader.MoveToNextEntry())
                        {
                            try
                            {
                                // Skip entries with null keys
                                if (string.IsNullOrEmpty(reader.Entry.Key))
                                {
                                    continue;
                                }

                                // Normalize the entry key to handle various archive formats
                                string entryKey = reader.Entry.Key.Replace('\\', '/').TrimStart('/');

                                // Handle entry path cleanup - skip any parent directory references
                                // This is a security measure to prevent path traversal attacks
                                if (entryKey.StartsWith("..") || entryKey.Contains("/../") || entryKey.Contains("/./"))
                                {
                                    continue;
                                }

                                // Get the full path for the entry
                                string entryPath = Path.Combine(destinationPath, entryKey);

                                // Handle directory entries
                                if (reader.Entry.IsDirectory)
                                {
                                    if (!Directory.Exists(entryPath))
                                    {
                                        Directory.CreateDirectory(entryPath);
                                    }
                                    continue;
                                }

                                // For file entries, create the containing directory if needed
                                string directoryPath = Path.GetDirectoryName(entryPath);
                                if (directoryPath != null && !Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }

                                // Extract the file
                                using (var entryStream = File.Create(entryPath))
                                {
                                    reader.WriteEntryTo(entryStream);
                                }

                                // Set the file's last write time if available
                                if (reader.Entry.LastModifiedTime.HasValue)
                                {
                                    File.SetLastWriteTime(entryPath, reader.Entry.LastModifiedTime.Value);
                                }
                            }
                            catch (Exception)
                            {
                                // Silently ignore extraction errors
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(statusLabel, $"Error in tar.gz extraction: {ex.Message}");
                throw; // Re-throw to be handled by caller
            }
        }
        
        /// <summary>
        /// Extracts a zip archive to a specified destination path.
        /// Handles path normalization, security checks, and error handling during extraction.
        /// </summary>
        /// <param name="zipFilePath">Path to the zip archive</param>
        /// <param name="destinationPath">Destination directory for extracted files</param>
        /// <param name="statusLabel">Label for status updates</param>
        private static void ExtractZipWithSharpCompress(string zipFilePath, string destinationPath, Label statusLabel)
        {
            try
            {
                using (var archive = ZipArchive.Open(zipFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        try
                        {
                            // Skip entries with null keys or directory entries
                            if (string.IsNullOrEmpty(entry.Key) || entry.IsDirectory)
                            {
                                continue;
                            }
                            
                            // Normalize the entry key
                            string entryKey = entry.Key.Replace('\\', '/').TrimStart('/');
                            
                            // Handle entry path cleanup - skip any parent directory references
                            // This is a security measure to prevent path traversal attacks
                            if (entryKey.StartsWith("..") || entryKey.Contains("/../") || entryKey.Contains("/./"))
                            {
                                continue;
                            }
                            
                            // Extract the entry to the destination
                            entry.WriteToDirectory(destinationPath, new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                        catch (Exception)
                        {
                            // Silently ignore extraction errors
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(statusLabel, $"Error in zip extraction: {ex.Message}");
                throw; // Re-throw to be handled by caller
            }
        }

        /// <summary>
        /// Updates the status label with a message, ensuring thread-safe UI updates.
        /// </summary>
        /// <param name="statusLabel">Label to update</param>
        /// <param name="message">Message to display</param>
        private static void UpdateStatus(Label statusLabel, string message)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    statusLabel.Text = message;
                    Application.DoEvents();
                });
            }
            else
            {
                statusLabel.Text = message;
                Application.DoEvents();
            }
        }
    }
}