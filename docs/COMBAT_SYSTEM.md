# Defender Chronicles — Combat System

Durum: Faz 3 kismi uygulama ve tasarim notu
Son guncelleme: 2026-08-15

## Ortak Savas Varligi Sozlesmesi

Tum savas varliklari ayni kavramlari paylasacak:

- Saglik
- Maksimum saglik
- Hasar alabilirlik
- Hedeflenebilirlik
- Takim
- Rota ilerlemesi veya sabit savunma konumu
- Durum etkileri
- Olum/yeniden dogma/temizlenme akisi

Ilk teknik temel:

- `DamageResolver` eklendi.
- `DamageRequest` ve `DamageResult` hasar sirasini merkezi hale getirir.
- `EnemyBase.TakeDamage(...)` artik hasari bu cozumleyiciden gecirir.

## Birlik Rolleri

### Yakin Dovus Birligi

Hedef davranis:

- Barracks karakolundan dogar.
- Kendi karakolunun yerel savunma noktasina gider.
- Menziline giren dusmani tutar.
- Dusmanla temas halinde saldirir.
- Olurse belirli sure sonra karakoldan yeniden dogar.

Ilk rol:

- Dusmani gecici olarak yavaslatan ve savunma hattini tutan dayanikli birlik.

Tamamlanan kod temeli:

- `FriendlyUnitBase`
- `MeleeUnit`

### Menzilli Birlik

Hedef davranis:

- Menzil icindeki dusmani hedefler.
- Hedef olurse veya menzilden cikarsa yeni hedef secer.
- Dusmani fiziksel olarak tutmaz.
- Ucan dusmanlari hedefleyebilen ilk birlik adayidir.

Tamamlanan kod temeli:

- `RangedUnit`

### Buyu Birligi

Hedef davranis:

- Alan etkisi uygular.
- Yavaslatma, yanma veya zayiflatma gibi durum etkilerini kullanir.
- Direnc ve bagisiklik kurallariyla birlikte calisir.

Tamamlanan kod temeli:

- `MagicUnit`
- Mevcut slow davranisini kullanir.

## Hedefleme Stratejileri

Tamamlanan kod degisikligi:

- `First`: Rota ilerlemesi en yuksek dusman.
- `Last`: Rota ilerlemesi en dusuk dusman.
- `Strongest`: En yuksek mevcut can.
- `Weakest`: En dusuk mevcut can.
- `Closest`: Kuleye en yakin dusman.

Sonraki hedef:

- Etiket bazli oncelik: ucan, boss, destek, kusatma.
- Hedef tercihleri UI'da secilebilir hale getirilecek.

## Hasar Cozumleme Sirasi

Merkezi sira:

1. Ham hasar
2. Zirh azaltimi
3. Direnc azaltimi
4. Kritik carpani
5. Kalkan azaltimi
6. Minimum hasar
7. Sagliktan dusme
8. Olum tetigi

Ilk uygulama:

- `DamageResolver.Resolve(...)` bu sirayi uygular.
- Su an dusmanlar zirh/direnc/kalkan tasimadigi icin sonuc mevcut davranisla uyumlu kalir.

## Durum Etkileri

Ilk teknik temel:

- `StatusEffectType` eklendi.
- `StatusEffectStackingPolicy` eklendi.
- Mevcut `EnemyBase.ApplySlow(...)` prototip davranisi korunur.

Desteklenecek etkiler:

- Slow
- Stun
- Burn
- Poison
- ArmorBreak

Yigilma politikalari:

- Sure yenileme
- En guclu etkiyi koruma
- Yogunluk biriktirme

## Mermi Sistemi

Mevcut durum:

- Hedef yoksa veya hedef olduysa mermi temizleniyor.
- Hareketli hedef takip ediliyor.
- Fireball alan hasari uyguluyor.

Sonraki saglamlastirma:

- Havuzdan donus desteklenecek.
- Alan patlamasi hedef kaybinda da son bilinen pozisyona uygulanabilecek.
- Mermi eski hedef referansi tasimayacak.

## Carpisma Politikasi

Ilk kural:

- Dusmanlar birbirini fiziksel olarak itmeyecek.
- Ayni karakoldaki dost birlikler yerel savunma noktasinda kucuk formasyon ofseti kullanacak.
- Yol tamamen kapanmayacak; buyuk dusman ve boss gecisi oyun kilidi yaratmayacak.
- Ucan dusmanlar temas engelinden etkilenmeyecek.

## Olum ve Yeniden Dogma

Kurallar:

- Dusman olumu altin ve dalga ilerlemesini tetikler.
- Kaleye ulasan dusman can azaltir ve dalga takibinden cikar.
- Kahraman olumu yenilgi degil, gecici bayilma yaratir.
- Dost birlik olumu karakol bazli yeniden dogma sirasina girer.

## Savas Okunabilirligi

Mobil ekranda korunacak isaretler:

- Takim rengi
- Saglik cubugu
- Elit/boss isareti
- Direnc veya bagisiklik ikonu
- Hedeflenme vurgusu
- Kritik ve alan hasari icin farkli VFX sekli

## Oyun Hizi

Tamamlanan kod degisikligi:

- `GameManager.SetGameSpeed(...)` eklendi.
- Desteklenen hizlar 1x ve 2x.
- Pause durumundan donunce secili hiz korunur.

## Geri Bildirim Butcesi

Ilk hedefler:

- Hafif vurus VFX'i kisa ve dusuk yogunlukta kalir.
- Kritik vurus daha belirgin olur.
- Boss saldirisi kamera ve sesle vurgulanir.
- Kamera sarsintisi ayarlardan azaltilabilir.
- Hasar sayilari havuzlanmadan yogun savasta artirilmaz.
