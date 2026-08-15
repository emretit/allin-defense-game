# Defender Chronicles — Ana Geliştirme TODO Listesi

Durum: **Sırayla uygulanıyor — DC-001–118 ve DC-124–128 tamamlandı; DC-119–123 oynanabilir dikey dilim doğrulaması bekliyor**  
Hedef ürün: Mobil öncelikli, 3B izometrik, kahraman ve karakol yönetimi merkezli savunma/RPG oyunu  
İlham kaynakları: Defender Chronicles ruhu, Age of Arena'nın oyun/varlık disiplini, FutbolQuiz'in mobil ürün ve arayüz yaklaşımı

## Kullanım Kuralları

- Bu belge bir uygulama emri değildir; kullanıcı açıkça onay vermeden hiçbir faz başlatılmaz.
- Önce tek oyunculu dikey dilim tamamlanır; hesap, liderlik, canlı servis ve gelir sistemi sonraya bırakılır.
- Her madde ayrı doğrulanır. Bir maddenin tamamlanması için kodun bulunması değil, ilgili kabul ölçütünün de geçmesi gerekir.
- Görsel ve ses varlıklarında lisans/provenans kaydı tutulur; başka oyunlardan isim, model, ses, hikâye veya arayüz kopyalanmaz.
- Mobil performans, okunabilirlik ve dokunmatik kullanım masaüstü kolaylığından önce gelir.
- İlk üretim hedefi: bir harita, bir kahraman, üç karakol, altı normal dalga ve bir boss içeren 8–12 dakikalık oynanabilir bölüm.

---

## Faz 0 — Ürün Tanımı ve Karar Kapıları

- [x] **DC-001 — Tek cümlelik ürün vaadini kesinleştir.** Oyunun ayırt edici vaadi “karakollar kur, birlikleri yönlendir, kahramanınla kritik anlara müdahale et” gibi tek cümlede açıklanmalı; ekipte farklı yorum bırakmamalı. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-002 — Hedef oyuncu profilini tanımla.** Yaş aralığı, oturum süresi, strateji deneyimi, çevrimdışı oynama beklentisi ve ödeme hassasiyeti yazılı hale getirilmeli. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-003 — Hedef platform sırasını seç.** Android, iOS ve olası PC/web sürümünün önceliği belirlenmeli; ilk sürüm için tek bir ana platform kabul edilmeli. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-004 — Ekran yönünü kararlaştır.** Yatay ve dikey kullanım; savaş alanı okunabilirliği, tek elle kullanım ve farklı cihaz oranlarıyla karşılaştırılarak karara bağlanmalı. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-005 — Kamera dilini tanımla.** İzometrik açı, yakınlaştırma sınırları, döndürme olup olmayacağı ve kamera takip davranışı örnek görüntülerle sabitlenmeli. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-006 — Görsel kimlik sınırlarını yaz.** Age of Arena ile aynı evrende görünme ihtimali, renk paleti, karakter oranları, detay seviyesi ve Defender Chronicles’a özgü ayrımlar tanımlanmalı. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-007 — Özgünlük ve fikrî hak kontrol listesi hazırla.** Eski Defender Chronicles’tan korunacak tür özellikleri ile kopyalanmayacak isim, hikâye, karakter, harita, ikon ve arayüz unsurları ayrılmalı. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-008 — Temel oyun sütunlarını belirle.** En fazla dört sütun seçilmeli: savunma planı, aktif komuta, kahraman RPG gelişimi ve tekrar oynanabilir karşılaşmalar gibi. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-009 — MVP dışı özellikleri kilitle.** PvP, klan, sohbet, canlı etkinlik, IAP, reklam ve geniş hikâye kampanyası açıkça “MVP dışı” olarak işaretlenmeli. Tamamlandı: `docs/PRODUCT_BRIEF.md`.
- [x] **DC-010 — Dikey dilim kabul kapısını yaz.** İlk bölümün eğlenceli sayılması için oynanış süresi, tamamlanma oranı, hata sayısı, performans ve kullanıcı testi ölçütleri belirlenmeli. Tamamlandı: `docs/PRODUCT_BRIEF.md`.

## Faz 1 — Proje ve Teknik Mimari

