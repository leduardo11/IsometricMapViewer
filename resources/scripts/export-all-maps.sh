#!/bin/bash

# Export all .amd maps to BudgetDungeon format
# Usage: ./export-all-maps.sh [output-directory]

OUTPUT_DIR="${1:-/home/leduardo/exported-maps}"
MAPS_DIR="resources/maps"

echo "Exporting all maps from $MAPS_DIR to $OUTPUT_DIR"
echo "================================================"

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Counter
count=0

# Loop through all .amd files
for map_file in "$MAPS_DIR"/*.amd; do
    if [ -f "$map_file" ]; then
        # Extract map name without extension
        map_name=$(basename "$map_file" .amd)
        
        echo ""
        echo "Exporting: $map_name"
        echo "---"
        
        # Run the exporter
        dotnet run -- --export-budget-dungeon "$map_name" "$OUTPUT_DIR"
        
        ((count++))
    fi
done

echo ""
echo "================================================"
echo "Exported $count maps to: $OUTPUT_DIR"
echo ""
echo "To also generate PNG images, use GUI mode:"
echo "  1. Run: dotnet run"
echo "  2. Press Ctrl+B for each map"
