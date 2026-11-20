# İlerleme Planı — sentez (2026-10-17)

Kaynak: [PROGRESSION_claude.md](PROGRESSION_claude.md) (280 satır) + [PROGRESSION_codex.md](PROGRESSION_codex.md) (246 satır). İki rapor birbirini görmeden yazıldı.

## Checklist

**Faz 0 — Emre kararları**
- [ ] Aşağıdaki 5 karar (önerilerle)

**Faz 1 — Bölge 1 dikey kesit (bölüm 1-10), telefonda dene**
- [ ] K1 Yıldız kapısı mantığı + kayıt v6 (açılmış bölge korunur) — yalnız istemci
- [ ] K2 Yol düzeni tablosu + üretici desenleri (tek, uzun, çift erken/geç birleşme, yan kapı, çatal) + 8 yuva
- [ ] K3 Bölge 1 dalgaları: sayı + tip karışımı + giriş kullanımı, bot dengesi
- [ ] K4 Dalga önizlemesi: "Sıradaki" şeridinde hangi girişten ne geliyor + giriş okları
- [ ] K5 Harita/sonuç ekranı: "Kül Geçidi'ne 4 yıldız kaldı" + 3 yıldıza çıkarılabilecek bölümler (TR/EN)
- [ ] Koordinatör: EditMode + CampaignBalanceTests + telefonda deneme

**Faz 2 — Bölge 2-5 (bölüm 11-50)**
- [ ] Desenler: sırayla üç giriş, üç cephe, dönüşümlü/sızma; bölge 4-5 karışımı (yağmacı dışı pay %2 → ~%25)
- [ ] 5 bölge JSON + checkpoint contentVersion artışı + tam RegionBalance
- [ ] Cihazda SetPass/kadraj ölçümü → TestFlight

## İki raporun anlaştığı yerler
- **Kapı:** her bölgede 30 yıldızdan 20 + bölge boss'u yenilmiş. Zaten açılmış bölgeler kilitlenmez (kayıt v6).
- **Teşhis:** yıldız hiçbir şey açmıyor; bölüm 14-42 arası 29 bölüm aynı 2 girişli T-kavşak; yağmacı dışı düşman payı R2'de %21'den R5'te %2'ye düşüyor.
- **Yeni düşman tipi gerekmiyor:** mevcut tiplerin karışımı yetiyor.
- **En büyük riskler:** (1) yuvalar yoldan türediği için yol değişince bot dengesi bozulur; (2) checkpoint kuleleri yuva indeksiyle kurar → yol/yuva değişince contentVersion artmalı.

## Ayrıştıkları yerler (sentez kararı)
| Konu | Claude | Codex | Sentez |
|---|---|---|---|
| Süre | ~24-29 saat | ~23 kişi-gün | Codex sunucu, tam kabul ve cihaz ölçümünü de sayıyor. Önce Bölge 1 dikey kesit (~1-1.5 gün) → ölç → kalanı planla. |
| Sunucu | sunucu doğrusal kalır, kapı istemcide | sunucu kuralı da güncellensin | İlk sürüm: kapı yalnız istemcide (sunucu daha serbest kalır → kayıp risk yok, deploy gerekmez). |
| Yenilik zamanı | ilk yan kapı bölüm 9, 3. giriş bölüm 37 | çift giriş bölüm 3, çatal 13, 3. giriş 33 | Çift giriş bölüm 5, çatal 13, 3. giriş 35 (küçük grupla), tam üç cephe 43. |
| Çatal (ayrıl-birleş) | opsiyonel (+5 sa) | 8 desenin parçası | Evet — "farklı geliş varyasyonu" isteğini en iyi bu karşılıyor. |

## Emre'ye 5 karar
1. Kapı 20/30 + boss — **öneri: evet**.
2. Yenilik takvimi: çift giriş 5 · çatal 13 · üç giriş 35 · üç cephe 43 — **öneri: evet**.
3. Çatal deseni yapılsın — **öneri: evet**.
4. Önce yalnız Bölge 1 (1-10) yapılıp telefonda denensin, sonra 11-50 — **öneri: evet**.
5. Kapı yalnız uygulamada (sunucu değişmez) — **öneri: evet**.
