#!/bin/bash

# Analyze exported map JSON to show tile statistics
# Usage: ./analyze-map.sh <json-file>

if [ $# -eq 0 ]; then
    echo "Usage: ./analyze-map.sh <json-file>"
    echo "Example: ./analyze-map.sh /tmp/test-export/arefarm/arefarm.json"
    exit 1
fi

JSON_FILE="$1"

if [ ! -f "$JSON_FILE" ]; then
    echo "Error: File not found: $JSON_FILE"
    exit 1
fi

echo "Analyzing: $JSON_FILE"
echo "========================================"

# Extract basic info using grep and sed
MAP_ID=$(grep '"id"' "$JSON_FILE" | head -1 | sed 's/.*: "\(.*\)".*/\1/')
WIDTH=$(grep '"width"' "$JSON_FILE" | head -1 | sed 's/.*: \([0-9]*\).*/\1/')
HEIGHT=$(grep '"height"' "$JSON_FILE" | head -1 | sed 's/.*: \([0-9]*\).*/\1/')
TILE_SIZE=$(grep '"tileSize"' "$JSON_FILE" | head -1 | sed 's/.*: \([0-9]*\).*/\1/')

TOTAL_TILES=$((WIDTH * HEIGHT))

echo "Map ID: $MAP_ID"
echo "Dimensions: ${WIDTH}x${HEIGHT} tiles"
echo "Tile Size: ${TILE_SIZE}px"
echo "Total Tiles: $TOTAL_TILES"
echo ""

# Count tiles by type
echo "Tile Type Distribution:"
echo "----------------------------------------"

BLOCKED=$(grep -c '"type": "BLOCKED"' "$JSON_FILE")
TELEPORT=$(grep -c '"type": "TELEPORT"' "$JSON_FILE")
FARM=$(grep -c '"type": "FARM"' "$JSON_FILE")
WATER=$(grep -c '"type": "WATER"' "$JSON_FILE")

NON_WALKABLE=$((BLOCKED + TELEPORT + FARM + WATER))
WALKABLE=$((TOTAL_TILES - NON_WALKABLE))

# Calculate percentages
WALKABLE_PCT=$(awk "BEGIN {printf \"%.1f\", $WALKABLE * 100 / $TOTAL_TILES}")
BLOCKED_PCT=$(awk "BEGIN {printf \"%.1f\", $BLOCKED * 100 / $TOTAL_TILES}")
TELEPORT_PCT=$(awk "BEGIN {printf \"%.1f\", $TELEPORT * 100 / $TOTAL_TILES}")
FARM_PCT=$(awk "BEGIN {printf \"%.1f\", $FARM * 100 / $TOTAL_TILES}")
WATER_PCT=$(awk "BEGIN {printf \"%.1f\", $WATER * 100 / $TOTAL_TILES}")

printf "WALKABLE:  %7d (%5s%%)\n" $WALKABLE "$WALKABLE_PCT"
printf "BLOCKED:   %7d (%5s%%)\n" $BLOCKED "$BLOCKED_PCT"
printf "TELEPORT:  %7d (%5s%%)\n" $TELEPORT "$TELEPORT_PCT"
printf "FARM:      %7d (%5s%%)\n" $FARM "$FARM_PCT"
printf "WATER:     %7d (%5s%%)\n" $WATER "$WATER_PCT"

echo ""
echo "File Size: $(du -h "$JSON_FILE" | cut -f1)"
echo "Exported Tiles: $NON_WALKABLE (only non-walkable)"
