# TO DOs for YOLOv5-Object-Detection-Tool

## ~~Issues in Current Implementation~~
1. ~~Environment setup is not handled, due to whcih the app will most likely fail on a machine without cmd and required dependencies~~
2. ~~The inferences are run on one image at a time, this means, for each file a separate command is generated, and a new inference session starts, this leads to slower processing and unnecessary manaul runs for each image~~

## ~~Add Functionality for Folder Inferences and Customn Code~~
1. ~~Add UI to handle folder detection~~
    + ~~Add a separate selection panel for folders~~
    + ~~Add previous and next button for image navigation in folders~~

2. ~~Add custom code for YOLOv5 inference~~
    + ~~Write code to handle command line arguments~~
    + ~~Load the model on app launch, keep it loaded until app exit~~
    + ~~Create separate code for folders and single images~~
    + ~~Run inferences image by image, while keeping the model loaded~~
    + ~~Run inferences on entire folder in one go, while keeping the model loaded~~
    + ~~Add functionality to save cordinates and other detection information in multiple formats~~
    + ~~Add code for cpu only devices~~
    + ~~Modify yolov5/detect.py to work in server mode~~

## Future Tasks
1. ~~Automatic environment setup~~
2. ~~Add code for FP16 inferences~~
3. ~~Add delay after starting the server to ensure detection happens smoothly to check server readiness~~
4. ~~Remove user defined flag to enable/disable GPU/CUDA~~
5. ~~Add a messaage box when quitting the server to indicate the location of saved results~~
6. ~~Enable FP16 automatically~~
7. Documentation for Github
8. Remove temporary folder (for image handling) related code
8. Minimize the python environment size as much as possible
9. Create a one click installer to run the application
10. Create a docker image to run the application in containerized manner