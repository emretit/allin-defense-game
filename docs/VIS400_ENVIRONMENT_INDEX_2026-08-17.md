# ALLIN: Defense - VIS-400 Environment Index

Tarih: 2026-08-17

Bu indeks, Greenwatch Valley environment pass'lerini tek review haritasında toplar. VIS-404, VIS-405, VIS-410, VIS-411 ve VIS-412 mevcut generated/runtime kanıtlarıyla incelemeye hazırdır. Üç ekran oranı capture seti, slot/path PlayMode sonucu ve Profiler/Frame Debugger bütçe kanıtı açık kalır.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-404 | İncelemede | `docs/GROUND_VARIATION_PASS_2026-08-16.md`, `docs/ENVIRONMENT_VALUE_RUNTIME_PASS_2026-08-16.md`, `docs/evidence/visual-development/ground-variation-sheet-2026-08-16.png`, `docs/evidence/runtime/defender-simulator-environment-value-pass-adjusted-late-2026-08-16-landscape-readable.png` | West/Mid/Final ground value overlays, runtime low-contrast value patches and accepted readable landscape proof |
| VIS-405 | İncelemede | `docs/ROAD_VARIATION_PASS_2026-08-16.md`, `docs/ROAD_SURFACE_RUNTIME_PASS_2026-08-16.md`, `docs/ROAD_BORDER_RHYTHM_PASS_2026-08-16.md`, `docs/evidence/runtime/defender-simulator-road-organic-surface-normals-late-2026-08-16-landscape-readable.png`, `docs/evidence/runtime/defender-simulator-road-cobble-rhythm-late-2026-08-16-landscape-readable.png` | Road surface, edge stones, travel wear and accepted runtime road readability passes |
| VIS-410 | İncelemede | `docs/BOUNDARY_DRESSING_PASS_2026-08-16.md`, `docs/evidence/visual-development/boundary-layout-2026-08-16.png`, `docs/evidence/visual-development/boundary-layout-metrics-2026-08-16.json`, `docs/evidence/visual-development/boundary-asset-metrics-2026-08-16.json` | North/south/edge/silhouette boundary layout, 10 placements, path/slot/repeat rule proof and six generated boundary FBX candidates |
| VIS-411 | İncelemede | `docs/ATMOSPHERE_PASS_2026-08-16.md`, `docs/evidence/visual-development/atmosphere-asset-metrics-2026-08-16.json` | Moon disc, fog ribbon and dust veil generated atmosphere candidates with collider-free import proof |
| VIS-412 | İncelemede | `docs/LIGHTING_PASS_2026-08-16.md`, `docs/LIGHTING_MATERIAL_BALANCE_RUNTIME_PASS_2026-08-16.md`, `docs/LIGHTING_QUALITY_56_CONTROL_2026-08-16.md`, `docs/evidence/runtime/defender-simulator-lighting-material-balance-late-2026-08-16-landscape-readable.png` | Single-key lighting plan, Low/Medium shadow-cost guardrails, material balance and accepted readable runtime proof |

## Açık Kapılar

- VIS-413: 16:9, 20:9 ve 4:3 için hazırlık/yoğun screenshot seti hâlâ gerçek Editor/Simulator capture bekler.
- VIS-414: slot/path/collider regresyonunun resmi PlayMode sonucu ve video kanıtı açık kalır.
- VIS-415: draw call, shadow caster, triangle ve overdraw için Profiler/Frame Debugger raporu açık kalır.
- VIS-403: gelecekte dış çevre asset'i import edilirse lisans/hash/revision/provenance kaydı aynı değişiklikte eklenmelidir.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateEnvironmentIndexCheckpoint()` ile doğrulanır. Checkpoint; environment indeks coverage'ını, generated ground/road/boundary/atmosphere FBX dosyalarını, Unity `.meta` varlığını, layout/metrics/sheet/runtime screenshot kanıtlarını, boundary path/slot/repeat temizliğini ve açık QA kapılarının belgede korunmasını kontrol eder.
