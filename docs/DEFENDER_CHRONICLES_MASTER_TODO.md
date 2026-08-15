# Defender Chronicles — Gerçek Durum Listesi

Son güncelleme: 2026-08-16

Bu belge eski “belgelendi = tamamlandı” yaklaşımını kaldırır. Aşağıdaki durumlar kabul kapısının gerçek seviyesini gösterir:

- **Doğrulandı:** Kodlandı ve ilgili otomatik/Simulator kabulü geçti.
- **Uygulandı:** Kod mevcut; bütün hedef cihaz, denge veya kullanıcı kabulleri henüz geçmedi.
- **Tasarlandı:** Yalnız belge, şema veya karar mevcut; tamamlanmış sayılmaz.
- **Bekliyor:** Henüz bu teslimin parçası değil.

## Ürün ve teknik temel

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-001–010 | Tasarlandı | Ürün ve kapsam belgeleri var; dış oyuncu doğrulaması yok. |
| DC-011 | Doğrulandı | Unity 6000.4.1f1 ve paket kilidiyle build alındı. |
| DC-012 | Tasarlandı | Kod envanteri belgeli; eski prototip kodu hâlâ arşiv olarak projede. |
| DC-013 | Uygulandı | `CombatMath`, tanımlar ve oturum ayrıldı; daha fazla saf mantık ayrıştırması mümkün. |
| DC-014 | Doğrulandı | Boot, Preparing, Wave, Intermission, Paused, Victory, Defeat çalışıyor. |
| DC-015–016 | Uygulandı | Tanım sınıfları ve ID alanları var; gri kutu koşusu henüz kalıcı asset’lere taşınmadı. |
| DC-017–018 | Doğrulandı | Durum/altın/can olayları ve pause zaman yönetimi çalışıyor. |
| DC-019–020 | Tasarlandı | Tohumlu rastgelelik ve object pool bu dilimde gerekmiyor; uygulanmadı. |
| DC-021 | Doğrulandı | Bootstrap yalnız oturum oluşturma ve doğrulama yapıyor. |
| DC-022–024 | Tasarlandı | Addressables, kayıt migration ve tam geliştirici menüsü kapsam dışı. |
| DC-025 | Doğrulandı | Development runtime error/exception kaydı ve sıfır hata kapısı var. |

## Harita, savaş ve Knight

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-026–030 | Doğrulandı | XZ rota, rota ilerlemesi, 8 sabit slot ve mobil kurulum akışı çalışıyor. |
| DC-031 | Doğrulandı | Revize kapsamın tek yükseltme seviyesi çalışıyor; dallanan ağaç kapsam dışı. |
| DC-032–037 | Doğrulandı | %60 iade, geçici bozma, 4 komuta noktası, görünür yol ve dokunma önceliği var. |
| DC-038–039 | Doğrulandı | Ortak hasar sözleşmesi ve iki engelleyici/yeniden doğan kışla birliği çalışıyor. |
| DC-040–041 | Tasarlandı | Ayrı menzilli/büyü dost birlikleri revize dikey dilimde yok. |
| DC-042–044 | Uygulandı | En ileride hedefleme, hasar, yavaşlatma ve stun var; tam strateji/direnç matrisi yok. |
| DC-045–046 | Uygulandı | Hasardan bağımsız kozmetik VFX havuzu var; gelişmiş kümelenme kapsam dışı. |
| DC-047 | Doğrulandı | Düşman temizliği, birlik yeniden doğması ve Knight geri dönüşü çalışıyor. |
| DC-048 | Doğrulandı | Takım rengi, siluet, animasyon ve sağlık çubuğu production sunumunda okunur. |
| DC-049–050 | Uygulandı | Havuzlu VFX ve ses geri bildirimi var; oyuncu 2× hızı kapsam dışı. |
| DC-051–055 | Doğrulandı | Knight rolü, komuta hareketi, saldırı, hedeflenme, Shield Bash ve cooldown HUD çalışıyor. |
| DC-056–060 | Tasarlandı | XP, seviye, ekipman ve yükleme ekranı bu teslimin dışında. |

