#!/bin/zsh

set -u
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

"$SCRIPT_DIR/api.sh"
"$SCRIPT_DIR/web.sh"

API_URL="http://localhost:5026"
WEB_URL="http://localhost:5173"

payload='{
  "planets": [
    {
      "name": "Earth",
      "massKilograms": 5.9722e+24,
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
        "founderCount": 48,
        "seed": 125,
        "centerLatitudeDegrees": 0,
        "centerLongitudeDegrees": 20,
        "spreadDegrees": 4,
        "minimumAgeYears": 8,
        "maximumAgeYears": 35,
        "liveBiomassKilogramsPerPerson": 70,
        "liveNitrogenKilogramsPerPerson": 1.75
      },
      "syntheticAnimals": {
        "wolfCount": 8,
        "seed": 126,
        "centerLatitudeDegrees": 0,
        "centerLongitudeDegrees": 25,
        "spreadDegrees": 1
      },
      "generatedTerrain": {
        "seed": 20260914,
        "latitudeBandCount": 72,
        "longitudeBandCount": 144,
        "plateCount": 24,
        "continentalPlateFraction": 0.45
      },
      "generatedHydrology": {
        "surfaceLiquidWaterInventoryKilograms": 1.4e+21
      },
      "hydrologyModel": {},
      "generatedVegetation": {
        "initialLiveBiomassKilogramsPerSquareMeter": 2.0
      },
      "vegetationModel": {},
      "generatedInvertebrates": {
        "carryingCapacityKilogramsPerKilogramLiveVegetation": 0.02,
        "initialFractionOfLocalCarryingCapacity": 0.25,
        "liveNitrogenKilogramsPerKilogramLiveBiomass": 0
      },
      "invertebrateModel": {},
      "generatedBirds": {
        "carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass": 1e-06,
        "initialFractionOfLocalCarryingCapacity": 0.25,
        "minimumInitialFlockMemberCount": 10,
        "maximumInitialFlockCount": 32,
        "liveBiomassKilogramsPerBird": 1,
        "liveNitrogenKilogramsPerBird": 0.025
      },
      "birdModel": {},
      "generatedGrazers": {
        "carryingCapacityGrazersPerKilogramLiveVegetationBiomass": 1e-06,
        "initialFractionOfLocalCarryingCapacity": 0.25,
        "minimumInitialCohortMemberCount": 10,
        "maximumInitialCohortCount": 256,
        "liveBiomassKilogramsPerGrazer": 250,
        "liveNitrogenKilogramsPerGrazer": 6.25
      },
      "grazerModel": {},
      "generatedBiogeochemistry": {
        "initialDetritalBiomassKilogramsPerSquareMeter": 0,
        "initialDetritalNitrogenKilogramsPerSquareMeter": 0,
        "initialPlantAvailableNitrogenKilogramsPerSquareMeter": 0
      },
      "biogeochemistryModel": {}
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

url="${WEB_URL}/?session=${session_id}&focus=fauna"

echo "Created Est Earth session: $session_id"
echo "Opening: $url"

open "$url"
