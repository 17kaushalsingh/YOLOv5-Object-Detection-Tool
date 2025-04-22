import os
import sys
import tensorrt as trt
import pycuda.driver as cuda
import pycuda.autoinit
import cv2
import numpy as np
from PIL import Image
import yaml
import time
import math
import argparse
import json
import socket
import threading
import signal
import traceback
import csv
from queue import Queue
from pathlib import Path

class TRTInferenceYolov5:
    """
    TRTInferenceYolov5 is a class for performing YOLOv5 object detection using TensorRT.
    
    This class loads a TensorRT engine once and keeps it in memory for multiple inference requests.
    It supports both single image inference and folder inference.
    """

    def __init__(self, engine_file_path, class_label_file, 
                 input_shape, output_shape, conf_th, nms_th, 
                 batch_size=1, show_labels=True, show_conf=True):
        """
        Initialize the inference engine with model parameters.

    Args:
        engine_file_path (str): Path to the TensorRT engine file.
        class_label_file (str): Path to the .yaml file containing class labels.
        input_shape (tuple): Shape of the input tensor.
        output_shape (tuple): Shape of the output tensor.
        conf_th (float): Confidence threshold for detections.
        nms_th (float): Non-maximum suppression threshold for detections.
        batch_size (int, optional): Batch size for inference. Defaults to 1.
        show_labels (bool, optional): Whether to show labels on detections. Defaults to True.
        show_conf (bool, optional): Whether to show confidence values on detections. Defaults to True.
    """
        # Create logger
        self.logger = trt.Logger(trt.Logger.WARNING)

        # Define engine parameters
        self.engine_file_path = engine_file_path
        self.batch_size = batch_size
        
        # Display options
        self.show_labels = show_labels
        self.show_conf = show_conf
        
        self.engine = self.load_engine()
        
        self.context = self.engine.create_execution_context()

        # Get information about engine bindings
        self.get_engine_info()

        # Define class labels
        self.class_label_file = class_label_file
        with open(class_label_file, "r") as f:
            data = yaml.safe_load(f)
            self.class_labels = [name for name in data['names'].values()]

        # Define image input shape, output shape and labels
        self.input_shape = input_shape
        self.output_shape = output_shape

        # Define thresholds for detection
        self.conf_th = conf_th
        self.nms_th = nms_th

        # Initialize counters
        self.reset_counters()
        
    def __del__(self):
        """Cleanup CUDA resources properly when object is deleted."""
        self.cleanup()
    
    def cleanup(self):
        """
        Properly clean up CUDA and TensorRT resources.
        This helps prevent CUDA errors on shutdown.
        """
        try:
            # Free CUDA resources in reverse order of creation
            if hasattr(self, 'context') and self.context:
                self.context = None
            
            if hasattr(self, 'engine') and self.engine:
                self.engine = None
                

        except Exception as e:
            # Just log errors during cleanup, don't raise
            print(f"Warning during resource cleanup: {e}")

    def reset_counters(self):
        """Reset detection counters for a new inference run."""
        self.total_objects = 0
        self.objects_by_class = {label: 0 for label in self.class_labels}
        self.invalid_outputs = 0
        self.total_time = 0
        self.inference_time = 0
        self.num_frames = 0

    def get_engine_info(self):
        """Gets binding information for the TensorRT engine."""
        try:
            # Try to get number of bindings using different methods
            try:
                # Try newer API first
                self.num_bindings = self.engine.num_io_tensors
            except AttributeError:
                # Try legacy API
                try:
                    self.num_bindings = self.engine.num_bindings
                except:
                    self.num_bindings = 2  # Default for YOLOv5
            
            # Set input dtype based to float32
            self.input_dtype = np.float32
            
        except Exception as e:
            print(f"Warning: Error during engine info gathering: {e}")
            self.num_bindings = 2
            self.input_dtype = np.float32

    def load_engine(self):
        """
        Loads the TensorRT engine from the specified file path.
        
        Returns:
            trt.ICudaEngine: The loaded TensorRT engine.
        """
        try:
            with open(self.engine_file_path, 'rb') as f:
                # Initialize runtime
                runtime = trt.Runtime(self.logger)

                # Deserialize engine
                engine_deserialized = runtime.deserialize_cuda_engine(f.read())
                
                if engine_deserialized is None:
                    raise RuntimeError("Failed to deserialize engine")

                return engine_deserialized
        except Exception as e:
            print(f"Error loading engine: {str(e)}")
            raise

    def preprocess_image(self, image_path):
        """
        Preprocess a single image for inference.
        
        Args:
            image_path (str): Path to the image file.
        
        Returns:
            tuple: A tuple containing the preprocessed image tensor and original dimensions.
        """
        # Read image
        img = cv2.imread(image_path)
        if img is None:
            raise ValueError(f"Could not read image {image_path}")

        # Store original dimensions
        original_dimensions = img.shape[:2]  # height, width

        # Resize to YOLOv5 input size
        resized_img = cv2.resize(img, (self.input_shape[2], self.input_shape[3]), interpolation=cv2.INTER_AREA)

        # Get the shape of resized image for later use
        self.resized_img_w, self.resized_img_h = resized_img.shape[:2]

        # Convert to numpy img and normalize
        np_img = np.array(resized_img).astype(np.float32) / 255.0

        # Transpose to NCHW format (batch, channels, height, width)
        np_img = np.transpose(np_img, (2, 0, 1))

        # Expand dimensions to match input shape
        np_img = np.expand_dims(np_img, axis=0)

        return np_img, original_dimensions

    def get_images_from_folder(self, folder_path):
        """
        Get a list of image files from a folder.
        
        Args:
            folder_path (str): Path to the folder containing images.
            
        Returns:
            list: List of image file paths.
        """
        image_extensions = ['.jpg', '.jpeg', '.png', '.bmp']
        images = []
        
        for filename in os.listdir(folder_path):
            if any(filename.lower().endswith(ext) for ext in image_extensions):
                images.append(os.path.join(folder_path, filename))
                
        return images

    def infer_single_image(self, image_path, save_dir_path, reset_counters=True):
        """
        Run inference on a single image.
        
        Args:
            image_path (str): Path to the image file.
            save_dir_path (str): Directory to save the output image.
            reset_counters (bool, optional): Whether to reset counters. Defaults to True.
            
        Returns:
            dict: Dictionary containing detection results and paths.
        """
        os.makedirs(save_dir_path, exist_ok=True)
        
        # Reset counters for this inference
        if reset_counters:
            self.reset_counters()
            self.num_frames = 1
        
        try:
            # Preprocess the image
            start_time = time.time()
            input_data, original_dimensions = self.preprocess_image(image_path)
            preprocess_time = time.time() - start_time
            self.total_time += preprocess_time
            
            # Prepare input data for inference
            input_data = np.ascontiguousarray(input_data)
            output = np.empty(self.output_shape, dtype=self.input_dtype)

            # Run inference
            inference_start = time.time()
            
            # Allocate GPU memory
            d_input = cuda.mem_alloc(1 * input_data.nbytes)
            d_output = cuda.mem_alloc(1 * output.nbytes)

            # Copy input data to GPU
            cuda.memcpy_htod(d_input, input_data)

            # Run inference - using the approach from run_inferences.py
            bindings = [d_input, d_output]
            self.context.execute_v2(bindings)

            # Copy results back to host
            cuda.memcpy_dtoh(output, d_output)

            # Free GPU memory
            d_input.free()
            d_output.free()

            # Record inference time
            inference_time = time.time() - inference_start
            self.inference_time += inference_time
            self.total_time += inference_time

            # Postprocess the result
            output_path = os.path.join(save_dir_path, os.path.basename(image_path))
            detections = self.postprocess_image(image_path, output, original_dimensions, output_path)
            
            return {
                "success": True,
                "input_path": image_path,
                "output_path": output_path,
                "detections": detections
            }
            
        except Exception as e:
            error_msg = f"Error processing image {image_path}: {str(e)}"
            print(error_msg)
            traceback.print_exc()
            return {
                "success": False,
                "input_path": image_path,
                "error": error_msg
            }

    def infer_folder(self, folder_path, save_dir_path):
        """
        Run inference on all images in a folder.
        
        Args:
            folder_path (str): Path to the folder containing images.
            save_dir_path (str): Directory to save the output images.
            
        Returns:
            dict: Dictionary containing detection results for all images.
        """
        os.makedirs(save_dir_path, exist_ok=True)
        
        # Reset counters for this inference batch
        self.reset_counters()
        
        # Get list of images
        image_paths = self.get_images_from_folder(folder_path)
        self.num_frames = len(image_paths)
        
        if not image_paths:
            return {
                "success": False,
                "error": f"No valid images found in {folder_path}"
            }
        
        # Process each image
        results = []
        
        for idx, image_path in enumerate(image_paths):
            try:
                # Call infer_single_image without resetting counters
                result = self.infer_single_image(image_path, save_dir_path, reset_counters=False)
                
                # Add to results list
                results.append(result)
                
                print(f"\rProcessing {idx+1}/{len(image_paths)} images", end="")
                
            except Exception as e:
                error_msg = f"Error processing image {image_path}: {str(e)}"
                print(f"\n{error_msg}")
                traceback.print_exc()
                results.append({
                    "success": False,
                    "input_path": image_path,
                    "error": error_msg
                })
        
        print()
                
        # Return metrics as part of the result
        metrics = self.generate_metrics()
        
        return {
            "success": True,
            "metrics": metrics,
            "results": results
        }

    def postprocess_image(self, image_path, yolov5_output, original_dimensions, output_path):
        """
        Postprocesses the YOLOv5 output to draw bounding boxes and labels on the image.
        
        Args:
            image_path (str): Path to the input image.
            yolov5_output (np.ndarray): The output from the YOLOv5 model.
            original_dimensions (tuple): Original dimensions of the image (height, width).
            output_path (str): Path to save the processed image.
            
        Returns:
            list: List of detection objects with class, confidence, and bounding box.
        """
        # List to store detection results
        detections = []

        # Check for NaN values in the output
        if np.isnan(yolov5_output).any():
            self.invalid_outputs += 1
            # Create a blank image with text
            image = cv2.imread(image_path)
            h, w = image.shape[:2]
            cv2.putText(image, "No valid detections (NaN output)", (w//10, h//2), 
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 255), 2, cv2.LINE_AA)
            
            # Save the image with error text
            cv2.imwrite(output_path, image)
            return detections

        # Read the image
        image = cv2.imread(image_path)
        if image is None:
            print(f"Warning: Could not read image for postprocessing: {image_path}")
            return detections
            
        height, width = original_dimensions

        # Calculate scale factors between model input and original image
        scale_x = width / self.resized_img_w
        scale_y = height / self.resized_img_h

        # Lists to store detection information
        class_ids = []
        confidences = []
        bboxes = []
        yolo_bboxes = []  # To store normalized YOLO format boxes

        # Process all detections
        for detect in yolov5_output[0]:
            # Skip if any value is NaN
            if np.isnan(detect).any():
                continue
                
            conf = float(detect[4])
            
            if conf < self.conf_th:
                continue

            class_score = detect[5:]
            class_idx = np.argmax(class_score)

            # Use the same confidence threshold for class score as in standard YOLOv5
            if class_score[class_idx] < self.conf_th:
                continue

            # YOLO format normalized coordinates (center_x, center_y, width, height)
            cx, cy, w, h = float(detect[0]), float(detect[1]), float(detect[2]), float(detect[3])
            
            # Check for invalid values
            if not all(map(lambda x: math.isfinite(x), [cx, cy, w, h])):
                continue
            
            # Store original YOLO format coordinates (normalized)
            yolo_box = [cx, cy, w, h]
                
            # Prepare box for bboxes
            try:
                # Calculate box coordinates
                top_left_x = max(0, int((cx - w/2) * scale_x))
                top_left_y = max(0, int((cy - h/2) * scale_y))
                width_scaled = min(width - top_left_x, int(w * scale_x))
                height_scaled = min(height - top_left_y, int(h * scale_y))
                
                # Skip if box dimensions are invalid
                if width_scaled <= 0 or height_scaled <= 0:
                    continue
                    
                box = np.array([top_left_x, top_left_y, width_scaled, height_scaled])
                
                # Add to our lists
                class_ids.append(int(class_idx))
                confidences.append(float(conf))
                bboxes.append(box)
                yolo_bboxes.append(yolo_box)
                
            except (ValueError, OverflowError) as e:
                print(f"Error processing detection: {e}")
                continue

        # Apply NMS Suppression
        if len(bboxes) > 0:
            try:
                indices_nms = cv2.dnn.NMSBoxes(bboxes, confidences, self.conf_th, self.nms_th)
            except cv2.error as e:
                print(f"OpenCV error during NMS: {e}")
                indices_nms = []
                
            # Draw all detections that survived NMS
            for i in indices_nms:
                # In newer OpenCV versions, i is a scalar rather than a 1-element array
                if isinstance(i, np.ndarray):
                    i = i[0]
                    
                # Update counters
                self.total_objects += 1
                class_idx = class_ids[i]
                
                # Get safe class name
                if 0 <= class_idx < len(self.class_labels):
                    class_name = self.class_labels[class_idx]
                    self.objects_by_class[class_name] += 1
                else:
                    class_name = "Unknown"  # Use generic name for invalid class index
                
                # Get box coordinates
                box = bboxes[i]
                top_left_x, top_left_y, width_scaled, height_scaled = box.astype(int)
                
                # Get YOLO format coordinates
                yolo_box = yolo_bboxes[i]
                
                # Store detection info
                detection = {
                    "class": class_name,
                    "confidence": float(confidences[i]),
                    "bbox": [
                        int(top_left_x),
                        int(top_left_y),
                        int(width_scaled),
                        int(height_scaled)
                    ],
                    "yolo_bbox": [
                        float(yolo_box[0]),  # center_x
                        float(yolo_box[1]),  # center_y
                        float(yolo_box[2]),  # width
                        float(yolo_box[3])   # height
                    ]
                }
                detections.append(detection)
                
                # Create label based on settings
                label = ""
                if self.show_labels:
                    label = class_name
                    
                if self.show_conf:
                    if label:
                        label += f", conf: {confidences[i]:.2f}"
                    else:
                        label = f"conf: {confidences[i]:.2f}"
                
                color = (0, 255, 0)  # Green for detections

                # Make rectangle with appropriate color
                cv2.rectangle(image, (top_left_x, top_left_y), 
                              (top_left_x+width_scaled, top_left_y+height_scaled), color, 3)

                # Only add text if we have a label to show
                if label:
                    text_size = cv2.getTextSize(label, cv2.FONT_HERSHEY_SIMPLEX, 0.7, 2)[0]
                    cv2.rectangle(image, (top_left_x, top_left_y-25), 
                                (top_left_x+text_size[0], top_left_y), color, -1)
                    cv2.putText(image, label, (top_left_x, top_left_y-5),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 0), 2, cv2.LINE_AA)

        # Save the processed image
        cv2.imwrite(output_path, image)

        return detections

    def generate_metrics(self):
        """
        Generates performance metrics for the detection run.
        
        Returns:
            dict: A dictionary containing performance metrics.
        """
        metrics = {}
        
        if self.num_frames > 0:
            metrics['num_frames'] = self.num_frames
            metrics['total_time'] = self.total_time
            metrics['inference_time'] = self.inference_time
            metrics['average_fps'] = self.num_frames / self.total_time
            metrics['average_inference_time'] = self.inference_time / self.num_frames
            metrics['total_objects'] = self.total_objects
            metrics['invalid_outputs'] = self.invalid_outputs
            metrics['objects_by_class'] = self.objects_by_class
        
        return metrics

