# ALLIN: Defense — 50 bölüm / 5 bölge birleşik plan (koordinatör, 2026-10-14)

Kaynaklar: [BOLGE_PLANI_claude.md](BOLGE_PLANI_claude.md) ve [BOLGE_PLANI_codex.md](BOLGE_PLANI_codex.md) (bağımsız, main `2a4c27f`).
Ayrıntılı envanter ve file:line kanıtları o iki belgededir; bu belge kararları ve uygulama sırasını sabitler.

## Kararlar (Emre, 2026-10-14)
- v1 = 50 bölüm / 5 bölge; valor gizli; tek kahraman + tek yetenek; kalıcı gelişim yok; ücretsiz/reklamsız.
- Dalga stili 11-50: bölge 1 gibi **kalabalık zayıf sürü** (eşzamanlı düşman ≤60 korunur).
- Bölüm süresi: **normal 4-7 dk, boss 7-9 dk**, bölgeler boyunca sabit.
- Zorluk: boss'lar **sınav, duvar değil** — ortalama oyuncu geçer, 3 yıldız iyi oyun ister.
- Final boss adı **Son Lord** (EN "The Last Lord"); "Lordrush" kaldırılır.

## İki planın ortak önerisi (koordinatör kararı olarak alındı)
- Yeni düşman tipi YOK. Bölge kimliği = düşman karışımı + dalga ritmi + giriş/rota + bölge boss profili (veriyle) + biyom görünümü.
- Mevcut 5 bölge adı korunur: Yeşil Nöbet → Kül Geçidi (zırh/kuşatma) → Buz Sınırı (hız/zamanlama, yeni don mekaniği YOK)
  → Batık Krallık (iki koldan dönüşümlü baskı, şifacı) → Son Hisar (43'te 3. giriş, ustalık).
- Boss profilleri: Geçit Efendisi (20) · Buz Hükümdarı (30) · Derinliklerin Lordu (40) · Son Lord (50). Yeni AI sınıfı değil, takviye tipi/girişi/faz hızı verisi.
- Veri: bölge başına elle onaylı JSON (`Assets/Resources/Campaign/region-0N.json`), formül üretici yalnız taslak; grup başına `routeId`
  (FirstRegionCampaign.cs:33 tuzağı: 4 parametreli kurucu routeId'yi düşürüyor). 11-50 eski formül, yeniden yazılır.
- Sunucu: 11-50'nin `id`, `startingLives`, `valorReward`, `heroXpReward` değerleri dondurulur → Cloud Code dağıtımı gerekmez.
- Checkpoint: bölge başına içerik sürümü (`first-region-v2` alias korunur, `region-02-v1`…); 11+ checkpoint bugün her açılışta siliniyor → düzelt.
- Akış: her 10 bölümde bölge sonu paneli, 50'de final ("Krallık savunuldu", x/150); bölge kilidi = önceki bölgenin 10. bölümü (yıldız barajı yok);
  yıldız hedefi 30 → 150, sabit "/30" kaldırılır; tek `PlayableLevelCount` politikası (bugün 9 noktada 10'a sabit).
- Görsel: biyom başına zemin + ağaç/kaya/yosun paleti ve siluet ailesi, ortak malzeme + vertex rengi (ek SetPass yok).
- Ses: mevcut 6 savaş parçası paylaşılır; bölgesel atmosfer isteğe bağlı.
- Denge: bölge başına bot testi (`CampaignBalanceTests` [bölge]), alt sınır testi (pasif savunma boss'u kaybeder), EditMode eğri testi; her bölge sonunda 2-3 kişilik oyuncu testi.

## Uygulama sırası
| Aşama | İş | Sahip dosyalar | Süre |
|---|---|---|---|
| 0d | Biyom görünüm profilleri (5 bölge) | `BiomeLook.cs` (yeni), `DefenderBattlefieldDressing.cs`, `DefenderSceneLook.cs` | M |
| 0a | Bölge içerik modeli + bölge bazlı checkpoint sürümü + boss profili verisi | `CampaignContent.cs` (yeni), `FirstRegionCampaign.cs`, `CampaignCatalog.cs`, `CampaignLevelFactory.cs`, `GameSessionController*.cs` | L (1,5-2 gün) |
| 0c | Denge koşumu (bölge parametreli) + eğri testi | `CampaignBalanceTests.cs`, `CampaignDifficultyCurveTests.cs` (yeni) | M |
| 0b | PlayableLevelCount, bölge sonu + final paneli, 150 yıldız | `AppFlowController*.cs`, `FrontendCopy.cs`, `CampaignProgressStore.cs` | L |
| 1 | Bölge 2 dikey dilimi: region-02.json, Geçit Efendisi, EN metin, denge turları, VisibleRegionCount=2 | | ~6 gün |
| 2-4 | Bölge 3, 4, 5 + final | | ~4 + 4 + 6 gün |
| 5 | 50 bölüm tam denge, eski üreticiyi silme, perf (43/50), EN taşma, uzun oyuncu testi | | ~3 gün |

Sıra kuralları: 0a ve 0b, akış düzeltmeleri worker'ı (AppFlowController/FrontendCopy/Checkpoints) commit'lenince başlar; 0a → 0b (Checkpoints çakışması).
0d şimdi başlayabilir (GameSessionController.cs'e dokunmaz). Aynı anda tek Unity batch koşusu.
Toplam: ~28-32 iş günü (~6 hafta).
