#!/bin/bash

set -e

ORIGINAL_DIR=$(pwd)
MODEL_DIR="$ORIGINAL_DIR/Models"
MODEL_FILE="all-MiniLM-L12-v2.onnx"
TOKENIZER_FILE="vocab.txt"

MODEL_REPO="sentence-transformers/all-MiniLM-L12-v2"

echo "Downloading all-MiniLM-L12-v2 embedding model for Conflux..."

if [ ! -d "$MODEL_DIR" ]; then
    echo "Creating Models directory..."
    mkdir -p "$MODEL_DIR"
fi

if ! command -v git-lfs &> /dev/null; then
    echo "Error: git-lfs is required to download the model files."
    echo "Please install git-lfs first"
    exit 1
fi

# Initialize git-lfs if not already done
git lfs install

# Check if model already exists
if [ -f "$MODEL_DIR/$MODEL_FILE" ] && [ -f "$MODEL_DIR/$TOKENIZER_FILE" ]; then
    echo "Model files already exist in $MODEL_DIR/"
    echo "   - $MODEL_FILE"
    echo "   - $TOKENIZER_FILE"
    echo ""
    
    # Skip interactive prompt in CI environments
    if [ "$CI" = "true" ]; then
        echo "CI environment detected, re-downloading models..."
    else
        read -p "Do you want to re-download? (y/N): " -n 1 -r
        echo ""
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            echo "Skipping download."
            exit 0
        fi
    fi
fi

echo "Downloading model files from HuggingFace..."

# Create temporary directory for cloning
TEMP_DIR=$(mktemp -d)
echo "Using temporary directory: $TEMP_DIR"

cd "$TEMP_DIR"

echo "Cloning HuggingFace repository..."
git clone "https://huggingface.co/$MODEL_REPO" all-MiniLM-L12-v2
cd all-MiniLM-L12-v2

echo "Repository contents:"
ls -la
echo ""
echo "ONNX directory contents:"
ls -la onnx/ 2>/dev/null || echo "No onnx directory found"

# Look for the ONNX model file (based on the repo structure you provided)
ONNX_FILE=""
if [ -f "onnx/model.onnx" ]; then
    ONNX_FILE="onnx/model.onnx"
    echo "Found ONNX model: $ONNX_FILE"
elif [ -f "onnx/model_O4.onnx" ]; then
    ONNX_FILE="onnx/model_O4.onnx"
    echo "Found optimized ONNX model: $ONNX_FILE"
elif [ -f "model.onnx" ]; then
    ONNX_FILE="model.onnx"
    echo "Found ONNX model: $ONNX_FILE"
else
    echo "Error: Could not find ONNX model file in the repository."
    echo "Available ONNX files:"
    find . -name "*.onnx" -type f
    echo ""
    echo "Available directories:"
    find . -type d -name "*onnx*" -o -name "*model*"
    exit 1
fi

TOKENIZER_SOURCE="vocab.txt"
if [ -f "vocab.txt" ]; then
    echo "Found tokenizer: vocab.txt"
    TOKENIZER_SOURCE="vocab.txt"
elif [ -f "onnx/vocab.txt" ]; then
    echo "Found tokenizer: onnx/vocab.txt"
    TOKENIZER_SOURCE="onnx/vocab.txt"
else
    echo "Error: Could not find vocab.txt"
    echo "Available tokenizer files:"
    find . -name "*tokenizer*" -o -name "*vocab*" -type f
    exit 1
fi

# Copy model files to the Models directory
echo "Copying model files..."

echo "Target directory: $MODEL_DIR"

if [ -f "$TEMP_DIR/all-MiniLM-L12-v2/$ONNX_FILE" ]; then
    cp "$TEMP_DIR/all-MiniLM-L12-v2/$ONNX_FILE" "$MODEL_DIR/$MODEL_FILE"
    echo "Copied ONNX model: $MODEL_FILE"
else
    echo "Error: ONNX model file not found at $TEMP_DIR/all-MiniLM-L12-v2/$ONNX_FILE"
    exit 1
fi

if [ -f "$TEMP_DIR/all-MiniLM-L12-v2/$TOKENIZER_SOURCE" ]; then
    cp "$TEMP_DIR/all-MiniLM-L12-v2/$TOKENIZER_SOURCE" "$MODEL_DIR/$TOKENIZER_FILE"
    echo "Copied tokenizer: $TOKENIZER_FILE"
else
    echo "Error: vocab.txt file not found at $TOKENIZER_SOURCE"
    echo "Available files in directory:"
    ls -la "$TEMP_DIR/all-MiniLM-L12-v2/"
    exit 1
fi

# Clean up temporary directory
echo "Cleaning up temporary files..."
rm -rf "$TEMP_DIR"

# Verify files were copied successfully
echo ""
echo "Model download completed successfully!"
echo "Model files are now available in $MODEL_DIR/:"
ls -lh "$MODEL_DIR/"
