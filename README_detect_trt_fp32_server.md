# YOLOv5 TensorRT Inference Server

This document provides instructions on how to use the `detect_trt_fp32_server.py` file for object detection using YOLOv5 with TensorRT acceleration.

## Overview

The `detect_trt_fp32_server.py` script is a high-performance object detection server that:

- Loads a YOLOv5 TensorRT engine once and keeps it in memory
- Performs inference on single images or entire folders
- Provides detailed performance metrics and detection results
- Outputs annotated images with bounding boxes and confidence scores

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

The script can be used in three main modes:

1. **Server mode**: Initialize the server with a TensorRT engine
2. **Image mode**: Process a single image
3. **Folder mode**: Process all images in a folder

### Server Mode

Start the server with your TensorRT engine:

```bash
python detect_trt_fp32_server.py server \
  --engine path/to/your/yolov5.engine \
  --labels path/to/your/labels.yaml \
  --input_shape 1,3,640,640 \
  --output_shape 1,25200,6 \
  --conf_thresh 0.25 \
  --score_thresh 0.25 \
  --nms_thresh 0.45
```

#### Optional Server Arguments

- `--fp16`: Use FP16 precision (default is FP32)
- `--port`: Set port for communication (default is 5000)

### Image Mode

Process a single image:

```bash
python detect_trt_fp32_server.py image \
  --image path/to/your/image.jpg \
  --output path/to/save/output.jpg \
  --json_output path/to/save/results.json
```

The `--json_output` parameter is optional.

### Folder Mode

Process all images in a folder:

```bash
python detect_trt_fp32_server.py folder \
  --folder path/to/your/images/ \
  --output path/to/save/outputs/ \
  --json_output path/to/save/results.json
```

The `--json_output` parameter is optional.

## Examples

### Example 1: Starting the server with a YOLOv5s model

```bash
python detect_trt_fp32_server.py server \
  --engine models/yolov5s.engine \
  --labels models/coco.yaml \
  --conf_thresh 0.3
```

### Example 2: Processing a single image

```bash
python detect_trt_fp32_server.py image \
  --image samples/person.jpg \
  --output results/person_detected.jpg
```

### Example 3: Processing a folder of images

```bash
python detect_trt_fp32_server.py folder \
  --folder samples/test_images/ \
  --output results/batch_results/
```

## Understanding the Output

### Image Output

For each processed image, the script:

1. Draws bounding boxes around detected objects
2. Labels each detection with class name and confidence score
3. Saves the annotated image to the specified output path

### JSON Output

If a JSON output path is provided, the script generates a JSON file containing:

- Overall metrics (FPS, total time, inference time)
- Detection details for each image (class, confidence, bounding box coordinates)
- Success/failure status

### Inference Logs

The script also generates an `inference_logs.txt` file in the output directory with:

- Model information
- Threshold values
- Performance metrics
- Object counts by class

## Troubleshooting

### Common Issues

1. **CUDA out of memory errors**: Try reducing batch size or using a smaller model
2. **TensorRT engine compatibility**: Ensure the engine was built with the same TensorRT version you're using for inference
3. **NaN outputs**: Check your model for proper normalization and preprocessing
4. **Slow performance**: Enable FP16 with the `--fp16` flag for faster inference

### Debug Tips

- Set lower confidence thresholds to see more detections
- Check the inference logs for detailed performance metrics
- Verify that your input and output shapes match the model's requirements

## Advanced Usage

### Custom Thresholds

Adjust detection sensitivity with threshold parameters:

- `--conf_thresh`: Detection confidence threshold (0.0-1.0)
- `--score_thresh`: Class score threshold (0.0-1.0)
- `--nms_thresh`: Non-maximum suppression threshold (0.0-1.0)

Lower values increase detection quantity but may include more false positives.

## Performance Notes

- FP16 mode (`--fp16`) typically provides 2-3x speedup with minimal accuracy loss
- The first inference may be slower due to CUDA initialization
- Processing a folder of images is more efficient than individual images due to reduced overhead 