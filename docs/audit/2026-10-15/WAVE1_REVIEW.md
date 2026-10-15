# Wave 1 Denetimi (WP1 + WP2 + WP3) — salt-okur

**Sonuç:** Derlemeyi/oyun akışını bozan P0 bulunmadı; nesne yolu, hız 1x↔2x, restart onayı, boss adı ve sistem azaltılmış hareket varsayılanı spec'e uygun. 3 adet P1 (biri test zayıflatma, ikisi iOS'ta kare başına maliyet/odak tuzağı) ve birkaç P2 var; Unity koşulmadı, bulgular kod okumasına dayanır.

Kapsam dışı bırakıldı: `DefenderHudResponsiveLayout.cs` alt bar (~:255-275).

## P0
Yok.

## P1

### P1-1 — Fiyat sarsıntısı testi asıl davranışı artık doğrulamıyor (WP2, test zayıflatma)
- `Assets/Tests/PlayMode/BattleHudFrameTests.cs:~86-96` (`InsufficientGoldShakesPriceAndRequestsErrorSound`)
- Eski: iki kare sonra `chip.anchoredPosition.x != rest.x` (fiyat çipi gerçekten kayıyor). Yeni: yalnız `rejectionMotion != null`, `IsComplete == false`, `rejectionTarget == chip`. Çip hiç hareket etmese de test geçer.
- Senaryo: `UiMotion.Shake` hedefi yanlış rect'e bağlanırsa / offset 0 olursa test yeşil kalır.
- Düzeltme: en az bir kare ilerletip `anchoredPosition != rest` assert'i geri konsun (zamanlama sorunu için `Time.timeScale`/unscaled süreyi açıkça ilerlet); `IsComplete` kontrolü ek olarak kalsın.
- Paket: WP2 (ayrıca WP1 radial değişikliği tetikledi).

### P1-2 — `UiAccessibleText` iOS'ta her karede, VoiceOver kapalıyken de düğüm yönetiyor (WP3)
- `Assets/Scripts/UI/Kit/UiAccessibleText.cs:36-39, 47-58`
- `LateUpdate` iOS'ta her kare `RefreshAccessibility()` çağırıyor; `AssistiveSupport.isScreenReaderEnabled` kontrolü yok (`UiAccessibleButton.RefreshNode` bu kontrolü yapıyor). Her metin için kare başına `IsAccessible` → `IsInAccessibleScope` (tüm scope'lar üzerinde `GetComponentInParent<Canvas>` + `GetComponentsInParent<CanvasGroup>`) ve `node.label = Label` ataması yapılıyor; VoiceOver kapalıyken bile hiyerarşi düğümü oluşuyor.
- Senaryo: WP7 düşman kartında ~20-40 `AccessibleText` kullanınca iPhone'da her karede yüzlerce `GetComponent*` + etiket atama; VoiceOver kapalı kullanıcıda da maliyet var. Şu an hiçbir yerde kullanılmadığı için gizli (latent).
- Düzeltme: `RefreshAccessibility`'e `!AssistiveSupport.isScreenReaderEnabled` (force değilken) erken çıkışı ekle; `node.label`'i yalnız değişince ata (önceki etiketi sakla).
- Paket: WP3.

