# Adım 4a — Savaş HUD çerçevesi

Tarih: 12 Ekim 2026. Çalışma ağacı: `/Users/emreaydin/AllinDefense-ui`; başlangıç: Adım 3 `3219f28`. Değişiklikler commit edilmedi.

## Uygulama

- `Assets/Editor/DefenderPresentationAssetBuilder.cs:1308`: HUD prefab'ı UiKit fabrikalarıyla üretiliyor; StatPill, ProgressSegments, IconButton, RadialCooldown, Modal ve Switch kullanılıyor. STATUS kutusu, geniş tehdit şeridi ve hazırlık Cancel düğmesi kaldırıldı. Prefab yerleşiminin tek sahibi `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs:222`.
- `Assets/Scripts/Presentation/DefenderHudController.Frame.cs:74`: gerçek dalga sayısı/fazı, +N altın göstergesi, kritik can eşik geçişi, yetenek cooldown/HAZIR, gerçek ödüle bağlı CTA ve ses tercihleri bağlandı. `:127`: yalnız sahadaki canlı boss için ad/can çubuğu ve %33/%66 çentikleri; 0,6 sn giriş ve reduced-motion yolu.
- `Assets/Scripts/Core/GameSessionController.cs:1127` ve `Assets/Scripts/UI/AppFlowController.cs:112`: yeniden başlat, mevcut bölüm yükleme yolunu kullanıyor ve yalnız savaş checkpoint'ini temizliyor. Haritaya dönüş UiKit onayı istiyor; pause ve onay scrim'i dünya etkileşimini kesiyor.
- `Assets/Scripts/Presentation/AdaptiveBattlefieldCamera.cs:13`: minimum ortografik yarı boyut 7,25→8,25; dikey kadraj ofseti 0,60→−0,90 ve safe-area merkezi telafisi. Gerekçe: geniş telefonlarda kalenin çatısını üst HUD'dan, yol/yuvaları alt kontrollerden ayırmak; savaş dengesi veya dünya yerleşimi değiştirilmedi.
- `Assets/Scripts/UI/FrontendCopy.cs:39`: TR/EN sabit metin çiftleri. `Assets/Scripts/UI/Kit/UiStatPill.cs:23`: aynı değer yenilemeleri delta süresini bozmaz, delta 0,4 sn sonra kapanır; ilk değer bağlanması delta üretmez. `Assets/Scripts/UI/Kit/UiOverlay.cs:13`: modalın dış tıklamayla kapanması yapılandırılabilir.
- `Assets/Tests/PlayMode/BattleHudFrameTests.cs:52`: yedi yeni HUD davranış testi; mevcut davranış testleri yeni hiyerarşiye uyarlandı, silinmedi.

## Kapılar

- Tam PlayMode: **135/135 geçti**, 0 başarısız; `Builds/adim4a-play2.xml`, 12 Ekim 19:48:46–19:57:24 UTC, 517,69 sn. Yedi yeni HUD testi dahil.
- Tam EditMode: **202/202 geçti**, 0 başarısız; `Builds/adim4a-edit.xml`, son kaynakla yeniden doğrulandı.
- Önceki `Builds/adim4a-play.xml` koşusu makine uykusuyla kesintiye uğradı ve altı gerçek zaman testi kırmızıydı; yukarıdaki tam tekrar koşusunda eşikler değiştirilmeden hepsi geçti.
- HUD builder yeniden üretimi tamamlandı: `Builds/adim4a-hud-regen.log`.
- `git diff --check` temiz; prefab GUID'i korundu.
- Build yan etkileri (`ProjectSettings.asset`, Addressables iOS content state, dinamik font asset'i) geri alındı; her batch sonrası `link.xml` ve meta dosyası geri yüklendi.

Oran testi `BattleHudFrameTests.cs:196`, ekran dikdörtgeni alanları toplamını ekran alanına böler. Rota segmentlerinin genişletilmiş ekran sınırları, sekiz yuvanın köşeleri ve kale renderer sınırlarının tamamı ayrıca ekran içinde ve HUD dikdörtgenlerinden ayrı doğrulanır.

| Oran | Test çözünürlüğü | HUD alanı | Rota | Yuva | Kale | HUD kesişimi |
|---|---:|---:|---:|---:|---|---:|
| 16:9 | 800×450 | %16,34 | 1 | 8 | Görünür | 0 |
| 19,5:9 yaklaşık | 844×390 | %17,87 | 1 | 8 | Görünür | 0 |
| iPad 4:3 | 1024×768 | %7,48 | 1 | 8 | Görünür | 0 |

HUD prefab GUID'i korundu: `d0eb9a43eadfb4d768c166ba3b9a6beb`.

## Simülatör

`zsh tools/build_and_run_ios_simulator.sh` ve `zsh tools/run_frontend_experience_simulator.sh` tamamlandı. `Builds/FrontendExperience/iOS/report.json`: **status = passed**, **errors = []**, 20 ekran görüntüsü. Gerçek boss spawn'ı beklenerek boss görünümü kaydedildi.

- Hazırlık: `Builds/FrontendExperience/iOS/battle.png`
- Aktif dalga: `Builds/FrontendExperience/iOS/battle-active-wave.png`
- Boss: `Builds/FrontendExperience/iOS/battle-level-10-boss.png`
- Pause: `Builds/FrontendExperience/iOS/pause.png`
- Haritaya dönüş onayı: `Builds/FrontendExperience/iOS/pause-map-confirmation.png`
- Oran görüntüleri: `Builds/HudFrame/preparation-800x450.png`, `preparation-844x390.png`, `preparation-1024x768.png`.

## Adım 4b'ye kalanlar

Simülatörün altı `knownIssues` kaydı hâlâ geçerli: İngilizce BuildChoices Mage/Barracks etiketleri (2), SelectedPanel Description taşmaları seçili kule/uzmanlaşma/aktif dalga/pause durumlarında (4). Bunların tamamı açıkça kapsam dışında bırakılan Adım 4b panelleridir; kayıtlar gizlenmedi veya allowlist genişletilmedi. Yeni sabit HUD çerçevesinde raporlanan taşma yoktur. Seçili kule, inşa, uzmanlaşma ve öğretici okunun yeniden tasarımı yapılmadı.
