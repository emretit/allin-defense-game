# Kahraman adı — 7 Eylül 2026

Varsayılan kahraman adı **Savaşçı** (EN: **Warrior**). Savaş düğmesi, can/dönüş durumu ve ilk savaş yönlendirmesi oyuncunun verdiği adı kullanır. Yetenek adı **Sarsıcı Darbe** olarak kalır.

**Ayarlar → Kahramanım** ekranından en fazla 20 karakterlik bir ad kaydedilebilir. Boş bırakıp Kaydet seçilirse varsayılan ada dönülür. İptal kaydedilmemiş değişikliği atar. Ad alanı mobil klavye kullanır; pencere klavye ve safe area sınırlarına göre yerleşir.

Ad, mevcut atomik v4 oyuncu kaydında isteğe bağlı `heroName` alanında saklanır. v3/v4 kayıtları ve 11–50. bölüm ilerlemesi korunur. Misafir ve her hesap kendi adını tutar; hesap değişimi açık ad penceresini kapatır. Hesap görünen adı ayrı kalır. Kampanya sunucu eşitlemesi yerel kahraman adını değiştirmez; bu değişiklik cihazlar arasında kahraman adı eşitlemesi eklemez.

## Doğrulama

- Unity CLI EditMode: **112/112** geçti (`evidence/hero-name-editmode.xml`). Yeniden açılış, dil değişimi, eski kayıtlar, hesap ayrımı, geçersiz giriş ve yazma hatasından sonra tekrar deneme dahil.
- Unity CLI PlayMode: **1/1** kahraman adı akışı geçti (`evidence/hero-name-playmode.xml`). Ayarlardan giriş, kaydetme, iptal, varsayılana dönüş, açık pencere sırasında hesap değişimi ve TR/EN savaş düğmesi doğrulandı. Oyuncunun `AYARLAR` gibi bir ad vermesi adın çevrilmesine yol açmaz.
- Bu değişiklik için yeni dağıtım paketi üretilmedi. Kurulu iOS Simulator uygulaması ve önceki paket manifesti önceki sürüme aittir. Mobil klavye/safe area yerleşiminin cihazda görsel kontrolü sonraki simülatör derlemesinde yapılmalıdır.
