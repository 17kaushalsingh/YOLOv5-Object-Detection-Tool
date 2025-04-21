import gdown
import os

# List of Google Drive file URLs and their corresponding output filenames
files = [
    ("https://drive.google.com/uc?id=199GTyTxzaTxSp6QhKvgdIbYYgs9LpD7t", "petris_yolov5x.pt"),
    ("https://drive.google.com/uc?id=1qx567W8z4xtbN3X9JhmRZiMmEBLb7f0T", "petris_yolov5x_fp16.onnx"),
    ("https://drive.google.com/uc?id=1PsMTw9vmfGM7j5LLFFiZadvA7_rBWhN0", "petris_yolov5x_fp32.onnx"),
    ("https://drive.google.com/uc?id=1KNQwq29hXc4nyMnTsmccHBOGq4hsTdfO", "petris_yolov5x_fp16.engine"),
    ("https://drive.google.com/uc?id=1CIql-aBZStBnAjMO6jrzQnri_WhoeTS6", "petris_yolov5x_fp32.engine"),
    ("https://drive.google.com/uc?id=1580AgoYQuoL2BKhLW0fB8fQAibn9YffK", "petris_data.yaml"),
]

# Download each file
for url, output in files:
    if not os.path.exists(output):
        print(f"Downloading {output}  ...")
        gdown.download(url, output, quiet=False)
        print()
    else:
        print(f"{output} already exists. Skipping download.")
        print()