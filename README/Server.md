# YOLOv5 Inference Server

## Overview

The YOLOv5 Inference Server is a Python-based tool that loads a YOLOv5 model and provides an interactive command-line interface for running object detection on images. It allows users to process individual images or entire folders of images with a single command and consolidates all detection results into a CSV file.

## Features

- **Interactive CLI**: Simple command-line interface for processing images
- **Efficient Processing**: Loads the YOLOv5 model once and keeps it in memory
- **Batch Processing**: Process entire folders of images with a single command
- **CSV Output**: Generates a consolidated CSV file with detection results in customized column order
- **Automatic GPU Detection**: Automatically uses GPU if available for faster inference
- **Proper Resource Management**: Ensures CUDA resources are properly cleaned up
- **Organized Output**: Results stored in project-specific directories with proper naming conventions
- **Graceful Shutdown**: Proper cleanup on exit using signal handlers and atexit registration

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
python server.py --model weights/custom_model.pt --labels data/custom.yaml
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

If a project name is provided but a directory with that name already exists, a timestamp-based directory name will be used instead to prevent overwriting existing results.

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
- Proper resource cleanup and memory management

### YOLOv5Server

This class provides the command-line interface and user interaction. It includes:

- The main command processing loop
- Signal handling for graceful shutdown
- Methods for processing user commands
- Resource cleanup on exit
- Error handling and exception management

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

## Resource Management

The server implements proper resource management to prevent memory leaks and ensure clean shutdown:

- CUDA memory is properly freed after inference
- PyTorch models are unloaded when the server exits
- Signal handlers ensure proper cleanup on keyboard interrupts
- The `atexit` module is used to register cleanup functions even if the program crashes
- Exception handling ensures cleanup occurs even during errors

## Error Handling

The server implements comprehensive error handling:

1. **Command Processing**:
   - Invalid commands are silently ignored
   - Missing arguments are handled gracefully
   - File/folder existence is verified before processing

2. **Inference Errors**:
   - Individual image processing errors are caught and logged
   - Batch processing continues even if individual images fail
   - Traceback information is preserved for debugging

3. **Resource Cleanup**:
   - Cleanup is guaranteed through try-finally blocks
   - Multiple cleanup mechanisms (signal handlers, atexit)
   - Graceful handling of missing resources

## Troubleshooting

### GPU Memory Issues

If you encounter GPU memory errors, try:
1. Reducing the batch size (first value in `--input_shape`)
2. Reducing the input resolution
3. Using a smaller YOLOv5 model (e.g., YOLOv5n instead of YOLOv5x)

### Missing Labels

If detected objects don't have proper class names, verify that:
1. The correct labels file is specified
2. The labels file has the correct format (YAML with a 'names' list or dictionary)
3. The model was trained with the same class labels

### Server Becomes Unresponsive

If the server becomes unresponsive:
1. Press Ctrl+C to trigger the signal handler for graceful shutdown
2. If that doesn't work, terminate the process and restart

## Contributing

Contributions to improve the server are welcome. Please ensure any changes maintain backward compatibility with existing functionality.

## License

This project is distributed under the same license as YOLOv5. 