

+---------------------+          PIPE/STDIN/STDOUT         +----------------------------+
|                     |----------------------------------->|                            |
|     C# UI Client    |                                    |  Python YOLOv5 Server      |
|  (WinForms - Form1) |                                    |  (runs server.py script)   |
|                     |<-----------------------------------|                            |
+---------------------+       Detection Results/Logs       +----------------------------+
        |
        | Calls into
        v
+-----------------------+    Starts/Stops server    +---------------------------------+
|  YoloDetectionService |<------------------------->|  Python Environment (venv)      |
| (C# bridge & manager) |                           |  with YOLOv5 + dependencies     |
+-----------------------+                           +---------------------------------+





