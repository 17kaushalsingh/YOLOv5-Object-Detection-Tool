# Test Software for AI Automatic Mixer Cleaning Machine

# Getting Started: Download Project Repo
Clone the repo
```bash
git clone https://github.com/17kaushalsingh/YOLOv5-Object-Detection-Tool
```

# UI
![alt text](Yolov5-Object-Detection-Tool-UI.png)

# 🧭 How to Bundle Python Environment with conda-pack

## Get Anaconda Dependencies
### Activate your conda environment
```sh
conda activate yolov5
```

### List packages in readable table format
```sh
conda list > requirements_conda.txt
```

### Export packages in simplified format (for rebuilding the env)
```sh
conda list --export >environment_export.txt
```

### Export exact package URLs (for exact reproduction)
```sh
conda list --explicit> environment_explicit.txt
```

### Export to a YAML file (recommended for sharing)
```sh
conda env export > environment.yml
```

## 🔹 Step-by-Step Overview to Add YOLOv5 Environment to YOLOv5-Object-Detection-Tool
1. Install and Set Up Conda Environment
2. Install Required Dependencies (YOLOv5, torch, etc.)
3. Install conda-pack
4. Pack the Environment
5. Move/Unpack the Environment on Target System
6. Modify C# App to Call Python from Packed Environment
7. Test End-to-End on a Fresh System

## ✅ Step 1: Install conda-pack and Pack Your Conda Environment
### 🧰 Install conda-pack (if not already installed)

Activate your yolov5 environment first:

```
conda activate yolov5
```

Then install conda-pack:

```
conda install -c conda-forge conda-pack
```
If you want to install it globally (outside env), you can run:
```
conda install -n base -c conda-forge conda-pack
```

### 📦 Pack the Environment
Once installed, run the following command outside the activated env (from base or cmd):
```
conda-pack -n yolov5 -o yolov5_env.tar.gz
```
-n yolov5: name of your conda environment
-o yolov5_env.tar.gz: output tarball containing the full packed environment

### 📁 Unpack the Environment (for testing or deployment)
Pick any folder to extract it:
```
mkdir yolov5_env
tar -xzf yolov5_env.tar.gz -C yolov5_env
```
You’ll now have a fully self-contained Python environment in yolov5_env/.

### ⚠️ Fix Activation Scripts (Change hardcoded absolute locations to relative locations)
To make it portable, run this inside the unpacked folder:
```
./yolov5_env/bin/conda-unpack      # On Linux/macOS
yolov5_env/Scripts/conda-unpack.exe  # On Windows
```
This updates hardcoded paths inside the environment to match the new location.

### ✅ Extract yolov5_env in Application Directory on First Use or When Dependencies are not available
[YoloInitialSetup.cs](YoloInitialSetup.cs/)

## Testing The App: Download YOLOv5x Models for PETRIS to Models Directory

### Option 1: Manual Download

YOLOv5x Models for Petris <br>
NOTE: If unable to download, please request access at kaushal.singh@nikko-net.co.jp

