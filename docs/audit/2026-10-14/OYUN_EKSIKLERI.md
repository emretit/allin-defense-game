# ALLIN: Defense — Oyunu bitirmek için ne eksik?

Oyun, tek bölgeli ve 10 bölümlük bir v1.0 için çalışan savaş, ilerleme, öğretici ve kayıt temeline sahip; baştan yapılması gereken bir prototip aşamasında değil. En büyük üç boşluk kampanya sonunun doğru kapanmaması, sonuç ekranındaki işlevsiz valor ödülü ve “Devam et” düğmesinin yarım kalan tekrar savaşını seçmemesi. Önerim, aşağıdaki iki P0 işini ve P1 oyun akışlarını tamamlayıp 10 bölümü gerçek oyuncuyla doğrulamak; 50 bölümü, kalıcı güçlenmeyi ve yeni kahramanları ilk sürümün şartı yapmamak.

## Kapsam ve kanıt sınırı

14 Ekim 2026, `main`, `2a4c27f` üzerindeki kaynak kod, Resources verisi, sahne/asset bağlantıları ve test kaynakları incelendi. Unity/Editor, test koşusu, simülatör, build veya canlı servis çağrısı yapılmadı; aşağıdaki davranışlar **statik kod bulgusudur**, cihazda yeniden üretilmiş hata iddiası değildir. Eski denetimlerin durum iddiaları kullanılmadı; güncel UI planı, handoff ve TODO bağlam olarak okundu, bulgular kodla doğrulandı. Giriş/yayın işleri ve görsel cila kapsam dışıdır.

Öncelikler: **P0** v1 için şart, **P1** v1'de olmalı, **P2** sonra veya ürün kararına bağlı. Büyüklük: **S ≤2 saat**, **M ≤1 gün**, **L >1 gün**; aşağıdaki tahminler önerilen en küçük çözüm içindir, hazır uygulama süresi taahhüdü değildir. Dosya konumları repo köküne göredir.

## Tamam olanlar

- **Oyuncuya açık içerik:** 1 bölge, 10 bölüm, `first-region-v2`; 3–8 dalga, 10. bölümün son dalgasında boss (`Assets/Scripts/Data/FirstRegionCampaign.cs:17`, `Assets/Scripts/UI/AppFlowController.ClosedTest.cs:12`, `Assets/Resources/FirstRegionCampaign.json:1`).
- **Haritalar:** 5 temel yol şekli ve bölüm bazında kaydırmalar var; ilk 10 bölüm tek girişli, aynı biyomda (`Assets/Scripts/Data/CampaignLevelFactory.cs:45`, `Assets/Scripts/Data/CampaignLevelFactory.cs:174`, `Assets/Scripts/Data/CampaignLevelFactory.cs:191`).
- **Düşmanlar:** 5 normal tip + 1 boss; yenilenme, iyileştirme, kuleyi geçici durdurma ve %66/%33 boss safhaları uygulanmış (`Assets/Scripts/Data/GreyboxDefinitions.cs:13`, `Assets/Scripts/Core/GreyboxCombatants.cs:134`, `Assets/Scripts/Core/GreyboxCombatants.cs:182`).
- **Kuleler:** okçu/kışla/büyü, savaş içi yükseltme ve her kuleye iki uzmanlaşma; kışla 2, büyü 3, uzmanlaşmalar 6/7/8. bölüm kilitleriyle açılıyor (`Assets/Scripts/Data/TowerProgression.cs:21`, `Assets/Scripts/Data/TowerProgression.cs:28`, `Assets/Scripts/Core/GameSessionController.Battle.cs:39`).
- **Kahraman ve aktif yetenek:** sabit kale savunucusu otomatik saldırıyor, yeniden doğuyor; Sarsıcı Darbe alan hasarı/sersemletme ve 6 saniye bekleme süresiyle çalışıyor (`Assets/Scripts/Core/GreyboxCombatants.cs:771`, `Assets/Scripts/Core/GreyboxCombatants.cs:817`, `Assets/Scripts/Core/GreyboxCombatants.cs:834`).
- **İlerleme ve eğitim:** yıldızlar, sonraki bölüm kilidi, ilk savaş rehberi, bölüm hedefleri, ilk karşılaşma düşman kartı ve yenilgi tavsiyesi var (`Assets/Scripts/Core/CampaignProgressStore.cs:451`, `Assets/Scripts/Core/GameSessionController.Tutorial.cs:13`, `Assets/Scripts/UI/AppFlowController.ClosedTest.cs:49`, `Assets/Scripts/UI/AppFlowController.Screens.cs:617`).
- **Kayıt:** atomik yerel kayıt/backup, profil ayrımı, çevrimdışı tamamlanma kuyruğu, Cloud Code eşitleme ve dalga başı checkpoint mevcut; test kaynakları geri yükleme ve eski sürüm ayrımını kapsıyor (`Assets/Scripts/Core/AtomicSaveFile.cs:24`, `Assets/Scripts/Core/UnityGameServicesController.cs:313`, `Assets/Scripts/Core/GameSessionController.Checkpoints.cs:48`, `Assets/Tests/PlayMode/ClosedTestAcceptanceTests.cs:58`).
- **Ses ve titreşim:** katalogdaki 22 efekt + 6 müzik klibinin GUID'leri mevcut ses dosyalarına çözülüyor; savaş durumları oynatıcıya bağlı, iOS haptik köprüsü var (`Assets/Resources/Presentation/DefenderPresentationCatalog.asset:148`, `Assets/Resources/Presentation/DefenderPresentationCatalog.asset:259`, `Assets/Scripts/Presentation/DefenderPresentationDirector.cs:116`, `Assets/Scripts/UI/UiHaptics.cs:16`).
- **Ayarlar ve dil:** müzik/efekt, titreşim, kamera sarsıntısı, azaltılmış hareket ve TR/EN kalıcı ayarları var; 30 HUD anahtarı eşlenmiş, taranan 283 tek satırlık TR/EN çifti boş değil, Unity dil tablolarının 10'ar kimliği eşleşiyor (`Assets/Scripts/UI/FrontendPreferences.cs:18`, `Assets/Scripts/Presentation/UiCopyCatalog.cs:56`, `Assets/Scripts/UI/FrontendCopy.cs:35`, `Assets/Scripts/UI/GameLocalization.cs:24`).

