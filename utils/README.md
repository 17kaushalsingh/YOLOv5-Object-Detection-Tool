# Utils Directory Documentation

This directory contains utility scripts that support the YOLO Object Detection application.

## Overview

The utils folder includes standalone Python scripts that perform supporting functions for the main application:

1. **download_models.py** - Downloads pre-trained YOLOv5 models from Google Drive
2. **compare_detections.py** - Compares detection results by placing images from two folders side-by-side

## Utility Scripts

### download_models.py

A Python script that downloads pre-trained YOLOv5 models and configuration files from Google Drive.

#### Purpose
- Provides easy access to pre-trained PETRIS-specific YOLOv5 models
- Downloads various model formats (PyTorch, ONNX, TensorRT engines)
- Automatically places files in the specified directory

#### Usage
```bash
python utils/download_models.py
```

#### Files Downloaded
- `petris_yolov5x.pt` - PyTorch format model
- `petris_yolov5x_fp16.onnx` - ONNX format model (FP16 precision)
- `petris_yolov5x_fp32.onnx` - ONNX format model (FP32 precision)
- `petris_yolov5x_fp16.engine` - TensorRT engine (FP16 precision)
- `petris_yolov5x_fp32.engine` - TensorRT engine (FP32 precision)
- `petris_data.yaml` - Dataset configuration file

#### Dependencies
- `gdown`: Google Drive download library
```bash
pip install gdown
```

#### Features
- Prompts for target directory
- Skips files that already exist
- Creates directory if it doesn't exist

### compare_detections.py

A Python script that creates side-by-side comparisons of detection results from two different folders.

#### Purpose
- Visually compare results between different model versions
- Compare results with different confidence/IoU thresholds
- Evaluate detection quality across different processing methods

#### Usage
```bash
python utils/compare_detections.py
```

#### How It Works
1. Scans two input folders for JPG images
2. Matches images by filename
3. Resizes images to a common height while maintaining aspect ratio
4. Places images side-by-side in a single output image
5. Saves combined images to the specified output folder

#### Example Use Cases
- Compare detections from different model versions (e.g., YOLOv5x vs YOLOv5s)
- Compare detections with different confidence thresholds
- Evaluate the impact of different image preprocessing techniques

#### Dependencies
- `PIL` (Python Imaging Library / Pillow): For image manipulation
```bash
pip install pillow
```

#### Features
- Interactive prompts for input and output folder paths
- Automatic image resizing to match heights
- Preserves aspect ratios of original images

## Integration with Main Application

These utilities are stand-alone scripts that complement the main YOLO Detection application:

- **download_models.py** should be used first to obtain the model files needed by the application
- **compare_detections.py** is a post-processing tool for analyzing and comparing detection results

## Notes

- Both scripts use interactive prompts rather than command-line arguments for simplicity
- Scripts can be run independently of the main application
- Model files downloaded by download_models.py should be placed in the Models directory for the main application to use them 