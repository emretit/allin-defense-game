# Defender Chronicles: Heroes of Athelia
> Defender Chronicles II'den ilham alan mobil RPG Tower Defense oyunu

---

## Kurulum Rehberi (Sıfırdan)

### 1. Unity Kurulumu
1. **Unity Hub** indir: https://unity.com/download
2. Unity Hub → **Install Editor** → bu proje **Unity 6000.4.1f1** (Unity 6) ile hazırlandı. Hub’da aynı sürümü kur (veya projeyi açtığında Hub önerdiği sürümü yükle).
3. Mobil için modülleri ekle:
   - ✅ iOS Build Support
   - ✅ Android Build Support

---

### 2. Projeyi Açma (tam proje kurulu)
1. Unity Hub → **Add** → klasör olarak şunu seç: `~/DefenderChronicles` (veya bu repo’nun kökü).
2. Proje listesinden **Defender Chronicles**’a çift tıkla.
3. İlk açılışta Package Manager bağımlılıklarını indirir; `Packages/packages-lock.json` yeniden oluşabilir (normal).

**Not:** `ProjectSettings`, `Packages/manifest.json`, URP ayarları, `Assets/Scenes/Game.unity` ve Input System dosyaları repoda hazır; yeni boş proje oluşturmana gerek yok.

### 2b. TextMesh Pro (ilk açılış)
1. Menü: **Window → TextMeshPro → Import TMP Essential Resources** (bir kez).
2. Böylece `LiberationSans SDF` ve TMP ayarları oluşur; `GameBootstrap` runtime UI metinleri için fontu `Resources` veya TMP ayarlarından çözer.

### 2c. Minimal sahne + otomatik kurulum
`Game.unity` içinde yalnızca **Main Camera** ve **Bootstrap** (`GameBootstrap`) yeterlidir. Play’e basınca şunlar oluşturulur: `GameManager`, `GridManager` (hücre vurgusu dahil), `PathManager` (harita ön ayarı), `WaveManager`, `TowerPlacer`, `AudioManager`, Canvas + HUD, dalga ilerleme ve ses slider’ları, sağdaki kule bilgi paneli, kamerada dokunma + kule seçimi, varsayılan **Knight** kahramanı.

---

### 3. Proje Yapısı
```
DefenderChronicles/
├── ProjectSettings/   → Unity 6 proje ayarları (indirildi)
├── Packages/          → manifest.json (URP, Input System, TextMeshPro, …)
├── Assets/
│   ├── Settings/      → UniversalRP + 2D Renderer
│   ├── InputSystem*.  → Input System ayarları
│   ├── Scenes/Game.unity → Varsayılan sahne (Main Camera)
│   ├── Scripts/
│   │   ├── Core/      → GameManager, WaveManager, PathManager
│   │   ├── Grid/      → GridManager
│   │   ├── Towers/    → Kuleler + TowerPlacer
│   │   ├── Enemies/
│   │   ├── Heroes/
│   │   ├── Projectiles/
│   │   ├── UI/
│   │   ├── Utils/
│   │   └── Data/
│   ├── Prefabs/       → Prefab’ları burada oluştur
│   ├── Sprites/
│   └── Audio/
└── .gitignore
```

---

### 3b. Dalga verisi (isteğe bağlı ScriptableObject)
1. **Create → DefenderChronicles → Wave Set** ile bir `WaveSet` asset’i oluştur.
2. Dosyayı `Assets/Resources/Data/DefaultWaveSet.asset` yoluna kaydet (klasör yoksa oluştur).
3. `WaveManager` çalışma zamanında bu asset’i yükler; yoksa kod içi varsayılan dalgalar kullanılır.

### 3c. Harita seçimi (PlayerPrefs)
- `PlayerProgress.SelectedMapIndex` → `MapPathPresets` içindeki 0–2 yollarından birini seçer.
- Menü veya ayarlardan `PlayerProgress.SetSelectedMapIndex(i)` çağırarak haritayı değiştir (şu an kod ile; ileride UI eklenebilir).

---

### 4. Sahne Kurulumu (Unity Editor'de)

#### GameObjects oluştur:
```
Hierarchy:
├── [GameObject] "GameManager"     → GameManager.cs ekle
├── [GameObject] "WaveManager"     → WaveManager.cs ekle
├── [GameObject] "GridManager"     → GridManager.cs ekle
├── [GameObject] "PathManager"     → PathManager.cs ekle
├── [GameObject] "UIManager"       → UIManager.cs ekle
├── [GameObject] "AudioManager"    → AudioManager.cs ekle
├── [GameObject] "TowerPlacer"     → TowerPlacer.cs ekle
├── Main Camera                    → TouchInputHandler.cs ekle
└── Canvas (UI)                    → UIManager referanslarını bağla
```

