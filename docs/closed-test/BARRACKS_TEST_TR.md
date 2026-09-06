# Üç askerlik kışla — 6 Eylül 2026

Kaynak: `4215702` (savaş değişikliği `cf0c033`, toplanma/menzil `198e50b`), izole `codex/first-region-closed-test` dalı. Unity 6000.4.1f1; test ve dışa aktarımlar Unity CLI üzerinden.

Kışla artık üç asker çıkarır. Askerler binanın yol tarafındaki çıkışından yürüyerek ayrılır, bina tarafından örtülmeyecek şekilde yolun biraz ilerisindeki üç ayrı toplanma noktasına yerleşir ve yakınlarındaki düşmana yaklaşarak yakın dövüşe girer. Düşman da askere hasar verir ve asker hayattayken yolda tutulur. Çatışma bitince asker kendi yerine döner. Yol köşelerinde toplanma ve takip konumu gerçek yol parçalarından hesaplanır.

Kalkan Birliği ve Hücum Birliği de üç askerden oluşur. Asker menzili ve kule kartı aynı tanımdan 0,85 birim okur. Mevcut kurulum, yükseltme ve uzmanlaşma fiyatları korunur. Bir asker ölünce kışladan çıkan yenisi eksik kalan sırayı tamamlar; hayatta kalan askerler yeniden oluşturulmaz. Kışlayı satmak bütün birliği kaldırır.

Dalga başı kaydı askerlerin canını, konumunu, yürüyüş durumunu ve toplanma sırasını saklar. Eksik asker bulunan kayıt aynı birlikle açılır; yenisi normal kışla davranışıyla çıkar. Eski iki askerlik kayıtlar okunur ve üçüncü asker eklenir. 11–50. bölüm ilerlemesi korunur.

## Doğrulama

- Önce eski davranışta test çalıştırıldı: üç asker beklenirken iki asker bulundu. Kanıt: `evidence/barracks-before.xml`.
- Yeni testler; üç askerin yürüyerek yerleşmesini, düşmanla karşılıklı yakın dövüşü, yerine asker çıkarılmasını, satışta kaldırılmasını, iki uzmanlaşmada üçlü düzeni ve eski/yeni kayıt dönüşünü kapsıyor.
- İlk tam koşuda 34 kontrolün 33'ü geçti; kışla ağırlıklı boss kurulumunun yenilgisi incelendi. Bekleyen düşmanın saldırı süresinin eksiye düşerek peş peşe vuruş biriktirmesi ve yüksek model merkezinin yakın dövüş menzilini bozması iki ayrı testle yeniden üretildi. Önceki sonuçlar: `barracks-combat-before.xml`, `barracks-melee-before.xml`. Saldırı beklemesi sıfırda sınırlanıyor; asker yakın dövüş mesafesini yol düzleminde ölçüyor.
- Simülatörde görülen yatık asker modeli ayrıca düzeltildi: aktör oluşturucu FBX modelinin yön düzeltmesini sıfırlıyordu. Aktörlerde içe aktarım yönü korunuyor. Önceki testte baş-ayak yüksekliği farkı 0,028 birimdi; düzeltmeden sonra bekleme/yürüme/saldırı ve dört yönde 0,70 birim. Kanıt: `barracks-pose-before.xml`, `barracks-editmode.xml`. Bu düzeltme asker, kahraman ve düşmanların aynı aktör oluşturucusuna uygulanır; bina yönleri korunur.
- Kayıt, veri, sunum sözleşmesi ve temel savaş kurallarının EditMode kontrolü **103/103** geçti: `evidence/barracks-editmode.xml`.
- Son model düzeltmesi ardından kışla ve savaş arayüzü kontrolleri **21/21** geçti: `evidence/barracks-presentation-playmode.xml`. Bunlar aşağıdaki tam koşudaki testlerin bir bölümünü tekrarlar.
- Son tam oynanış koşusu **36/36** geçti. Buna sekiz kışla testi, ilk 10 bölümün gerçek altınlı 17 kurulum senaryosu ve 97 dalgası, üç kule ağırlıklı boss stratejisi, stun/arayüz/kayıt kontrolleri dahildir. Bu sayılar birbirine eklenmez; tek test birden fazla senaryo çalıştırabilir. Sonuç: `evidence/barracks-playmode.xml`.
- Kışla ağırlıklı boss kurulumu sekiz dalgayı **20 canla** tamamladı. Altı uzmanlaşmanın kontrollü karşılaştırmasında Kalkan Birliği ilk asker kaybına kadar süreyi **2,30 → 4,22 saniyeye**, Hücum Birliği verilen hasarı **208 → 320** değerine çıkardı. Bu deneyler rolü ayırmak için ek kaynak kullanır; 17 kampanya senaryosu normal oyun altını kullanır.
- `campaign-balance.csv`, `wave-balance.csv`, `purchase-timing.csv` ve `specialization-combat.csv` yeni koşuyla güncellendi. Bu kampanya koşularının otomatik olay verileri `evidence/barracks-campaign-telemetry.json` içindedir.
- iPhone 13 / iOS 26.5 simülatöründe son paket kuruldu; kurulu UnityFramework dosyasının özeti derlemeyle aynı. Bölüm 2 test sahnesinde normal 230 altınla kışla kuruldu (130 kaldı); üç asker bina önünde ayakta ve ayrı yol noktalarında görüldü, düşmana yaklaşıp çatıştı. İlk iki dalga gözlendi; ilk dalga 20, ikinci dalga 8 canla bitti. Tek kışlalı bu sınırlı görsel koşu, bölümün tam denge kabulü değildir. Yakalanmamış istisna bulunmadı. Kanıt: `evidence/barracks-simulator.json` ve aynı önekli ekran görüntüleri.
- Android APK, iOS Simulator uygulaması ve iOS cihaz dışa aktarımı/imzasız uygulaması `4215702` kaynağıyla yenilendi. Android imzası doğrulandı. Simülatörde kışla kurulmuş ikinci bölüm test sahnesi açık bırakıldı.
- Fiziksel iPhone performansı ve gerçek oyuncu testi bu raporda ölçülmüş değildir.
