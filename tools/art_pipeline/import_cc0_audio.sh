#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h:h}"
source_root="$project_root/Assets/ThirdParty/Audio/CC0Sources"
sfx_root="$project_root/Assets/Audio/Presentation/SFX"
music_root="$project_root/Assets/Audio/Presentation/Music"
work_root="$(mktemp -d)"
trap 'rm -rf "$work_root"' EXIT

mkdir -p "$source_root/Kenney" "$source_root/OpenGameArt" "$sfx_root" "$music_root"

curl -L --fail -o "$source_root/Kenney/kenney_interface-sounds.zip" \
  "https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip"
curl -L --fail -o "$source_root/Kenney/kenney_impact-sounds.zip" \
  "https://www.kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip"
curl -L --fail -o "$source_root/Kenney/kenney_rpg-audio.zip" \
  "https://www.kenney.nl/media/pages/assets/rpg-audio/8e99002d76-1677590336/kenney_rpg-audio.zip"

unzip -q "$source_root/Kenney/kenney_interface-sounds.zip" -d "$work_root/interface"
unzip -q "$source_root/Kenney/kenney_impact-sounds.zip" -d "$work_root/impact"
unzip -q "$source_root/Kenney/kenney_rpg-audio.zip" -d "$work_root/rpg"
cp "$work_root/interface/License.txt" "$source_root/Kenney/Interface-Sounds-License.txt"
cp "$work_root/rpg/License.txt" "$source_root/Kenney/RPG-Audio-License.txt"

convert_sfx() {
  ffmpeg -hide_banner -loglevel error -y -i "$1" -ac 1 -ar 44100 -c:a pcm_s16le "$sfx_root/$2.wav"
}

convert_sfx "$work_root/interface/Audio/click_001.ogg" UiClick
convert_sfx "$work_root/interface/Audio/error_001.ogg" UiError
convert_sfx "$work_root/rpg/Audio/metalPot1.ogg" OutpostBuilt
convert_sfx "$work_root/interface/Audio/confirmation_001.ogg" OutpostUpgraded
convert_sfx "$work_root/rpg/Audio/handleCoins.ogg" OutpostSold
convert_sfx "$work_root/rpg/Audio/drawKnife1.ogg" UnitSpawned
convert_sfx "$work_root/impact/Audio/impactSoft_heavy_000.ogg" UnitDefeated
convert_sfx "$work_root/impact/Audio/impactMetal_light_000.ogg" EnemyHit
convert_sfx "$work_root/impact/Audio/impactMetal_heavy_000.ogg" EnemyDefeated
convert_sfx "$work_root/rpg/Audio/footstep00.ogg" HeroMoved
convert_sfx "$work_root/rpg/Audio/knifeSlice.ogg" HeroAttacked
convert_sfx "$work_root/interface/Audio/glass_001.ogg" HeroAbility
convert_sfx "$work_root/impact/Audio/impactWood_medium_000.ogg" OutpostAttacked
convert_sfx "$work_root/interface/Audio/glass_003.ogg" MageImpact
convert_sfx "$work_root/interface/Audio/confirmation_004.ogg" HealPulse
convert_sfx "$work_root/impact/Audio/impactWood_heavy_000.ogg" OutpostDisrupted
convert_sfx "$work_root/interface/Audio/glass_006.ogg" SlowApplied
convert_sfx "$work_root/interface/Audio/bong_001.ogg" BossPhase
convert_sfx "$work_root/impact/Audio/impactMetal_heavy_004.ogg" BossAttack
convert_sfx "$work_root/interface/Audio/open_004.ogg" WaveStarted
convert_sfx "$work_root/interface/Audio/confirmation_003.ogg" Victory
convert_sfx "$work_root/interface/Audio/error_008.ogg" Defeat

download_music() {
  curl -L --fail -o "$source_root/OpenGameArt/$2" "$1"
}

download_music "https://opengameart.org/sites/default/files/plains_of_luminescence_loop.mp3" preparation.mp3
download_music "https://opengameart.org/sites/default/files/heartfelt-battle_loop.ogg" combat.ogg
download_music "https://opengameart.org/sites/default/files/jrpg_battle_loop.mp3" intense.mp3
download_music "https://opengameart.org/sites/default/files/battle_11.mp3" boss.mp3
download_music "https://opengameart.org/sites/default/files/fanfare1.ogg" victory.ogg
download_music "https://opengameart.org/sites/default/files/game_over.wav" defeat.wav
curl -L --fail -o "$source_root/CC0-1.0-Legal-Code.txt" \
  "https://creativecommons.org/publicdomain/zero/1.0/legalcode.txt"

convert_music() {
  ffmpeg -hide_banner -loglevel error -y -i "$1" -t "$3" -ac 1 -ar 44100 -c:a pcm_s16le "$music_root/$2.wav"
}

convert_music "$source_root/OpenGameArt/preparation.mp3" Preparation 24
convert_music "$source_root/OpenGameArt/combat.ogg" Combat 24
convert_music "$source_root/OpenGameArt/intense.mp3" Intense 24
convert_music "$source_root/OpenGameArt/boss.mp3" Boss 24
convert_music "$source_root/OpenGameArt/victory.ogg" Victory 12
convert_music "$source_root/OpenGameArt/defeat.wav" Defeat 12

echo "CC0 audio sources imported and presentation clips rebuilt."