1. [petris_yolov5x.pt](https://drive.google.com/uc?id=199GTyTxzaTxSp6QhKvgdIbYYgs9LpD7t)
2. [petris_yolov5x_fp16.onnx](https://drive.google.com/uc?id=1qx567W8z4xtbN3X9JhmRZiMmEBLb7f0T)
3. [petris_yolov5x_fp32.onnx](https://drive.google.com/uc?id=1PsMTw9vmfGM7j5LLFFiZadvA7_rBWhN0)
4. [petris_yolov5x_fp16.engine](https://drive.google.com/uc?id=1KNQwq29hXc4nyMnTsmccHBOGq4hsTdfO)
5. [petris_yolov5x_fp32.engine](https://drive.google.com/uc?id=1CIql-aBZStBnAjMO6jrzQnri_WhoeTS6)
6. [petris_data.yaml](https://drive.google.com/uc?id=1580AgoYQuoL2BKhLW0fB8fQAibn9YffK)

### Option 2: Download using gdrive(gdown) API

Run this command to download the models
```bash
pip install gdown
cd Models && python download_models.py && cd ..
```

# Create .exe app

## 1. Add Required Files as Embedded Resources
- Add dependency files (YOLOv5 repository and models) as embedded resources to the C# solution explorer
- Add code to extract dependencies during first app run or if dependencies are missing
- Include all necessary YAML configuration files

## 2. Switch to Release Build
- In Visual Studio, go to the toolbar (near the green "Start" button)
- Change the build configuration from Debug → Release
- This ensures all debug info is removed and the app is optimized for performance

## 3. Build the Project
- Press Ctrl + Shift + B or go to Build → Build Solution
- The compiled .exe will be in: `bin\Release\`

## 4. Error Handling and Logging
- Add try-catch blocks and show user-friendly messages
- Log errors to a text file using System.IO
- Test all features to ensure proper exception handling

## 5. Create an Installer (Recommended for Distribution)
Use one of the following methods:

### Option 1: ClickOnce (Built-in to Visual Studio)
- Right-click the project → Publish
- Choose Folder as the target
- This creates a simple installer with automatic updates capability

### Option 2: Custom Installer
Use free tools like:
1. [Inno Setup](https://jrsoftware.org/isinfo.php)
2. [NSIS (Nullsoft Scriptable Install System)](https://nsis.sourceforge.io/Main_Page)
- Bundle your .exe, assets, and all dependencies
- Customize the installer GUI for better user experience

### Option 3: MSIX Packaging
- Great for deploying to Windows Store or enterprise environments
- Follow the [Microsoft MSIX Packaging Guide](https://docs.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps)

## 6. Test on Another Machine
Try running the application on a different PC that doesn't have Visual Studio installed.

Ensure:
- All DLLs and external files are bundled correctly
- .NET Desktop Runtime is installed ([Download .NET Runtime](https://dotnet.microsoft.com/download/dotnet))
- Conda environment can be created with necessary Python dependencies
- YOLOv5 and models extract correctly

## 7. Code Signing (Recommended)
- Purchase a code-signing certificate from a trusted certificate authority
- Sign your executable and installer to prevent "unknown publisher" warnings
- This increases user trust and bypasses Windows SmartScreen warnings


# File Specific README

<details>
<summary>DETECT_TRT_SERVER.PY</summary>

# YOLOv5 TensorRT Inference Server

This document provides instructions on how to use the `detect_trt_server.py` file for object detection using YOLOv5 with TensorRT acceleration.

## Overview

The `detect_trt_server.py` script is a high-performance object detection server that:

- Loads a YOLOv5 TensorRT engine once and keeps it in memory
- Performs inference on single images or entire folders
- Provides detailed performance metrics and detection results
- Outputs annotated images with bounding boxes and confidence scores
- Generates CSV files with detection data for further analysis 
- Stores bounding box coordinates in both pixel and normalized YOLO formats
- Offers options to customize visualization (show/hide labels and confidence values)

This approach significantly improves performance compared to loading the model for each inference request.

## Prerequisites

- Python 3.6+
- CUDA and cuDNN (compatible with your TensorRT version)
- TensorRT installation
- PyCUDA
- OpenCV
- NumPy
- PyYAML

You can install the Python dependencies with:

```bash
pip install numpy opencv-python pycuda pyyaml
```

TensorRT must be installed separately according to your GPU and CUDA version.

## Usage

The script initializes a server with a TensorRT engine and accepts commands for processing images:

```bash
python detect_trt_server.py \
  --engine Models/petris_yolov5x_fp32.engine \
  --labels Models/petris_data.yaml \
  --input_shape 1,3,1280,1280 \
  --output_shape 1,100800,15 \
  --conf_thresh 0.25 \
  --nms_thresh 0.45 \
  --output_dir results
```

### Command-line Arguments

- `--engine`: Path to TensorRT engine file (required)
- `--labels`: Path to YAML file containing class labels (required)
- `--input_shape`: Input shape as comma-separated values (default: 1,3,1280,1280)
- `--output_shape`: Output shape as comma-separated values (default: 1,100800,15)
- `--conf_thresh`: Confidence threshold (default: 0.25)
- `--nms_thresh`: NMS threshold (default: 0.45)
- `--hide_labels`: Hide class labels on detections (default shows labels)
- `--hide_conf`: Hide confidence values on detections (default shows confidence)
- `--output_dir`: Specify output subdirectory name (default uses timestamp-based directory)
- `--port`: Set port for communication (default is 5000, not currently used)

### Using the Server Interactively

Once the server is running, you can use the following commands:

1. Process a single image:
   ```
   --image Test_Images/Image_250325_162056.jpg
   ```

2. Process all images in a folder:
   ```
   --folder Test_Images/
   ```

3. Quit the server:
   ```
   quit
   ```
   
You can also use `exit` or `q` to exit the server.

## Output Structure

All output files are saved in the following structure:

```
Detections/                  # Main output directory
└── your_output_dir/         # Your specified output directory or timestamp-based folder
    ├── image1.jpg           # Processed images with bounding boxes
    ├── image2.jpg
    └── detections.csv       # CSV file with all detections
```

> **Note:** To prevent overwriting existing results, if the specified output directory already exists, the script will automatically append a timestamp to the directory name (e.g., `output_dir_20230815_123045`).

The CSV file contains the following columns:
- `image_file`: Name of the processed image file
- `class`: Detected object class
- `confidence`: Detection confidence score
- `x`, `y`: Top-left coordinates of bounding box (in pixels)
- `width`, `height`: Dimensions of bounding box (in pixels)
- `center_x_norm`, `center_y_norm`: Normalized center coordinates (YOLO format, 0-1)
- `width_norm`, `height_norm`: Normalized width and height (YOLO format, 0-1)

## Bounding Box Formats

The detection results include two formats for bounding boxes:

1. **Pixel Coordinates** (bbox): 
   - `[x, y, width, height]`
   - `x, y`: Pixel coordinates of the top-left corner
   - `width, height`: Width and height in pixels

2. **YOLO Normalized Coordinates** (yolo_bbox):
   - `[center_x, center_y, width, height]`
   - All values normalized to range [0-1]
   - `center_x, center_y`: Center point of the bounding box
   - `width, height`: Width and height relative to image dimensions

These dual formats make it easier to use the results with different systems and applications.

## Examples

### Example 1: Starting the server

```bash
python detect_trt_server.py \
  --engine Models/petris_yolov5x_fp32.engine \
  --labels Models/petris_data.yaml \
  --conf_thresh 0.3 \
  --output_dir detection_results
```

### Example 2: Interactive server commands

```
# After starting the server
> --image Test_Images/cat.jpg
> --folder Test_Images
Processing 5/5 images
> quit
```

## Understanding the Output

### Image Output

For each processed image, the script:

1. Draws bounding boxes around detected objects
2. Optionally labels each detection with class name and/or confidence score (can be turned off)
3. Saves the annotated image to the specified output directory

### CSV Output

A CSV file is generated with all detection results, which can be:
- Loaded into spreadsheet applications for analysis
- Used for further processing or integration with other systems
- Useful for tracking detections across multiple images

## Troubleshooting

### Common Issues

1. **CUDA out of memory errors**: Try using a smaller model
2. **TensorRT engine compatibility**: Ensure the engine was built with the same TensorRT version you're using for inference
3. **NaN outputs**: Check your model for proper normalization and preprocessing

### Debug Tips

- Set lower confidence thresholds to see more detections
- Check the processed images for detection quality
- Verify that your input and output shapes match the model's requirements

## Advanced Usage

### Custom Thresholds

Adjust detection sensitivity with threshold parameters:

- `--conf_thresh`: Detection confidence threshold (0.0-1.0)
- `--nms_thresh`: Non-maximum suppression threshold (0.0-1.0)

Lower values increase detection quantity but may include more false positives.

### Visualization Options

Control what is displayed on the output images:

- Use `--hide_labels` to show only bounding boxes without class names
- Use `--hide_conf` to hide confidence scores
- Use both flags to show only bounding boxes with no text

### Keyboard Shortcuts

- Use `q`, `quit`, or `exit` to stop the server
- Press Ctrl+C to exit the program immediately

## Performance Notes

- The first inference may be slower due to CUDA initialization
- Processing a folder of images is more efficient than individual images due to reduced overhead
- All inference is done in FP32 precision for maximum accuracy 

</details>

<details>
<summary>SERVER.PY</summary>

# YOLOv5 Inference Server

## Overview

The YOLOv5 Inference Server is a Python-based tool that loads a YOLOv5 model and provides an interactive command-line interface for running object detection on images. It allows users to process individual images or entire folders of images with a single command and consolidates all detection results into a CSV file.

## Features

- **Interactive CLI**: Simple command-line interface for processing images
- **Efficient Processing**: Loads the YOLOv5 model once and keeps it in memory
- **Batch Processing**: Process entire folders of images with a single command
- **CSV Output**: Generates a consolidated CSV file with detection results in customized column order
- **GPU Support**: Optional GPU acceleration for faster inference
- **Proper Resource Management**: Ensures CUDA resources are properly cleaned up

## Requirements

- Python 3.6+
- PyTorch 1.7+
- YOLOv5 requirements (already included in the YOLOv5 repository)
- CUDA-compatible GPU (optional, for GPU acceleration)

## Installation

1. Clone the YOLOv5 repository:
   ```bash
   git clone https://github.com/ultralytics/yolov5.git
   cd yolov5
   pip install -r requirements.txt
   ```

2. Place the `server.py` file in the YOLOv5 directory or ensure the YOLOv5 repository is in your Python path.

## Usage

### Command-line Arguments

```bash
python server.py --model [MODEL_PATH] --labels [LABELS_PATH] [OPTIONS]
```

Required arguments:
- `--model`: Path to the YOLOv5 model weights (.pt file)
- `--labels`: Path to the YAML file containing class labels

Optional arguments:
- `--enable_gpu`: Enable GPU acceleration if available (default: False)
- `--input_shape`: Model input shape as comma-separated values (default: 1,3,1280,1280)
- `--conf_thresh`: Confidence threshold for detections (default: 0.25)
- `--iou_thresh`: IoU threshold for NMS (default: 0.45)
- `--project_name`: Output directory name (default: timestamp-based name)

### Server Commands

Once the server is running, the following commands are available:

- `--image [PATH]`: Process a single image
- `--folder [PATH]`: Process all images in a folder
- `quit`, `exit`, or `q`: Exit the server

### Examples

#### Start the server with a custom model:

```bash
python server.py --model weights/custom_model.pt --labels data/custom.yaml --enable_gpu
```

#### Process a single image:

```
> --image path/to/image.jpg
```

#### Process a folder of images:

```
> --folder path/to/images/
```

#### Exit the server:

```
> quit
```

## Output Directory Structure

```
Detections/
└── project_name/ (or results_YYYYMMDD_HHMMSS/)
    ├── [labeled images with detection boxes]
    └── results.csv
```

## CSV Output Format

The CSV output file contains the following columns in order:

1. `image`: Filename of the processed image
2. `name`: Class name of the detected object
3. `class`: Class ID of the detected object
4. `confidence`: Detection confidence score (0-1)
5. `xmin`: Left coordinate of the bounding box
6. `ymin`: Top coordinate of the bounding box
7. `xmax`: Right coordinate of the bounding box
8. `ymax`: Bottom coordinate of the bounding box

## Code Structure

The codebase consists of two main classes:

### YOLOv5Inference

This class handles the low-level operations of model loading, inference, and result processing. It includes methods for:

- Loading and configuring the YOLOv5 model
- Running inference on individual images
- Processing folders of images
- Saving labeled images with detection boxes
- Consolidating detection results and saving to CSV

### YOLOv5Server

This class provides the command-line interface and user interaction. It includes:

- The main command processing loop
- Signal handling for graceful shutdown
- Methods for processing user commands
- Resource cleanup on exit

## Advanced Usage

### Custom Detection Threshold

You can adjust the confidence threshold for detections:

```bash
python server.py --model weights/yolov5s.pt --labels data/coco128.yaml --conf_thresh 0.5
```

### Custom Input Resolution

For higher accuracy or faster inference, adjust the input resolution:

```bash
python server.py --model weights/yolov5s.pt --labels data/coco128.yaml --input_shape 1,3,640,640
```

### Custom Output Directory

Specify a custom output directory name:

```bash
python server.py --model weights/yolov5s.pt --labels data/coco128.yaml --project_name my_detections
```

## Troubleshooting

### GPU Memory Issues

If you encounter GPU memory errors, try:
1. Reducing the batch size (first value in `--input_shape`)
2. Reducing the input resolution
3. Using a smaller YOLOv5 model (e.g., YOLOv5n instead of YOLOv5x)

### Missing Labels

If detected objects don't have proper class names, verify that:
1. The correct labels file is specified
2. The labels file has the correct format (YAML with a 'names' list)

## Contributing

Contributions to improve the server are welcome. Please ensure any changes maintain backward compatibility with existing functionality.

## License

This project is distributed under the same license as YOLOv5. 

</details>

<details>
<summary>FORM1.CS</summary>

</details>

<details>
<summary>YoloAplicationUi.CS</summary>

# YOLO Application UI Module Documentation

## Overview

The `YoloApplicationUI` class provides a comprehensive UI component creation and management system for the YOLO Object Detection application. It encapsulates the creation, configuration, and organization of UI elements into logical groups, making the main application code cleaner and more maintainable.

This utility class follows a modular design pattern where different aspects of the UI are handled by separate methods, each responsible for initializing a specific group of related UI components. The components created include group boxes, labels, combo boxes, text boxes, buttons, checkboxes, and picture boxes for image display.

## Key Components

### Main Methods

| Method | Purpose |
|--------|---------|
| `CreateGroupBox()` | Creates and configures a GroupBox with specified properties |
| `InitializeDetectionConfigControls()` | Creates controls for model selection and input source configuration |
| `InitializeDetectionParametersControls()` | Creates controls for setting detection parameters (resolution, thresholds) |
| `CreateServerControlButtons()` | Creates buttons for server control and detection initiation |
| `InitializeImagePanelControls()` | Creates controls for displaying and navigating through images |

### UI Component Groups

The UI is organized into several logical component groups:

1. **Detection Configuration** - Selection of model weights, labels file, input source, and GPU settings
2. **Detection Parameters** - Configuration of image resolution, confidence threshold, IOU threshold, and project name
3. **Server Controls** - Buttons for starting/stopping the detection server and initiating detection
4. **Image Display Panel** - Display areas for input and output images with navigation controls

## Detailed Method Descriptions

### CreateGroupBox()

Creates a standard GroupBox control with consistent styling that serves as a container for related UI components.

```csharp
public static GroupBox CreateGroupBox(Form parent, string text, Point location, Size size, Font boldFont)
```

**Parameters:**
- `parent` - The parent form where the GroupBox will be added
- `text` - The text to display in the GroupBox header
- `location` - The location (coordinates) where the GroupBox should be positioned
- `size` - The size (width and height) of the GroupBox
- `boldFont` - The font to use for the GroupBox title

**Returns:** A configured GroupBox control that has been added to the parent form

### InitializeDetectionConfigControls()

Creates and configures controls related to detection configuration within a specified parent GroupBox.

```csharp
public static void InitializeDetectionConfigControls(GroupBox parentGroup, Font regularFont, 
    ref Label selectWeightsFileLabel, ref Label selectLabelsFileLabel, 
    ref ComboBox selectWeightsFileComboBox, ref ComboBox selectLabelsFileComboBox, 
    ref Button selectImageButton, ref Button selectFolderButton, 
    ref CheckBox enableGpuCheckBox, EventHandler selectImageButtonClickHandler, 
    EventHandler selectFolderButtonClickHandler)
```

**Controls Created:**
- Labels for weights and labels file selection
- ComboBoxes for selecting model weights and labels files (pre-populated with options)
- Buttons for selecting image or folder input
- Checkbox for enabling/disabling GPU acceleration

**Default Settings:**
- Weights file defaults to "petris_yolov5x_fp32.engine"
- Labels file defaults to "petris_data.yaml"
- GPU acceleration is enabled by default

### InitializeDetectionParametersControls()

Creates and configures controls for setting detection parameters within a specified parent GroupBox.

```csharp
public static void InitializeDetectionParametersControls(GroupBox parentGroup, Font regularFont,
    ref Label imageResolutionLabel, ref Label confidenceThresholdLabel,
    ref Label iouThresholdLabel, ref Label projectNameLabel,
    ref TextBox imageResolutionHorizontalTextBox, ref TextBox imageResolutionVerticalTextBox,
    ref TextBox confidenceThresholdTextBox, ref TextBox iouThresholdTextBox,
    ref TextBox projectNameTextBox)
```

**Controls Created:**
- Labels for each parameter
- TextBoxes for entering parameter values
- Special formatting for image resolution (width × height)

**Default Settings:**
- Image resolution: 1280 × 1280 pixels (standard for YOLOv5x)
- Confidence threshold: 0.25 (25%)
- IOU threshold: 0.45 (45%)
- Project name: "PETRIS_Test_Data"

### CreateServerControlButtons()

Creates buttons for controlling the detection server and initiating the detection process.

```csharp
public static void CreateServerControlButtons(Form parent, 
    ref Button startServerButton, ref Button quitServerButton, ref Button startDetectionButton,
    EventHandler startServerButtonClickHandler, EventHandler quitServerButtonClickHandler,
    EventHandler startDetectionButtonClickHandler)
```

**Controls Created:**
- Start Server button (green) - Initiates the Python server
- Stop Server button (red) - Terminates the Python server
- Start Detection button (blue) - Triggers the detection process

**Initial States:**
- Start Server button is enabled
- Stop Server button is disabled (until server is started)
- Start Detection button is disabled (until server is started)

### InitializeImagePanelControls()

Creates controls for displaying and navigating through input and output images.

```csharp
public static void InitializeImagePanelControls(GroupBox parentGroup, Font boldFont, Font regularFont,
    ref Label inputImageLabel, ref Label outputImageLabel,
    ref PictureBox inputPictureBox, ref PictureBox outputPictureBox,
    ref Button previousButton, ref Button nextButton,
    EventHandler previousButtonClickHandler, EventHandler nextButtonClickHandler)
```

**Controls Created:**
- Labels for input and output image sections
- PictureBoxes for displaying the input and output images
- Navigation buttons (Previous/Next) for browsing through multiple images

**PictureBox Properties:**
- Both PictureBoxes use `SizeMode.Zoom` to maintain aspect ratio
- Both have a fixed border and light background for contrast
- Equal dimensions (410×500 pixels) for consistent display

**Navigation Controls:**
- Previous and Next buttons are initially disabled
- Enabled when multiple images are loaded (batch processing)

## UI Layout Design

The UI components are arranged in a logical and user-friendly layout:

1. **Top Section** - Configuration controls for model and parameters
2. **Middle Section** - Server control buttons
3. **Main Section** - Side-by-side image display panels (input/output)
4. **Bottom Section** - Image navigation controls

This arrangement follows a top-to-bottom workflow that matches the typical user process:
1. Configure detection settings
2. Start the server
3. Initiate detection
4. View and navigate through results

## Visual Design

The UI employs a consistent visual design with the following characteristics:

- **Color Coding:**
  - Green for Start Server (positive action)
  - Red for Stop Server (terminating action)
  - Blue for Start Detection (primary action)
  - Light gray for secondary buttons
  - Light background for picture boxes

- **Typography:**
  - Bold fonts for group titles and important labels
  - Regular fonts for input controls and secondary text
  - Larger font sizes for primary action buttons

- **Layout Consistency:**
  - Uniform spacing between controls (typically 20-30 pixels)
  - Aligned edges for related controls
  - Logical grouping of related components

## Usage Pattern

The `YoloApplicationUI` class is designed to be used by the main application form during its initialization. The typical usage pattern is:

1. Create font objects for regular and bold text
2. Create group boxes for different control sections
3. Call the initialization methods to populate each group box
4. Store references to the created controls for later access
5. Wire up event handlers for user interactions

This approach separates the UI creation from the application logic, making the code more maintainable and easier to understand.

## Dependencies

The UI module relies on these .NET Framework components:

- System.Windows.Forms - For all UI controls and forms
- System.Drawing - For graphics, colors, fonts, and layout

## Design Considerations

The `YoloApplicationUI` class was designed with several important considerations:

1. **Separation of Concerns** - UI creation is separated from application logic
2. **Modularity** - Related UI elements are grouped and created together
3. **Consistency** - Uniform styling and layout across the application
4. **Flexibility** - Control references are passed by reference to allow later modification
5. **Usability** - Intuitive layout follows user workflow


</details>

<details>
<summary>YOLO_INITIAL_SETUP.CS</summary>

# Initial Setup Module Documentation

## Overview

The `YoloInitialSetup` class handles the crucial first-time setup process for the YOLO Object Detection application. Its primary responsibilities are:

1. Verifying and extracting the portable Python environment (`yolov5_env`)
2. Verifying and extracting the YOLOv5 codebase (`yolov5` directory)
3. Verifying the presence of model files and configuration files
4. Verifying the Python server script

This process ensures that all dependencies are correctly installed before the application attempts to use them, creating a portable, self-contained application that does not require users to manually install Python or the YOLOv5 framework.

## Key Components

### Main Methods

| Method | Purpose |
|--------|---------|
| `ExtractEnvironmentIfNeeded()` | Main entry point that checks for and extracts dependencies if needed |
| `VerifyDependencies()` | Checks for all required components and reports missing dependencies |
| `PerformExtraction()` | Orchestrates the extraction process for both components |
| `ExtractPythonEnvironment()` | Handles extracting the portable Python environment from the tar.gz file |
| `ExtractYolov5Directory()` | Handles extracting the YOLOv5 codebase from the zip file |
| `ExtractTarGzWithSharpCompress()` | Low-level method for extracting tar.gz archives |
| `ExtractZipWithSharpCompress()` | Low-level method for extracting zip archives |
| `UpdateStatus()` | Updates the UI with status information |

### Required Files

The setup requires these files to be present in the application's base directory:

- `yolov5_env.tar.gz` - Contains the portable Python environment
- `yolov5.zip` - Contains the YOLOv5 codebase
- `server.py` - Python server script that handles communication between the application and YOLO
- `Models/` directory - Should contain model weight files (*.pt or *.engine) and configuration files (*.yml or *.yaml)

## Detailed Method Descriptions

### ExtractEnvironmentIfNeeded()

This is the primary method that should be called during application startup. It:

1. Checks if all dependencies exist using `VerifyDependencies()`
2. If all dependencies exist, returns `true` immediately (no extraction needed)
3. Otherwise, displays a message indicating the missing dependency and starts the extraction process
4. Shows a progress form with a marquee progress bar during extraction
5. Runs the extraction process on a background thread to keep the UI responsive
6. Performs a final verification after extraction to ensure all components are available
7. Returns `true` if extraction and verification succeed, `false` otherwise

```csharp
public static bool ExtractEnvironmentIfNeeded()
```

### VerifyDependencies()

Checks for all required dependencies and reports which specific component is missing:

1. Checks for the existence of the Python environment directory
2. Checks for the Python executable
3. Checks for the YOLOv5 directory
4. Checks for the Models directory
5. Verifies model weight files exist (*.pt or *.engine)
6. Verifies YAML configuration files exist (*.yml or *.yaml)
7. Checks for the Python server script
8. Returns a detailed message about any missing component via the `missingDependency` output parameter

```csharp
private static bool VerifyDependencies(string envDirectoryPath, string pythonExePath, string yolov5DirectoryPath, string modelsDirectoryPath, string pythonServerScript, out string missingDependency)
```

### PerformExtraction()

Coordinates the extraction of both the Python environment and the YOLOv5 directory:

1. First extracts the Python environment (critical component)
2. If Python extraction succeeds, proceeds to extract the YOLOv5 codebase
3. Returns `true` only if both extractions succeed

```csharp
private static bool PerformExtraction(string basePath, string envDirPath, string yolov5DirectoryPath, Label statusLabel)
```

### ExtractPythonEnvironment()

Handles the Python environment extraction process:

1. Checks if the Python environment already exists
2. If not, looks for the `yolov5_env.tar.gz` file
3. Extracts the archive to the `yolov5_env` directory
4. Verifies the extraction by checking for `python.exe`

```csharp
private static bool ExtractPythonEnvironment(string basePath, string envDirPath, Label statusLabel)
```

### ExtractYolov5Directory()

Handles the YOLOv5 codebase extraction:

1. Checks if the YOLOv5 directory already exists
2. If not, looks for the `yolov5.zip` file
3. Extracts the archive to the `yolov5` directory
4. Verifies the extraction by checking if the directory contains files

```csharp
private static bool ExtractYolov5Directory(string basePath, string yolov5DirectoryPath, Label statusLabel)
```

## Extraction Details

### TAR.GZ Extraction

The `ExtractTarGzWithSharpCompress()` method extracts TAR.GZ archives using the SharpCompress library:

1. Opens the archive and creates a reader
2. Iterates through each entry, creating directories and files as needed
3. Includes safety checks to prevent path traversal attacks (skipping unsafe paths)
4. Silently skips any problematic entries that cannot be extracted
5. Sets file timestamps to match the archive

### ZIP Extraction

The `ExtractZipWithSharpCompress()` method extracts ZIP archives:

1. Opens the archive using the ZipArchive class from SharpCompress
2. Extracts each entry, skipping directories and null keys
3. Includes path normalization and security checks
4. Silently skips any problematic entries
5. Uses the WriteToDirectory method for extraction

## User Interface

The extraction process provides a minimal, clean user interface:

1. Initial message box indicating which dependency is missing and that extraction will be attempted
2. A progress form with a marquee progress bar (continuous animation rather than percentage-based)
3. Simple status text updates showing the current operation ("Extracting Python environment...", etc.)
4. Final validation after extraction with appropriate error messages if issues are found

## Error Handling

The code includes robust error handling:

1. Each extraction step is wrapped in try-catch blocks
2. Specific errors (missing files, extraction failures) display targeted error messages
3. Individual file extraction errors are silently ignored to prevent overwhelming the user
4. Final verification step ensures all required components are present after extraction
5. Comprehensive error messages that indicate exactly which component is missing

## Thread Safety

The extraction process runs on a background thread to keep the UI responsive:

1. The progress form is shown on the UI thread
2. The extraction thread runs in the background
3. Status updates use `Invoke` when needed to safely update UI from the background thread
4. The main thread waits for extraction to complete using `Join()`

## Dependencies

The code relies on these external dependencies:

- SharpCompress - Used for extracting both TAR.GZ and ZIP archives
- .NET System.Windows.Forms - For UI components
- System.IO - For file and directory operations 

</details>

<details>
<summary>YOLO_DETECTION_SERVICE.CS</summary>

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

</details>