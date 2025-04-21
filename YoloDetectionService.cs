using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Test_Software_AI_Automatic_Cleaning_Machine
{
    public class YoloDetectionService
    {
        private readonly string _assetsPath;
        private readonly string _scriptPath;

        public YoloDetectionService(string basePath)
        {
            _assetsPath = Path.Combine(basePath, "Assets");
            _scriptPath = Path.Combine(basePath, "Assets", "detect_trt_server.py");
        }

        public string GetAssetsPath() => _assetsPath;
        public string GetScriptPath() => _scriptPath;

        // Returns true if detect_trt_server.py and models are available
        public bool VerifyDependencies(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Verify Assets directory exists
            if (!Directory.Exists(_assetsPath))
            {
                errorMessage = "Assets directory not found. Please ensure the Assets.zip is extracted properly.";
                return false;
            }

            // Verify detect_trt_server.py script
            if (!File.Exists(_scriptPath))
            {
                errorMessage = "detect_trt_server.py file not found. Please ensure it is in the Assets directory.";
                return false;
            }

            // Verify model files
            bool modelsExist = Directory.GetFiles(_assetsPath, "*.pt").Length > 0 ||
                              Directory.GetFiles(_assetsPath, "*.engine").Length > 0;
            if (!modelsExist)
            {
                errorMessage = "No model files found in the Assets directory. Please add model files.";
                return false;
            }

            // Verify YAML files
            bool yamlExists = Directory.GetFiles(_assetsPath, "*.yaml").Length > 0;
            if (!yamlExists)
            {
                errorMessage = "No YAML data files found in the Assets directory. Please add YAML files.";
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
            string horizontalResolution,
            string verticalResolution,
            string confidenceThreshold,
            string iouThreshold,
            bool enableGpu,
            bool hideLabels,
            bool hideConfidence,
            string appBasePath)
        {
            // Determine the command mode based on whether a folder or single image is selected
            bool isFolder = Directory.Exists(sourceImagePath);
            string mode = isFolder ? "folder" : "image";

            // Define engine and labels paths relative to application base path
            string enginePath = Path.Combine(appBasePath, "Assets", weightsFile).Replace("\\", "\\\\");
            string labelsPath = Path.Combine(appBasePath, "Assets", yamlFile).Replace("\\", "\\\\");

            // Build the main command with required parameters
            List<string> command = new List<string>
            {
                pythonPath,
                $"\"{Path.Combine(appBasePath, "Assets", "detect_trt_server.py").Replace("\\", "\\\\")}\"",
                mode,
                $"--engine \"{enginePath}\"",
                $"--labels \"{labelsPath}\"",
                $"--input_shape 1,3,{horizontalResolution},{verticalResolution}",
                $"--output_shape 1,100800,15",
                $"--conf_thresh {confidenceThreshold}",
                $"--score_thresh {confidenceThreshold}",
                $"--nms_thresh {iouThreshold}",
                $"--output_dir \"{projectName}\"",
            };

            // Add source parameter based on mode
            if (isFolder)
            {
                command.Add($"--folder \"{sourceImagePath.Replace("\\", "\\\\")}\"");
            }
            else
            {
                command.Add($"--image \"{sourceImagePath.Replace("\\", "\\\\")}\"");
            }

            // Add display options if enabled
            if (hideLabels)
                command.Add("--hide_labels");

            if (hideConfidence)
                command.Add("--hide_conf");

            // Add device selection based on GPU flag
            if (!enableGpu)
                command.Add("--device cpu");

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
}