# ALLIN: Defense — Technical Foundation

Durum: Faz 1 teknik karar notu
Son guncelleme: 2026-08-15

## Unity Surumu ve Paketler

Unity editor surumu:

- `6000.4.1f1`

Ana paketler:

- Universal Render Pipeline: `17.4.0`
- Input System: `1.19.0`
- uGUI: `2.0.0`
- AI Navigation: `2.0.11`
- Timeline: `1.8.11`
- 2D Pixel Perfect: `5.1.1`
- 2D Tilemap Extras: `package_6000.3` branch

Ilk dikey dilimde korunacak kararlar:

- Proje Unity 6 uzerinde kalacak.
- Mobil hedef icin URP korunacak.
- Input System korunacak.
- Runtime oyun verileri once ScriptableObject ve acik registry yapisi ile duzenlenecek.
- Addressables hemen zorunlu degil; MVP'de `Resources` yalnizca bootstrap/prototip icin sinirli kullanilabilir.

Test araci karari:

- Kod ayrismasi basladiginda Unity Test Framework eklenecek.
- Saf oyun mantigi siniflari editorden bagimsiz test edilecek.
- Ilk test hedefleri hasar, hedefleme, ekonomi, rota ilerlemesi, cooldown ve kayit migration kurallari olacak.

## Mevcut Kod Envanteri

### Core

- `GameBootstrap.cs`: Donustur. Mevcut prototipi ayaga kaldiriyor, ama buyudukce tek dev bootstrap sinifina donusmemeli.
- `GameManager.cs`: Donustur. Altin, can, pause, victory/defeat akisi var; bunu oyun durum makinesi ve ekonomi servisine ayirmak gerekiyor.
- `WaveManager.cs`: Donustur. Dalgalar calisiyor; spawn tanimi, boss, onizleme ve dalga butcesi icin veri odakli hale getirilmeli.
- `MapPathPresets.cs`: Donustur. Prototip icin iyi; editorden dogrulanabilir yol agina evrilmeli.
- `PlayerProgress.cs`: Donustur. PlayerPrefs kullanimi erken prototip icin yeterli; surumlu kayit modeline gecilecek.

### Data

- `GameData.cs`: Donustur. Veri siniflari var; kalici ID, direncler, roller, yetenekler, karakol uzmanliklari ve bolum tanimlari eklenecek.
- `WaveSet.cs`: Koru ve genislet. ScriptableObject yaklasimi dogru; dalga girisleri, tehdit puani ve ozel kurallar eklenecek.

### Grid ve Yol

- `GridManager.cs`: Donustur. Serbest grid yerlestirme yerine tasarimci tarafindan belirlenmis karakol slotlari gelecek.
- `PathManager.cs`: Donustur. Basit waypoint sistemi korunabilir; rota ilerleme degeri ve gelecek kavsak destekleri eklenecek.

### Towers

- `TowerBase.cs`: Donustur. Saldiri/upgrade/targeting var; hedefleme dünya X koordinatindan rota ilerlemesine tasinmali.
- `TowerPlacer.cs`: Donustur. Grid tabanli kurulum, karakol slotu secme/preview/onay akisi olacak.
- `ArcherTower.cs`, `MageTower.cs`, `WarriorTower.cs`, `CatapultTower.cs`: Koru ve yeniden adlandir. Bunlar ilk karakol rollerine temel olabilir, ama klasik kule yerine karakol/birlik odakli hale getirilecek.

### Enemies

- `EnemyBase.cs`: Donustur. Hareket ve hasar var; rota ilerlemesi, rol etiketleri, durum etkisi ve boss safhalari eklenecek.
- `GoblinEnemy.cs`, `OrcEnemy.cs`, `TrollEnemy.cs`, `DragonEnemy.cs`: Donustur. Prototip dusman davranislari tutulabilir; yeni ozgun roller ve adlar daha sonra belirlenecek.

### Heroes

- `HeroBase.cs`: Donustur. XP, seviye ve yetenek var; XP her saldirida degil oldurme/asist/dalga/gorev kaynaklarindan gelmeli.
- `KnightHero.cs`, `ElvenHero.cs`, `WizardHero.cs`, `RangerHero.cs`: Donustur. Ilk dikey dilim tek kahramanla baslayacak; digerleri sonraya alinacak.

### Projectiles

- `ProjectileBase.cs`, `ArrowProjectile.cs`, `FireballProjectile.cs`: Donustur. Hedef kaybi, alan patlamasi, havuz sistemi ve hareketli hedef guvenligi eklenecek.

### UI