## Eksikler

### P0 — v1 için şart

1. **10. bölüm sonunda kampanya tamamlanma akışı yok; açılamayan 11. bölüm duyuruluyor.**
   **Kanıt:** `Assets/Scripts/Core/CampaignProgressStore.cs:461` kilidi 50'ye kadar artırıyor; `Assets/Scripts/UI/AppFlowController.cs:644` artışı doğrudan “bölüm açıldı” metnine taşıyor; `Assets/Scripts/UI/AppFlowController.ClosedTest.cs:14` oynanabilir sınırı 10'da tutuyor; `Assets/Scripts/UI/AppFlowController.cs:650` sonraki düğmesini gizleyip tekrarı gösteriyor; `Assets/Scripts/Core/CampaignProgressStore.cs:212` bütün bölümler bitince yine 10'u seçiyor.
   **Oyuncu:** İlk 10. bölüm zaferinde 11'in açıldığı mesajını görebilir, fakat haritada 11 yoktur; normal zafer/tekrar ekranı dışında bitiş veya “devamı gelecek” açıklaması bulunmaz, ana menü yeniden 10'a yönlendirir.
   **Büyüklük:** M.
   **Önerilen çözüm:** 10. bölüm zaferini “Yeşil Nöbet tamamlandı” durumuna bağla, oynanabilir sınırın ötesindeki açılma mesajını bastır ve harita/eksik yıldızları tamamlama seçeneklerini sun. Kabul testi gerçek akışı, `CompleteLevel(10)` → sonuç → menü şeklinde kapsasın; mevcut son-bölüm testi doğrudan 50'yi seçip yalnız düğmeyi denetliyor (`Assets/Tests/PlayMode/FrontendExperienceTests.cs:379`).

