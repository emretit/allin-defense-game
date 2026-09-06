# Kapalı test kurulum notları — ct-20260906-01

Kaynak: `/Users/emreaydin/DefenderChronicles-first-region`. Unity 6000.4.1f1, uygulama sürümü 0.4.0. Test uygulamasının adı **Defender Chronicles Test**, kimliği **com.emreaydin.defenderchronicles.closedtest**. Bulut ortamı `closed-test`.

## Android

APK: `Builds/ClosedTest-Android/DefenderChronicles-ct-20260906-01.apk`. Geliştirme paketi olduğu için mağaza dağıtımı için kullanılmaz. ARM64 Android cihazında dosyayı açarak kurun veya Android SDK ile:

```sh
adb install -r Builds/ClosedTest-Android/DefenderChronicles-ct-20260906-01.apk
```

## iPhone

Unity dışa aktarımı ve imzasız Xcode derlemesi geçti. İmzalı derleme henüz hazır değil: bu Mac'te Xcode, geliştirme ekibi için hesap oturumu ve uygulama profili bulamadı.

1. Xcode → Settings → Accounts bölümünde Apple Developer hesabına giriş yapın.
2. `Builds/ClosedTest-iOS/Unity-iPhone.xcodeproj` projesini açın. Unity-iPhone hedefinde Signing & Capabilities altında kendi geliştirme ekibinizi seçin; Automatic Signing açık olsun.
3. iPhone 13'ü bağlayın, kilidini açın ve Xcode'da hedef cihaz olarak seçin. Gerekiyorsa cihazdaki geliştirici modu ve bilgisayara güven adımlarını tamamlayın.
4. Run ile kurun. Mevcut uygulamadan ayrı test simgesi açılır.

İmzalı paket oluşmadan `.app` dosyasının kopyalanması iPhone kurulumu sağlamaz. Şifre veya hesap anahtarı bu projeye yazılmamalı.

## Yeniden derleme ve test

Bu çalışma kopyasının Editor'ü kapalıyken çalıştırın; ortak projeye ait açık Editor'ü kullanmayın.

```sh
unity build /Users/emreaydin/DefenderChronicles-first-region --target Android --execute-method DefenderChronicles.Editor.ClosedTestBuilds.Android --allow-dirty-build --timeout 1200
unity build /Users/emreaydin/DefenderChronicles-first-region --target iOS --execute-method DefenderChronicles.Editor.ClosedTestBuilds.Ios --allow-dirty-build --timeout 1200
unity test /Users/emreaydin/DefenderChronicles-first-region --mode EditMode --output /tmp/defender-editmode.xml --timeout 300
unity test /Users/emreaydin/DefenderChronicles-first-region --mode PlayMode --output /tmp/defender-playmode.xml --timeout 900
```

`Assets/Editor/ClosedTestBuilds.cs` gerçek derleme giriş adları için kaynak kabul edilir. Telefon ölçümü ve oyuncu testi adımları `PLAYTEST_PROTOCOL_TR.md` içindedir. Otomatik geliştirme koşuları oyuncu verisinden ayrı tutulur.