- `UIManager.cs`: Donustur. Calisan HUD var; mobil safe area, savas HUD hiyerarsisi ve secili karakol paneli daha moduler olacak.
- `TowerInfoPanel.cs`: Donustur. Karsilastirmali upgrade, rol bilgisi ve mobil okunabilirlik eklenecek.
- `TowerWorldTapHandler.cs`: Donustur. Dokunma oncelikleri harita/slot/birim/UI katmanlari olarak ayrilacak.
- `FloatingText.cs`: Koru ve sinirla. Yogunluk butcesi ve performans icin havuzlanacak.

### Utils

- `TouchInputHandler.cs`: Donustur. Kamera, secim ve komutlar ayri sorumluluklara ayrilacak.
- `AudioManager.cs`: Donustur. Basit ses yonetimi olay katalogu ve muzik durum sistemiyle genisletilecek.
- `SimpleVfx.cs`: Koru ve sonra havuzla. Gri kutu geri bildirim icin yararli.

## Mantik ve Sunum Ayrimi

Hedef mimari:

- `Domain`: Saf oyun kurallari. Hasar, rota ilerlemesi, ekonomi, cooldown, XP ve durum etkileri burada hesaplanir.
- `Runtime`: Unity sahnesindeki yasam dongusu. MonoBehaviour siniflari Domain'i cagirir, sonucu sahneye uygular.
- `Presentation`: Sprite/model, animasyon, VFX, ses ve UI geri bildirimleri.
- `Content`: ScriptableObject tanimlari ve kalici ID'li icerik kayitlari.

Kural:

- Yeni oynanis kurali mumkunse once Unity'den bagimsiz kucuk bir sinifta yazilacak.
- MonoBehaviour siniflari karar veren degil, baglayan ve gosteren katman olacak.
- Hasar, ekonomi ve hedefleme gibi kurallar sahne objesi Instantiate/Destroy davranisina baglanmayacak.

## Oyun Durum Makinesi

Durumlar:

- `Boot`: Servisler ve icerik yuklenir.
- `MainMenu`: Oyuncu ana menudadir.
- `Loadout`: Kahraman, ekipman ve bolum hazirligi.
- `Preparing`: Harita acik, oyuncu ilk savunmayi kurar.
- `Wave`: Dalgalar aktif ilerler.
- `Intermission`: Dalga arasi hazirlik.
- `Paused`: Oyun duraklatilir.
- `Victory`: Bolum kazanilir.
- `Defeat`: Bolum kaybedilir.

Temel gecisler:

- `Boot -> MainMenu`
- `MainMenu -> Loadout`
- `Loadout -> Preparing`
- `Preparing -> Wave`
- `Wave -> Intermission`
- `Intermission -> Wave`
- `Wave -> Victory`
- `Wave -> Defeat`
- Her savas durumu `Paused` durumuna girip onceki duruma donebilir.

## Veri Odakli Icerik Semasi

Icerik tipleri:

- `HeroDefinition`
- `HeroAbilityDefinition`
- `OutpostDefinition`
- `OutpostSpecializationDefinition`
- `UnitDefinition`
- `EnemyDefinition`
- `StatusEffectDefinition`
- `ProjectileDefinition`
- `WaveDefinition`
- `EncounterDefinition`
- `LevelDefinition`
- `RewardDefinition`

Her tanimda bulunacak temel alanlar:

- Kalici ID
- Gorunen ad anahtari
- Aciklama anahtari
- Rol/etiket listesi
- Sayisal denge degerleri
- Gorsel/ses referanslari
- Gerekirse unlock kosulu

## Kalici Kimlik Standardi

ID formati:

- Kucuk harf
- ASCII
- Bosluk yerine `_`
- Tip prefiksi ile baslar

Ornekler:

- `hero_knight_commander`
- `ability_shield_bash`
- `outpost_barracks`
- `unit_footman`
- `enemy_raider_fast`
- `wave_valley_01_03`
- `level_valley_01`

Kural:

- ID gorunen ada bagli degildir.
- Bir ID yayinlanmis kayitta kullanildiysa yeniden baska icerige atanmaz.
- Silinen icerikler icin migration veya fallback tanimlanir.

## Olay Katmani

Ilk olay sozlesmeleri:

- `GoldChanged`
- `LivesChanged`
- `WaveStarted`
- `WaveCompleted`
- `EnemySpawned`
- `EnemyDamaged`
- `EnemyDefeated`
- `EnemyReachedBase`
- `OutpostBuilt`
- `OutpostUpgraded`
- `OutpostSold`
- `HeroMoved`
- `HeroDamaged`
- `HeroDefeated`
- `HeroLevelUp`
- `AbilityUsed`
- `GamePaused`
- `GameSpeedChanged`
- `Victory`
- `Defeat`

