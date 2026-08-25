## HUD Ability State Pass — 2026-08-16

Kapsam: `VIS-608` için Kalkan Darbesi button'ının ready, cooldown ve blocked state'lerini runtime'da daha okunur hale getiren ilk pass.

### Bu pass'te yapılanlar

- `Assets/Scripts/Presentation/DefenderHudController.cs`
  - Kalkan Darbesi button'ına runtime `CooldownFill`, `ReadyGlow` ve `BlockedVeil` katmanları eklendi.
  - Button label artık hero state'e göre `HAZIR`, cooldown saniyesi, `DURAKLADI` ve `DÖNÜYOR` kopyaları arasında geçiyor.
  - Ready state için yeşil accent, blocked state için veil ve cooldown state için daha belirgin disabled/background ayrımı uygulanıyor.
- `Assets/Scripts/Core/GreyboxCombatants.cs`
  - Hero cooldown süresi HUD tarafından okunabilir hale getirildi.
- `Assets/Scripts/Core/GameSessionController.cs`
  - Simulator kanıtı için `DC_ABILITY_CAPTURE=1` akışı eklendi; bu akış sahne açıldıktan sonra Kalkan Darbesi'ni otomatik tetikleyip cooldown state'inin capture alınmasını kolaylaştırıyor.

### Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Ready state | `docs/evidence/runtime/defender-simulator-hud-ability-state-ready-landscape-readable.png` | Ability hazır state'i |
| Cooldown state | `docs/evidence/runtime/defender-simulator-hud-ability-state-cooldown-pass-landscape-readable.png` | Sahne açıldıktan sonra otomatik tetiklenen cooldown capture'ı |
| Cooldown raw | `docs/evidence/runtime/defender-simulator-hud-ability-state-cooldown-pass-raw.png` | Simulator ham framebuffer çıktısı |

### Gözlem

Kabul edilen cooldown capture'ında button `KALKAN / 8 sn` state'ine düşüyor ve alt hero satırı da eşzamanlı olarak cooldown bilgisini gösteriyor. Böylece önceki belirsiz capture probleminden ayrışmış güvenilir bir runtime kanıtı elde edildi.

### Açık kalanlar

- Cooldown fill/motion hâlâ final kalite seviyesinde değil; özellikle metinsiz okunurluk daha da güçlendirilmeli.
- Tablet / 4:3 oran QA'sı yapılmadı.
- Ready pulse ile combat VFX yoğunluğu birlikte test edilmedi.

### Karar

Bu pass `VIS-608` için işlevsel bir runtime milestone'dur; ancak final kapanış değildir. Mevcut durumda state geçişi güvenilir biçimde görünür, fakat motion/accessibility kapanışları hâlâ açıktır.
