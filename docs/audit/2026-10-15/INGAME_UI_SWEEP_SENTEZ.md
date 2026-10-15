# Oyun içi UI kod taraması — sentez · 2026-10-15 13:45

Kaynak: [INGAME_UI_SWEEP_A.md](INGAME_UI_SWEEP_A.md) (savaş çerçevesi: 1 P0 · 8 P1 · 7 P2) ve [INGAME_UI_SWEEP_B.md](INGAME_UI_SWEEP_B.md)
(dünya etkileşimi: 4 P1 · 12 P2). İki raporda ortak 2 bulgu var (A P1-2 = B P1-2; A P1-3 ≈ B P2-1), bu yüzden ~33 tekil bulgu çıktı.
Hepsi kodla hesap; Unity koşulmadı.

## Koordinatörün yaptıkları
- B P1-1 (kamera sarsıntısı halkayı yeniden diziyor) **düzeltildi**: `Radial.cs` `RadialFollowThresholdPt = 6`. Halka ancak merkez 6 pt'den fazla kayarsa yeniden diziliyor.
  Test `PreviewingChoicesKeepsTheRingShapeItOpenedWith`'e 3 px sarsıntı adımı eklendi.
- Doğrulananlar: A P0-1 (`ResponsiveLayout.cs:282` listesinde RestartConfirmation yok), A/B P1-2 (`:233` compact koşulu karta bağlı).

## Öncelik
**Önce (S, net):** A P0-1 restart onayı ölçeği · A/B P1-2 Pro Max üst bar · A P1-5 yeni düşman kartı + pause çift açılıyor ·
A P1-6 popup'lar pause üstünde donuyor · A P1-3 + B P2-1 + A P2-12 toast'a sabit yuva.
**Sonra (kompaktlık, M):** B P1-3/P1-4 bilgi kartını seçimin karşı tarafına al (yuvayı örtmesin, rezerv tek tarafta kalsın) ·
A P2-11 üst bar küçültme · A P1-4 can iki kez (Emre kararı: hap mı, plaka mı).
**Küçük P1'ler:** A P1-7 dalga bandı · A P1-8 kahraman etiketi · A P1-9 + P2-16 öğretici balonu çakışmaları.
**P2 (halka cilası):** B P2-3 basınca halka büyüyor (UiPointSize kendi ölçeğini sayıyor) · B P2-4 eylem düğmesi yer değiştiriyor · B P2-2 kart boyu/fiyat tekrarı ·
B P2-5 uzmanlaşmada NextWave kayboluyor · B P2-6/7/8/9/10/11/12 · A P2-10/13/14/15.

## Paketleme önerisi (dosya sahipliği çakışmasız)
- **X — yerleşim:** `DefenderHudResponsiveLayout.cs`, `DefenderHudController.Radial.cs`, `.WaveForecast.cs`, `UiPointSize.cs`.
  A P0-1, A/B P1-2, toast (A P1-3, B P2-1, A P2-12), kart yeri (B P1-3/4), B P2-2/3/4.
- **Y — çerçeve davranışı:** `.Frame.cs`, `.Motion.cs`, `.Tutorial.cs`, `DefenderRewardPopupPool.cs`, `GameSessionController.Modals.cs`, `AppFlowController.Screens.cs`.
  A P1-5/6/7/8/9, A P2-16, B P2-5.
- **Z — sonra:** üst bar küçültme + can kararı (A P1-4, A P2-11; X ile aynı dosya, X'ten sonra), dünya can barları (B P2-8/9).