2. **Valor ödülü gösteriliyor, fakat oyuncunun kullanacağı karşılığı yok.**
   **Kanıt:** `Assets/Scripts/Core/CampaignProgressStore.cs:455` valor ve heroXp hesaplayıp biriktiriyor; `Assets/Scripts/UI/AppFlowController.cs:641` ve `Assets/Scripts/UI/AppFlowController.cs:666` valoru altın simgeli sonuç satırında gösteriyor; `Assets/Scripts/Core/GameSessionController.Battle.cs:44` uzmanlaşmayı yalnız savaş altınından satın alıyor; `Assets/Scripts/Core/GreyboxCombatants.cs:771` kahraman değerleri sabit. `Assets/Scripts` genelindeki valor/heroXp kullanımları kayıt, eşitleme, ödül hesabı ve bu sonuç satırıyla sınırlı; mağaza/harcama veya XP'den seviye üretme yolu yok.
   **Oyuncu:** Zaferde “+N” kazanır ama bu miktarı harcayamaz, kahramanı güçlenmez; savaş altınıyla kalıcı ödül arasındaki anlam da belirsizdir. HeroXp ayrıca sonuçta gösterilmeyen bir kayıt sayacıdır.
   **Büyüklük:** S — ödülü gizleme seçeneği; ekonomi eklemek L.
   **Önerilen çözüm:** İlk sürümde kullanılmayan valoru sonuçtan kaldır, yıldız ve gerçek bölüm/kule açılımlarını ödül yap; mevcut kayıt alanlarını koru. Kalıcı ekonomi istenirse bunu ayrı tasarım işi olarak ele al; bu P0'ın kapanması için mağaza yapmak şart değil.

### P1 — v1'de olmalı

3. **“Devam et”, yarım kalan tekrar savaşına dönmüyor.**
   **Kanıt:** `Assets/Scripts/UI/AppFlowController.cs:425` yalnız `NextUnfinishedLevel()` çağırıyor; `Assets/Scripts/Core/CampaignProgressStore.cs:212` tamamlanmamış ilk bölümü seçiyor; `Assets/Scripts/Core/GameSessionController.Checkpoints.cs:62` checkpoint'i yalnız seçilen bölümle aynıysa yüklüyor; `Assets/Scripts/Core/GameSessionController.Checkpoints.cs:51` tek checkpoint yuvasını yeniden yazıyor.
   **Oyuncu:** Örneğin 8'e kadar açmışken 3'ü daha çok yıldız için oynayıp uygulamadan çıkarsa “Devam et” 8'i başlatır; 3'ün kaydına dönmek için haritadan kendisi 3'ü seçmelidir. Yeni bölümün ilk dalgasını başlatırsa eski yarım savaş kaydı üzerine yazılır.
   **Büyüklük:** M.
   **Önerilen çözüm:** Geçerli yerel checkpoint varsa ana eylem onu ve kayıtlı dalgayı göstersin; yeni bölüm başlatmak ayrı eylem olsun. Tamamlanmış bir bölümün tekrarından çıkıp açılışta devam etmeyi uçtan uca test et.

4. **Kural sürümü değişince yarım savaş sessizce siliniyor.**
   **Kanıt:** `Assets/Scripts/Core/GameSessionController.Checkpoints.cs:63` geçersiz checkpoint'i silip dönüyor; `Assets/Scripts/Core/GameSessionController.Checkpoints.cs:94` tam `FirstRegionCampaign.Version` eşitliği arıyor; `Assets/Tests/PlayMode/ClosedTestAcceptanceTests.cs:91` `first-region-v1` kaydının silinmesini açıkça bekliyor. Yıldızların korunması aynı testte doğrulanıyor.
   **Oyuncu:** Güncelleme sonrası bölüm baştan açılır; bunun nedenini göremez, ilerlemenin tamamının kaybolduğunu sanabilir. Normal devam da savaşın herhangi bir anından değil dalga başındandır (`Assets/Scripts/Core/GameSessionController.cs:950`).
   **Büyüklük:** S.
   **Önerilen çözüm:** Sürüm nedeniyle sıfırlamada bir kez “Denge güncellendi; bu savaş baştan başlayacak, bölüm yıldızların korundu” açıklaması göster. Devam eyleminde “dalga başından” sınırını belirt; bütün savaş anını kaydetmek gerekmiyor.

