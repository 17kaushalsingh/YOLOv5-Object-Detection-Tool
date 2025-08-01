# YoloDetectionService.cs: Documentation

## Purpose and Overview

`YoloDetectionService` is a C# class designed to provide an interface between a C# Windows Forms application and a Python-based YOLOv5 object detection system. Its primary purpose is to:

1. Manage communication with a Python-based YOLOv5 detection server
2. Start and stop the Python server for object detection
3. Send detection commands for images and folders
4. Handle communication between the C# application and Python scripts
5. Monitor server readiness and status

This service enables the application to run YOLOv5 neural networks for object detection without requiring users to install Python or set up dependencies manually.

## Key Components and Workflow

The service works by:
1. Verifying required files exist (model weights, labels, output directories)
2. Starting a Python process that loads YOLOv5 models
3. Sending commands to the Python process via standard input
4. Capturing output from the Python process
5. Monitoring server readiness through project directory creation
6. Managing the lifecycle of the detection server

## Detailed Method Breakdown

### Constructor

```csharp
public YoloDetectionService(string basePath)
```

**Purpose**: Initializes the service with paths to key components.

**Parameters**:
- `basePath`: Base directory where the application is running

**Key Operations**:
- Sets paths for models, Python script, and Python executable
- Configures paths with forward slashes for Python compatibility
- Sets up detections directory path

### Properties

```csharp
public bool IsServerRunning => _isServerRunning;
public bool IsServerReady => _isServerReady;
```

**Purpose**: 
- `IsServerRunning`: Indicates if the server process is currently running
- `IsServerReady`: Indicates if the server is fully initialized and ready for inference

### CanStartDetection

```csharp
public bool CanStartDetection(ComboBox selectWeightsFileComboBox, ComboBox selectLabelsFileComboBox, string projectName)
```

**Purpose**: Validates that all necessary files exist and conditions are met to start detection.

**Key Operations**:
- Checks if a weights file is selected and exists
- Checks if a labels file is selected and exists
- Ensures the output directory doesn't already exist
- Returns error messages via MessageBox for any missing components

### StartServer

```csharp
public bool StartServer(string modelFile, string yamlFile, string horizontalResolution, 
    string verticalResolution, string confidenceThreshold, string iouThreshold, 
    string projectName, out string errorMessage)
```

**Purpose**: Launches the Python detection server with the specified parameters.

**Parameters**:
- `modelFile`: YOLOv5 model file to use (.pt or .engine)
- `yamlFile`: Labels/configuration file
- `horizontalResolution`/`verticalResolution`: Input image dimensions
- `confidenceThreshold`: Minimum confidence score for detections
- `iouThreshold`: Intersection over Union threshold for non-max suppression
- `projectName`: Name for saving detection results
- `errorMessage`: Out parameter for error details

**Key Operations**:
- Checks if server is already running
- Sets up project directory path
- Cleans up existing project directory if present
- Builds the Python command with parameters
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
- `ServerProcess_OutputDataReceived`: Monitors server output for command completion
- `ServerProcess_ErrorDataReceived`: Logs server errors for debugging

### StopServer

```csharp
public bool StopServer(out string errorMessage)
```

**Purpose**: Gracefully stops the detection server.

**Key Operations**:
- Sends "quit" command to Python process
- Waits for process to exit
- Kills process if it doesn't exit cleanly within timeout
- Cleans up event handlers and resources
- Resets server state variables

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
- Converts path to use forward slashes for Python
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
- Converts path to use forward slashes for Python
- Sends folder detection command to Python process
- Sets processing flag for tracking completion

### Cleanup

```csharp
public void Cleanup()
```

**Purpose**: Releases resources when the service is no longer needed.

**Key Operations**:
- Stops server if running
- Resets server state variables
- Handles cleanup issues silently to avoid unnecessary error messages

## Member Variables

- `_modelsPath`: Path to model files directory
- `_scriptPath`: Path to Python detection script
- `_pythonPath`: Path to Python executable
- `_detectionsDirectory`: Path where detection results are stored
- `_serverProcess`: Reference to the running Python process
- `_isServerRunning`: Flag indicating if server is active
- `_isProcessingDetection`: Flag indicating detection in progress
- `_isServerReady`: Flag indicating if server is ready for inference
- `_currentProjectDir`: Path to current project directory for server readiness detection

## Communication Protocol

The service communicates with the Python script through:
- Standard input: Sending commands
- Standard output: Receiving results and status
- Standard error: Receiving error messages

Commands follow this format:
- `--image <path>`: Detect objects in a single image
- `--folder <path>`: Detect objects in all images in a folder
- `quit`: Stop the server

## Error Handling

The service includes comprehensive error handling:
- Validates inputs and file existence before sending commands
- Uses out parameters to return detailed error messages
- Implements graceful process termination with timeout
- Includes try-catch blocks around all file and process operations
- Logs server errors for debugging purposes
- Handles server readiness monitoring failures

This architecture enables reliable object detection within a Windows Forms application without requiring users to manage Python installations or dependencies manually. 