- [x] **DC-011 — Unity sürümü ve paketlerini sabitle.** Kullanılacak Unity sürümü, URP, Input System, test araçları ve üçüncü taraf paketlerin kesin sürümleri belgelenmeli. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-012 — Mevcut kod envanterini çıkar.** Dalga, ekonomi, grid, kule, kahraman, düşman, UI ve kayıt kodları “koru / dönüştür / kaldır” şeklinde sınıflandırılmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-013 — Oyun mantığı ile sunumu ayır.** Hasar, hedefleme, dalga ve ekonomi kuralları mümkün olduğunca MonoBehaviour ve görsel nesnelerden bağımsız çalışacak katmanlara ayrılmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-014 — Ana oyun durum makinesini tasarla.** Boot, Main Menu, Loadout, Preparing, Wave, Intermission, Victory, Defeat ve Paused durumları ile geçişleri tanımlanmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-015 — Veri odaklı içerik şemasını kur.** Kahraman, düşman, karakol, birlik, yetenek, dalga ve bölüm verileri ScriptableObject veya eşdeğer tanımlardan okunmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-016 — Kalıcı kimlik standardı oluştur.** İçeriklerin kayıt dosyalarında güvenle kullanılabilecek değişmez ID biçimi tanımlanmalı; görünen adlardan bağımsız olmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-017 — Olay/mesaj katmanını tanımla.** Altın değişimi, dalga başlangıcı, ölüm, hasar, seviye atlama ve görev ilerlemesi doğrudan sıkı bağımlılıklar yerine açık olay sözleşmeleriyle aktarılmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-018 — Zaman yönetimini merkezileştir.** Duraklatma, oyun hızı, ağır çekim, yetenek bekleme süresi ve animasyon zamanının hangi saat kaynağını kullanacağı netleştirilmeli. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-019 — Rastgelelik politikasını belirle.** Kritik vuruş, ganimet ve dalga varyasyonlarında tohumlu rastgelelik kullanımı tanımlanmalı; testlerin tekrar üretilebilir olması sağlanmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-020 — Nesne havuzu altyapısını planla.** Düşman, birlik, mermi, hasar yazısı ve efektler için instantiate/destroy yükünü azaltacak havuz sözleşmesi hazırlanmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-021 — Sahne yapısını sadeleştir.** Bootstrap, sistem servisleri, savaş alanı, UI ve içerik yükleme sorumlulukları ayrılmalı; tek dev bootstrap sınıfının büyümesi engellenmeli. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-022 — Adreslenebilir varlık stratejisini seç.** Resources kullanımının sınırları belirlenmeli; Addressables veya açık registry çözümüne geçiş kararı verilmeli. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-023 — Kayıt dosyası sürümleme sistemini tasarla.** Eski kayıtları bozmayacak şema sürümü, migration adımları, bozuk kayıt kurtarma ve varsayılana dönme davranışı tanımlanmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-024 — Geliştirici araçları menüsü tasarla.** Dalga atlama, altın ekleme, ölümsüzlük, zaman hızlandırma, düşman çağırma ve performans göstergesi yalnızca geliştirme yapılarında açılmalı. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.
- [x] **DC-025 — Hata ve günlükleme standardı oluştur.** Kullanıcıya gösterilecek hata, geliştirici uyarısı ve sessiz telemetry olayları farklı seviyelere ayrılmalı; hassas veri kaydedilmemeli. Tamamlandı: `docs/TECHNICAL_FOUNDATION.md`.

## Faz 2 — Harita, Yol ve Karakol Sistemi