5. **Denge ve öğreticinin gerçek oyuncuyla bitirme kabulü henüz kod testlerinden çıkarılamıyor.**
   **Kanıt:** `Assets/Tests/PlayMode/FirstRegionBalanceTests.cs:19` ilk 10 bölümü otomatik planlarla oynuyor; `Assets/Tests/PlayMode/FirstRegionBalanceTests.cs:32` yuvaları sırayla işliyor ve `Assets/Tests/PlayMode/FirstRegionBalanceTests.cs:43` yeteneği her saniyelik eylem turunda deniyor. `Assets/Tests/PlayMode/StrategyBalanceAcceptanceTests.cs:56` diğer strateji kabulünü 10. bölüm üzerinden yürütüyor; bunlar gerçek dokunma/öğrenme gecikmesini ölçmüyor.
   **Oyuncu:** Doğru kuleyi tanımak, üç dokunuşla kurmak ve yeteneği zamanlamak için otomasyondan daha fazla süreye ihtiyaç duyar; mevcut testlerin başarısı tek başına kampanyanın anlaşılır veya eğlenceli olduğunu kanıtlamaz.
   **Büyüklük:** L — oyuncu bulma, gözlem ve gerekirse düzeltme dahil.
   **Önerilen çözüm:** Daha önce oynamamış birkaç kişiyle 1–10 bitirme, ilk kurulum/yükseltme zamanı, takılma bölümü ve yardım ihtiyacını kaydet. Bu madde “denge bozuk” hükmü değildir; v2 dengesini yeniden yazmadan önce eksik insan doğrulamasıdır.

6. **Menüde müzik akışı bulunmuyor.**
   **Kanıt:** `Assets/Scripts/UI/AppFlowController.cs:138` yalnız UI efekt kaynağı kuruyor; `Assets/Scripts/UI/AppFlowController.cs:1312` tekil tıklama/harita sesini oynatıyor. Müzik oynatıcısı savaş kurulurken doğuyor (`Assets/Scripts/Core/GameSessionController.cs:311`), menüye dönüşte savaş nesnesi kapatılıyor (`Assets/Scripts/Core/GameSessionController.cs:265`); katalogda yalnız altı savaş durumu var (`Assets/Resources/Presentation/DefenderPresentationCatalog.asset:259`).
   **Oyuncu:** Açılış, ana menü ve haritada düğme sesleri vardır, sürekli menü müziği yoktur; bu bir eksik ses dosyası veya bozuk savaş sesi değildir.
   **Büyüklük:** M.
   **Önerilen çözüm:** İsteniyorsa mevcut hazırlık parçasını menüde de kullanan ve savaş geçişinde sesi devreden tek bir yaşam döngüsü ekle. Sessiz menü bilinçli tercihse bu maddeyi ürün kararıyla kapat.

7. **Öğrenilen bilgiyi yeniden açma yolu yok.**
   **Kanıt:** `Assets/Scripts/Core/GameSessionController.Tutorial.cs:15` 1. bölüm kazanıldıktan sonra rehberi kapatıyor; `Assets/Scripts/UI/AppFlowController.Screens.cs:623` görülen düşman kartını atlıyor; `Assets/Scripts/UI/FrontendPreferences.cs:54` görülme bilgisini cihazın PlayerPrefs'inde saklıyor. Bölüm hedefleri ve yenilgi tavsiyesi mevcut (`Assets/Scripts/UI/AppFlowController.ClosedTest.cs:49`), fakat öğretici tekrarını veya kart arşivini açan kullanıcı eylemi bulunmuyor.
   **Oyuncu:** Şifacı/kuşatma kartını hızlıca kapatan veya oyuna ara veren kişi o açıklamayı tekrar okuyamaz; aynı cihazdaki yeni profil de önceki görülme bayraklarını paylaşır.
   **Büyüklük:** M.
   **Önerilen çözüm:** Brifingdeki düşman simgesinden mevcut kartı tekrar aç ve ayarlara rehberi yeniden göster eylemi ekle. Yeni ansiklopedi içeriği yazmak yerine var olan açıklamaları kullan.

8. **Erişilebilirlik düğüm kapsamı kısmi; oyun yolu baştan sona tanımlı değil.**
   **Kanıt:** Native düğüm kaydı `Assets/Scripts/UI/Kit/UiAccessibleButton.cs:21` içinde; ortak `Assets/Scripts/UI/Kit/UiKit.cs:128` düğme üretimi bu bileşeni eklemiyor, `Assets/Scripts/UI/AppFlowController.Screens.cs:50` menü düğmesi de eklemiyor. Harita bölüm düğmeleri doğrudan bu üreticiyi kullanıyor (`Assets/Scripts/UI/AppFlowController.Screens.cs:302`); ayar slider/switch'leri ayrı üretiliyor (`Assets/Scripts/UI/AppFlowController.Screens.cs:537`, `Assets/Scripts/UI/AppFlowController.Screens.cs:583`). Radial ve bazı sonuç düğmelerinde özel etiketler gerçekten mevcut.
   **Oyuncu:** VoiceOver için bazı eylemlerin düğümü var, menü → bölüm seçimi → ayarlar hattının tamamının bu kod üzerinden erişilebilir olması sağlanmıyor. Bu statik kapsam boşluğudur; fiziksel cihazdaki davranış testi TODO'da ayrı duruyor.
   **Büyüklük:** L.
   **Önerilen çözüm:** Önce ortak menü/harita eylemlerine ve ayar kontrollerine etiket, değer ve eylem düğümlerini bağla; modal açıkken odak kapsamını belirle. Savaşın tümünü ekran okuyucuyla oynanabilir kılmak ayrı kapsam kararıdır; mevcut kısmi desteği tamamlanmış sayma.

