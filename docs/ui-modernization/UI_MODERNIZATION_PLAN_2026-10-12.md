# UI modernizasyon planı — menü ve savaş

Durum: Plan onaylandı, uygulama başlamadı
Tarih: 2026-10-12

## Kararlar

| Konu | Karar |
|---|---|
| Görsel yön | **Temiz Fantastik**: koyu yarı saydam paneller, ince altın/ay mavisi kontur, yuvarlak köşe, büyük oyun ikonları, az metin |
| Kapsam | HUD önce, sonra 3B savaş alanı görseli |
| Varlık kaynağı | Kodla üretilen şekiller (builder), mevcut ikon atlası; yeni AI görseli yok |
| Öncelik | Savaş HUD → menü |
| Teknik | UGUI'de kalınır. UI Toolkit'e geçilmez |

A ve C mockup'ları kullanıcı tarafından onaylandı (2026-10-12). Bu incelemede yuvaların yoldan kopuk durduğu fark edildi; slot üreticisi ayrıca düzeltildi (`docs/MAP_OUTPOST_SYSTEM.md`).

Mockup'lar: [A — savaş](mockups/A-battle-combat.png), [C — hazırlık + inşa menüsü](mockups/C-battle-prep.png), [B — ana menü](mockups/B-menu.png). Token değerleri: [tokens.css](mockups/tokens.css). Mockup'lar 25 Ağustos simülatör görüntülerinin üzerine çizildi. Arkadaki 3B sahne güncel değil, değerlendirmede yalnız arayüze bakılmalı.

## Audit özeti (2026-10-12, iPhone 17 Pro simülatörü)

Menü:
- Tek bir 45° kesik köşeli, düz dolgulu çerçeve (`MedievalPanel/Button.png`, 128 px) her yerde kullanılıyor. Gölge, gradient ve yuvarlak köşe yok.
- Ana menüde teknik durum metni ("Kaydedildi · Eşitlendi") var. İki eşit ağırlıklı dev CTA aynı anda dikkat çekmeye çalışıyor.
- Harita: bölge sekmesi tam genişlikte, üstünde sadece "1" yazan bir buton. Kilitli düğümler "× KİLİTLİ" tekrarından ibaret. Düğümlerde yıldız yok.
- Kabul akışı 3 metin taşması yakaladı: brifingdeki `HEDEF` satırı (TR/EN) ve yenilgi ipucu.
- Ayarlar ve hesap ekranları form görünümünde. Sonuç ekranındaki yıldızlar yalnız metin. Yenilgide "Sonraki Bölüm" aktif görünüyor.

