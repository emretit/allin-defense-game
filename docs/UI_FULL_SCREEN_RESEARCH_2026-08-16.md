# Defender Chronicles — Tam Ekran Mobil HUD Araştırması

Durum: Uygulandı
Tarih: 2026-08-16

Simulator kabul görüntüsü: `docs/evidence/defender-fullscreen-iphone17pro-2026-08-16.png`

## 1. Açık Kaynak Karşılaştırması

### UnityRoyale

Kaynak: https://github.com/ciro-unity/UnityRoyale-Public

- Mobil için baştan tasarlanmış, savaş alanı ile kart/aksiyon katmanlarını ayıran bir Unity örneği.
- Çıkarım: oyun dünyası ve HUD aynı sabit dikdörtgene sıkıştırılmamalı; HUD ayrı bir ekran katmanı olarak davranmalı.
- Defender Chronicles kararı: kamera kadrajı cihaz oranına göre ayarlanırken HUD bağımsız olarak safe area içinde çalışır.

### MiniTD / TDGame

Kaynak: https://github.com/eldemirberkay0/TDGame

- İnşa, yükseltme, yok etme ve dalga akışını ayrı UI durumları olarak ele alan tamamlanmış bir mini tower-defense döngüsü.
- Çıkarım: sürekli görünen savaş bilgileri ile bağlamsal seçim panelleri ayrılmalı.
- Defender Chronicles kararı: üst durum şeridi ve alt aksiyon şeridi kalıcı; karakol paneli yalnızca seçim varken görünür kalır.

### UnityMobileToolkit

Kaynak: https://github.com/janezk7/UnityMobileToolkit

- Safe area, üst bar ve içerik katmanlarını ayrı hiyerarşilerde tutan responsive mobil örneği.
- Çıkarım: safe area bütün görsel yüzeyi küçültmemeli; yalnız okunabilir ve dokunulabilir içeriği sınırlandırmalı.
- Defender Chronicles kararı: dekoratif HUD rayları fiziksel ekran kenarına uzanır, `SafeContent` katmanları çentik ve sistem hareket alanlarından korunur.

## 2. Kök Neden

- Kamera her cihazda sabit `orthographicSize = 8.9` kullanıyordu.
- 16:9'dan geniş telefonlarda yatay görüş hacmi gereksiz büyüyor, harita ekranın ortasında küçük kalıyordu.
- Üretim HUD prefabı bütünüyle safe area altına taşınıyor; bu nedenle bar arka planları da ekran kenarlarından geri çekiliyordu.
- HUD elemanları 1920×1080 sabit koordinatlarına bağlıydı; geniş cihazlarda boşluklar büyüyor fakat bilgi hiyerarşisi gelişmiyordu.

## 3. Uygulanan Yerleşim Modeli

- `AdaptiveBattlefieldCamera`, kritik savaş alanının yatay genişliğini safe area oranına göre kadraja sığdırır.
- Ultra-geniş cihazlarda kamera yakınlaşır; 4:3 gibi dar yatay oranlarda bütün kritik rota görünür kalacak kadar uzaklaşır.
- Alt aksiyon şeridi üst şeritten daha yüksek olduğu için kamera görsel merkezi hafif yukarı taşır.
- HUD kökü artık tam Canvas'a bağlıdır; üst ve alt dekoratif yüzeyler kenardan kenara çizilir.
- Üst ve alt şerit içinde çalışma anında `SafeContent` katmanları kurulur.
- Kaynaklar, dalga, durum, duraklatma, kule türleri, kahraman yeteneği ve dalga CTA'sı yüzde tabanlı yatay bölgelere yerleşir.
- Sonuç perdesi tam ekran kalır; seçim paneli ve mesajlar safe area merkezine/kenarına bağlanır.

## 4. Eklenen Codex Becerileri

Kaynak: https://github.com/Unity-Technologies/skills

- `unity-cli`: Unity çalıştırma, test, build ve canlı Editor otomasyonu.
- `ui`: Unity UI isteklerini doğru UI sistemine yönlendirme.
- `ui-ugui`: Canvas, RectTransform, anchor, Layout Group ve prefab tabanlı uGUI geliştirme.

Kurulum hedefi: `~/.codex/skills`. Beceriler bir sonraki Codex turundan itibaren kullanılabilir.

## 5. Kabul Matrisi

- 16:9 yatay: bütün rota ve sekiz slot görünür, HUD dengeli.
- 19.5:9–20:9 yatay: harita daha büyük görünür, dekoratif raylar ekranı doldurur.
- Çentikli iPhone yatay: dokunulabilir kontroller safe area içinde kalır.
- 4:3 tablet yatay: kamera kritik yatay alanı korumak için uzaklaşır.
- Yön/çözünürlük değişimi: kamera ve HUD çalışma anında yeniden hesaplanır.
- Sonuç, pause ve seçili karakol katmanları mevcut davranışını korur.
