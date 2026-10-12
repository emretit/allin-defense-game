# Faz 0 — UI temel çalışması

Worktree: `/Users/emreaydin/orca/workspaces/AllinDefense/ui-faz0`  
Unity: `6000.4.1f1`  
Durum: Uygulama ve doğrulama tamamlandı; EditMode 143/143, PlayMode 117/117 başarılı. Değişiklikler commit edilmedi.

## Değişiklikler

- `UIManager`, `TowerInfoPanel`, `TowerWorldTapHandler`, `FloatingText` kaynakları ve `.meta` dosyaları silindi. `Assets`, `Packages`, `ProjectSettings` içindeki kod, test ve asmdef taramasında dış kullanım yoktu; yalnız eski dosyalar arasında referans vardı. Dört GUID'in `.unity`, `.prefab`, `.asset` ve `.asmdef` taramasında referansı bulunmadı.
- Ön yüzün 32 statik etiketi `FrontendCopy` içindeki sabit C# üye kimlikleriyle bağlandı. Her kimlik bir `LocalizedCopy` TR/EN çifti tutar; `AppFlowController.CreateText` artık gösterilen Türkçe metni sözlükte arayıp çevirmen eklemez, açık çiftin `IsLocalized` bilgisini kullanır. Çiftin Türkçe tarafı değişse bile İngilizce tarafı bağımsız kalır. Boş, sayısal, oyuncu adı ve mevcut dinamik çiftlerle hesaplanan metinler literal olarak kalır.
- Kampanya verisi, eski HUD ve servis durumlarının kaynak metin API'si uyumluluk için korundu. Ön yüz kataloguna taşınan eski sözlük satırları aynı çiftleri paylaşır; kopya çeviri metni oluşturulmadı.
- `EFEKTLER Row` dahil beş ayar satırının nesne adları görünür etiketten ayrıldı. Nesne adları ve mevcut testler değiştirilmedi.
- `CreateHudPrefab` varsayılan olarak mevcut prefabı döndürmeye devam eder; açık `regenerate: true` yolu mevcut prefabı aynı dosyada yeniden üretir. Menü/batch giriş noktası `DefenderPresentationAssetBuilder.RegenerateHudPrefab` gerekli katalog/sprite/font varlıklarını kontrol eder ve yalnız HUD ile katalog referansını kaydeder.
- `CampaignLevelFactory.cs` ve `GameSessionController.cs` dahil slot/route koduna dokunulmadı. Teslimde mevcut HUD prefabı ve katalog varlıkları korunur.

## Doğrulama

- Kaynak karşılaştırması: Eski çalışma zamanı tablosundaki 95 TR/EN çifti UTF-8 bakımından birebir aynı. Tüm açık ön yüz nesne adları aynı; etiketlerden üretilmiş ayar satırları aynı adları bağımsız sabitler olarak kullanıyor.
- EditMode: **143/143 geçti**, 0 başarısız / 0 atlanan. `Builds/editmode-results.xml`, `Builds/editmode-tests.log`.
- PlayMode: **117/117 geçti**, 0 başarısız / 0 atlanan; 536,66 saniye. `Builds/playmode-results.xml`, `Builds/playmode-tests.log`. Toplam **260/260** test başarılı.
- Açık HUD yeniden üretimi: Batch metodu exit 0 ile tamamlandı ve mevcut prefab dosyasını gerçekten yeniden yazdı. Prefab/katalog GUID metaları değişmedi. Doğrulamadan sonra prefab, katalog ve metaları başlangıç SHA-256 değerleriyle birebir geri yüklendi; teslimde bu varlıklar değişmedi. Günlük: `Builds/hud-regeneration.log`; hash kanıtı ve üretilmiş örnek: `Builds/faz0-regeneration-backup/`.
- PlayMode sırasında Unity tarafından silinen `Assets/AddressableAssetsData/link.xml` ve metası HEAD içerikleriyle geri yüklendi; bu iki dosyada teslim farkı yok.
- Yeni EditMode testleri literal oyuncu metninin kazara çevrilmemesini ve varsayılan HUD üretiminin prefab baytlarını/GUID'ini korumasını kontrol eder.
- Yeni PlayMode testi sözlükte bulunmayan değiştirilmiş Türkçe etiketle TR ve EN başlangıcını, canlı dil değişikliklerini ve yeniden etkinleşmeyi kontrol eder.
- `git diff --check` geçti.

Unity komutlarında README'deki `-batchmode -projectPath -runTests -testPlatform -testResults -logFile` bayrakları kullanıldı. İlk import öncesi yalnız bu worktree'nin `Library` klasörü main'den APFS clone ile oluşturuldu; main projesi açılmadı veya kilitlenmedi.

## Kullanım

Metin değişikliklerini `Assets/Scripts/UI/FrontendCopy.cs` içindeki kimliğin TR/EN çiftinde yapın; test nesne adlarını etiketten üretmeyin. Dinamik metinlerde mevcut açık `GameLocalization.Text(tr, en)` çiftleri kullanılabilir.

HUD builder değişikliğinden sonra Unity menüsünde **ALLIN: Defense > Regenerate HUD Prefab** komutunu çalıştırın veya:

```sh
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$PWD" \
  -executeMethod AllinDefense.Editor.DefenderPresentationAssetBuilder.RegenerateHudPrefab \
  -logFile "$PWD/Builds/hud-regeneration.log"
```

Normal presentation build idempotent kalır; açık yeniden üretim mevcut prefabın özel düzenlemelerinin yerine builder çıktısını yazar.

## Değişen dosyalar

- `Assets/Editor/DefenderPresentationAssetBuilder.cs`
- `Assets/Scripts/UI/AppFlowController.ClosedTest.cs`
- `Assets/Scripts/UI/AppFlowController.Hero.cs`
- `Assets/Scripts/UI/AppFlowController.Screens.cs`
- `Assets/Scripts/UI/AppFlowController.cs`
- `Assets/Scripts/UI/FloatingText.cs`
- `Assets/Scripts/UI/FloatingText.cs.meta`
- `Assets/Scripts/UI/FrontendCopy.cs`
- `Assets/Scripts/UI/FrontendCopy.cs.meta`
- `Assets/Scripts/UI/GameLocalization.cs`
- `Assets/Scripts/UI/TowerInfoPanel.cs`
- `Assets/Scripts/UI/TowerInfoPanel.cs.meta`
- `Assets/Scripts/UI/TowerWorldTapHandler.cs`
- `Assets/Scripts/UI/TowerWorldTapHandler.cs.meta`
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/UIManager.cs.meta`
- `Assets/Tests/EditMode/UiFoundationTests.cs`
- `Assets/Tests/EditMode/UiFoundationTests.cs.meta`
- `Assets/Tests/PlayMode/FrontendCopyBindingTests.cs`
- `Assets/Tests/PlayMode/FrontendCopyBindingTests.cs.meta`
- `README.md`
- `docs/ui-modernization/FAZ0_FOUNDATION_2026-10-12.md`
