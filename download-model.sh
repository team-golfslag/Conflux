#!/bin/bash

set -e

ORIGINAL_DIR=$(pwd)
MODEL_DIR="$ORIGINAL_DIR/Models"
MODEL_FILE="multilingual-e5-small.onnx"
TOKENIZER_FILE="sentencepiece.bpe.model"

MODEL_REPO="intfloat/multilingual-e5-small"

echo "Downloading multilingual-e5-small embedding model for Conflux..."

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
git clone "https://huggingface.co/$MODEL_REPO" multilingual-e5-small
cd multilingual-e5-small

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

TOKENIZER_SOURCE="sentencepiece.bpe.model"
if [ -f "sentencepiece.bpe.model" ]; then
    echo "Found tokenizer: sentencepiece.bpe.model"
    TOKENIZER_SOURCE="sentencepiece.bpe.model"
elif [ -f "onnx/sentencepiece.bpe.model" ]; then
    echo "Found tokenizer: onnx/sentencepiece.bpe.model"
    TOKENIZER_SOURCE="onnx/sentencepiece.bpe.model"
else
    echo "Error: Could not find sentencepiece.bpe.model"
    echo "Available tokenizer files:"
    find . -name "*tokenizer*" -o -name "*vocab*" -type f
    exit 1
fi

# Copy model files to the Models directory
echo "Copying model files..."

echo "Target directory: $MODEL_DIR"

if [ -f "$TEMP_DIR/multilingual-e5-small/$ONNX_FILE" ]; then
    cp "$TEMP_DIR/multilingual-e5-small/$ONNX_FILE" "$MODEL_DIR/$MODEL_FILE"
    echo "Copied ONNX model: $MODEL_FILE"
else
    echo "Error: ONNX model file not found at $TEMP_DIR/multilingual-e5-small/$ONNX_FILE"
    exit 1
fi

if [ -f "$TEMP_DIR/multilingual-e5-small/$TOKENIZER_SOURCE" ]; then
    cp "$TEMP_DIR/multilingual-e5-small/$TOKENIZER_SOURCE" "$MODEL_DIR/$TOKENIZER_FILE"
    echo "Copied tokenizer: $TOKENIZER_FILE"
else
    echo "Error: sentencepiece.bpe.model file not found at $TOKENIZER_SOURCE"
    echo "Available files in directory:"
    ls -la "$TEMP_DIR/multilingual-e5-small/"
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