### P2 — sonraya veya ürün kararına bağlı

9. **Kalıcı kahraman gelişimi, yetenek ağacı ve yeni aktif büyüler oyuncu sürümüne bağlı değil.**
   **Kanıt:** Gerçek kahraman `Assets/Scripts/Core/GameSessionController.cs:829` ile `GreyboxHero` olarak yaratılıyor; can/hasar/yetenek sabit (`Assets/Scripts/Core/GreyboxCombatants.cs:771`, `Assets/Scripts/Core/GreyboxCombatants.cs:822`, `Assets/Scripts/Core/GreyboxCombatants.cs:834`). `Assets/Scripts/Heroes/HeroBase.cs:54` gibi eski sınıflarda seviye bulunması bu akışı çalışır yapmıyor; canlı kule ağacı `Assets/Scripts/Data/TowerProgression.cs:8` ile altı savaş içi dala sınırlı.
   **Oyuncu:** Tek sabit kahraman ve tek aktif yetenek kullanır; heroXp birikimiyle güçlenmez, kalıcı kule ağacı/ikinci büyü seçmez. Bu özellikler gösterilmiyorsa 10 bölümlük v1'i bitirmeye engel değildir.
   **Büyüklük:** L.
   **Önerilen çözüm:** v1'de mevcut üç kule/altı dal/tek yeteneği koru ve P0-2 ile ödül beklentisini düzelt. Kalıcı güçlenme seçilirse mevcut dengeyi de etkileyen ayrı bir ürün tasarımı olarak planla; eski sınıfları doğrudan bağlamak yeterli değildir.

10. **11–50 ve diğer bölgeler hazır oyuncu içeriği olarak kabul edilemez.**
    **Kanıt:** `Assets/Resources/LiveGame/campaign-rules-v2.json:3` beş bölgelik kural verisini, `Assets/Scripts/Data/CampaignCatalog.cs:45` 50 bölümlük kataloğu tanımlıyor; menü yine tek bölgeyle sınırlı (`Assets/Scripts/UI/AppFlowController.ClosedTest.cs:12`). İlk 10 dışındaki dalgalar genel üreticiden geliyor (`Assets/Scripts/Data/CampaignLevelFactory.cs:101`), ikinci giriş ancak 14'te, üçüncüsü 43'te açılıyor (`Assets/Scripts/Data/CampaignLevelFactory.cs:174`); İngilizce isim sözlüğü ilk bölgeyi içeriyor (`Assets/Scripts/UI/GameLocalization.cs:24`), bilinmeyen çeviriyi Türkçe bırakıyor (`Assets/Scripts/UI/GameLocalization.cs:138`).
    **Oyuncu:** Bugün aynı biyomdaki beş rota şeklinin varyasyonlarını, tek giriş ve bir bölgesel boss görür; diğer dört bölge kilitli düğümler olarak bile sunulmaz. Sınırı kaldırmak, 40 bitmiş bölüm veya çevrilmiş yeni bölgeler teslim etmek anlamına gelmez.
    **Büyüklük:** L.
    **Önerilen çözüm:** İlk sürümü açıkça 10 bölümlük kampanya olarak kapat. Ek bölge seçilirse kendine özgü oynanış/harita, bölüm bazlı denge, isim çevirileri ve oyuncu kabulünü birlikte tamamla; sadece sabitleri değiştirme.

