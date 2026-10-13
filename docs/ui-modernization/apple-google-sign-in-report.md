# Apple / Google giriş uygulaması — 2026-10-13

Welcome ve hesap ekranındaki Apple/Google düğmeleri gerçek sağlayıcı enum değerlerini kullanır. Apple iOS/Editor'da görünür, Android'de gizlidir; Google ayarı boşken düğme görünür ve dokunulduğunda güvenli yapılandırma metni gösterilir. iOS dışındaki yerel köprü ağ veya UGS çağrısı yapmadan güvenli hata sonucu döndürür.

## Değişiklikler

- `Assets/Scripts/Core/ExternalSignIn.cs:5`: token, iptal, hata ve yapılandırılmadı sonuçları; sağlayıcı ve test kimlik servisi arayüzleri.
- `Assets/Scripts/Core/IosExternalSignInBridge.cs:9`: UnitySendMessage JSON ABI, benzersiz istek/nesne kimliği, geç gelen sonuç filtresi ve oturum temizliği; ID token diske yazılmaz.
- `Assets/Plugins/iOS/AuthBridge.mm:72`: Apple AuthenticationServices, yalnız `fullName`, UTF-8 identityToken, state denetimi ve ayrı iptal/hata sonuçları.
- `Assets/Plugins/iOS/AuthBridge.mm:98`: Google sistem ASWebAuthenticationSession, OAuth code + S256 PKCE, `openid profile`, ters iOS client ID şeması, state/nonce/aud/iss/exp denetimi ve secretsiz token değişimi; token imzasını UGS doğrular, `prefersEphemeralWebBrowserSession = NO`.
- `Assets/Editor/IosExternalSignInPostprocessor.cs:18`: 1001 sırasıyla mevcut 1000 numaralı yönelim postprocessor'ından sonra Apple capability/entitlement ve AuthenticationServices/Security framework ekleme; mevcut URL şemalarını koruyarak Google şemasını ekleme.
- `Assets/Scripts/Core/UnityGameServicesController.cs:35`: sahte sağlayıcı/kimlik servisi enjeksiyonu, tokenı SignInWithAppleAsync/SignInWithGoogleAsync'e iletme, kayıtlı oturum yenileme, restore/sync/giriş/silme yarış korumaları ve iki sağlayıcının yerel durumunu silme; misafir ithali ayrı ve açık kalır.
- `Assets/Scripts/Core/CampaignProgressStore.cs:74`: yerel kayıt sürümü 5; eski sürümdeki Apple=2/Google=3 değerleri önceki Unity hesabı kimliğini korumak için PlayerAccount'a göçer, yeni Apple/Google kayıtları sağlayıcılarını korur; v4 ilerlemesi yeniden göç incelemesine işaretlenmez.
- `Assets/Scripts/Core/CampaignProgressionContracts.cs:35`: mevcut sunucu şeması 4 olarak korunur; Cloud Code değişikliği veya deploy yok.
- `Assets/Scripts/UI/AppFlowController.Screens.cs:322`: Welcome sırası Apple → Google → mevcut misafir seçeneği; hesap ekranında misafirken “bağla”, kayıtlı hesapta “devam et”; Unity hesabı düğmesi korunur, resmî logolar, siyah/beyaz sağlayıcı yüzeyleri, VoiceOver ve en az 44pt gerçek dokunma alanları.
- `Assets/Scripts/UI/FrontendCopy.cs:35`: yeni metinlerin sabit TR/EN çiftleri; iptal sessiz, hata metni güvenli ve exception ayrıntıları arayüze taşınmaz.

## Yapılandırma ve kalan işler

`Assets/Resources/Presentation/FrontendPresentationTheme.asset` üzerindeki `googleIosClientId` boş bırakıldı. Koordinatör Google Cloud'daki iOS OAuth client ID'yi (hedef build bundle ID `com.emreaydin.allindefense`) bu alana yazmalı ve UGS Authentication Apple/Google sağlayıcılarını yapılandırmalıdır; alan secret değildir. Client ID biçimi `<id>.apps.googleusercontent.com`, callback `com.googleusercontent.apps.<id>:/oauthredirect` olur; build sonrası URL şeması otomatik kaydedilir.

Apple token REST revoke kapsam dışıdır ve sunucu gerektirir; UGS hesabı silinir, yerel köprü ve UGS session token temizlenir, sistem Apple/Google hesabının OS/browser SSO oturumu uygulama tarafından silinmez. Xcode projesi üretimi ve iOS/simülatör build yapılmadı; gerçek cihaz kimlik kabulü, Dashboard/Console ayarları ve simülatör koordinatöre bırakıldı. Yeni paket/SDK/CocoaPods eklenmedi ve commit/push yapılmadı.

## Resmî kaynaklar

- Apple logo PNG: https://devimages-cdn.apple.com/design/resources/download/Logo-Sign-in-with-Apple.dmg — orijinal raster korunur, Unity sprite dikdörtgeni logoyu çevresindeki siyah boşluktan ayırır.
- Google renkli G: https://developers.google.com/static/identity/images/g-logo.png — orijinal raster korunur.
- Google OAuth: https://developers.google.com/identity/protocols/oauth2/native-app
- Apple AuthenticationServices: https://developer.apple.com/documentation/authenticationservices/implementing-user-authentication-with-sign-in-with-apple
- Marka kuralları: https://developers.google.com/identity/branding-guidelines ve https://developer.apple.com/design/human-interface-guidelines/sign-in-with-apple/

## Kanıtlar

Kapı sayıları ve sonuçlar `Builds/auth/report.json` içindedir; tam EditMode XML/log `Builds/auth/edit.xml`, `Builds/auth/edit.log`; seçili PlayMode XML/log `Builds/auth/play.xml`, `Builds/auth/play.log`. Seçili sınıflar: ExternalSignInFlowTests, AccountDeletionTests, FrontendCopyBindingTests, UiIdentityAndTouchTests, GreyboxSceneTests.

Yeni 12 EditMode vakası eski 2/3 değerlerini, v4 ilerleme korumasını, gerçek sağlayıcının kayıt/restart davranışını, açık misafir ithalini ve kayıtlı oturum gereksinimini kapsar. Yeni 13 PlayMode vakası Apple/Google başarı, Welcome'dan çıkış, Apple adı/fallback ad, sessiz iptal, güvenli sağlayıcı/servis hatası, boş Google ID, Android Apple görünürlüğü, eşit boyutlar/TR-EN taşma/VoiceOver/44pt dokunma, silmede geç token, mevcut silme yolu ve eşzamanlı sync/giriş korumasını kapsar; testler gerçek UGS/Cloud Code çağrısı yapmaz.

Görsel olarak incelenmiş görüntüler: `Builds/auth/Welcome-Turkish.png`, `Welcome-English.png`, `Login-Turkish.png`, `Login-English.png` (1080×1920). Objective-C++ sözdizimi kontrolü: `xcrun clang -fsyntax-only -fobjc-arc -isysroot "$(xcrun --sdk iphoneos --show-sdk-path)" -arch arm64 -miphoneos-version-min=15.0 Assets/Plugins/iOS/AuthBridge.mm`; log `Builds/auth/native-syntax.log`, ARC plugin import ayarı eklendi ve UnityGetGLViewController C linkage ile bildirildi.