Kural:

- UI, ses ve VFX bu olaylari dinler.
- Ekonomi, dalga ve savas mantigi birbirini dogrudan UI referanslariyla cagirmamalidir.

## Zaman Yonetimi

Tek kaynakli zaman politikasi:

- Oynanis zamani: pause ve hiz degisiminden etkilenir.
- UI zamani: pause sirasinda da calisabilir.
- Gercek zaman: yukleme, analytics ve sistem beklemeleri icin kullanilir.

Ilk desteklenecek hizlar:

- 1x
- 2x

Kural:

- Cooldown, spawn, hareket, mermi ve durum etkileri ayni oynanis zamani kaynagini kullanacak.
- Pause, `Time.timeScale` ile baslayabilir; ileride merkezi `GameClock` ile ayrilacak.

## Rastgelelik Politikasi

Ilk dikey dilimde rastgelelik sinirli tutulacak.

Kullanilabilecek alanlar:

- Kritik vurus
- Ganimet varyasyonu
- Dalga icinde kucuk zamanlama varyasyonu
- Savas ici guc secenekleri

Kural:

- Test edilecek sistemlerde tohumlu rastgelelik kullanilir.
- Ayni seed ve ayni oyuncu girdisi ayni sonucu uretmelidir.
- Kritik ana kararlar tamamen rastgele sonuc hissi vermemelidir.

## Nesne Havuzu Plani

Havuzlanacak nesneler:

- Dusmanlar
- Dost birlikler
- Mermiler
- Hasar yazilari
- Kisa omurlu VFX
- Ses kaynaklari

Ilk havuz sozlesmesi:

- `Spawn`
- `Despawn`
- `ResetState`
- `Warmup`
- `MaxPoolSize`

Kural:

- Yogun savas aninda surekli Instantiate/Destroy kabul edilmeyecek.
- Havuzdan donen nesne eski hedef, olay aboneligi veya durum etkisi tasimayacak.

## Sahne Yapisi

Hedef sahne ayrimi:

- `Bootstrap`: Kalici servisleri kurar.
- `BattleScene`: Harita, yol, karakol slotlari, dusman girisleri ve kamera.
- `BattleUI`: HUD ve savas panelleri.
- `Content`: ScriptableObject/registry kaynaklari.
- `DevTools`: Yalnizca gelistirme yapilarinda aktif olur.

Kural:

- `GameBootstrap` gecici olarak korunacak, ama her yeni sorumluluk oraya eklenmeyecek.
- Yeni sistemler kucuk, test edilebilir servisler olarak ayrilacak.

## Varlik Yukleme Stratejisi

MVP karari:

- `Resources` prototip ve bootstrap icin sinirli kalabilir.
- Icerik sayisi arttikca acik registry veya Addressables karari yeniden verilecek.

Ilk tercih:

- Dikey dilim icin ScriptableObject registry.
- Kampanya genislerse Addressables.

## Kayit Dosyasi Surumleme

Kayit modeli:

- `schemaVersion`
- `profileId`
- `selectedHeroId`
- `unlockedContentIds`
- `completedLevelIds`
- `currencyBalances`
- `equipmentInventory`
- `settings`

Kurtarma kurallari:

- Eksik alan varsayilanla doldurulur.
- Bilinmeyen ID yoksayilir ama kayit bozulmaz.
- Eski surum migration ile guncellenir.
- Bozuk kayit icin yedekten donme veya temiz profil secenegi sunulur.

## Gelistirici Araclari

Sadece development build/editor icin:

- Dalga baslat/atla
- Altin ekle
- Can ekle
- Oyuncu hasar almaz modu
- 1x/2x/4x hiz
- Dusman cagir
- Tum dusmanlari temizle
- FPS/bellek gostergesi
- Aktif olay gunlugu

## Hata ve Gunlukleme Standardi

Seviyeler:

- `Info`: Normal gelistirme bilgisi.
- `Warning`: Oyun devam eder ama tasarim/veri sorunu vardir.
- `Error`: Kullanici deneyimi bozulabilir.
- `Fatal`: Ilerleme kaybi, savas kilidi veya acilis engeli.

Kural:

- Hassas veri gunluge yazilmaz.
- Kullaniciya teknik hata metni yerine anlasilir mesaj gosterilir.
- Telemetry MVP'de zorunlu degildir; eklenirse acik izin ve gizlilik kurallariyla eklenir.