11. **Tekrar oynanabilirlik yıldız iyileştirme ile sınırlı.**
    **Kanıt:** `Assets/Scripts/Core/CampaignProgressStore.cs:451` yalnız yeni yıldızlar için ek valor veriyor ve XP'yi ilk tamamlamada veriyor; `Assets/Scripts/Core/CampaignProgressStore.cs:212` bitiş sonrası yine son bölümü seçiyor. Zorluk değeri bölüm tanımında sabit (`Assets/Scripts/Data/CampaignCatalog.cs:124`); oyuncuya zorluk modu/sonsuz mod/meydan okuma seçimi sunan akış yok.
    **Oyuncu:** 30 yıldızı tamamladıktan sonra aynı koşullardaki bölümleri kendi isteğiyle tekrar oynar; yeni hedef veya mod açılmaz.
    **Büyüklük:** L.
    **Önerilen çözüm:** v1'de 30 yıldızı açık son hedef yap; bitiş ekranından eksik yıldızlara yönlendir. Ek mod gerekiyorsa yalnız bir zor mod veya sınırlı meydan okuma seçip ayrı ödül/kayıt/dengesiyle tasarla.

12. **Bulut kaydı tam oyun oturumunu kapsamıyor.**
    **Kanıt:** Yerel kayıt kahraman adı ve checkpoint içeriyor (`Assets/Scripts/Core/CampaignProgressStore.cs:41`, `Assets/Scripts/Core/CampaignProgressStore.cs:52`); sunucu durum sözleşmesi ise bölüm/ödül/claim alanlarıyla sınırlı (`Assets/Scripts/Core/CampaignProgressionContracts.cs:33`). Düşman görülme bilgisi ve ayarlar PlayerPrefs'te (`Assets/Scripts/UI/FrontendPreferences.cs:54`).
    **Oyuncu:** Bölüm ilerlemesi eşitlense bile diğer cihazda yarım dalgasını, kahraman adını ve öğrenme tercihlerini aynı şekilde bulacağı garanti edilmez. Canlı eşitlemenin hizmet durumu bu denetimde sınanmadı.
    **Büyüklük:** L — bu alanları eşitleme; mevcut kapsamı açıklamak S.
    **Önerilen çözüm:** v1 için bulutun tamamlanmış bölüm ilerlemesini taşıdığını, savaş devamının cihaza ait olduğunu netleştir. Cihazlar arası oturum devamı istenirse checkpoint sürümü/sahibi/çatışma davranışını da tasarla; giriş entegrasyonunu bu işe dahil etme.

13. **Para kazanma sistemi yok — eklenmesi ürün kararı.**
    **Kanıt:** `ProjectSettings/UnityConnectSettings.asset:23` satın alma, `ProjectSettings/UnityConnectSettings.asset:31` reklam kapalı; `Packages/manifest.json:2` bağımlılıklarında IAP/reklam SDK'sı yok, oyun/UI kodunda satın alma veya ödüllü reklam akışı bulunmuyor.
    **Oyuncu:** Reklam, oyun içi ödeme, ücretli güçlendirme veya mağaza görmez. Bu, yarım çalışan bir mağaza değil; uygulanmamış ve v1 için zorunluluğu belirlenmemiş bir modeldir.
    **Büyüklük:** L — uygulama seçilirse.
    **Önerilen çözüm:** İlk sürümün ücretsiz/reklamsız mı, mağazada tek sefer ücretli mi, yoksa uygulama içi gelirli mi olacağını kararlaştır. Oyunu bitirme hedefi için reklam/IAP eklemeyi şart koşma.

14. **Ürün analitiği yok; yalnız yerel kapalı-test telemetrisi var.**
    **Kanıt:** `Assets/Scripts/Core/BattleTelemetry.cs:8` eski kapalı-test build kimliğini kullanıyor; `Assets/Scripts/Core/BattleTelemetry.cs:46` olayları cihazda JSONL dosyasına ekliyor. `ProjectSettings/UnityConnectSettings.asset:26` Unity Analytics kapalı; kaynakta merkezi ürün olayı gönderimi bulunmuyor. Aynı ayarda açık olan Cloud Diagnostics, bu oynanış ölçümünün yerine geçmiyor (`ProjectSettings/UnityConnectSettings.asset:20`).
    **Oyuncu:** Doğrudan eksik bir ekran görmez; ekip oyuncuların hangi bölümde/öğretici adımında bıraktığını toplu izleyemez. Yerel dosyalar için bu sınıfta saklama süresi/boyut temizliği de yok.
    **Büyüklük:** M — yerel kimlik/saklama düzeni; merkezi analitik L.
    **Önerilen çözüm:** v1'de insan gözlemiyle yetinilecekse yerel telemetriyi geliştirme kapsamına al veya boyut/süre sınırı koy, build kimliğini güncel sürümden üret. Merkezi olay analitiğini gerekli görürsen ayrı karar ver; oyun bitirmek için otomatik olarak SDK ekleme.

