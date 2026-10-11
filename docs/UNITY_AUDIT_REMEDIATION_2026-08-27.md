# Unity audit remediation — 27 Ağustos 2026

## Sonuç

Denetimde bulunan yüksek ve orta öncelikli kod/varlık riskleri `codex/audit-remediation` dalında giderildi. Üretim Unity Services ortamına otomatik dağıtım yapılmadı; sunucu kaynakları önce geliştirme ortamında doğrulanıp daha sonra kontrollü olarak üretime alınmalıdır.

## Uygulanan düzeltmeler

- İlerleme ödülleri istemciden alınmak yerine `CampaignProgression` Cloud Code C# modülünde kanonik kurallarla hesaplanıyor.
- Protected Cloud Save durumu sürüm, yazma kilidi, son işlenen claim kimlikleri ve tekrar oynatma koruması taşıyor.
- Çevrimdışıyken bölüm sonuçları sıralı ve kalıcı bir claim kuyruğuna yazılıyor; bağlantı geldiğinde en fazla 20’lik gruplarla senkronlanıyor.
- Misafir akışı korunurken Apple/Google’a özel iki düğme yerine tek Unity Player Account bağlantısı kullanılıyor.
- Türkçe ve İngilizce locale varlıkları/string tabloları eklendi. Dil ayarı UI, profil, bölüm seçimi, brifing, sonuç ve savaş HUD’ını sahne yeniden başlatmadan yeniliyor.
- Ana TMP fontları TR/EN karakter setiyle statik hale getirildi. Beklenmeyen çevrimiçi görünen ad karakterleri için ayrı dinamik Noto Sans fallback bırakıldı.
- UI ikonları rotasyonsuz, tight-packing kapalı ve 9-slice güvenli padding ile tek SpriteAtlas altında toplandı.
- Master mixer’a hızlı saldırı/yüksek oranlı güvenlik kompresörü eklendi. Mevcut ses havuzu ve kritik cue öncelikleri korundu.
- Dünya seçim ışını yalnız `Selectable` katmanını ve trigger olmayan collider’ları sorguluyor; öndeki dekor/aktör collider’ı seçimi engellemiyor.
- Kullanılmayan URP depth texture isteği iki mobil pipeline varlığında kapatıldı; volume profili tekrar kurulumda yeniden kullanılacak şekilde idempotent hale getirildi.
- Android ARM64/IL2CPP, API 29 ve APK/AAB build komutları eklendi.
- AI Navigation ve Multiplayer Center kullanılmadıkları için kaldırıldı; Localization, Cloud Code, Deployment, Tooling ve Unity Pipeline paketleri sabitlendi.

## Kanonik canlı oyun kaynakları

- İstemci/sunucu ortak kural kaynağı: `Assets/Resources/LiveGame/campaign-rules-v1.json`
- Cloud Code modülü: `CampaignProgressionCCM/`
- Deployment kaynakları: `Assets/CloudCode/CampaignProgression.ccmr` ve `CampaignProgression.ac`
- Korunan kayıt anahtarı: `campaign_state_v3`

Access Control dosyası eski istemciler için geçiş planı yapılmadan üretime dağıtılmamalıdır. Protected bucket zaten doğrudan oyuncu yazımına kapalıdır; genel Player Data yazma yasağı ancak eski sürüm kullanım oranı kabul eşiğinin altına indiğinde etkinleştirilmelidir.

## Otomatik kapılar

Son çalışma sonuçları:

- Unity EditMode: 78/78 geçti.
- Unity PlayMode: 13/13 geçti.
- Cloud Code .NET: 4/4 geçti.
- `unity projects verify`: 0 hata, 0 uyarı.
- Android Development APK: 90 MB, `com.emreaydin.defenderchronicles`, min SDK 29, yalnız ARM64/IL2CPP.
- APK SHA-256: `7db84f74e05a7de148946d4cfa1562d7919a38b84506a379966c76c6abeee1d3`.
- Güncel iOS Simulator Development Xcode projesi üretildi; imzasız Debug `.app` derlemesi x86_64 + arm64 için geçti.

- 50 istemci seviyesinin JSON kurallarıyla birebir eşleşmesi
- Claim kuyruğunun restart sonrası sırası ve sunucu cevabının kanonik sonucu uygulaması
- TR/EN locale ve temel metin erişimi
- Statik ana TMP fontları, dinamik fallback ve Türkçe glif kapsamı
- 9-slice güvenli SpriteAtlas ayarları
- Master güvenlik kompresörü
- Selectable fizik katmanı ve engelleyici collider regresyonu
- Mobil URP depth texture kapısı

## Dağıtım öncesi dış kapılar

1. Cloud Code modülünü ve kuralları Unity Services geliştirme ortamına dağıt; aynı claim’in tekrar gönderimi, iki cihaz eşzamanlı yazımı ve çevrimdışı→çevrimiçi senaryolarını gerçek servisle doğrula.
2. Access Control kaynağını önce geliştirme ortamında doğrula; üretime ancak eski istemci geçiş koşulu sağlandığında al.
3. Release Android AAB için keystore/alias bilgilerini CI secret olarak tanımla.
4. Bir fiziksel iPhone ve bir fiziksel Android cihazda uzun savaş, düşük bellek, termal durum, arka plan/geri dönüş ve ağ kopma testlerini çalıştır.
5. App Store/Play Console kimlikleri, gizlilik ve kullanım koşulları URL’leri ile Unity Environment eşlemesini release yapılandırmasında doğrula.
