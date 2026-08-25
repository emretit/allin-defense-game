#!/bin/sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/../.." && pwd)
BRAND_TMP=$(mktemp -d)
trap 'rm -rf "$BRAND_TMP"' EXIT INT TERM

LOGO_SOURCE="$PROJECT_ROOT/Assets/Art/Brand/DefenderChronicles-Logo-v2.png"
MARK_SOURCE="$PROJECT_ROOT/Assets/Art/Brand/DefenderChronicles-Mark-v2.svg"
ICON_BACKGROUND_SOURCE="$PROJECT_ROOT/Assets/Art/AppIcon/BrandV2/DefenderChronicles-AppIcon-Background-v2.svg"
WELCOME_SOURCE="$PROJECT_ROOT/Assets/Resources/Presentation/Frontend/GreenwatchWelcome.png"

RUNTIME_BRAND_DIR="$PROJECT_ROOT/Assets/Resources/Presentation/Brand"
ICON_OUTPUT_DIR="$PROJECT_ROOT/Assets/Art/AppIcon/BrandV2"
LAUNCH_OUTPUT_DIR="$PROJECT_ROOT/Assets/Art/Brand/Launch"

mkdir -p "$RUNTIME_BRAND_DIR" "$ICON_OUTPUT_DIR" "$LAUNCH_OUTPUT_DIR"

for required_file in "$LOGO_SOURCE" "$MARK_SOURCE" "$ICON_BACKGROUND_SOURCE" "$WELCOME_SOURCE"; do
    if [ ! -f "$required_file" ]; then
        echo "Missing required brand source: $required_file" >&2
        exit 1
    fi
done

if ! command -v rsvg-convert >/dev/null 2>&1; then
    echo "rsvg-convert is required to build Defender Chronicles brand assets." >&2
    exit 1
fi
if ! command -v ffmpeg >/dev/null 2>&1; then
    echo "ffmpeg is required to composite Defender Chronicles brand assets." >&2
    exit 1
fi

rsvg-convert -w 2048 -h 2048 "$MARK_SOURCE" -o "$PROJECT_ROOT/Assets/Art/Brand/DefenderChronicles-Mark-v2.png"
cp "$LOGO_SOURCE" "$RUNTIME_BRAND_DIR/DefenderChronicles-Logo-v2.png"
rsvg-convert -w 1024 -h 1024 "$MARK_SOURCE" -o "$RUNTIME_BRAND_DIR/DefenderChronicles-Mark-v2.png"
rsvg-convert -w 1024 -h 1024 "$ICON_BACKGROUND_SOURCE" -o "$BRAND_TMP/icon-background.png"

ffmpeg -v error -y \
    -i "$BRAND_TMP/icon-background.png" \
    -i "$PROJECT_ROOT/Assets/Art/Brand/DefenderChronicles-Mark-v2.png" \
    -filter_complex '[1:v]scale=780:780[mark];[0:v][mark]overlay=122:122:format=auto,format=rgb24[out]' \
    -map '[out]' -frames:v 1 \
    "$ICON_OUTPUT_DIR/DefenderChronicles-AppIcon-v2-1024.png"

ffmpeg -v error -y \
    -i "$WELCOME_SOURCE" \
    -i "$LOGO_SOURCE" \
    -filter_complex '[1:v]scale=324:324[logo];[0:v][logo]overlay=264:129:format=auto,format=rgb24[out]' \
    -map '[out]' -frames:v 1 \
    "$LAUNCH_OUTPUT_DIR/GreenwatchLaunch-v2.png"

echo "Defender Chronicles V2 brand assets built successfully."