Savaş:
- Üst bar 5 ayrı kutu ve altlarındaki renkli çizgilerden oluşuyor. İkonlar ~20 px. "DURUM" kutusu gereksiz yer kaplıyor.
- Tehdit önizlemesi ve ipucu balonu rotanın üstüne biniyor.
- Yetenek düğmesi bir metin kutusu ("Warrior READY"). Radyal cooldown ve portre yok.
- Menüdeki boyalı illüstrasyon ile düz 3B savaş alanı arasında kalite farkı var (6 Eylül audit'i V01).

Kod:
- İki ayrı palet var (`FrontendPresentationTheme`, `DefenderHudController.cs:145-159`) ve ~77 satır içi `Color(...)` bulunuyor. Menü metni soğuk beyaz, HUD metni sıcak beyaz.
- Menüler tamamen runtime C# ile kuruluyor (`AppFlowController*.cs`). `CreateHudPrefab` prefab zaten varsa erken dönüyor; builder değişiklikleri sahaya yansımıyor.
- Yalnız NotoSans Regular var; 33 `FontStyles.Bold` kullanımı sahte bold üretiyor.
- Temadaki hareket süreleri hiçbir yerde okunmuyor ve çıkış animasyonu yok.
- Testler ~60 nesne adına bağlı. İngilizce tablo Türkçe kaynak metinle anahtarlanıyor (`AppFlowController.cs:1358`).
- Ölü dosyalar: `UIManager.cs`, `TowerInfoPanel.cs`, `TowerWorldTapHandler.cs`, `FloatingText.cs`.

## Tasarım dili

- **Panel:** `#091222` %68–90 opaklık, üstte %7 beyaz highlight gradient'i, 1 px `rgba(155,203,255,.16)` kontur, yumuşak alt gölge. Unity'de gerçek arka plan bulanıklığı **yok**, çünkü mobilde pahalı. Aynı his yüksek opaklık ve gradient ile verilir.
- **Köşe yarıçapı:** 10 (küçük), 14 (orta), 20 (kart) ve tam yuvarlak (pill / dairesel düğme).
- **Ana CTA:** altın dikey gradient `#FFD98A → #F2B84B → #D4922C`, koyu metin `#1D1305`, altın parıltı gölgesi. Ekranda tek bir ana CTA bulunur.
- **İkincil:** cam panel ve açık metin. Tehlike eylemi (Sat) kırmızımsı metin ve kontur alır.
- **Tipografi:** başlıklarda Cinzel SemiBold, gövdede Noto Sans Regular/SemiBold/Bold. Sayılar tabular ve kalın. Etiketler küçük, büyük harf, geniş harf aralıklı.
- **İkon ayrımı:** oyun kaynakları (altın, kale, dalga, kuleler, yetenek) mevcut boyalı atlas ikonlarını kullanır. Sistem eylemleri (pause, hız, ayar, kapat) ince çizgili ikonlarla gösterilir.
- **Durumlar:** normal / basılı (koyulaşma + 0,975 ölçek) / pasif (gri ikon + kırmızı fiyat; yalnız opaklık düşürülmez) / seçili (altın kontur + parıltı) / hazır (altın halka, sınırlı nabız).

## HUD bilgi mimarisi (mockup A ve C)

- **Sol üst:** kale canı ve altın pill'leri. Altın değişiminde kısa `+12` göstergesi çıkar.
- **Orta üst:** dalga `1/7`, faz etiketi (HAZIRLIK / SAVAŞTA / BOSS) ve 7 parçalı ilerleme çubuğu. Altında tek satırlık "Sıradaki" tehdit çipi bulunur. Bu bilgiler rotayı kapatmaz.
- **Sağ üst:** hız pill'i (1×/2×/4×/8×) ve pause düğmesi.
- **Sağ alt:** başparmak bölgesi. Kahraman yeteneği 74 pt dairesel düğmedir; radyal cooldown halkası, HAZIR rozeti ve can çubuğu taşır. Yanında erken çağırma düğmesi ve saniye rozeti yer alır. Hazırlıkta bunların yerine altın "İlk dalgayı başlat" pill'i gelir.
- **İnşa:** boş slota dokununca slotun etrafında 3 seçenekli radyal menü açılır. Her seçenekte ikon ve fiyat çipi bulunur. Yetersiz altında ikon griye döner ve fiyat kırmızı görünür. Seçili seçeneğin açıklaması sol alttaki bilgi alanında gösterilir.
- **Seçili kule:** sağda kompakt kart. İçinde ikon, ad, rol, seviye pip'leri, 3 istatistik kutusu, Yükselt/Uzmanlaş (altın) ve Sat (+iade) bulunur. Dünyada altın seçim halkası ve kesikli menzil elipsi gösterilir.
- **Kamera:** HUD yüzen hale geldiği için alt bar şeridi boşalır. Savaş kamerası kadrajı yeniden ölçülür (tahmini +%10–15 alan). Rota, 8 slot ve kale 16:9, 19.5:9 ve tablette görünür kalmalıdır.

## Fazlar

| Faz | İş | Süre | Kabul |
|---|---|---|---|
| 0 | Temel: ölü UI dosyalarını sil, çeviri anahtarlarını sabit ID'ye bağla, `CreateHudPrefab` için yeniden üretim yolu ekle, "önce" görüntülerini kabul modunda al | ~1 sa | Testler yeşil, TR/EN metinler değişmedi |
| 1 | Tasarım sistemi: `UiTokens` (renk, tipografi ölçeği, 8 pt boşluk, yarıçap, gölge, hareket), menü teması ile HUD paletinin buna bağlanması, Noto Sans SemiBold/Bold ve TMP atlası, builder ile yeni sprite seti (yuvarlak 9-slice, gölge, gradient, daire, halka) | ~3 sa | Satır içi `Color(...)` sayısı ≈0; Türkçe glif testi geçiyor |
| 2 | Bileşen kiti: `Button` (altın/cam/tehlike), `IconButton`, `StatPill`, `Card`, `Chip`, `Switch`, `Slider`, `SegmentedControl`, `ProgressSegments`, `RadialCooldown`, `RadialMenu`, `Modal`, `BottomSheet`. Hepsi 5 durumlu ve ≥44 pt dokunma alanlı | ~4 sa | Bileşen başına EditMode testi |
| 3 | Savaş HUD'u (mockup A/C), pause, uzmanlaşma modalı, kamera kadrajı | ~6 sa | PlayMode HUD testleri; 3 oranda görüntü; savaş alanı kapanma oranı ölçülür |
| 4 | Menü: ana menü (mockup B), harita (yıldızlı madalyon düğümler, bölge segmenti, alttan açılan brifing paneli), ayarlar (gruplu kart + switch), hesap, sonuç (yıldız grafiği + animasyon) | ~5 sa | Taşma hatası 0; FrontendExperience testleri yeşil |
| 5 | 3B savaş alanı: zemin malzemesi/rengi, slot platformu (mavi disk yerine taş kaide + altın halka), rota kenarı, ışık ve post-process'in menü illüstrasyonuyla eşleşmesi | ~1–2 gün | Önce/sonra görüntü; fiziksel cihazda FPS kapısı |
| 6 | Cila: giriş/çıkış geçişleri, sayı tick'i, yıldız patlaması, haptik; azaltılmış hareket desteği korunur | ~0,5 gün | Reduced-motion testi |

Her fazın sonunda EditMode + PlayMode testleri, simülatör kabul modu (`DC_FRONTEND_ACCEPTANCE=1`) ile önce/sonra görüntüsü, TR/EN taşma ve 44 pt dokunma kontrolü yapılır.

## Riskler

- **Test adları:** mevcut nesne adları korunur. Hiyerarşi değişirse testler aynı commit'te güncellenir.
- **Çeviri tuzağı:** Türkçe etiket değişince İngilizce çeviri sessizce bozulur. Faz 0 bunu kapatmadan metin değiştirilmez.
- **Performans:** dinamik sayılar (altın, cooldown) ayrı alt canvas'ta tutulur. Blur ve grab-pass kullanılmaz. Yeni sprite'lar mevcut atlas'a eklenir.
- **Kapsam kayması:** yeni ekran ve menü eklenmez (`MOBILE_UX.md`: MVP dışı boş menü yok).
