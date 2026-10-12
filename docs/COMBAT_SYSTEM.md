# ALLIN: Defense — Combat System

Durum: Canli savas kurallari ve eski Faz 3 prototip/tasarim notlari
Son guncelleme: 2026-10-11

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

Canli savas `GreyboxEnemy`, `GreyboxOutpost`, `GreyboxFriendlyUnit` ve `GreyboxHero` ile `GameSessionController.AdvanceBattle` uzerinden ilerler (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`). Asagidaki `EnemyBase`/birlik/mermi siniflari eski prototip temelidir; canli aktorlerin yerine calismaz.

Ilk teknik temel:

- `DamageResolver` eklendi.
- `DamageRequest` ve `DamageResult` hasar sirasini merkezi hale getirir.
- `EnemyBase.TakeDamage(...)` artik hasari bu cozumleyiciden gecirir.

## Birlik Rolleri

### Yakin Dovus Birligi

Hedef davranis:

- Barracks karakolundan uc asker dogar (`Assets/Scripts/Data/TowerProgression.cs`, `BarracksSquadSize`).
- Kendi karakolunun yerel savunma noktasina gider.
- Menziline giren dusmani tutar.
- Dusmanla temas halinde saldirir.
- Olurse karakol eksik formasyon yuvasini yeniden doldurur; uretimler arasi sayac 2.25 sn'dir.
- Bozulan kislada askerler hareket ve saldiri yapmaz; canli ve konuslanmis askerler yolu bloklamaya devam eder, henuz konuslanmayanlar bloklamaz. Bozulma bitince normal davranis surer (`Assets/Scripts/Core/GreyboxCombatants.cs`, `GreyboxOutpost.Tick`, `GreyboxFriendlyUnit.Tick` / `CanBlock`).

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

Eski prototipte tanimli stratejiler:

- `First`: Rota ilerlemesi en yuksek dusman.
- `Last`: Rota ilerlemesi en dusuk dusman.
- `Strongest`: En yuksek mevcut can.
- `Weakest`: En dusuk mevcut can.
- `Closest`: Kuleye en yakin dusman.

Canli karakollar en ilerideki hedefi secer; Sharpshooter once Support, sonra Siege, sonra rota ilerlemesini kullanir (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`).

Sonraki hedef:

- Diger etiket bazli oncelikler: ucan, boss.
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
- Canli Vanguard %50 fiziksel direnc tasir; buyu hasarina bu direnc uygulanmaz. Diger canli arketiplerin bu yolda direnci sifirdir (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Combat/BattleRules.cs`).

## Durum Etkileri

Ilk teknik temel:

- `StatusEffectType` eklendi.
- `StatusEffectStackingPolicy` eklendi.
- Mevcut `EnemyBase.ApplySlow(...)` prototip davranisi korunur.

Canli savasta slow ve stun uygulanir; Frost 2 sn %50 slow verir, Knight Kalkan Darbesi 1 sn stun uygular (`Assets/Scripts/Data/TowerProgression.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`).

Prototip/tasarim etki listesi:

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

Canli Archer/Mage hasari saldiri aninda uygulanir; mermi gorunumu sunum/VFX katmanindadir (`Assets/Scripts/Core/GreyboxCombatants.cs`, `GreyboxOutpost.Tick`).

Eski prototip durumu:

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
- Canli sistemde yakin canli Knight veya konuslanmis asker bulununca dusman saldirmak icin durur; boss da durabilir. Eszamanli tutma kotasi veya boss hareket tabani yoktur (`Assets/Scripts/Core/GameSessionController.cs`, `FindNearestBlocker`; `Assets/Scripts/Core/GreyboxCombatants.cs`, `GreyboxEnemy.Tick`).
- Ucan dusmanin temas engelini yok saymasi gelecek tasarimdir; canli ucan arketip yoktur.

## Olum ve Yeniden Dogma

Kurallar:

- Dusman olumu altin ve dalga ilerlemesini tetikler.
- Kaleye ulasan dusman can azaltir ve dalga takibinden cikar.
- Kahraman olumu yenilgi degil, 5 sn gecici bayilma yaratir; tam canla ayni muhafiz konumuna doner. Normal saldiri 20 hasar / 0.35 sn; cooldown sifira clamp edilir, bosta saldiri birikmez (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/CombatMath.cs`).
- Boss sizmasi normal hasardan kalan cani tek yildiz tavanina dusurur (20→7); tek basina otomatik yenilgi yaratmaz. Can kalirsa tum dalgalar bitince en fazla bir yildizla zafer, can biterse yenilgi olur (`Assets/Scripts/Combat/BattleRules.cs`, `Assets/Scripts/Core/GameSessionController.cs`).
- Victory/Defeat olustugu adimda kalan dusman, karakol, asker ve Knight tick'leri durur; zaman birikimi sifirlanir, terminal sonrasinda olum odulu kazanilmaz (`Assets/Scripts/Core/GameSessionController.Battle.cs`).
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

Canli uygulama:

- `GameSessionController` hiz secimini ve savas saatini yonetir; eski `GameManager.SetGameSpeed(...)` prototip API'sidir.
- Desteklenen hizlar 1x, 2x, 4x ve 8x.
- Pause durumundan donunce secili hiz korunur.

Kaynak: `Assets/Scripts/Core/GameSessionController.cs` (`SpeedSteps`, `OnPausePressed`, `ToggleGameSpeed`), `Assets/Scripts/Core/GameSessionController.Battle.cs`. Sureler 1x oyun zamani saniyesidir (`Assets/Scripts/Combat/BattleRules.cs`).

## Geri Bildirim Butcesi

Ilk hedefler:

- Hafif vurus VFX'i kisa ve dusuk yogunlukta kalir.
- Kritik vurus daha belirgin olur.
- Boss saldirisi kamera ve sesle vurgulanir.
- Kamera sarsintisi ayarlardan kapatilabilir.
- Muzik tercihi savas muziginin iki crossfade kanalina, SFX tercihi calan efekt/UI seslerine ve ambiyansa aninda uygulanir. Kayitsiz tercih varsayilani 1'dir; nominal ses seviyeleri korunur (`Assets/Scripts/UI/FrontendPreferences.cs`, `Assets/Scripts/Presentation/DefenderAudioController.cs`, `Assets/Scripts/Presentation/DefenderAmbientAudioController.cs`).
- Hasar sayilari havuzlanmadan yogun savasta artirilmaz.
