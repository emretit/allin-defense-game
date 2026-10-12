# ALLIN: Defense — HUD Input Regression Pass

Tarih: 2026-08-16
Kapsam: `VIS-613`
Durum: İnceleme

## Özet

Production HUD butonlarının pause, build, upgrade/sell ve gold-bazli availability durumlarında interactable davranışını doğrulayan PlayMode regression testi eklendi.

## Eklenen değişiklikler

- `Assets/Tests/PlayMode/GreyboxSceneTests.cs`
  - `ProductionHudButtonsAreInteractableAndClickable`
- `Assets/Scripts/Presentation/DefenderHudController.cs`
  - `GetButtonInteractable(string path)`

## Test kapsamı

- Initial interactability: Archer/Barracks/Mage/Ability/Pause/NextWave interactable, Cancel non-interactable
- Build sonrası SelectedPanel Upgrade/Sell interactable
- Full occupancy sonrası build butonları non-interactable
- Pause sonrası Ability non-interactable, Pause interactable

## Kanıt

| Tür | Yol |
|---|---|
| PlayMode test log | `Builds/unity-playmode-tests.log` |
| Test kaynağı | `Assets/Tests/PlayMode/GreyboxSceneTests.cs` |
