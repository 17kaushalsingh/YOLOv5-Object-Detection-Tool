# Get Anaconda Dependencies

Activate your conda environment
```sh
conda activate yolov5
```

1. List packages in readable table format
```sh
conda list > requirements_conda.txt
```

2. Export packages in simplified format (for rebuilding the env)
```sh
conda list --export >environment_export.txt
```

3. Export exact package URLs (for exact reproduction)
```sh
conda list --explicit> environment_explicit.txt
```

4. Export to a YAML file (recommended for sharing)
```sh
conda env export > environment.yml
```