### P1-3 — `UiOverlay.LateUpdate` odak tuzağı başarısız olunca her karede `GetComponentsInChildren` (WP3)
- `Assets/Scripts/UI/Kit/UiOverlay.cs:66-82`
- Koşul: açık modal + seçili nesne modal dışında + modal kapsamda → `FocusFirst()` çağrılıyor; `FocusFirst` tüm `Component`'leri (`GetComponentsInChildren<Component>()`, her çağrıda liste ayırır) geziyor. Hiçbir çocuk erişilebilir/etkileşilebilir değilse (ör. fade sırasında `interactable=false`, boş etiket, Selectable kapalı) `Focus` false döner, seçim modal dışında kalır ve bu **her kare** tekrarlanır.
- Senaryo: modal açıkken seçili nesne arka plan HUD düğmesi (EventSystem son tıklananı tutar) ve modalda erişilebilir öğe yok → kare başına allocation + `RefreshAll()` (tüm `controls` üzerinde `RefreshNode(true)`).
- Düzeltme: başarısızlıkta bir sonraki denemeyi sınırla (ör. `focusPending` bayrağı + yalnız seçim değiştiğinde yeniden dene), `FocusFirst`'te `GetComponentsInChildren<UiAccessibleButton>`/`<UiAccessibleText>` yeniden kullanılan listeyle.
- Ek (doğrulanmadı): `FocusFirst` hiyerarşide ilk erişilebilir öğeyi seçiyor; RestartConfirmation'da "Oyuna dön" ilk olmak zorunda — `frameKit.Modal` içinde kapat (X) düğmesi gövdeden önce oluşuyorsa varsayılan odak "Oyuna dön" yerine kapat düğmesine gidebilir. `Bind` içindeki `SetSelectedGameObject(returnToGame)` fade bitince `focusPending` ile ezilir. Spec "varsayılan güvenli eylem = Oyuna dön"; testle doğrulanmalı (`EventSystem.currentSelectedGameObject == ReturnToGame`).
- Paket: WP3 (WP2 restart onayı etkilenir).

## P2

1. **Boss adı her karede hesaplanıyor** — `Frame.cs:156` → `GameSessionController.Battle.cs:15` → `CampaignCatalog.cs:91-108`. Boss görünürken `RefreshBoss` kare başına `BossName()` çalışıyor: bölge 2+ için `preview.Split('•')`, `Substring`, `Translate` alloc. Düzeltme: boss değişince (`shownBoss`) bir kez hesapla/önbellekle. Paket: WP2.
2. **Boss adı `preview` metnini ayrıştırıyor** — `CampaignCatalog.cs:97-104`: `groups[index]` ile `preview.Split('•')[index]` aynı sırada varsayılıyor; ×/boşluk biçimi değişirse sessizce "Bölge Lordu"na düşer (TR/EN doğruluğu `BossNameTests` 10/20/30/40/50'yi kapsıyor, kırılganlık doğrulanmadı). Daha sağlam kaynak: boss profil/wave verisinde ad alanı. Paket: WP2.
3. **Kare başına string alloc** — `Frame.cs:121-123,129` `WaveCountdown.Format(...)` ve `WaveHeroReturning.Format(...)` her `Refresh`'te; ayrıca `UiRadialCooldown.SetCooldown` zaten alıyordu. Düzeltme: saniye değişince güncelle. Paket: WP2.
4. **`SetReturning(false)` no-op, ölü kahramanda kare başına titreşim** — `UiRadialCooldown.cs:21-30` + `Frame.cs:127-130`: ölüyken önce `SetCooldown(0,..)` (Ready durumu + rozet açık), sonra `SetReturning(true)` (Disabled + rozet kapalı) her karede; `SetActive` ve `SetState` çiftli churn. `UiState.Ready` geçişi animasyon/ses tetikliyorsa titreme (doğrulanmadı). Düzeltme: ölüyken `SetCooldown` çağırma; `SetReturning(false)` ile açık durum geri yüklensin. Paket: WP2.
5. **Geri sayımda "SONRAKİ DALGA · 0"** — `Frame.cs:121-123`: eski davranış 0'da "SONRAKİ DALGA" idi; test (`BattleHudFrameTests.cs:~228`) `WaveCountdown.Format(0)` bekliyor, yani değişiklik teste yazılmış. Pratikte faz hemen Wave'e geçtiği için nadir. Paket: WP2.
6. **Kilit bölümü sabitleri çift kaynak** — `Radial.cs:508`: `kind == Barracks ? 2 : 3`; gerçek kural `TowerProgression.CanBuild` (`Barracks>=2`, `Mage>=3`). Biri değişirse kart yanlış bölüm yazar. Düzeltme: `TowerProgression`'a `BuildUnlockLevel(kind)` ekle (ownership dışı — koordinatör kararı). Paket: WP1.
7. **Satır içi ölçüler** — `Radial.cs:~203-215` (76×46 kutucuk, 244/84/52, font 11/13/16) ve `FrontendCopy.BattleCardRate` "HIZ /sn" biçimi UiTokens/UiMetrics'e bağlı değil; spec "satır içi renk yok" — renk uyumlu, yalnız ölçüler. Paket: WP1.
8. **`UiAccessibleButton.RefreshAll` Android/Editor'da düğüm oluşturuyor** — `UiAccessibleButton.cs:~124-131`: `force` ile `RefreshNode(true)`; `AssistiveSupport.activeHierarchy` yalnız iOS'ta atandığından her düğüm kendi `AccessibilityHierarchy`'sini yaratıyor (her modal aç/kapa'da). iOS-only hedef için zararsız, test dışında gereksiz. Paket: WP3.
9. **Test tercih sızıntısı** — `BattleHudFrameTests.cs:Setup/Cleanup`: `ReducedMotion = false; CameraShake = true` setter'ı kalıcı açık tercih (PlayerPrefs anahtarı) yazıyor; teardown eski *değeri* yazıyor ama anahtarı silmiyor → sonraki koşularda "açık tercih yok" yolu (sistem varsayılanı) bu makinede artık test edilmez. Düzeltme: `PlayerPrefs.DeleteKey` ile geri al (anahtar yoktuysa). Paket: WP2.
10. **Şerit tam genişlik** (doğrulanmadı) — `WaveForecast.cs:23-27`: anchor (0,0)-(1,0): TopBar tüm güvenli genişliğinde panel; `FrameScreenRects` (`ResponsiveLayout.cs:191`) listesine girdiği için hazırlık/ara fazında radyal eylem/kart yerleşim testleri bu bandı engel sayıyor. Görsel kabulde (WP11) üst orta/kenar çakışması kontrol edilsin. Paket: WP2.
11. **Ownership** — `FrontendCopy.cs` (yalnız `static` → `static partial`) WP2 "kapsam dışı"ydı; derleme için zorunlu, tek satır, kabul edilebilir. Başka ownership ihlali yok (`git status` ile spec listeleri karşılaştırıldı; `AuditLayer2UxTests`/`CoreRulesTests` yalnız hız satırları).

