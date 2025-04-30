# Test Software for AI Automatic Mixer Cleaning Machine
![alt text](Yolov5-Object-Detection-Tool-UI.png)

## Overview
This project implements an AI-powered object detection system for automatic mixer cleaning machines using YOLOv5. The system combines Python-based detection services with a C# Windows Forms application for a user-friendly interface.

## Quick Start
1. Download the project files and executable from: [YOLOv5-Object-Detection-Tool](https://drive.google.com/drive/folders/1REzCtH1H-8uFze_JUyMQvq9kVlZJ_ldQ?usp=drive_link)
2. Double-click the `application.exe` file to run

## Project Structure
### Core Components
- **Python Services**:
  - `detect_trt_server.py`: TensorRT-based object detection server
  - `server.py`: Main server implementation for handling detection requests

- **C# Application**:
  - `Form1.cs`: Main application form and UI logic
  - `YoloApplicationUI.cs`: YOLO-specific UI components
  - `YoloInitialSetup.cs`: Environment and model initialization
  - `YoloDetectionService.cs`: Core detection service implementation

### Required Files
- `yolov5.zip`: YOLOv5 repository
- `yolov5_env.tar.gz`: Python environment
- Model files in `Models/` directory

## Setup Instructions
1. **Python Environment**:
   - Extract `yolov5_env.tar.gz` to project directory
   - Set file property: Copy to output directory → Copy if newer

2. **Model Files**:
   - Place model weights and labels in `Models/` directory
   - Set file properties: Copy to output directory → Copy if newer
   - Update ComboBox range in `YoloInitialSetup.cs` for new models

3. **Python Scripts**:
   - Place required scripts in project directory
   - Set file properties: Copy to output directory → Copy if newer
   - Update script paths in `YoloDetectionService.cs`

## Documentation
Detailed documentation for each component is available in the `README/` directory:
- [Detect TRT Server](README/Detect_TRT_Server.md)
- [Server](README/Server.md)
- [Form1](README/Form1.md)
- [Yolo Application UI](README/YoloApplicationUI.md)
- [Yolo Initial Setup](README/YoloInitialSetup.md)
- [Yolo Detection Service](README/YoloDetectionService.md)

## Prerequisites
- Visual Studio (or compatible C# IDE)
- YOLOv5 repository
- Model files (`.pt`, `.onnx`, or `.engine` formats)
- YAML data files

## Troubleshooting
1. **Resource Not Found**: Verify namespace matches project namespace
2. **File Size Issues**: Consider splitting large files or using server-based distribution
3. **Python Environment**: Ensure proper Python installation and package dependencies

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt/) file for details.

Copyright (c) 2025 Kaushal Singh