## TODO, placeholder ve eski sistem taraması

`Assets/Scripts` taramasında açık `TODO`, `FIXME`, `NotImplementedException` veya oyuncuya gösterilen “coming soon” özelliği bulunmadı; `placeholder` eşleşmesi kahraman adı girişinin normal ipucu alanı (`Assets/Scripts/UI/AppFlowController.Hero.cs:54`). Bu sonuç bütün özelliklerin bittiği anlamına gelmez: yukarıdaki eksikler çoğunlukla yorum bırakılmadan sınırlı uygulanmış akışlardır.

`Greybox` adlı sınıflar bugün gerçek savaşın da çekirdeği; adları tek başına geçici görsel veya eksik oyun kanıtı değil. Build'in etkin sahnesi `Assets/Scenes/Game.unity` (`ProjectSettings/EditorBuildSettings.asset:8`), sahnede katalog bağlantısı var (`Assets/Scenes/Game.unity:267`), bootstrap katalog yoksa hata veriyor (`Assets/Scripts/Core/GameBootstrap.cs:35`) ve kahramanı üretim prefab'ıyla kuruyor (`Assets/Scripts/Core/GameSessionController.cs:812`). Eski `HeroBase` seviye sistemi veya `CatapultTower` sınıfı/prefab'ı bulunması ise oyuncuya dördüncü kule ya da çoklu kahraman sunulduğunu kanıtlamıyor; etkin enum yalnız üç kule içeriyor (`Assets/Scripts/Data/GreyboxDefinitions.cs:6`, `Assets/Scripts/Towers/CatapultTower.cs:8`). Bu eski sınıfların temizliği v1 oyun kapsamını genişletmek için gerekçe yapılmadı.

TR/EN kontrolünde açık ilk 10 bölüm adları ve HUD anahtarlarında somut eksik bulunmadı; bu sonuç tüm dinamik cümlelerin dil kalitesi veya ekran taşması kabulü değildir. Test kaynakları savaş, uzmanlaşma, ses ayarları, ilk karşılaşma ve checkpoint alanlarını kapsıyor; bu denetimde testler koşturulmadığı için yeni bir “testler geçti” sonucu verilmedi.

## Emre'nin karar vermesi gerekenler

1. **v1 kaç bölümle tamamlanmış sayılacak?** A) 10 bölüm + net final + 30 yıldız hedefi (**önerim**). B) İkinci bölgeyi de bitirip 20 bölüm. C) Beş bölge/50 bölüm; büyük içerik ve denge işi. İlgili bulgular: 1, 10, 11.
2. **Kalıcı ödül ve kahraman gelişimi v1'e girecek mi?** A) Valor gösterimini kaldır, yalnız yıldız/kilit açma; mevcut tek kahraman ve yetenek (**önerim**). B) Küçük kalıcı kule/hero gelişimi; yeni ekonomi ve denge gerekir. C) Mağaza + kahraman/yetenek ağacı; sonraki büyük sürüm kapsamı. İlgili bulgular: 2, 9.
3. **v1'in gelir modeli ne?** A) İlk sürüm ücretsiz ve reklamsız (**oyunu bitirme odağı için önerim**). B) Mağazada tek sefer ücretli; oyun içi mağaza gerekmez. C) IAP/ödüllü reklam; yeni sistem ve kabul işi. İlgili bulgu: 13; burada mağaza yayını yapılması önerilmiyor.
4. **İlk sürümde oyuncu davranışı nasıl ölçülecek?** A) Gözlemli küçük oyuncu testi + sınırlı yerel kayıt (**önerim**). B) Bölüm başlangıcı/sonucu ve öğretici terkini içeren merkezi asgari analitik. C) Geniş ürün analitiği; sonraya. İlgili bulgular: 5, 14.
5. **Bulut devamının kapsamı ne olsun?** A) Tamamlanan bölümler bulutta, yarım savaş cihazda; metin ve Devam et akışı bunu açıklasın (**önerim**). B) Kahraman adı/tercihler de eşitlensin. C) Yarım savaş dahil tam cihazlar arası devam. İlgili bulgular: 3, 4, 12.
