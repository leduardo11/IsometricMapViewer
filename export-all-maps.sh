#!/bin/bash

# Export all .amd maps to BudgetDungeon format
# Usage: ./export-all-maps.sh [output-directory]

OUTPUT_DIR="${1:-/home/leduardo/exported-maps}"
MAPS_DIR="resources/maps"

echo "╔════════════════════════════════════════════╗"
echo "║  BudgetDungeon Map Exporter                ║"
echo "╚════════════════════════════════════════════╝"
echo ""
echo "Exporting from: $MAPS_DIR"
echo "Output to: $OUTPUT_DIR"
echo ""

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Counter
count=0
success=0

# Loop through all .amd files
for map_file in "$MAPS_DIR"/*.amd; do
    if [ -f "$map_file" ]; then
        map_name=$(basename "$map_file" .amd)
        
        echo "[$((count + 1))] Exporting: $map_name"
        
        # Run the exporter
        if dotnet run -- --export "$map_name" "$OUTPUT_DIR" 2>&1 | grep -q "✓ Exported"; then
            ((success++))
            echo "    ✓ Success"
        else
            echo "    ✗ Failed"
        fi
        
        ((count++))
        echo ""
    fi
done

echo "════════════════════════════════════════════"
echo "Exported $success/$count maps successfully"
echo "Output directory: $OUTPUT_DIR"
