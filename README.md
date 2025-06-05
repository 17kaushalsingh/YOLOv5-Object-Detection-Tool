# Test Software for AI Automatic Mixer Cleaning Machine

<video controls src="YOLOv5 Object Detection Tool Demo_2.mp4" title="YOLOv5-Object-Detection-Tool-Demonstartion"></video>

## Overview
This project implements an AI-powered object detection system for automatic mixer cleaning machines using YOLOv5. The system combines Python-based detection services with a C# Windows Forms application for a user-friendly interface.

## Quick Start
1. Download the project files and executable from: [YOLOv5-Object-Detection-Tool](https://drive.google.com/drive/folders/1nxDFu4rGmvPkNdYlqTQ4rmEkHwq4hWj8?usp=sharing)
2. Double-click the `application.exe` file to run

## Project Structure
### Core Components
- **Python Services**:
  - `detect_trt_server.py`: TensorRT-based object detection server for optimized inference
  - `server.py`: Main server implementation for handling detection requests and batch processing

- **C# Application**:
  - `Form1.cs`: Main application form with UI layout and event handling
  - `YoloApplicationUI.cs`: UI component creation and management
  - `YoloInitialSetup.cs`: Automatic environment and model initialization
  - `YoloDetectionService.cs`: Core detection service and server communication

### Required Files
- `yolov5.zip`: YOLOv5 repository with detection code
- `yolov5_env.tar.gz`: Portable Python environment
- Model files in `Models/` directory:
  - Weight files (`.pt`, `.onnx`, or `.engine` formats)
  - YAML configuration files for class labels

## Setup Instructions
1. **Python Environment**:
   - The application automatically extracts `yolov5_env.tar.gz` on first run
   - No manual Python installation required
   - Environment includes all necessary dependencies

2. **Model Files**:
   - Place model weights and labels in `Models/` directory
   - Supported formats: `.pt`, `.onnx`, `.engine`
   - Include corresponding YAML files for class labels
   - Files are automatically verified during startup

3. **Python Scripts**:
   - Required scripts are included in the distribution
   - Server script handles communication and batch processing
   - TensorRT server provides optimized inference

## Documentation
Detailed documentation for each component is available in the `README/` directory:
- [Detect TRT Server](README/Detect_TRT_Server.md): TensorRT optimization and inference
- [Server](README/Server.md): Main detection server implementation
- [Form1](README/Form1.md): Main application interface
- [Yolo Application UI](README/YoloApplicationUI.md): UI component management
- [Yolo Initial Setup](README/YoloInitialSetup.md): Environment initialization
- [Yolo Detection Service](README/YoloDetectionService.md): Core detection functionality

## Features
- **Automatic Setup**: Self-contained environment with no manual installation
- **Batch Processing**: Process entire folders of images with a single command
- **Real-time Detection**: View detection results as they are processed
- **Flexible Input**: Support for single images or entire folders
- **Configurable Parameters**: Adjust resolution, confidence, and IOU thresholds
- **Organized Output**: Results stored in project-specific directories
- **CSV Export**: Consolidated detection results in CSV format

## Prerequisites
- Windows 10 or later
- No Python installation required (included in distribution)
- NVIDIA GPU recommended for TensorRT acceleration (optional)

## Troubleshooting
1. **Missing Dependencies**: Application will automatically attempt to extract required files
2. **GPU Acceleration**: Ensure NVIDIA drivers are up to date for TensorRT support
3. **Model Files**: Verify model weights and YAML files are in the `Models/` directory
4. **Output Directory**: Check for existing directories before starting new detection

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt/) file for details.

Copyright (c) 2025 Kaushal Singh