#### Path Waypoints (Düşman yolu):
1. Yeni boş GameObject: `PathManager`
2. İçine 6-8 boş GameObject: `Waypoint_01`, `Waypoint_02` vb.
3. Haritada zig-zag yol oluşturacak şekilde yerleştir
4. PathManager'da `waypoints` dizisine sürükle

---

### 5. Oyun Mekanikleri

#### Kuleler
| Kule        | Maliyet | Hasar | Menzil | Özellik               |
|-------------|---------|-------|--------|-----------------------|
| **Archer**  | 75g     | 20    | Orta   | Kritik vuruş %10      |
| **Mage**    | 125g    | 35    | Geniş  | AoE + Yavaşlatma      |
| **Warrior** | 100g    | 50    | Yakın  | Zırh aurası           |
| **Catapult**| 150g    | 80    | Uzun   | Büyük AoE + Sersem    |

#### Düşmanlar
| Düşman     | HP    | Hız    | Ödül | Özellik              |
|------------|-------|--------|------|----------------------|
| **Goblin** | 60    | Hızlı  | 10g  | %15 dodge şansı      |
| **Orc**    | 200   | Orta   | 20g  | Kan gözü → 2x hız    |
| **Troll**  | 500   | Yavaş  | 40g  | Her saniye rejenerasyon |
| **Dragon** | 1000  | Orta   | 100g | Yakın kulelere ateş nefesi |

#### Herolar (Defender Chronicles 2 Ruhu)
| Hero       | Özel Yetenek     | Açıklaması                         |
|------------|------------------|------------------------------------|
| **Elven**  | Rain of Arrows   | 8 ok yağdırır, AoE                 |
| **Knight** | Shield Bash      | Yakındakileri sersem eder          |
| **Wizard** | Blizzard         | Tüm düşmanları dondurur + hasar    |
| **Ranger** | Explosive Shot   | En güçlü düşmana 5x hasar          |

---

### 6. Mobil Build (iOS ilk test)

1. **File → Build Settings → iOS**
2. **Switch Platform** de.
   - Alternatif: üst menüden **Defender Chronicles → Apply iOS First Settings** çalıştır.
   - Simulator için: **Defender Chronicles → Apply iOS Simulator Settings** çalıştır.
3. Player Settings:
   - Company Name: senin adın veya stüdyo adı
   - Product Name: Defender Chronicles
   - Bundle Identifier: `com.yourname.defenderchronicles`
   - Target minimum iOS version: iOS 15.0
   - Orientation: Landscape Left / Landscape Right
4. **Build** ile Xcode projesi çıkar.
5. Xcode’da Apple hesabını ve imza ayarını seç.
6. iPhone bağlıyken **Run** ile cihaza yükle.

Simulator açıksa Xcode’da cihaz olarak açık Simulator’u seçip **Run** demen yeterli.

### 6b. Mobil Build (Android ikinci hedef)

1. **File → Build Settings → Android**
2. Player Settings:
   - Company Name: senin adın
   - Product Name: Defender Chronicles
   - Package Name: `com.yourname.defenderchronicles`
   - Minimum API Level: Android 8.0 (API 26)
3. **Build and Run** → telefona bağlıysa direkt yükler

### 6c. QA checklist (kısa)
- **Input:** Proje ayarında Active Input Handling = **Input System Package**; gerçek cihazda dokunma, yerleştirme iptali (sağ tık / Esc editörde), pinch zoom dene.
- **Ses:** HUD’daki Music / SFX slider’ları; zafer / oyun bitti müzik geçişi.
- **Kule:** Yerleştir → dünyada kuleye dokun / tıkla → panel → upgrade / satış (satış sonrası grid hücresi boşalmalı).
- **Dalga:** Next wave → ilerleme çubuğu; düşman ölümü / çıkışı ile dolması.
- **Kahraman:** **Shield Bash** butonu (cooldown süresince tekrar basılmaz).

---

### 7. Sprite Kaynakları (Ücretsiz)
- **itch.io**: https://itch.io/game-assets/free/tag-tower-defense
- **OpenGameArt**: https://opengameart.org
- **Kenney Assets**: https://kenney.nl/assets (tower defense pack mevcut)

---

## Geliştirme Yol Haritası

- [x] Temel gameplay (kule yerleştirme + düşman dalgaları)
- [ ] Sprite/görsel entegrasyonu
- [ ] Hero seçim ekranı
- [x] 3 farklı harita (ön ayar yolları `MapPathPresets`; seçim `PlayerProgress`)
- [ ] Kule yükseltme ağacı (skill tree)
- [x] Kayıt sistemi (PlayerPrefs: yüksek dalga, toplam altın, harita, kule maskesi)
- [ ] IAP (In-App Purchase) entegrasyonu
- [ ] Leaderboard sistemi

---

## Sorular?
Her adımda takılırsan sormaktan çekinme!