class YOLOv5Server:
    """
    A server that loads a YOLOv5 TensorRT model once and processes multiple inference requests.
    
    This server loads the model into memory at startup and keeps it loaded,
    which eliminates the overhead of reloading the model for each inference request.
    This approach significantly improves performance for repeated inference calls.
    """
    
    def __init__(self, engine_path, labels_path, input_shape, output_shape, 
                 conf_thresh, nms_thresh, port=5000, 
                 show_labels=True, show_conf=True, output_dir=None):
        """
        Initialize the YOLOv5 inference server.
        
        Args:
            engine_path (str): Path to the TensorRT engine file.
            labels_path (str): Path to the YAML file containing class labels.
            input_shape (tuple): Shape of the input tensor.
            output_shape (tuple): Shape of the output tensor.
            conf_thresh (float): Confidence threshold for detections.
            nms_thresh (float): NMS threshold for detections.
            port (int, optional): Port to listen on for commands. Defaults to 5000.
            show_labels (bool, optional): Whether to show labels on detections. Defaults to True.
            show_conf (bool, optional): Whether to show confidence values on detections. Defaults to True.
            output_dir (str, optional): User-specified output subdirectory. Defaults to None.
        """
        # Initialize the inference engine
        self.inferencer = TRTInferenceYolov5(
            engine_file_path=engine_path,
            class_label_file=labels_path,
            input_shape=input_shape,
            output_shape=output_shape,
            conf_th=conf_thresh,
            nms_th=nms_thresh,
            show_labels=show_labels,
            show_conf=show_conf
        )
        
        self.port = port
        self.running = False
        self.command_queue = Queue()
        self.result_queue = Queue()

        # Create detection directories
        self.detections_dir = "Detections"
        
        # Create output directory
        if output_dir:
            # If user specified directory exists, append timestamp
            temp_output_dir = os.path.join(self.detections_dir, output_dir)
            if os.path.exists(temp_output_dir):
                timestamp = time.strftime("%Y%m%d_%H%M%S")
                self.output_dir = os.path.join(self.detections_dir, f"{output_dir}_{timestamp}")
            else:
                self.output_dir = temp_output_dir
        else:
            # Create timestamp-based directory if no directory specified
            timestamp = time.strftime("%Y%m%d_%H%M%S")
            self.output_dir = os.path.join(self.detections_dir, f"results_{timestamp}")
        
        # Create directories if they don't exist
        os.makedirs(self.detections_dir, exist_ok=True)
        os.makedirs(self.output_dir, exist_ok=True)
        
        # Create CSV file for detections
        self.csv_path = os.path.join(self.output_dir, "detections.csv")
        self._init_csv_file()
        
    def _init_csv_file(self):
        """Initialize the CSV file with headers if it doesn't exist."""
        if not os.path.exists(self.csv_path):
            with open(self.csv_path, 'w', newline='') as csvfile:
                writer = csv.writer(csvfile)
                writer.writerow(['image_file', 'class', 'confidence', 
                                'x', 'y', 'width', 'height', 
                                'center_x_norm', 'center_y_norm', 'width_norm', 'height_norm'])
        
    def __del__(self):
        """Clean up resources when server is deleted."""
        self.cleanup()
        
    def cleanup(self):
        """
        Properly clean up resources on server shutdown.
        This helps prevent CUDA errors when quitting.
        """
        try:
            # First clean up the inferencer (TensorRT engine)
            if hasattr(self, 'inferencer') and self.inferencer:
                self.inferencer.cleanup()
                self.inferencer = None
        except Exception as e:
            print(f"Warning during server cleanup: {e}")

    def start(self):
        """
        Start the server and listen for commands.
        
        This method initializes the server and starts processing commands.
        It sets up signal interrupts for clean shutdown.
        """
        self.running = True
        
        # Set up signal handler for clean shutdown
        signal.signal(signal.SIGINT, self._handle_sigint)
        
        # Start processing commands
        try:
            self._process_commands()
        except KeyboardInterrupt:
            pass
        finally:
            self.running = False
            # Ensure proper cleanup on exit
            self.cleanup()
    
    def _handle_sigint(self, sig, frame):
        """
        Handle SIGINT (Ctrl+C) to shut down gracefully.
        
        Args:
            sig: Signal number
            frame: Current stack frame
        """
        self.running = False
        self.cleanup() # Ensure proper cleanup on signal
    
    def _process_commands(self):
        """
        Process commands from user input.
        """
        while self.running:
            try:
                # Get command from user
                command = input("> ").strip().lower()
                
                if command.startswith("image"):
                    # Parse command like: image --image path/to/img.jpg
                    args = command.split()
                    
                    # Check for empty command or missing arguments
                    if len(args) < 3:
                        continue
                    
                    # Find the image path
                    try:
                        img_index = args.index("--image") + 1
                        if img_index >= len(args):
                            continue
                        image_path = args[img_index]
                        
                        # Process the image
                        self._process_single_image(image_path)
                    except ValueError:
                        pass
                    
                elif command.startswith("folder"):
                    # Parse command like: folder --folder path/to/folder
                    args = command.split()
                    
                    # Check for empty command or missing arguments
                    if len(args) < 3:
                        continue
                    
                    # Find the folder path
                    try:
                        folder_index = args.index("--folder") + 1
                        if folder_index >= len(args):
                            continue
                        folder_path = args[folder_index]
                        
                        # Process the folder
                        self._process_folder(folder_path)
                    except ValueError:
                        pass
                    
                elif command == "quit":
                    self.running = False
                else:
                    pass
                
            except Exception as e:
                print(f"Error processing command: {str(e)}")
                traceback.print_exc()
    
    def _write_detections_to_csv(self, image_path, detections):
        """
        Write detection results to CSV file.
        
        Args:
            image_path (str): Path to the input image.
            detections (list): List of detection objects.
        """
        if not detections:
            return
            
        image_filename = os.path.basename(image_path)
        
        with open(self.csv_path, 'a', newline='') as csvfile:
            writer = csv.writer(csvfile)
            for detection in detections:
                bbox = detection['bbox']
                yolo_bbox = detection['yolo_bbox']
                writer.writerow([
                    image_filename,
                    detection['class'],
                    detection['confidence'],
                    bbox[0],  # x
                    bbox[1],  # y
                    bbox[2],  # width
                    bbox[3],  # height
                    yolo_bbox[0],  # center_x_norm
                    yolo_bbox[1],  # center_y_norm
                    yolo_bbox[2],  # width_norm
                    yolo_bbox[3]   # height_norm
                ])
    
    def _process_single_image(self, image_path):
        """
        Process a single image.
        """
        try:
            if not os.path.exists(image_path):
                print(f"Error: Image not found at {image_path}")
                return
            
            # Run inference
            result = self.inferencer.infer_single_image(image_path, self.output_dir)
            
            # Write detections to CSV
            if result["success"] and "detections" in result:
                self._write_detections_to_csv(image_path, result["detections"])

        except Exception as e:
            print(f"Error processing image {image_path}: {str(e)}")
            traceback.print_exc()
    
    def _process_folder(self, folder_path):
        """
        Process all images in a folder.
        """
        try:
            if not os.path.exists(folder_path):
                print(f"Error: Folder not found at {folder_path}")
                return
            
            # Run inference
            result = self.inferencer.infer_folder(folder_path, self.output_dir)
            
            # Write all detections to CSV
            if result["success"] and "results" in result:
                for img_result in result["results"]:
                    if img_result["success"] and "detections" in img_result:
                        self._write_detections_to_csv(
                            img_result["input_path"], 
                            img_result["detections"]
                        )

        except Exception as e:
            print(f"Error processing folder {folder_path}: {str(e)}")
            traceback.print_exc()

