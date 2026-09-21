#!/bin/zsh

set -u
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

view="embodied"
focus=""

while (( $# > 0 )); do
    case "$1" in
        --view)
            shift

            if (( $# == 0 )); then
                echo "Missing value for --view."
                exit 2
            fi

            view="$1"
            ;;
        --focus)
            shift

            if (( $# == 0 )); then
                echo "Missing value for --focus."
                exit 2
            fi

            focus="$1"
            ;;
        -h|--help)
            cat <<'EOF'
Usage:
  ./scripts/dev/play.sh [--focus <target>]

Development options:
  --focus fauna    Manifest a new Ester near authoritative fauna for local proof work.
EOF
            exit 0
            ;;
        *)
            echo "Unknown argument: $1"
            exit 2
            ;;
    esac

    shift
done

case "$view" in
    embodied|observatory)
        ;;
    *)
        echo "Unsupported Est view: $view"
        exit 2
        ;;
esac

if ! "$SCRIPT_DIR/api.sh"; then
    echo "Est.Api failed to start or validate. Play aborted."
    exit 1
fi

if ! "$SCRIPT_DIR/web.sh"; then
    echo "Est.Web failed to start or validate. Play aborted."
    exit 1
fi

API_URL="http://localhost:5026"
WEB_URL="http://127.0.0.1:5173"

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
        "liveNitrogenKilogramsPerBird": 0
      },
      "birdModel": {
        "maximumInitialFlockCount": 32,
        "liveBiomassKilogramsPerBird": 1,
        "liveNitrogenKilogramsPerBird": 0,
        "maximumPreyConsumptionKilogramsPerBirdPerDay": 0.01,
        "maximumRecruitmentRatePerDay": 0.001
      },
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

if [[ "$focus" == "fauna" ]]; then
    payload="$(
        printf '%s' "$payload" |
            python3 -c '
import json
import sys

payload = json.load(sys.stdin)
planet = payload["planets"][0]

population = planet["syntheticPopulation"]
population["founderCount"] = 1
population["centerLatitudeDegrees"] = 0
population["centerLongitudeDegrees"] = 25
population["spreadDegrees"] = 0.00015

animals = planet["syntheticAnimals"]
animals["wolfCount"] = 1
animals["centerLatitudeDegrees"] = 0
animals["centerLongitudeDegrees"] = 25
animals["spreadDegrees"] = 0.00015

json.dump(payload, sys.stdout, separators=(",", ":"))
'
    )"
fi

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

query="session=${session_id}&view=${view}"

if [[ -n "$focus" ]]; then
    focus_encoded="$(
        python3 -c \
            'import sys, urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=""))' \
            "$focus"
    )"

    query="${query}&focus=${focus_encoded}"
fi

url="${WEB_URL}/?${query}"

echo "Created Est Earth session: $session_id"
echo "View: $view"

if [[ -n "$focus" ]]; then
    echo "Focus: $focus"
fi

echo "Opening: $url"

open "$url"
