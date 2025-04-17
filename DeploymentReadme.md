# Deployment Guide for YOLOv5 Object Detection Tool

This guide will help you prepare the application for end-user deployment by properly embedding the required dependencies.

## Prerequisites

- Visual Studio (or your preferred C# IDE)
- YOLOv5 repository
- Model files (`.pt`, `.onnx`, or `.engine` formats)
- YAML data files

## Step 1: Embed the YOLOv5 Repository

1. Clone or download the YOLOv5 repository from GitHub: https://github.com/ultralytics/yolov5

2. Create a ZIP file of the YOLOv5 repository:
   - Open the YOLOv5 folder
   - Select all files and folders
   - Right-click and select "Send to" > "Compressed (zipped) folder"
   - Name the ZIP file `yolov5.zip`

3. Add the ZIP file to your project as an embedded resource:
   - In Visual Studio, right-click on your project in Solution Explorer
   - Select "Add" > "Existing Item..."
   - Browse to and select the `yolov5.zip` file
   - After adding, select the file in Solution Explorer
   - In the Properties window, change "Build Action" to "Embedded Resource"

## Step 2: Embed Model and YAML Files

1. Gather your model files and YAML files:
   - Model files: `.pt`, `.onnx`, or `.engine` files
   - YAML data files (e.g., `petris_data.yaml`)

2. Add these files to your project as embedded resources:
   - In Visual Studio, right-click on your project in Solution Explorer
   - Select "Add" > "Existing Item..."
   - Browse to and select all model and YAML files
   - After adding, select each file in Solution Explorer
   - In the Properties window, change "Build Action" to "Embedded Resource"

## Step 3: Update Project File

For larger files like YOLOv5 and model files, you might need to increase the maximum file size limit for embedded resources in your `.csproj` file:

1. Right-click on your project in Solution Explorer and select "Edit Project File"
2. Add the following property group:

```xml
<PropertyGroup>
  <GenerateResourceUsePreserializedResources>true</GenerateResourceUsePreserializedResources>
  <EmbeddedResourceUseDependentUponConvention>false</EmbeddedResourceUseDependentUponConvention>
</PropertyGroup>
```

## Step 4: Add Required Packages

Make sure your project includes the necessary NuGet packages:

1. In Visual Studio, right-click on your project in Solution Explorer
2. Select "Manage NuGet Packages..."
3. Search for and install:
   - `System.IO.Compression`
   - `System.IO.Compression.ZipFile`

## Step 5: Build and Test

1. Build your project in Release mode
2. Test the application to ensure it correctly:
   - Extracts the YOLOv5 repository if not present
   - Extracts model files if not present
   - Successfully runs object detection

## Troubleshooting

### Common Issues

1. **Resource Not Found**: Ensure the namespace for embedded resources matches your project namespace
   - Check the full resource names by adding this code temporarily:
     ```csharp
     foreach (var resource in Assembly.GetExecutingAssembly().GetManifestResourceNames())
     {
         Console.WriteLine(resource);
     }
     ```

2. **File Too Large**: Visual Studio might have issues with large embedded resources
   - Consider splitting large files or using an alternative approach like downloading from a server on first run

3. **Python Environment Issues**: Ensure the target machine has Python and the necessary packages installed
   - Consider creating a Python virtual environment installation script as part of your deployment

## Additional Deployment Considerations

### Environment Setup

Create a batch file or PowerShell script that sets up the Python environment required for YOLOv5:

```batch
@echo off
REM Install Miniconda if needed
REM Create and configure the YOLOv5 environment

call conda create -n yolov5 python=3.8 -y
call activate yolov5
pip install -r %~dp0\yolov5\requirements.txt

echo YOLOv5 environment setup complete
```

Include this script with your application and run it during the first launch if needed.

### Simplified Distribution

For easier distribution, consider creating an installer using a tool like Inno Setup or NSIS that includes:
- Your application
- The YOLOv5 repository
- Model files
- Python/Conda environment setup scripts

This approach makes deployment smoother for end users who may not have the necessary technical expertise. 