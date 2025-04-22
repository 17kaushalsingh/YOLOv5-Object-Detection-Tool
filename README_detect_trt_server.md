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