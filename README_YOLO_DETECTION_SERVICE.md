# YoloDetectionService.cs: Documentation

## Purpose and Overview

`YoloDetectionService` is a C# class designed to provide an interface between a C# Windows Forms application and a Python-based YOLOv5 object detection system. Its primary purpose is to:

1. Manage a portable Python environment
2. Start and stop a Python server for object detection
3. Send detection commands for images and folders
4. Handle communication between the C# application and Python scripts
5. Manage temporary files and resources

This service enables the application to run YOLOv5 neural networks for object detection without requiring users to install Python or set up dependencies manually.

## Key Components and Workflow

The service works by:
1. Detecting a portable Python environment
2. Verifying all dependencies (Python executable, models, YAML files)
3. Starting a Python process that loads YOLOv5 models
4. Sending commands to the Python process via standard input
5. Capturing output from the Python process
6. Managing the lifecycle of the detection server and temporary resources

## Detailed Method Breakdown

### Constructor

```csharp
public YoloDetectionService(string basePath)
```

**Purpose**: Initializes the service with paths to key components and creates the temporary directory.

**Parameters**:
- `basePath`: Base directory where the application is running

**Key Operations**:
- Sets paths for models, Python script, and Python executable
- Creates a temporary directory for file operations

### Properties and Getters

```csharp
public string GetAssetsPath() => _assetsPath;
public string GetScriptPath() => _scriptPath;
public string GetPythonPath() => _pythonPath;
public bool IsServerRunning => _isServerRunning;
public bool IsPortableEnvironmentAvailable => _portableEnvironmentAvailable;
```

**Purpose**: Provides access to internal state and paths.

### VerifyDependencies

```csharp
public bool VerifyDependencies(out string errorMessage)
```

**Purpose**: Confirms all required components are available before starting detection.

**Key Operations**:
- Checks Python environment exists and sets _portableEnvironmentAvailable flag
- Shows error messages if Python is missing
- Verifies models directory exists
- Confirms the detection script exists
- Checks for model files (*.pt or *.engine)
- Checks for YAML configuration files
- Returns error messages if any component is missing

### StartServer

```csharp
public bool StartServer(string engineFile, string yamlFile, string horizontalResolution, 
    string verticalResolution, string confidenceThreshold, string iouThreshold, 
    bool hideLabels, bool hideConfidence, string projectName, out string errorMessage)
```

**Purpose**: Launches the Python detection server with the specified parameters.

**Parameters**:
- `engineFile`: YOLOv5 model file to use (.pt or .engine)
- `yamlFile`: Labels/configuration file
- `horizontalResolution`/`verticalResolution`: Input image dimensions
- `confidenceThreshold`: Minimum confidence score for detections
- `iouThreshold`: Intersection over Union threshold for non-max suppression
- `hideLabels`/`hideConfidence`: Output formatting options
- `projectName`: Name for saving detection results
- `errorMessage`: Out parameter for error details

**Key Operations**:
- Verifies Python environment and dependencies
- Checks if the specific model and labels files exist
- Builds the Python command with parameters in a modular way
- Starts Python as a subprocess
- Establishes input/output pipes for communication
- Sets up event handlers for output and errors

### ServerProcess Event Handlers

```csharp
private void ServerProcess_Exited(object sender, EventArgs e)
private void ServerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
private void ServerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
```

**Purpose**: Handle events from the Python process.

**Key Operations**:
- `ServerProcess_Exited`: Updates state when server stops
- `ServerProcess_OutputDataReceived`: Tracks detection completion status
- `ServerProcess_ErrorDataReceived`: Handles error messages from the Python process

### StopServer

```csharp
public bool StopServer(out string errorMessage)
```

**Purpose**: Gracefully stops the detection server.

**Key Operations**:
- Sends "quit" command to Python process
- Waits for process to exit
- Kills process if it doesn't exit cleanly
- Cleans up event handlers and resources

### DetectImage

```csharp
public bool DetectImage(string imagePath, out string errorMessage)
```

**Purpose**: Sends command to detect objects in a single image.

**Parameters**:
- `imagePath`: Path to the image file
- `errorMessage`: Out parameter for error details

**Key Operations**:
- Verifies server is running
- Checks image file exists
- Creates a temporary copy with original filename
- Sends detection command to Python process
- Sets processing flag for tracking completion

### DetectFolder

```csharp
public bool DetectFolder(string folderPath, out string errorMessage)
```

**Purpose**: Sends command to detect objects in all images in a folder.

**Parameters**:
- `folderPath`: Path to folder containing images
- `errorMessage`: Out parameter for error details

**Key Operations**:
- Verifies server is running
- Checks folder exists
- Creates temporary folder for image copies
- Copies all images to temp folder
- Sends folder detection command to Python process

### Cleanup

```csharp
public void Cleanup()
```

**Purpose**: Releases resources when the service is no longer needed.

**Key Operations**:
- Stops server if running
- Deletes temporary directories
- Shows error messages if cleanup fails

## Member Variables

- `_assetsPath`: Path to model files directory
- `_scriptPath`: Path to Python detection script
- `_pythonPath`: Path to Python executable
- `_serverProcess`: Reference to the running Python process
- `_isServerRunning`: Flag indicating if server is active
- `_isProcessingDetection`: Flag indicating detection in progress
- `_tempDirectory`: Path for temporary file operations
- `_portableEnvironmentAvailable`: Flag indicating if Python is available

## Communication Protocol

The service communicates with the Python script through:
- Standard input: Sending commands
- Standard output: Receiving results and status
- Standard error: Capturing error messages

Commands follow this format:
- `--image <path>`: Detect objects in a single image
- `--folder <path>`: Detect objects in all images in a folder
- `quit`: Stop the server

## Error Handling

The service includes comprehensive error handling:
- Checks for required components before starting
- Validates inputs before sending commands
- Shows appropriate error messages via MessageBox
- Gracefully handles server crashes
- Implements timeouts and process monitoring

## Recent Improvements

The service has been improved with:
1. Removed redundant directory checks
2. Added clear error messages for cleanup failures
3. Improved command building with a modular approach
4. Better commented code for maintainability
5. Streamlined communication with Python process

This architecture enables reliable object detection within a Windows Forms application without requiring users to manage Python installations or dependencies manually. 