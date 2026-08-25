# Defender Chronicles — Building Slot Runtime Regression

Status: VIS-345 runtime smoke evidence
Date: 2026-08-16
Related TODO: VIS-317, VIS-326, VIS-336, VIS-345, VIS-811

## 1. Decision

Sekiz fixed build slot için build → upgrade → sell → rebuild → sell zinciri gerçek iOS Simulator runtime'ında doğrulandı.

Bu pass resmi Unity PlayMode XML sonucunun yerini tamamen almaz, fakat mevcut flaky batch test runner davranışı karşısında güçlü bir runtime evidence sağlar:

- Her slotta ilk build geçti.
- Her slotta L1→L2 upgrade geçti.
- Her slotta sell sonrası slot boşaldı.
- Her slotta Archer ile yeniden build geçti.
- Her slotta ikinci sell sonrası slot tekrar boşaldı.

## 2. Evidence

- Runtime smoke log: `Builds/ios-slot-regression-smoke.log`
- Generated catalog runtime screenshot: `docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-upgraded-state-landscape-readable.png`
- Build compile log: `Builds/unity-slot-regression-smoke-compile.log`

## 3. Slot pass summary

`Builds/ios-slot-regression-smoke.log` içinde aşağıdaki PASS satırları kaydedildi:

- Slot 1 — Archer
- Slot 2 — Barracks
- Slot 3 — Mage
- Slot 4 — Archer
- Slot 5 — Mage
- Slot 6 — Barracks
- Slot 7 — Archer
- Slot 8 — Mage

Terminal summary marker:

- `[DC_SLOT_REGRESSION_COMPLETE] all_slots_passed=true`

## 4. Implementation note

`Assets/Scripts/Core/GameSessionController.cs` içine `DC_SLOT_REGRESSION=1` ortam değişkeniyle çalışan kısa smoke akışı eklendi. Bu akış production presentation altında sekiz slotu tek tek doğrular ve sonunda app'i kapatır.

`Assets/Scripts/Core/GreyboxCombatants.cs` içine `GreyboxOutpost.Level` okunur property eklendi; smoke doğrulaması bu değerle L1/L2 geçişini denetler.

## 5. Remaining open gates

- Unity batch test runner hâlâ `ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot` için güncel XML üretmedi; bu nedenle resmi PlayMode report kapısı hâlâ açıktır.
- Build/upgrade/sell video capture hâlâ açık.
- Final six-building acceptance board ve insan kör tanıma sonucu hâlâ açık.

## 6. Practical outcome

VIS-345 için artık “sekiz slot runtime davranışı bilinmiyor” durumu yok. Davranış zinciri gerçek iOS Simulator runtime'ında log tabanlı olarak kanıtlanmış durumda.

## 7. Replay

Aynı runtime regression akışını tekrar çalıştırmak için:

```bash
./tools/run_ios_slot_regression_smoke.sh
```
