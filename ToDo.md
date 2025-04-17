# TO DOs for YOLOv5-Object-Detection-Tool

## Issues in Current Implementation
    1. Environment setup is not handled, due to whcih
        the app will most likely fail on a machine without cmd and required dependencies
    2. The inferences are run on one image at a time, this means, for each file
        a separate command is generated, and a new inference session starts, this leads to
        slower processing and unnecessary manaul runs

## Suggested Changes
### UI Related
    1. Add a separate selection panel for folders
    2. Have a single picture box and label in the image panel
        Add a toggle to switch between input and output 

### Add Functionality for Folder Inferences
    1. Add custom code for YOLOv5 inference
        + Load the model on app launch, keep it loaded until app exit
          Run inferences image by image
          Since the model is loaded, the inferences will be faster
        + Custom code to run inferences on entire folder in one go
          Add functionality to save cordinates and other detection information in multiple formats

### 

## Future Tasks
    1. Craete a docker image to run the application
    2. Create a one click installer to run the application