## Doğrulananlar (sorun yok)
- `ResponsiveLayout` "TopBar/WaveForecast" yolu = `InitializeWaveForecast` oluşturduğu `TopBar` altındaki `WaveForecast` düğümü (`WaveForecast.cs:23`); `MessageChrome` yolu mevcut. Etkin değilken atlanıyor, null güvenli.
- Hız: `SpeedSteps {1,2}`, döngü `% Length`; eski 4x/8x → `SpeedLabelForScale` en yakın "2x"; kalıcı kayıtta hız tutulmuyor (`speedStepIndex = 0` başlangıçta) → eski kayıt sorunu yok. Denge/bot kodunda `ToggleGameSpeed` 4x/8x'e bağımlı yok.
- Restart: `PauseRestart` → onay; "Oyuna dön" = `Hide` + devam, "Yeniden başlat" = `restart`; pause kapanınca `restartConfirmation.Hide()`. İptal checkpoint'e dokunmuyor (testte `checkpoint` assert'leri korunmuş).
- Geri sayım/oto-başlatma zamanlaması: yalnız etiket/gösterim değişti (`IntermissionSeconds` const 8, mantık dokunulmamış). İlk dalga düğmesi (`WaveActionLabel(first…)`) korunmuş.
- `FrontendPreferences`: açık tercih (`HasKey`) varsa sistem değeri yok sayılıyor; `MotionChanged` yalnız açık tercih yokken sistem değişiminde tetikleniyor. `.mm` + `DllImport` deseni uyumlu; Editor/Android'de `false`.
- Modal kapsam: `UiOverlay` BottomSheet (`bottom`) `BeginScope/LateUpdate/FocusFirst`'te erken çıkıyor → modeless.
- Metinler TR/EN: "Chapter"/"Bölüm", "Guard"/"Squad" terimleri sözlükle uyumlu; yeni satır içi renk yok.