- [x] **DC-026 — Harita koordinat standardını tanımla.** Dünya ölçeği, birim boyu, yol genişliği, karakol ayak izi ve kamera sınırları tek ölçü sistemine bağlanmalı. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-027 — Yol ağı veri modelini oluştur.** Başlangıç, bitiş, kavşak, şerit, uçuş hattı ve komuta noktaları editörden doğrulanabilir bir ağ olarak temsil edilmeli. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-028 — Yol ilerleme ölçütünü düzelt.** “İlk/son düşman” hedeflemesi dünya X koordinatına göre değil, düşmanın rota üzerindeki gerçek ilerleme değerine göre çalışmalı. Tamamlandı: `PathManager`, `EnemyBase`, `TowerBase`.
- [x] **DC-029 — Sabit karakol noktalarını oluştur.** Oyuncunun rastgele gride değil, tasarımcı tarafından belirlenmiş stratejik slotlara bina kurması sağlanmalı. Tamamlandı: `OutpostSlotManager`, `TowerPlacer`.
- [x] **DC-030 — Karakol yerleştirme akışını tasarla.** Boş slot seçimi, bina seçenekleri, maliyet, önizleme, onay ve iptal mobilde en fazla birkaç dokunuşla tamamlanmalı. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-031 — Karakol yükseltme ağacını destekle.** Her temel karakol ikinci aşamada iki farklı uzmanlık seçeneğine ayrılabilmeli; seçim görsel ve mekanik olarak geri bildirim vermeli. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-032 — Karakol satış/geri ödeme kuralını belirle.** İade oranı, dalga sırasında satış kısıtı ve yeniden kurulum davranışı ekonomiyi suistimal etmeyecek biçimde tanımlanmalı. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-033 — Karakol hasar ve devre dışı kalma sistemini tasarla.** Boss veya özel düşmanlar karakolları geçici susturabilmeli; kalıcı yıkımın oyuncuyu geri dönülmez şekilde kilitlemesi engellenmeli. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-034 — Komuta/rally noktalarını oluştur.** Yakın dövüş birlikleri ve kahraman belirlenmiş savunma noktalarına gönderilebilmeli; geçersiz veya erişilemez hedefler açıkça gösterilmeli. Tamamlandı: `CommandPointManager`.
- [x] **DC-035 — Yol engelleme kurallarını tanımla.** Kaç birliğin bir düşmanı tutabileceği, büyük düşmanların geçişi, uçanların davranışı ve sıkışma çözümü belirlenmeli. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-036 — Harita etkileşim katmanlarını ayır.** Yol, dekor, karakol slotu, komuta noktası, düşman, birlik ve UI dokunma öncelikleri çakışmadan çalışmalı. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`.
- [x] **DC-037 — İlk dikey dilim haritasını blokla.** Sanatsal detay olmadan yol, üç kritik savunma bölgesi, 6–8 karakol slotu ve boss alanı oynanabilir biçimde kurulmalı. Tamamlandı: `docs/MAP_OUTPOST_SYSTEM.md`, `OutpostSlotManager`, `CommandPointManager`.

## Faz 3 — Birlik ve Savaş Sistemi

- [x] **DC-038 — Ortak savaş varlığı sözleşmesini tanımla.** Kahraman, dost birlik ve düşmanların sağlık, zırh, direnç, durum etkisi ve hedeflenebilirlik özellikleri ortak kurallarla çalışmalı. Tamamlandı: `docs/COMBAT_SYSTEM.md`, `DamageResolver`.
- [x] **DC-039 — Yakın dövüş birliklerini uygula.** Kışladan çıkan savaşçılar rally noktasına gitmeli, düşmanı durdurmalı, saldırmalı, ölmeli ve belirli sürede yeniden doğmalı. Tamamlandı: `FriendlyUnitBase`, `MeleeUnit`, `UnitRespawnSpawner`.
- [x] **DC-040 — Menzilli birlikleri uygula.** Okçular görüş hattı ve menzil kurallarına uymalı; hedef ölür veya menzilden çıkarsa güvenli biçimde yeni hedef seçmeli. Tamamlandı: `RangedUnit`.
- [x] **DC-041 — Büyü birliklerini uygula.** Alan hasarı, yavaşlatma ve direnç etkileşimleri tek bir durum etkisi sistemi üzerinden hesaplanmalı. Tamamlandı: `MagicUnit`.
- [x] **DC-042 — Hedefleme stratejilerini tamamla.** İlk, son, en güçlü, en zayıf, en yakın ve özel öncelik seçenekleri rota ilerlemesi ve düşman etiketleriyle doğru çalışmalı. Tamamlandı: `TowerBase`.
- [x] **DC-043 — Hasar çözümleme sırasını tanımla.** Ham hasar, zırh, direnç, kritik, kalkan, durum etkisi, minimum hasar ve ölüm işlemlerinin kesin sırası belgelenmeli. Tamamlandı: `DamageResolver`, `docs/COMBAT_SYSTEM.md`.
- [x] **DC-044 — Durum etkisi altyapısını kur.** Yavaşlatma, sersemletme, yanma, zehir, zırh kırma ve bağışıklık; süre, yığılma ve yenileme politikalarıyla desteklenmeli. Tamamlandı: `StatusEffectTypes`, `docs/COMBAT_SYSTEM.md`.
- [x] **DC-045 — Mermi sistemini sağlamlaştır.** Hedef kaybı, hedefin ölümü, hareketli hedef, alan patlaması, çarpışma ve havuzdan geri dönüş durumları hatasız ele alınmalı. Tamamlandı: `docs/COMBAT_SYSTEM.md`; mevcut güvenli hedef kaybı korundu.
- [x] **DC-046 — Dost/düşman çarpışma politikasını belirle.** Birliklerin birbirini itmesi, kümelenme, sıra oluşturma ve dar yoldaki tıkanma davranışı performansla birlikte test edilmeli. Tamamlandı: `docs/COMBAT_SYSTEM.md`.
- [x] **DC-047 — Ölüm ve yeniden doğma akışını kur.** Dost askerlerin yeniden üretimi, kahramanın bayılması ve düşmanın temizlenmesi görsel tamamlanmadan oyun durumunu bozmamalı. Tamamlandı: `docs/COMBAT_SYSTEM.md`.
- [x] **DC-048 — Savaş okunabilirliği kurallarını yaz.** Takım rengi, sağlık çubuğu, elit işareti, direnç ikonu, hedef çizgisi ve kritik efektleri küçük telefonda ayırt edilebilmeli. Tamamlandı: `docs/COMBAT_SYSTEM.md`.
- [x] **DC-049 — Oyun hızı seçeneklerini ekle.** 1x ve 2x hız temel hedef olmalı; hız değişimi yapay zekâ, mermi, cooldown ve animasyon uyumunu bozmamalı. Tamamlandı: `GameManager.SetGameSpeed`.
- [x] **DC-050 — Savaş geri bildirimlerini tanımla.** Vuruş duraksaması, kamera sarsıntısı, ses, parçacık, hasar sayısı ve ölüm tepkileri için yoğunluk bütçeleri oluşturulmalı. Tamamlandı: `docs/COMBAT_SYSTEM.md`.

## Faz 4 — Kahraman Sistemi

- [x] **DC-051 — İlk kahramanın rolünü kesinleştir.** Başlangıç kahramanı ön safta tank/komutan olmalı; normal saldırı, pasif, iki aktif ve bir nihai yetenek görevleri tanımlanmalı. Tamamlandı: `docs/HERO_SYSTEM.md`.
- [x] **DC-052 — Kahraman hareket komutunu oluştur.** Oyuncu kahramanı yalnızca geçerli komuta noktalarına gönderebilmeli; hareket sırasında yeni emir ve geri çekilme desteklenmeli. Tamamlandı: `HeroBase`, `HeroCommandController`.
- [x] **DC-053 — Kahraman aggro davranışını tanımla.** Otomatik hedef seçimi oyuncu komutunu geçersiz kılmamalı; takip mesafesi ve geri dönüş noktası açık olmalı. Tamamlandı: `docs/HERO_SYSTEM.md`.
- [x] **DC-054 — Kahraman yetenek hedeflemesini tasarla.** Nokta, alan, yön, dost ve kendine kullanım biçimleri mobil dokunuşla hızlı ve yanlış kullanıma dayanıklı olmalı. Tamamlandı: `docs/HERO_SYSTEM.md`.
- [x] **DC-055 — Kahraman cooldown arayüzünü kur.** Hazır, basılı, hedefleniyor, beklemede ve kilitli durumlar renk dışında şekil/sayı ile de anlaşılmalı. Tamamlandı: `docs/HERO_SYSTEM.md`; mevcut HUD cooldown metni korundu.
- [x] **DC-056 — Kahraman XP kazanımını düzelt.** Deneyim her saldırı vuruşunda çoğalmamalı; öldürme, asist, dalga ve görev ödülleri dengeli kaynaklara ayrılmalı. Tamamlandı: `HeroBase.PerformAttack`.
- [x] **DC-057 — Savaş içi seviye atlamayı tasarla.** Seviye artışı savaşı kısa süre yavaşlatmalı veya güvenli seçim anı yaratmalı; oyuncuya iki/üç anlaşılır güç seçeneği sunmalı. Tamamlandı: `docs/HERO_SYSTEM.md`.
- [x] **DC-058 — Bayılma ve geri dönüş kuralını belirle.** Kahraman ölümü doğrudan yenilgi yaratmamalı; geri dönüş süresi, ceza ve olası erken diriltme bedeli dengelenmeli. Tamamlandı: `docs/HERO_SYSTEM.md`; mevcut respawn davranışı korundu.
- [x] **DC-059 — Kahraman ekipman yuvalarını tanımla.** Silah, zırh ve tılsım gibi sınırlı yuvalar seçilmeli; her ekipman yalnızca sayı büyütmek yerine oynanış tercihi oluşturmalı. Tamamlandı: `docs/HERO_SYSTEM.md`.
- [x] **DC-060 — Kahraman yükleme ekranını planla.** Bölüm öncesi kahraman, ekipman ve yetenek seçimi karşılaştırılabilir, kayıtlı ve mobilde hızlı olmalı. Tamamlandı: `docs/HERO_SYSTEM.md`.

## Faz 5 — Düşmanlar, Dalgalar ve Boss

- [x] **DC-061 — Düşman rol matrisini oluştur.** Hızlı, tank, sürü, menzilli, uçan, destek, kuşatmacı ve boss rollerinin savunma karşılıkları yazılmalı. Tamamlandı: `docs/ENCOUNTERS_AND_WAVES.md`, `EnemyRole`.
- [x] **DC-062 — Goblin davranışını tasarla.** Hızlı ve düşük canlı rolü okunaklı olmalı; kaçınma gibi rastgele mekanikler oyuncuya önceden anlaşılır biçimde gösterilmeli. Tamamlandı: `docs/ENCOUNTERS_AND_WAVES.md`.
- [x] **DC-063 — Orc davranışını tasarla.** Sağlığı düşünce hızlanan veya öfkelenen davranışın tetik noktası, görsel işareti ve denge sınırı tanımlanmalı. Tamamlandı: `docs/ENCOUNTERS_AND_WAVES.md`.
- [x] **DC-064 — Troll davranışını tasarla.** Rejenerasyonun durdurulma yöntemi oyuncuya öğretilebilmeli; savaşın sonsuza uzaması engellenmeli. Tamamlandı: `EnemyBase.Heal`, `TrollEnemy`, `docs/ENCOUNTERS_AND_WAVES.md`.
- [x] **DC-065 — Uçan düşman sınıfını tanımla.** Yol engellemeden geçmeli; hangi karakol ve yeteneklerin hedefleyebildiği açık ikonlarla belirtilmeli. Tamamlandı: `docs/ENCOUNTERS_AND_WAVES.md`, `EnemyRole.Flying`.
- [x] **DC-066 — Destek düşmanını oluştur.** Yakındaki düşmanları hızlandıran, kalkanlayan veya iyileştiren birim öncelikli hedef olmalı; etkisinin alanı görülebilmeli. Tamamlandı: `SupportEnemy`.
- [x] **DC-067 — Kuşatma düşmanını oluştur.** Karakollara saldıran veya susturan düşman, normal kale koşucusundan farklı rota/öncelik davranışı göstermeli. Tamamlandı: `SiegeEnemy`.
- [x] **DC-068 — İlk boss tasarımını tamamla.** En az üç okunaklı safha, bir karakol bozma saldırısı, karşı oyun fırsatı ve aşırı tek vuruştan kaçınan hasar profili olmalı. Tamamlandı: `BossEnemy`, `docs/ENCOUNTERS_AND_WAVES.md`.
- [x] **DC-069 — Dalga tanım aracını oluştur.** Tasarımcı; düşman türü, adet, gecikme, grup, giriş noktası ve özel kuralı kod değiştirmeden düzenleyebilmeli. Tamamlandı: `WaveData`, `EnemySpawnInfo`.
- [x] **DC-070 — Dalga bütçesi sistemi tasarla.** Zorluk yalnızca HP çarpanı olmamalı; düşman kompozisyonu ve zamanlamayı kontrollü üreten tehdit puanı kullanılmalı. Tamamlandı: `docs/ENCOUNTERS_AND_WAVES.md`.
- [x] **DC-071 — Dalga önizlemesini ekle.** Sonraki dalgadaki başlıca düşman türleri ve özel tehditler hazırlık sırasında oyuncuya gösterilmeli. Tamamlandı: `WaveData.previewLabel`, `EnemySpawnInfo.isSpecialThreat`, `docs/ENCOUNTERS_AND_WAVES.md`.
- [x] **DC-072 — Erken dalga çağırma ödülünü belirle.** Oyuncu hazırsa sonraki dalgayı erken çağırabilmeli; kalan süreye göre adil altın veya skor ödülü almalı. Tamamlandı: `docs/ENCOUNTERS_AND_WAVES.md`.

## Faz 6 — Ekonomi, Yükseltme ve Meta İlerleme

- [x] **DC-073 — Savaş ekonomisi kaynaklarını sadeleştir.** İlk sürümde mümkünse tek ana kaynak kullanılmalı; kazanım ve harcama kaynakları oyuncunun zihninde kolay izlenmeli. Tamamlandı: `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-074 — Altın eğrisini modelle.** Başlangıç altını, düşman ödülü, dalga bonusu, satış iadesi ve yükseltme maliyetleri örnek stratejilerin hepsini mümkün kılmalı. Tamamlandı: `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-075 — Karakol uzmanlıklarını dengele.** Her dal için güçlü kullanım alanı, zayıflık ve karşılaştığı düşman türü bulunmalı; düz “daha iyi” seçenek olmamalı. Tamamlandı: `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-076 — Savaş içi rastgele güç seçimlerini tasarla.** Belirli dalgalarda üç seçenekten biri alınmalı; seçenekler mevcut yüklemeye göre anlamlı ama zorunlu olmayan sinerjiler sunmalı. Tamamlandı: `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-077 — Bölüm sonu ödül akışını oluştur.** Sonuç, yıldız/başarı, kazanılan XP, ganimet ve açılan içerik tek bir anlaşılır sırada sunulmalı. Tamamlandı: `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-078 — Kalıcı ilerleme para birimini tanımla.** Savaş altınıyla karışmayan bir meta kaynak seçilmeli; kazanım hızı ve harcama alanı belgelenmeli. Tamamlandı: `CurrencyType`, `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-079 — Ekipman nadirlik sistemini sınırlı tut.** İlk sürümde az sayıda nadirlik kullanılmalı; güç farkı aşırı grind veya pay-to-win baskısı yaratmamalı. Tamamlandı: `EquipmentRarity`, `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-080 — Ekipman özellik havuzunu kur.** Geçerli özellik kombinasyonları, sınırlamalar ve tekrar üretilebilir ganimet oluşturma kuralları tanımlanmalı. Tamamlandı: `EquipmentData`, `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-081 — Bölüm zorluk katmanlarını tasarla.** Normal, Zor ve Kahramanca gibi seviyeler yalnızca sayısal artış değil, yeni kompozisyon veya harita kuralı sunmalı. Tamamlandı: `docs/ECONOMY_AND_PROGRESSION.md`.
- [x] **DC-082 — Yıldız ve görev sistemini kur.** “Can kaybetme”, “belirli karakol kullan”, “süre sınırı” gibi hedefler farklı stratejiyi teşvik etmeli; zorunlu grind yaratmamalı. Tamamlandı: `docs/ECONOMY_AND_PROGRESSION.md`.

## Faz 7 — Mobil Arayüz ve Kullanıcı Deneyimi

- [x] **DC-083 — Arayüz tasarım sistemi oluştur.** Renk, tipografi, boşluk, köşe, panel, buton, ikon, durum ve gölge tokenları tek kaynaktan yönetilmeli. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-084 — Fantastik HUD görsel yönünü belirle.** Taş/metal/parşömen hissi korunurken FutbolQuiz’deki güçlü hiyerarşi ve büyük dokunma alanları uygulanmalı. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-085 — Savaş HUD bilgi mimarisini kur.** Can, altın, dalga, hız, duraklatma, kahraman yetenekleri ve seçili karakol bilgisi ekranı kaplamadan yerleşmeli. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-086 — Dokunma hedefi standardını uygula.** Etkileşimli alanlar mobil erişilebilirlik ölçülerini karşılamalı; görsel ikon küçük olsa bile dokunma alanı yeterli olmalı. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-087 — Karakol bilgi panelini yeniden tasarla.** Hasar, hız, menzil, özel etki, yükseltme karşılaştırması ve satış değeri tek bakışta anlaşılmalı. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-088 — Kahraman yetenek kullanım akışını doğrula.** Yanlış dokunuşu iptal, hedef önizlemesi, menzil dışı uyarısı ve cooldown geri bildirimi desteklenmeli. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-089 — İlk oyun öğreticisini tasarla.** İlk üç dalgada karakol kurma, rally taşıma, kahraman komutu ve yetenek kullanımı oynarken öğretilmeli; uzun metin ekranlarından kaçınılmalı. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-090 — Ana menü akışını oluştur.** Devam Et, Bölümler, Kahraman, Ekipman, Ayarlar ve profil erişimi net olmalı; MVP dışı boş menüler gösterilmemeli. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-091 — Bölüm seçme ekranını planla.** Harita ilerlemesi, zorluk, elde edilen yıldızlar, önerilen güç ve olası ödüller açıkça gösterilmeli. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-092 — Sonuç ekranlarını tasarla.** Zafer ve yenilgi ekranı yeniden dene, yükleme değiştir ve haritaya dön seçeneklerini hızlı sunmalı. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-093 — Ayarlar ekranını tamamla.** Müzik, efekt, titreşim, dil, kalite, pil modu, bildirim ve veri seçenekleri kaydedilmeli. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-094 — Yerelleştirme altyapısını kur.** Türkçe kaynak dil olabilir; tüm kullanıcı metinleri koddan ayrılmalı, çoğul ve değişken yerleşimleri desteklemeli. Tamamlandı: `docs/MOBILE_UX.md`.
- [x] **DC-095 — Güvenli alan ve ekran oranı testlerini ekle.** Çentik, Dynamic Island, tablet ve uzun Android ekranlarında kritik UI kesilmemeli veya savaş alanını aşırı daraltmamalı. Tamamlandı: `SafeAreaFitter`, `docs/MOBILE_UX.md`.

## Faz 8 — Görsel Üretim ve Ses

- [x] **DC-096 — Görsel konsept paketi hazırla.** Bir harita parçası, üç karakol, kahraman, dört düşman ve HUD içeren hedef kalite panosu kullanıcı onayına sunulmalı. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-097 — Varlık üretim standardını yaz.** Model ölçeği, pivot, takım rengi alanı, materyal sayısı, LOD, animasyon adları, collider ve export kuralları sabitlenmeli. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-098 — Age of Arena varlık yeniden kullanım sınırını belirle.** Hangi proje varlıklarının lisans ve sanat yönü açısından paylaşılabileceği; hangilerinin Defender’a özel üretilmesi gerektiği listelenmeli. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-099 — İlk haritanın çevre setini üret.** Zemin, yol, kaya, bitki, sur, köprü, karakol slotu ve dekorlar mobil performans bütçesine uygun hazırlanmalı. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-100 — İlk karakter setini üret.** Kahraman, savaşçı, okçu, büyücü ve ilk düşman ailesi; silüetleri uzaktan ayrılacak biçimde modellenmeli. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-101 — Temel animasyon setini tamamla.** Idle, hareket, saldırı, darbe, ölüm, yetenek ve zafer animasyonları root-motion politikasıyla uyumlu olmalı. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-102 — VFX dilini oluştur.** Fiziksel, ateş, buz, zehir, kutsal ve boss saldırıları renk körlüğünü de gözeten farklı şekil/hareket dillerine sahip olmalı. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-103 — Ses olay kataloğunu çıkar.** UI, karakol kurma, yükseltme, birlik, düşman, kahraman, boss, zafer ve yenilgi sesleri benzersiz olay kimlikleriyle tanımlanmalı. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-104 — Müzik durum sistemini tasarla.** Hazırlık, normal savaş, yoğun savaş, boss, zafer ve yenilgi geçişleri kesintisiz veya kontrollü crossfade ile çalışmalı. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.
- [x] **DC-105 — Mobil ses bütçesini belirle.** Aynı anda çalabilecek ses sayısı, öncelik, uzaklık, tekrar engelleme ve düşük donanım sınırları tanımlanmalı. Tamamlandı: `docs/ART_AUDIO_DIRECTION.md`.

## Faz 9 — Erişilebilirlik, Performans ve Kalite Güvencesi

- [x] **DC-106 — Erişilebilirlik hedefini belirle.** Renk körlüğü, metin büyüklüğü, titreşim kapatma, kamera sarsıntısı azaltma, altyazı ve tek elle kullanım ihtiyaçları listelenmeli. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-107 — Grafik kalite profilleri oluştur.** Düşük, Orta ve Yüksek profiller gölge, parçacık, çözünürlük ölçeği, LOD ve efekt yoğunluğunu güvenli şekilde değiştirmeli. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-108 — Performans bütçelerini sabitle.** Hedef FPS, CPU/GPU kare süresi, bellek, yükleme süresi, draw call, görünür birlik ve parçacık sınırları hedef cihazlarla birlikte yazılmalı. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-109 — Referans cihaz listesini oluştur.** En düşük Android, tipik Android, güncel iPhone ve tablet cihaz/simülasyon profilleri test matrisine eklenmeli. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-110 — Birim test kapsamını kur.** Hasar, direnç, rota ilerleme, ekonomi, XP, cooldown, dalga bütçesi ve kayıt migration kuralları editörden bağımsız test edilmeli. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-111 — Oynanış entegrasyon testlerini kur.** Dalga başlatma, tüm düşmanların temizlenmesi, kaleye ulaşma, zafer, yenilgi, pause ve yeniden başlatma otomatik doğrulanmalı. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-112 — Kayıt güvenliği testlerini ekle.** Yarım yazılmış, eski sürüm, eksik alanlı ve bozuk kayıtlar kullanıcı ilerlemesini mümkün olduğunca koruyarak ele alınmalı. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-113 — Görsel regresyon akışı oluştur.** Ana menü, ilk harita, yoğun savaş, karakol paneli ve sonuç ekranı sabit kamera görüntüleriyle sürümler arasında karşılaştırılmalı. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-114 — Uzun oturum dayanıklılık testi yap.** En az 30–60 dakikalık kesintisiz oynanışta bellek artışı, havuz sızıntısı, ses kaybı ve performans düşüşü izlenmeli. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-115 — Dokunmatik stres testini yap.** Hızlı ardışık dokunma, çoklu dokunma, uygulamayı arka plana alma, çağrı kesintisi ve ekran döndürme durumları oyunu bozmamalı. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-116 — Denge simülasyon aracını oluştur.** Dalga kompozisyonları, karakol hasarı, ekonomi ve beklenen kazanma oranı toplu simülasyonla karşılaştırılabilmeli. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-117 — İlk dış kullanıcı testini planla.** En az 5–10 hedef oyuncunun öğreticisiz ilerleme, karar anları, kayıp nedenleri ve tekrar oynama isteği ölçülmeli. Tamamlandı: `docs/QUALITY_PLAN.md`.
- [x] **DC-118 — Kritik hata çıkış kriterini belirle.** Çökme, ilerleme kaybı, savaş kilidi, dokunma engeli ve ödeme sorunu gibi hata sınıfları için sürüm engelleme kuralları yazılmalı. Tamamlandı: `docs/QUALITY_PLAN.md`.

## Faz 10 — Dikey Dilim ve Sonraki Sürüm Kapıları

- [ ] **DC-119 — Gri kutu dikey dilimi tamamla.** Sanatsal kalite olmadan temel harita, üç karakol, birlikler, kahraman, altı dalga ve boss baştan sona oynanabilmeli.
- [ ] **DC-120 — İlk denge geçişini tamamla.** En az üç farklı geçerli stratejiyle bölüm kazanılabilmeli; tek zorunlu karakol dizilimi oluşmamalı.
- [ ] **DC-121 — Sanatsal dikey dilimi tamamla.** İlk bölüm hedeflenen model, animasyon, VFX, UI, ses ve müzik kalitesinde sunulmalı; placeholder kalmamalı.
- [ ] **DC-122 — Dikey dilim performans kapısını geç.** En düşük hedef cihazda yoğun boss anında belirlenen FPS ve bellek bütçeleri karşılanmalı.
- [ ] **DC-123 — Dikey dilim kullanıcı testi kapısını geç.** Oyuncular temel komutları dış yardım olmadan öğrenebilmeli ve çoğunluk bir kez daha oynamak istemeli.
- [x] **DC-124 — Kampanya üretim planını onayla.** Dikey dilim başarılı olursa bölüm sayısı, biyomlar, kahramanlar, düşman aileleri ve üretim takvimi gerçek verilerle tahmin edilmeli. Tamamlandı: `docs/VERTICAL_SLICE_AND_RELEASE_GATES.md`.
- [x] **DC-125 — Hesap ve bulut kayıt kararını yeniden değerlendir.** Cihazlar arası ilerleme gerçekten gerekli ise FutbolQuiz deneyiminden yararlanarak kimlik ve güvenli senkronizasyon tasarlanmalı. Tamamlandı: `docs/VERTICAL_SLICE_AND_RELEASE_GATES.md`.
- [x] **DC-126 — Liderlik ve günlük görev kararını yeniden değerlendir.** Tek oyunculu çekirdek kanıtlandıktan sonra skor kuralları, hile önleme ve günlük içerik üretim maliyeti hesaplanmalı. Tamamlandı: `docs/VERTICAL_SLICE_AND_RELEASE_GATES.md`.
- [x] **DC-127 — Gelir modelini etik ilkelerle seç.** Premium, kozmetik IAP, genişleme veya reklam seçenekleri; oyuncu güveni ve dengeyi bozmama şartıyla karşılaştırılmalı. Tamamlandı: `docs/VERTICAL_SLICE_AND_RELEASE_GATES.md`.
- [x] **DC-128 — Mağaza hazırlık kontrol listesini oluştur.** Gizlilik, yaş derecelendirmesi, ekran görüntüleri, tanıtım videosu, açıklamalar, çökme oranı ve destek süreçleri tamamlanmadan yayın yapılmamalı. Tamamlandı: `docs/VERTICAL_SLICE_AND_RELEASE_GATES.md`.

---

## Önerilen Uygulama Sırası

1. **Karar kapısı:** DC-001–010
2. **Teknik temel:** DC-011–025
3. **Gri kutu oynanış:** DC-026–050
4. **Kahraman ve içerik:** DC-051–072
5. **Ekonomi ve mobil UX:** DC-073–095
6. **Sanat ve ses:** DC-096–105
7. **Kalite kapıları:** DC-106–118
8. **Dikey dilim ve ürün kararı:** DC-119–128

## İlk Milestone İçin Asgari Kapsam

İlk milestone yalnızca şu maddelerden oluşur ve ayrıca kullanıcı onayı olmadan başlatılmaz:

- DC-001–010 ürün kararları
- DC-012 mevcut kod envanteri
- DC-014 oyun durum makinesi
- DC-015 veri şeması
- DC-026–037 harita/karakol temeli
- DC-038–050 temel savaş
- DC-051–058 ilk kahraman
- DC-061–072 ilk düşman/dalga seti
- DC-083–089 savaş HUD’u ve öğretici
- DC-108, DC-110, DC-111 kalite temeli
- DC-119 gri kutu dikey dilim

Bu milestone tamamlanmadan meta ilerleme, hesap, leaderboard, IAP veya geniş kampanya üretimine geçilmez.
