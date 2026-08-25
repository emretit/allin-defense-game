# Defender Chronicles - VIS-415 Environment Budget Proxy

Tarih: 2026-08-17

Bu belge VIS-415 için batch-mode ara bütçe kanıtını kaydeder. Final kabul için iPhone sınıfı cihaz Profiler ve Unity Frame Debugger kanıtı hâlâ gereklidir; bu rapor o kapının yerine geçmez.

## Ölçüm Kapsamı

Unity checkpoint production presentation catalog ile oturum açar ve `Scenic Battlefield Dressing` altındaki runtime environment renderer/mesh/material/shadow/collider durumunu ölçer.

| Ölçüm | Sonuç | Proxy bütçe | Durum |
|---|---:|---:|---|
| Renderer | 16 | 16 | Geçti |
| MeshFilter | 16 | - | Bilgi |
| Triangle | 3792 | 8500 | Geçti |
| Material slot | 16 | 20 | Geçti |
| Unique material | 13 | 16 | Geçti |
| Transparent-like material | 0 | - | Bilgi |
| Shadow caster | 0 | 0 | Geçti |
| Receive shadow renderer | 0 | 0 | Geçti |
| Collider | 0 | 0 | Geçti |

## Kanıt

- Makine raporu: `docs/evidence/visual-development/environment-budget-proxy-report-2026-08-17.json`
- Unity checkpoint logu: `Builds/unity-environment-budget-proxy-checkpoint.log`
- Runtime düzeltmesi: `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs` scenic dressing renderer'larında `receiveShadows = false` kullanır.

## Açık Kapı

VIS-415 final kabulü için gerçek cihaz veya hedef build üzerinde Profiler/Frame Debugger raporu hâlâ açık kalır. Bu proxy yalnız sahne içi runtime object count, shadow/collider güvenliği, mesh triangle ve material baskısını erken kontrol eder.

## Doğrulama

Bu rapor `DefenderPresentationBuildValidator.ValidateEnvironmentBudgetProxyCheckpoint()` ile üretildi ve doğrulandı. Checkpoint; required runtime environment objelerini, renderer/mesh/material sayımlarını, shadow/collider durumunu ve JSON rapor üretimini kontrol eder.
