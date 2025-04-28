"""
YOLOv5 Detection Comparison Utility

This utility script compares detection results by placing images from two different
folders side by side. It's useful for visually comparing different model versions,
different confidence thresholds, or different post-processing methods.

The script matches images by filename, resizes them to a common height while
maintaining aspect ratio, and creates a combined view for easy comparison.

Usage:
    Run the script directly and follow the prompts to specify input and output folders.
"""

import os
from PIL import Image, ImageDraw, ImageFont

def compare_images(folder1, folder2, save_folder):
    """
    Compare images from two folders by placing them side by side in a single image.
    
    This function takes images from two different folders and creates a combined image
    where the original images are placed side by side with a label at the bottom.
    The images are automatically resized to have the same height while maintaining
    their aspect ratios.
    
    Args:
        folder1 (str): Path to the first folder containing images
        folder2 (str): Path to the second folder containing images
        save_folder (str): Path to the folder where combined images will be saved
        
    Returns:
        None
        
    Note:
        - Only .jpg images are processed
        - Images are matched by filename
        - Images are resized to have the same height while maintaining aspect ratio
        - A label is added at the bottom of the combined image
    """
    if not os.path.exists(save_folder):
        os.makedirs(save_folder)

    # Get sorted lists of .jpg images from both folders
    images1 = sorted([img for img in os.listdir(folder1) if img.lower().endswith('.jpg')])
    images2 = sorted([img for img in os.listdir(folder2) if img.lower().endswith('.jpg')])

    for img_name in images1:
        if img_name in images2:
            img1_path = os.path.join(folder1, img_name)
            img2_path = os.path.join(folder2, img_name)

            # Open both images
            img1 = Image.open(img1_path)
            img2 = Image.open(img2_path)

            # Ensure both images have the same height
            if img1.size[1] != img2.size[1]:
                new_height = min(img1.size[1], img2.size[1])
                img1 = img1.resize((int(img1.size[0] * new_height / img1.size[1]), new_height))
                img2 = img2.resize((int(img2.size[0] * new_height / img2.size[1]), new_height))

            # Create a new image with both images side by side
            combined_width = img1.size[0] + img2.size[0]
            combined_height = img1.size[1]
            combined_img = Image.new('RGB', (combined_width, combined_height), (255, 255, 255))

            # Paste both images side by side
            combined_img.paste(img1, (0, 0))
            combined_img.paste(img2, (img1.size[0], 0))

            # Save the combined image
            save_path = os.path.join(save_folder, img_name)
            combined_img.save(save_path)

if __name__ == "__main__":
    # Get input parameters from user
    folder_1 = input("Enter the path to the first folder (e.g., Detections/yolov5_custom): ")
    folder_2 = input("Enter the path to the second folder (e.g., Detections/yolov5_real): ")
    save_folder = input("Enter the path to the folder where you want to save the combined images: ")

    # Run the comparison function
    compare_images(folder_1, folder_2, save_folder)
    print(f"Images compared and saved in folder: {save_folder}")