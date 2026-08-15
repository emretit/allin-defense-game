#!/usr/bin/env python3
"""Create original, deterministic Defender Chronicles SFX and adaptive music loops."""

import math
import random
import struct
import wave
from pathlib import Path

RATE = 22050
ROOT = Path(__file__).resolve().parents[2]
SFX_DIR = ROOT / "Assets" / "Audio" / "Presentation" / "SFX"
MUSIC_DIR = ROOT / "Assets" / "Audio" / "Presentation" / "Music"


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    peak = max(1.0, max(abs(value) for value in samples))
    with wave.open(str(path), "wb") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, value / peak)) * 29400)) for value in samples))


def envelope(t, duration, attack=0.015, release=0.22):
    return min(1.0, t / max(attack, 0.001)) * min(1.0, (duration - t) / max(release, 0.001))


def tone(duration, frequencies, noise=0.0, decay=1.8, seed=1, sweep=0.0):
    rng = random.Random(seed)
    samples = []
    phases = [rng.random() * math.tau for _ in frequencies]
    for i in range(int(duration * RATE)):
        t = i / RATE
        env = envelope(t, duration) * math.exp(-decay * t)
        value = 0.0
        for index, frequency in enumerate(frequencies):
            value += math.sin(phases[index] + math.tau * (frequency * t + sweep * t * t)) / len(frequencies)
        value += (rng.random() * 2 - 1) * noise
        samples.append(value * env)
    return samples


def make_sfx():
    specs = {
        "UiClick": (0.11, [740, 1110], 0.01, 7.0, 3, 180),
        "UiError": (0.26, [155, 207], 0.03, 4.0, 5, -90),
        "OutpostBuilt": (0.52, [92, 184, 368], 0.06, 2.2, 7, 75),
        "OutpostUpgraded": (0.62, [220, 330, 550], 0.025, 1.8, 9, 130),
        "OutpostSold": (0.38, [420, 315, 210], 0.015, 2.8, 11, -140),
        "UnitSpawned": (0.32, [260, 520], 0.025, 3.2, 13, 80),
        "UnitDefeated": (0.42, [125, 166], 0.08, 3.6, 15, -110),
        "EnemyHit": (0.16, [105, 210], 0.16, 8.5, 17, -70),
        "EnemyDefeated": (0.46, [82, 123, 164], 0.12, 4.0, 19, -95),
        "HeroMoved": (0.14, [180], 0.1, 7.0, 21, -20),
        "HeroAttacked": (0.21, [165, 330], 0.12, 6.0, 23, 180),
        "HeroAbility": (0.78, [110, 220, 440, 880], 0.045, 1.8, 25, 210),
        "OutpostAttacked": (0.20, [290, 580], 0.055, 5.5, 27, 260),
        "MageImpact": (0.48, [240, 480, 720], 0.035, 2.5, 29, -180),
        "HealPulse": (0.58, [330, 495, 660], 0.012, 1.4, 31, 95),
        "OutpostDisrupted": (0.54, [88, 132, 176], 0.13, 3.0, 33, -70),
        "SlowApplied": (0.43, [260, 390, 520], 0.04, 2.2, 35, -190),
        "BossPhase": (1.05, [55, 82, 110], 0.11, 1.1, 37, 52),
        "BossAttack": (0.72, [48, 72, 96], 0.18, 2.2, 39, -45),
        "WaveStarted": (0.92, [196, 294, 392, 588], 0.018, 1.1, 41, 80),
        "Victory": (2.4, [261.63, 329.63, 392.0, 523.25], 0.008, 0.35, 43, 15),
        "Defeat": (2.4, [196.0, 155.56, 130.81, 98.0], 0.025, 0.55, 45, -18),
    }
    for name, args in specs.items():
        write_wav(SFX_DIR / f"{name}.wav", tone(*args))


def music_loop(name, duration, root, bpm, intensity, seed):
    rng = random.Random(seed)
    total = int(duration * RATE)
    samples = [0.0] * total
    beat = 60.0 / bpm
    chord = [root, root * 1.25, root * 1.5, root * 2.0]
    for i in range(total):
        t = i / RATE
        pad = sum(math.sin(math.tau * frequency * t + math.sin(t * 0.07) * 0.4) for frequency in chord) / len(chord)
        pulse = 0.0
        phase = (t % beat) / beat
        if phase < 0.18:
            pulse = math.sin(math.tau * root * 0.5 * t) * math.exp(-phase * 18)
        shimmer = math.sin(math.tau * root * 3 * t) * (0.04 + intensity * 0.03)
        noise = (rng.random() * 2 - 1) * 0.012 * intensity
        samples[i] = pad * (0.19 + intensity * 0.05) + pulse * intensity * 0.32 + shimmer + noise
    fade = int(RATE * 0.3)
    for i in range(fade):
        factor = i / fade
        samples[i] *= factor
        samples[-i - 1] *= factor
    write_wav(MUSIC_DIR / f"{name}.wav", samples)


def make_music():
    music_loop("Preparation", 18.0, 73.42, 54, 0.25, 101)
    music_loop("Combat", 18.0, 82.41, 82, 0.48, 103)
    music_loop("Intense", 18.0, 87.31, 104, 0.72, 105)
    music_loop("Boss", 18.0, 55.0, 118, 1.0, 107)
    music_loop("Victory", 7.0, 130.81, 72, 0.48, 109)
    music_loop("Defeat", 7.0, 65.41, 48, 0.36, 111)


if __name__ == "__main__":
    make_sfx()
    make_music()
    print("Generated original SFX and adaptive music loops.")
