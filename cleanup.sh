#!/bin/bash

# Ищем и удаляем папки bin и obj в текущей директории и всех поддиректориях
find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} +
find . -type f -name "*.csproj.user" -exec rm -f {} +

echo "Cleanup completed!"