## Düşman, dalga ve ekonomi

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-061 | Doğrulandı | Raider, Vanguard, Regenerator, Support, Siege ve boss ayrı production siluetlerine sahip. |
| DC-062–068 | Doğrulandı | Altı rol ve %66/%33 eşikli boss, geçici karakol bozma ile Simulator’da tamamlandı. |
| DC-069 | Uygulandı | `WaveDefinition` var; ilk bölüm tanımları şu an kod içi, asset editörü bekliyor. |
| DC-070 | Tasarlandı | Otomatik tehdit bütçesi uygulanmadı. |
| DC-071 | Doğrulandı | Sonraki ana tehdit HUD’da gösteriliyor. |
| DC-072 | Uygulandı | Erken dalga düğmesi var; kalan süreye göre ek ödül yok. |
| DC-073 | Doğrulandı | Tek savaş kaynağı altın. |
| DC-074–075 | Uygulandı | Maliyetler ve üç rol çalışıyor; üç ayrı stratejiyle manuel denge kabulü bekliyor. |
| DC-076–082 | Tasarlandı | Rastgele güç, meta para, ekipman, zorluk ve görevler kapsam dışı. |

## Mobil HUD ve sanat

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-083–084 | Doğrulandı | Prefab tabanlı TMP HUD, ortak panel dili, ikon/şekil desteği ve safe-area yerleşimi var. |
| DC-085–087 | Doğrulandı | Can, altın, dalga, Knight, karakol rol/değer/yükseltme/satış bilgileri çalışıyor. |
| DC-088 | Uygulandı | Shield Bash önizleme ve cooldown var; ayrı hedefleme/iptal modu yok. |
| DC-089 | Doğrulandı | İlk üç dalga için kısa oyun içi öğretici mesajları var. |
| DC-090–094 | Uygulandı | Türkçe kopya katalogda; İngilizce katalog eklemeye hazır. Menü/bölüm seçimi/ayarlar kapsam dışı. |
| DC-095 | Doğrulandı | HUD ayrı safe-area panelinde ve yatay Dynamic Island Simulator’da görünür. |
| DC-096 | Uygulandı | Özgün 1024×1024 uygulama ikonu üretildi. |
| DC-097–105 | Doğrulandı | Özgün Blender/FBX seti, stilize görsel durumlar, VFX havuzu, 22 ses olayı ve 6 müzik durumu production kataloğunda. |

## Kalite ve sürüm kapıları

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-106–109 | Tasarlandı | Erişilebilirlik ve performans bütçeleri gerçek cihaz geçişi bekliyor. |
| DC-110 | Uygulandı | 9 EditMode testi geçti; katalog ve Türkçe kopya bütünlüğü de kapsanıyor. |
| DC-111 | Doğrulandı | 2 PlayMode testi ekonomi/pause ve production sunumunu kapsıyor; iOS kabul koşuları geçti. |
| DC-112 | Tasarlandı | Kalıcı kayıt bu dilimde yok. |
| DC-113 | Uygulandı | Açılış ve zafer görüntüleri saklandı; otomatik piksel karşılaştırması yok. |
| DC-114 | Bekliyor | 30–60 dakikalık dayanıklılık testi yapılmadı. |
| DC-115 | Uygulandı | Input olayları null-safe; gerçek hızlı/çoklu dokunma cihaz matrisi bekliyor. |
| DC-116 | Uygulandı | Hızlandırılmış tam bölüm kabul modu var; istatistiksel denge simülatörü yok. |
| DC-117 | Bekliyor | 5–10 dış oyuncu testi yapılmadı. |
| DC-118 | Doğrulandı | Final Development Simulator açılışı ve tam koşularda yakalanmış error/exception yok. |
| DC-119 | Doğrulandı | Final iOS yapısı iki kez 6 normal dalga + boss zaferiyle baştan sona tamamlandı. |
| DC-120 | Uygulandı | Karma savunma kazanıyor; üç farklı karakol ağırlıklı strateji manuel kabulü bekliyor. |
| DC-121 | Doğrulandı | Tek haritanın tüm aktörleri, karakolları, HUD, VFX, ses ve müziği sanatsal dikey dilime taşındı. |
| DC-122 | Bekliyor | Performans Simulator’da değil gerçek iPhone’da ölçülecek. |
| DC-123 | Bekliyor | Dış kullanıcı testi yapılacak. |
| DC-124–128 | Tasarlandı | Sonraki kampanya, hesap, liderlik, gelir ve mağaza belgeleri var; uygulanmadı. |

## Sanatsal dikey dilim kabul özeti

- **Geçti:** production katalog doğrulaması, özgün görsel set, TMP HUD, Türkçe kopya, havuzlu VFX, mixer/müzik durumları, build, kurulum, yatay açılış, safe area, üç karakol, Knight, 7 dalga, boss, zafer, yenilgi, pause ve restart.
- **Kısmi:** gerçek parmakla hızlı/çoklu dokunma, üç ayrı strateji dengesi ve 8–12 dakikalık dış oyuncu süre ölçümü.
- **Sonraki kapı:** fiziksel iPhone'da dokunma, bellek, termal durum ve kesin 30/60 FPS performans testi.
