#!/bin/zsh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

"$SCRIPT_DIR/api.sh"
"$SCRIPT_DIR/web.sh"

API_URL="http://localhost:5026"
WEB_URL="http://localhost:5173"

payload='{
  "planets": [
    {
      "name": "Earth",
      "massKilograms": 5.9722e24,
      "meanRadiusMeters": 6371000,
      "environment": {
        "meanSurfaceTemperatureKelvin": 288.15,
        "surfaceWaterFraction": 0.71,
        "iceCoverageFraction": 0.03,
        "atmosphere": {
          "surfacePressurePascals": 101325,
          "compositionByMoleFraction": {
            "N2": 0.78,
            "O2": 0.21,
            "Ar": 0.01
          }
        }
      },
      "syntheticPopulation": {
        "founderCount": 100,
        "seed": 42,
        "centerLatitudeDegrees": 0,
        "centerLongitudeDegrees": 25,
        "spreadDegrees": 3,
        "minimumAgeYears": 18,
        "maximumAgeYears": 35
      },
      "syntheticFood": {
        "patchCount": 250,
        "seed": 84,
        "centerLatitudeDegrees": 0,
        "centerLongitudeDegrees": 25,
        "spreadDegrees": 3,
        "energyPerPatch": 100,
        "recoveryEnergyPerDay": 2
      }
    }
  ]
}'

response="$(
    curl -fsS \
        -H 'Content-Type: application/json' \
        -d "$payload" \
        "$API_URL/sessions"
)"

session_id="$(
    printf '%s' "$response" |
        python3 -c 'import json,sys; print(json.load(sys.stdin)["sessionId"])'
)"

url="${WEB_URL}/cesium.html?session=${session_id}"

echo "Created Est Earth session: $session_id"
echo "Opening: $url"

open "$url"
