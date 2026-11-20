# ALLIN: Defense — TODO

Odak: önce oyunu bitir. Aşağıdakiler bilerek sonraya bırakıldı (2026-10-14).

## Aktif iş
- [ ] v1.1 — oyun içi UI taraması commit'li (314bfca, d448e53); telefon geri bildirimi düzeltmeleri commit'siz (Emre telefonda deniyor). YENİ: ilerleme/amaç tasarımı (testçiler "amaçsız" dedi) — Emre seçimi bekleniyor. Handoff: `.claude/handoff-latest.md` (en üst EK).

- [x] Kule görünümü v3 (a65e72c).
- [x] Birlik görünümü v3 (0f42716). Küçük: HUD üst barda boss adı (ch30/50 "Bölge Lordu" yazıyor), boss rozeti taç ikonu.

## Sonraya: Giriş (Apple / Google)
- [ ] Fiziksel iPhone'da Apple ile giriş dene (simülatörde çalışmaz).
- [ ] Fiziksel iPhone'da Google ile giriş dene. UGS Google sağlayıcısı iOS client `aud`'unu reddederse
      OpenID Connect sağlayıcısına geç (iOS client ID tema asset'inde: `FrontendPresentationTheme.asset`).
- [ ] Apple token revoke (App Store kuralı: hesap silinince Apple bağlantısı da iptal): Cloud Code fonksiyonu + Apple `.p8` anahtarı.
- [ ] Hesap silmede `DeleteAccountAsync`'in Cloud Save verisini de sildiğini doğrula.

## Sonraya: Yayın kontrolleri
- [ ] Unity Dashboard'da Cloud Diagnostics'in (çökme raporu) açık olduğunu doğrula.
- [ ] Fiziksel iPhone'da FPS ölç (savaş + boss). Düşükse ilk aday: HUD canvas SetPass'i
      (W2b ölçümü: aktif dalga SetPass 87→102, 94'ün 42'si HUD canvas'ları — `Builds/env2-metrics.json`).
- [ ] Fiziksel iPhone'da haptik, UI sesleri, azaltılmış hareket, VoiceOver etiketleri.
