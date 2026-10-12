# ALLIN: Defense — Actor Marker And Contact Shadow Standard

Status: M1 actor grounding standard
Date: 2026-08-16
Related TODO: VIS-204, VIS-215, VIS-225, VIS-235, VIS-245, VIS-255, VIS-265, VIS-275, VIS-285

## 1. Decision

All capsule-based actors receive a low-cost mesh contact shadow under the visual root. Defender and boss actors keep an additional role aura for stronger readability.

Implementation entry point:

- `Assets/Scripts/Presentation/StylizedPresentationFactory.cs`

Current behavior:

- `Actor Contact Shadow` is created for every capsule actor.
- Contact shadow uses a small mesh ring, not a real-time shadow-casting light.
- Shadow renderer has `shadowCastingMode = Off` and `receiveShadows = false`.
- Boss contact shadow scales larger than normal units.
- `Defender Ground Aura` remains for Knight/Guard style identity.
- `Boss Ground Aura` remains for boss threat identity.

## 2. Visual Rules

| Element | Purpose | Use |
|---|---|---|
| Contact shadow | Grounding and depth read | All capsule actors |
| Defender aura | Friendly hero/unit emphasis | Knight and Guard |
| Boss aura | Threat and phase readability | Boss |
| Future selection marker | Explicit selected/commanded state | Only when player selection exists |
| Future status marker | Slow, disrupt, shield, heal, regen | Driven by VFX/status system, not permanent shadow |

The contact shadow must never be used as the only indicator for selection, slow, heal, boss phase or target state. Those states need distinct shape, timing or icon/VFX treatment.

## 3. Mobile Performance Rules

- No shadow-casting point light is added for actor grounding.
- Contact shadow uses a shared generated mesh.
- Contact shadow material is shared from the catalog.
- It does not receive shadows and does not cast shadows.
- It should remain readable without bloom and without post-processing.
- Low/Medium quality profiles may keep contact shadows because cost is predictable; expensive dynamic shadows remain profile-limited.

## 4. Acceptance Criteria

| Gate | Required proof |
|---|---|
| All capsule actors get grounding | Runtime hierarchy screenshot or PlayMode check |
| Contact shadow does not affect gameplay | Collider/pathing tests unchanged |
| Boss and defender aura still visible | Preparation/boss screenshots |
| Marker does not hide lane/slot info | Dense wave screenshot |
| Mobile cost remains low | Frame Debugger/profiler capture |

Unity runtime verification remains pending because Unity CLI/Editor is not available in the current shell.
