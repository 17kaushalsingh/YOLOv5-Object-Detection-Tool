# 🧭 How to Bundle Python Environment with conda-pack

## Get Anaconda Dependencies
### Activate your conda environment
```sh
conda activate yolov5
```

### List packages in readable table format
```sh
conda list > requirements_conda.txt
```

### Export packages in simplified format (for rebuilding the env)
```sh
conda list --export >environment_export.txt
```

### Export exact package URLs (for exact reproduction)
```sh
conda list --explicit> environment_explicit.txt
```

### Export to a YAML file (recommended for sharing)
```sh
conda env export > environment.yml
```

## 🔹 Step-by-Step Overview to Add YOLOv5 Environment to YOLOv5-Object-Detection-Tool
1. Install and Set Up Conda Environment
2. Install Required Dependencies (YOLOv5, torch, etc.)
3. Install conda-pack
4. Pack the Environment
5. Move/Unpack the Environment on Target System
6. Modify C# App to Call Python from Packed Environment
7. Test End-to-End on a Fresh System

## ✅ Step 1: Install conda-pack and Pack Your Conda Environment
### 🧰 Install conda-pack (if not already installed)

Activate your yolov5 environment first:

```
conda activate yolov5
```

Then install conda-pack:

```
conda install -c conda-forge conda-pack
```
If you want to install it globally (outside env), you can run:
```
conda install -n base -c conda-forge conda-pack
```

### 📦 Pack the Environment
Once installed, run the following command outside the activated env (from base or cmd):
```
conda-pack -n yolov5 -o yolov5_env.tar.gz
```
-n yolov5: name of your conda environment
-o yolov5_env.tar.gz: output tarball containing the full packed environment

### 📁 Unpack the Environment (for testing or deployment)
Pick any folder to extract it:
```
mkdir yolov5_env
tar -xzf yolov5_env.tar.gz -C yolov5_env
```
You’ll now have a fully self-contained Python environment in yolov5_env/.

### ⚠️ Fix Activation Scripts (Change hardcoded absolute locations to relative locations)
To make it portable, run this inside the unpacked folder:
```
./yolov5_env/bin/conda-unpack      # On Linux/macOS
yolov5_env/Scripts/conda-unpack.exe  # On Windows
```
This updates hardcoded paths inside the environment to match the new location.

### ✅ Extract yolov5_env in Application Directory on First Use or When Dependencies are not available
Handled in [YoloInitialSetup.cs](../YoloInitialSetup.cs/)