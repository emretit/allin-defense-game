# Defender Chronicles — iOS First Build Checklist

Durum: iPhone ilk test hazirligi
Son guncelleme: 2026-08-15

## Hedef

Ilk gercek cihaz testi iPhone uzerinden yapilacak. Android ikinci hedef olarak korunur.

## Unity Ayarlari

- Unity surumu: `6000.4.1f1`
- Build Target: iOS
- Minimum iOS: 15.0
- Ekran: yatay
- Portre autorotation: kapali
- Landscape Left/Right: acik
- Sahne: `Assets/Scenes/Game.unity`

Unity icinde hizli uygulama:

- `Defender Chronicles > Apply iOS First Settings`
- Simulator icin: `Defender Chronicles > Apply iOS Simulator Settings`

## Simulator'a Kurma

Simulator acikken en hizli yol:

1. Terminalde proje kokunden `./tools/build_and_run_ios_simulator.sh` calistir.
2. Akis Unity export, Xcode Debug build, kurulum ve acilisi otomatik tamamlar.
3. Release Xcode projesi icin Unity menusunden `Build iOS Simulator Release Xcode Project` kullan.

Not:

- Simulator icin Apple signing genelde fiziksel cihaz kadar ugrasli degildir.
- Gercek cihaz performansi yerine gecmez; ama UI, yatay ekran, safe area ve temel dokunma hissi icin cok iyi ilk testtir.

15 Agustos 2026 sonucu:

- Development ve Xcode Simulator build gecti.
- Dynamic Island yatay safe area goruntulendi.
- Normal acilista yakalanmis error/exception yok.
- Iki tam zafer ve bir yenilgi/restart kabul kosusu gecti.

## Xcode Gerekenleri

- Xcode kurulu olmali.
- iPhone Mac'e bagli olmali.
- Apple ID Xcode'a eklenmeli.
- Signing Team Xcode icinden secilmeli.
- Bundle Identifier benzersiz olmali.

Onerilen bundle id:

- `com.emreaydin.defenderchronicles`

## Ilk Cihaz Testi

Kontrol edilecekler:

- Oyun yatay aciliyor mu?
- Safe area UI'yi kesiyor mu?
- HUD okunuyor mu?
- Karakol slotuna dokunma calisiyor mu?
- Kahraman komuta noktasina gidiyor mu?
- Dalga baslatma calisiyor mu?
- Performans ilk elde akici mi?
- Console veya Xcode logunda kirmizi hata var mi?

## Hata Olursa

Once su sirayla bakilacak:

1. Unity Console derleme hatasi.
2. Xcode signing hatasi.
3. Cihazda acilis/crash hatasi.
4. Dokunma veya safe area hatasi.
5. Oynanis akisi hatasi.
