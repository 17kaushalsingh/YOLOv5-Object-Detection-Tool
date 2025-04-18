# TO DOs for YOLOv5-Object-Detection-Tool

## Issues in Current Implementation
1. Environment setup is not handled, due to whcih <br>
    the app will most likely fail on a machine without cmd and required dependencies
2. The inferences are run on one image at a time, this means, for each file <br>
    a separate command is generated, and a new inference session starts, this leads to <br>
    slower processing and unnecessary manaul runs for each image

## Add Functionality for Folder Inferences and Customn Code
1. Add UI to handle folder detection
    + Add a separate selection panel for folders
    + Add previous and next button for image navigation in folders

2. Add custom code for YOLOv5 inference
    + Write code to handle command line arguments
    + Load the model on app launch, keep it loaded until app exit
    + Create separate code for folders and single images
    + Run inferences image by image, while keeping the model loaded
    + Run inferences on entire folder in one go, while keeping the model loaded
    + Add functionality to save cordinates and other detection information in multiple formats

## Future Tasks
1. Automatic environment setup
    + Craete a docker image to run the application in containerized manner
2. Create a one click installer to run the application