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
from queue import Queue
from pathlib import Path

class TRTInferenceYolov5:
    """
    TRTInferenceYolov5 is a class for performing YOLOv5 object detection using TensorRT.

    This class loads a TensorRT engine once and keeps it in memory for multiple inference requests.
    It supports both single image inference and folder inference.
    """

    def __init__(self, engine_file_path, class_label_file, 
                 input_shape, output_shape, conf_th, score_th, nms_th, 
                 is_fp16=False, batch_size=1):
        """
        Initialize the inference engine with model parameters.

    Args:
        engine_file_path (str): Path to the TensorRT engine file.
        class_label_file (str): Path to the .yaml file containing class labels.
        input_shape (tuple): Shape of the input tensor.
        output_shape (tuple): Shape of the output tensor.
        conf_th (float): Confidence threshold for detections.
        score_th (float): Score threshold for detections.
        nms_th (float): Non-maximum suppression threshold for detections.
        is_fp16 (bool, optional): Whether the engine is FP16. Defaults to False.
        batch_size (int, optional): Batch size for inference. Defaults to 1.
    """
        # Create logger
        self.logger = trt.Logger(trt.Logger.WARNING)

        # Define engine parameters
        self.engine_file_path = engine_file_path
        self.is_fp16 = is_fp16
        self.batch_size = batch_size
        
        print(f"Loading TensorRT engine from {engine_file_path}")
        self.engine = self.load_engine()
        print(f"{'FP16' if is_fp16 else 'FP32'} TensorRT engine loaded successfully")
        
        self.context = self.engine.create_execution_context()

        # Get information about engine bindings
        self.get_engine_info()

        # Define class labels
        self.class_label_file = class_label_file
        with open(class_label_file, "r") as f:
            data = yaml.safe_load(f)
            self.class_labels = [name for name in data['names'].values()]
            print(f"Loaded {len(self.class_labels)} classes from {class_label_file}")

        # Define image input shape, output shape and labels
        self.input_shape = input_shape
        self.output_shape = output_shape

        # Define thresholds for detection
        self.conf_th = conf_th
        self.score_th = score_th
        self.nms_th = nms_th

        # Initialize counters
        self.reset_counters()

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
                print(f"Engine has {self.num_bindings} I/O tensors (using num_io_tensors)")
            except AttributeError:
                # Try legacy API
                try:
                    self.num_bindings = self.engine.num_bindings
                    print(f"Engine has {self.num_bindings} bindings (using num_bindings)")
                except:
                    self.num_bindings = 2  # Default for YOLOv5
                    print(f"Could not determine binding count, assuming {self.num_bindings}")
            
            # Set input dtype based on precision
            self.input_dtype = np.float16 if self.is_fp16 else np.float32
            
        except Exception as e:
            print(f"Warning: Error during engine info gathering: {e}")
            print(f"Using default settings for YOLOv5 with {'FP16' if self.is_fp16 else 'FP32'} precision")
            self.num_bindings = 2
            self.input_dtype = np.float16 if self.is_fp16 else np.float32

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
        resized_img = cv2.resize(img, (self.input_shape[2], self.input_shape[3]), 
                                interpolation=cv2.INTER_AREA)

        # Get the shape of resized image for later use
        self.resized_img_w, self.resized_img_h = resized_img.shape[:2]

        # Convert to numpy img and normalize
        np_img = np.array(resized_img).astype(np.float32) / 255.0

        # Transpose to NCHW format (batch, channels, height, width)
        np_img = np.transpose(np_img, (2, 0, 1))

        # Expand dimensions to match input shape
        np_img = np.expand_dims(np_img, axis=0)

        # Convert to FP16 if using FP16 engine
        if self.is_fp16:
            np_img = np_img.astype(np.float16)

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

    def infer_single_image(self, image_path, save_dir_path):
        """
        Run inference on a single image.
        
        Args:
            image_path (str): Path to the image file.
            save_dir_path (str): Directory to save the output image.
            
        Returns:
            dict: Dictionary containing detection results and paths.
        """
        os.makedirs(save_dir_path, exist_ok=True)
        
        # Reset counters for this inference
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

            # Run inference
            try:
                bindings = [int(d_input), int(d_output)]
                self.context.execute_v2(bindings)
            except (AttributeError, TypeError) as e:
                print(f"Warning: execute_v2 failed ({e}), trying legacy execute method")
                bindings = [int(d_input), int(d_output)]
                self.context.execute(batch_size=self.batch_size, bindings=bindings)

            # Copy results back to host
            cuda.memcpy_dtoh(output, d_output)

            # Free GPU memory
            d_input.free()
            d_output.free()

            # Record inference time
            self.inference_time = time.time() - inference_start
            self.total_time += self.inference_time

            # Postprocess the result
            output_path = os.path.join(save_dir_path, os.path.basename(image_path))
            detections = self.postprocess_image(image_path, output, original_dimensions, output_path)
            
            # Generate metrics
            metrics = self.generate_metrics()
            
            # Save logs if detections exist
            log_file = os.path.join(save_dir_path, "inference_logs.txt")
            self.save_logs(metrics, log_file)
            
            return {
                "success": True,
                "input_path": image_path,
                "output_path": output_path,
                "metrics": metrics,
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

                # Run inference
                try:
                    bindings = [int(d_input), int(d_output)]
                    self.context.execute_v2(bindings)
                except (AttributeError, TypeError) as e:
                    print(f"Warning: execute_v2 failed ({e}), trying legacy execute method")
                    bindings = [int(d_input), int(d_output)]
                    self.context.execute(batch_size=self.batch_size, bindings=bindings)

                # Copy results back to host
                cuda.memcpy_dtoh(output, d_output)

                # Free GPU memory
                d_input.free()
                d_output.free()

                # Record inference time
                frame_inference_time = time.time() - inference_start
                self.inference_time += frame_inference_time
                self.total_time += frame_inference_time

                # Postprocess the result
                output_path = os.path.join(save_dir_path, os.path.basename(image_path))
                detections = self.postprocess_image(image_path, output, original_dimensions, output_path)
                
                # Add to results
                results.append({
                    "success": True,
                    "input_path": image_path,
                    "output_path": output_path,
                    "detections": detections
                })
                
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
        
        print("\nFolder processing complete")
        
        # Generate metrics
        metrics = self.generate_metrics()
        
        # Save logs
        log_file = os.path.join(save_dir_path, "inference_logs.txt")
        self.save_logs(metrics, log_file)
        
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
        # Convert output to float32 for consistent handling
        if self.is_fp16:
            yolov5_output = yolov5_output.astype(np.float32)

        # List to store detection results
        detections = []

        # Check for NaN values in the output
        if np.isnan(yolov5_output).any():
            print(f"Warning: NaN values detected in model output for {image_path}")
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

            if class_score[class_idx] < self.score_th:
                continue

            cx, cy, w, h = float(detect[0]), float(detect[1]), float(detect[2]), float(detect[3])
            
            # Check for invalid values
            if not all(map(lambda x: math.isfinite(x), [cx, cy, w, h])):
                continue
                
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
                
                # Store detection info
                detection = {
                    "class": class_name,
                    "confidence": float(confidences[i]),
                    "bbox": [
                        int(top_left_x),
                        int(top_left_y),
                        int(width_scaled),
                        int(height_scaled)
                    ]
                }
                detections.append(detection)
                
                # Choose label based on confidence
                label = f"{class_name}, conf: {confidences[i]:.2f}"
                color = (0, 255, 0)  # Green for detections

                # Make rectangle with appropriate color
                cv2.rectangle(image, (top_left_x, top_left_y), 
                              (top_left_x+width_scaled, top_left_y+height_scaled), color, 3)

                # Put text with background for better visibility
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

    def save_logs(self, metrics, log_file):
        """
        Saves the inference logs to a file.
        
        Args:
            metrics (dict): The performance metrics to log.
            log_file (str): Path to save the log file.
        """
        if not metrics:
            return
        
        with open(log_file, "w") as f:
            f.write(f"TensorRT YOLOv5 {'FP16' if self.is_fp16 else 'FP32'} Inference Logs\n")
            f.write("=" * 60 + "\n")
            f.write(f"Model Used: YOLOv5 {'FP16' if self.is_fp16 else 'FP32'}\n")
            f.write(f"Confidence Threshold: {self.conf_th}\n")
            f.write(f"Score Threshold: {self.score_th}\n")
            f.write(f"NMS Threshold: {self.nms_th}\n")
            f.write("=" * 60 + "\n")
            f.write(f"Processed {metrics['num_frames']} images\n")
            f.write(f"Images with invalid outputs: {metrics['invalid_outputs']}\n")
            f.write(f"Total time (Preprocessing + Inference + Postprocessing): {metrics['total_time']:.3f}s\n")   
            f.write(f"Inference time: {metrics['inference_time']:.3f}s\n")
            f.write(f"Average FPS: {metrics['average_fps']:.3f}\n")
            f.write(f"Average inference time per image: {metrics['average_inference_time'] * 1000:.2f}ms\n")
            f.write("=" * 60 + "\n")
            f.write(f"Total objects detected: {metrics['total_objects']}\n")
            f.write("\nDetections by class:\n")
            for class_name, count in metrics['objects_by_class'].items():
                f.write(f"    {class_name}: {count}\n")
        
        print(f"Logs saved to {log_file}")
        
class YOLOv5Server:
    """
    A server that loads a YOLOv5 TensorRT model once and processes multiple inference requests.
    
    This server loads the model into memory at startup and keeps it loaded,
    which eliminates the overhead of reloading the model for each inference request.
    This approach significantly improves performance for repeated inference calls.
    """
    
    def __init__(self, engine_path, labels_path, input_shape, output_shape, 
                 conf_thresh, score_thresh, nms_thresh, is_fp16, port=5000):
        """
        Initialize the YOLOv5 inference server.
        
        Args:
            engine_path (str): Path to the TensorRT engine file.
            labels_path (str): Path to the YAML file containing class labels.
            input_shape (tuple): Shape of the input tensor.
            output_shape (tuple): Shape of the output tensor.
            conf_thresh (float): Confidence threshold for detections.
            score_thresh (float): Score threshold for detections.
            nms_thresh (float): NMS threshold for detections.
            is_fp16 (bool): Whether to use FP16 precision.
            port (int, optional): Port to listen on for commands. Defaults to 5000.
        """
        # Initialize the inference engine
        self.inferencer = TRTInferenceYolov5(
            engine_file_path=engine_path,
            class_label_file=labels_path,
            input_shape=input_shape,
            output_shape=output_shape,
            conf_th=conf_thresh,
            score_th=score_thresh,
            nms_th=nms_thresh,
            is_fp16=is_fp16
        )
        
        self.port = port
        self.running = False
        self.command_queue = Queue()
        self.result_queue = Queue()
        
    def start(self):
        """
        Start the server and listen for commands.
        
        This method initializes the server and starts processing commands.
        It sets up a separate thread for command processing and handles
        signal interrupts for clean shutdown.
        """
        self.running = True
        self.command_thread = threading.Thread(target=self._command_loop)
        self.command_thread.daemon = True
        self.command_thread.start()
        
        # Set up signal handler for clean shutdown
        signal.signal(signal.SIGINT, self._handle_sigint)
        
        # Start processing commands
        print(f"YOLOv5 TensorRT server started and ready for inference requests")
        try:
            self._process_commands()
        except KeyboardInterrupt:
            print("Received keyboard interrupt. Shutting down...")
        finally:
            self.running = False
            print("Server shutting down...")
    
    def _handle_sigint(self, sig, frame):
        """
        Handle SIGINT (Ctrl+C) to shut down gracefully.
        
        Args:
            sig: Signal number
            frame: Current stack frame
        """
        print("Received SIGINT. Shutting down...")
        self.running = False
    
    def _command_loop(self):
        """
        Listen for commands on a socket.
        
        In this implementation, we're using command-line arguments instead
        of socket communication for simplicity and easier C# integration.
        """
        pass
        
    def _process_commands(self):
        """
        Process commands from command-line arguments.
        
        This method reads the command-line arguments and dispatches
        to the appropriate handler based on the command type.
        """
        command = None
        while self.running:
            try:
                # Process command-line arguments directly
                if len(sys.argv) < 2:
                    print("No command provided. Use 'image', 'folder', or 'quit'.")
                    return
                
                command = sys.argv[1].lower()
                
                if command == "image":
                    self._handle_image_command()
                elif command == "folder":
                    self._handle_folder_command()
                elif command == "quit":
                    print("Received quit command")
                    self.running = False
                else:
                    print(f"Unknown command: {command}")
                
                # Exit after handling a single command
                # (C# will re-launch with new arguments for each command)
                return
                
            except Exception as e:
                print(f"Error processing command: {str(e)}")
                traceback.print_exc()
                
    def _handle_image_command(self):
        """
        Handle a command to process a single image.
        
        This method parses arguments for image inference and calls
        the TRTInferenceYolov5 class to process the image.
        """
        parser = argparse.ArgumentParser(description='Process a single image')
        parser.add_argument('--image', required=True, help='Path to the image file')
        parser.add_argument('--output', required=True, help='Path to save the output image')
        parser.add_argument('--json_output', help='Path to save detection results as JSON')
        
        # Parse arguments starting from index 2 (after 'image' command)
        args = parser.parse_args(sys.argv[2:])
        
        # Run inference
        result = self.inferencer.infer_single_image(args.image, os.path.dirname(args.output))
        
        # Save JSON output if requested
        if args.json_output:
            with open(args.json_output, 'w') as f:
                json.dump(result, f, indent=2)
        
        # Print summary
        if result["success"]:
            print(f"Successfully processed image: {args.image}")
            print(f"Output saved to: {result['output_path']}")
            print(f"Detected {result['metrics']['total_objects']} objects")
        else:
            print(f"Failed to process image: {args.image}")
            print(f"Error: {result.get('error', 'Unknown error')}")
    
    def _handle_folder_command(self):
        """
        Handle a command to process a folder of images.
        
        This method parses arguments for folder inference and calls
        the TRTInferenceYolov5 class to process all images in the folder.
        """
        parser = argparse.ArgumentParser(description='Process a folder of images')
        parser.add_argument('--folder', required=True, help='Path to the folder containing images')
        parser.add_argument('--output', required=True, help='Path to save the output images')
        parser.add_argument('--json_output', help='Path to save detection results as JSON')
        
        # Parse arguments starting from index 2 (after 'folder' command)
        args = parser.parse_args(sys.argv[2:])
        
        # Run inference
        result = self.inferencer.infer_folder(args.folder, args.output)
        
        # Save JSON output if requested
        if args.json_output:
            with open(args.json_output, 'w') as f:
                json.dump(result, f, indent=2)
        
        # Print summary
        if result["success"]:
            print(f"Successfully processed folder: {args.folder}")
            print(f"Outputs saved to: {args.output}")
            print(f"Processed {result['metrics']['num_frames']} images")
            print(f"Detected {result['metrics']['total_objects']} objects")
        else:
            print(f"Failed to process folder: {args.folder}")
            print(f"Error: {result.get('error', 'Unknown error')}")
        
def main():
    """
    Main entry point for the YOLOv5 TensorRT Inference Server.
    
    This function parses command-line arguments and dispatches to the appropriate
    handler based on the requested mode (server, image, folder, quit).
    """
    # Set up command line argument parser
    parser = argparse.ArgumentParser(description='YOLOv5 TensorRT Inference Server')
    
    # Create subparsers for different modes
    subparsers = parser.add_subparsers(dest='mode', help='Server mode')
    
    # Server initialization parser
    server_parser = subparsers.add_parser('server', help='Start the inference server')
    server_parser.add_argument('--engine', required=True, type=str,
                        help='Path to TensorRT engine file')
    server_parser.add_argument('--labels', required=True, type=str,
                        help='Path to YAML file containing class labels')
    server_parser.add_argument('--input_shape', type=str, default='1,3,640,640',
                        help='Input shape as comma-separated values (default: 1,3,640,640)')
    server_parser.add_argument('--output_shape', type=str, default='1,25200,6',
                        help='Output shape as comma-separated values (default: 1,25200,6)')
    server_parser.add_argument('--conf_thresh', type=float, default=0.25,
                        help='Confidence threshold (default: 0.25)')
    server_parser.add_argument('--score_thresh', type=float, default=0.25,
                        help='Score threshold (default: 0.25)')
    server_parser.add_argument('--nms_thresh', type=float, default=0.45,
                        help='NMS threshold (default: 0.45)')
    server_parser.add_argument('--fp16', action='store_true',
                        help='Use FP16 precision (default: False)')
    server_parser.add_argument('--port', type=int, default=5000,
                        help='Port for communication (default: 5000)')
    
    # Image inference parser
    image_parser = subparsers.add_parser('image', help='Run inference on a single image')
    image_parser.add_argument('--image', required=True, type=str,
                       help='Path to image file')
    image_parser.add_argument('--output', required=True, type=str,
                       help='Path to save output image')
    image_parser.add_argument('--json_output', type=str,
                       help='Path to save JSON output (optional)')
                       
    # Folder inference parser
    folder_parser = subparsers.add_parser('folder', help='Run inference on a folder of images')
    folder_parser.add_argument('--folder', required=True, type=str,
                        help='Path to folder containing images')
    folder_parser.add_argument('--output', required=True, type=str,
                        help='Path to save output images')
    folder_parser.add_argument('--json_output', type=str,
                        help='Path to save JSON output (optional)')
    
    # Quit command parser
    quit_parser = subparsers.add_parser('quit', help='Quit the server')
    
    # Parse arguments
    args = parser.parse_args()
    
    if args.mode == 'server':
        # Convert string shapes to tuples
        input_shape = tuple(map(int, args.input_shape.split(',')))
        output_shape = tuple(map(int, args.output_shape.split(',')))
        
        try:
            # Start the server
            server = YOLOv5Server(
                engine_path=args.engine,
                labels_path=args.labels,
                input_shape=input_shape,
                output_shape=output_shape,
                conf_thresh=args.conf_thresh,
                score_thresh=args.score_thresh,
                nms_thresh=args.nms_thresh,
                is_fp16=args.fp16,
                port=args.port
            )
            server.start()
            return 0
        except Exception as e:
            print(f"Error starting server: {str(e)}")
            traceback.print_exc()
            return 1
    elif args.mode in ['image', 'folder', 'quit']:
        # These modes are handled by the YOLOv5Server class
        # and are already in sys.argv
        print(f"Forwarding {args.mode} command to server")
        return 0
    else:
        parser.print_help()
        return 1

if __name__ == "__main__":
    sys.exit(main())
