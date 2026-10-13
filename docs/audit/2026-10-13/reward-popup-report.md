# Öldürme altını popup — 2026-10-13

- `Assets/Scripts/Core/GameSessionController.cs:1016`: EnemyDefeated sinyalindeki `value`, düşmanın gerçek `Reward` değeri; EarnGold ve oyun kuralları korunuyor.
- `Assets/Scripts/Presentation/DefenderPresentationDirector.cs:31`: havuz kurulumu; EnemyDefeated sinyalinden popup çağrısı.
- `Assets/Scripts/Presentation/DefenderRewardPopupPool.cs:43`: 24 öğe ön hazırlık; mevcut katalog goldIcon, UiTokens.Gold, koyu kontur, UiMetrics ile 24 pt ekran overlay. Boss 1,25× büyük ve daha yüksek.
- `Assets/Scripts/Presentation/DefenderRewardPopupPool.cs:100`: pozitif ödül için +N; dolu havuzda en eski öğe dönüşümü; oyuncuda önceden ayrılmış 11 karakterlik tampon. TMP Editor overload'ının Inspector string ayırması için 0–511 metin önbelleği; oyunun mevcut en yüksek ödülü 308.
- `Assets/Scripts/Presentation/DefenderRewardPopupPool.cs:134`: 0,8 saniye scaled time, 26 pt yükselme ve son 0,35 saniyede sönme; pause ilk karesinde ilerleme yok; reduced motion yükselmeyi kapatır.
- `Assets/Tests/PlayMode/RewardPopupTests.cs:51`: gerçek düşman ölümü/altın/metin/konum, dönüş/sönme, kapasite/en eski dönüşümü/GC, reduced motion, 0 ödül, boss ve pause/2× olmak üzere 7 test.
- `Assets/Scripts/UI/AppFlowController.Acceptance.cs:136`: battle-active-wave çekimi gerçek bir pozitif öldürme sinyalinden sonra yapılır; yoksa kabul raporuna hata eklenir.

## Kapılar

| Kapı | Sonuç | Kanıt |
| --- | --- | --- |
| EditMode tam suite | 210/210 passed | Builds/reward-final-edit.xml |
| PlayMode tam suite | 170/170 passed | Builds/reward-final-play.xml |
| Yeni popup testleri | 7/7 passed | Builds/reward-focused.xml ve final PlayMode XML |
| AuditLayer2PerfTests | 13/13 passed; eşikler değişmedi | Builds/reward-final-play.xml |
| git diff --check | temiz | son çalışma kontrolü |
| Simülatör / report.json / görüntü | koordinatöre bırakıldı | 07:37 UTC koordinatör mesajı msg_a66c3ae15787 |

Koordinatörün Emre kararını aktaran son talimatı gereği bu işte iOS build ve simülatör kabulü koşulmadı; yeni battle-active-wave.png üretilmedi ve report.json için yeni status/errors sonucu yok. Tam PlayMode koşusu talimat geldiğinde zaten bitmişti; grup sonunda görsel kabulü koordinatör yapacak. Kontrol anında başlatılmış olan simülatör, build çalıştırılmadan kapatıldı.

HUD altın pill'i, ödül miktarları ve prefab GUID'leri değişmedi; font ve Addressables test yan etkileri geri alındı. Commit/push yapılmadı. Açık kalan doğrulama: telefon üstünde popup'ın görünümü ve yeni kabul görüntüsü.