def main():
    """
    Main entry point for the YOLOv5 TensorRT Inference Server.
    """
    # Set up command line argument parser
    parser = argparse.ArgumentParser(description='YOLOv5 TensorRT Inference Server')
    
    # Server initialization parser - the only mode available
    parser.add_argument('--engine', required=True, type=str, help='Path to TensorRT engine file')
    parser.add_argument('--labels', required=True, type=str, help='Path to YAML file containing class labels')
    parser.add_argument('--input_shape', type=str, default='1,3,1280,1280', help='Input shape as comma-separated values (default: 1,3,1280,1280)')
    parser.add_argument('--output_shape', type=str, default='1,100800,15', help='Output shape as comma-separated values (default: 1,100800,15)')
    parser.add_argument('--conf_thresh', type=float, default=0.25, help='Confidence threshold (default: 0.25)')
    parser.add_argument('--nms_thresh', type=float, default=0.45, help='NMS threshold (default: 0.45)')
    parser.add_argument('--port', type=int, default=5000, help='Port for communication (default: 5000)')
    parser.add_argument('--hide_labels', action='store_true', help='Hide class labels on detections (default: False)')
    parser.add_argument('--hide_conf', action='store_true', help='Hide confidence values on detections (default: False)')
    parser.add_argument('--output_dir', type=str, help='Output subdirectory name (default: timestamp-based name)')
    
    # Parse arguments
    args = parser.parse_args()
    
    try:
        # Convert string shapes to tuples
        input_shape = tuple(map(int, args.input_shape.split(',')))
        output_shape = tuple(map(int, args.output_shape.split(',')))
        
        # Start the server
        server = YOLOv5Server(
            engine_path=args.engine,
            labels_path=args.labels,
            input_shape=input_shape,
            output_shape=output_shape,
            conf_thresh=args.conf_thresh,
            nms_thresh=args.nms_thresh,
            port=args.port,
            show_labels=not args.hide_labels,
            show_conf=not args.hide_conf,
            output_dir=args.output_dir
        )
        
        # Set up a try-finally block for proper resource cleanup
        try:
            # Register a cleanup function to be called on exit to prevent CUDA errors
            import atexit
            def safe_cleanup():
                try:
                    if 'server' in locals() and server:
                        pass
                except Exception:
                    pass
            atexit.register(safe_cleanup)
            
            # Start the server
            print("Starting YOLOv5 TensorRT server...")
            server.start()
        finally:
            # Ensure cleanup happens even if there's an exception
            if 'server' in locals():
                server.cleanup()
        return 0

    except Exception as e:
        print(f"Error starting server: {str(e)}")
        traceback.print_exc()
        return 1

if __name__ == "__main__":
    sys.exit(main())
