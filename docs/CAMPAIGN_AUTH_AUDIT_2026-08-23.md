# Campaign, Authentication and Device Audit - 2026-08-23

## Scope

This audit covers the issues raised during the current gameplay pass:

- Battlefield camera drift during play
- Slow gameplay after building or loading an outpost
- Real 1x, 2x, 4x and 8x speed controls
- Missing welcome, login, menu, campaign and result screens
- A 50-level campaign with progressive regions and difficulty
- Persistent stars, rewards and account-safe progress
- Guest, Apple and Google sign-in with Cloud Save
- iPhone 17 Pro layout, performance and long-session stability

The older visual-development backlog remains a separate production-art backlog. It is not used to
misrepresent the status of the gameplay and application-flow work in this audit.

## Completed

### Runtime and camera

- Camera feedback returns to a stable baseline and no longer accumulates downward drift.
- Building and outpost effects do not apply a persistent slow modifier.
- The speed selector exposes 1x, 2x, 4x and 8x as real time scales.
- The new 1x keeps the previously accepted normal gameplay pace through the gameplay clock.

### Application flow

- Welcome, login, main menu, five-region campaign map and result screens are available before battle.
- Login offers guest, Apple and Google choices.
- Account switching clears stale external credentials before a different provider is opened.
- The local Unity project is linked to the Defender Chronicles Unity Cloud project.
- The Unity Player Accounts OAuth provider is enabled for iOS/Android and its client ID is stored in
  the project Player Accounts settings.
- Guest sign-in creates a live Unity Authentication player and synchronizes campaign progress with
  Cloud Save. It still has a safe local fallback for service outages.

### Campaign and progression

- The catalog contains five regions and 50 unique levels.
- Each region has ten levels and ends in a regional boss.
- Enemy roles are introduced progressively; wave counts grow from 3 to 12.
- Paths vary by preset and region mirroring instead of reusing one battlefield route everywhere.
- Starting gold and lives tighten across the campaign while encounter pressure and target duration grow.
- Completion awards 1-3 stars, Valor and hero XP.
- Base rewards can only be claimed once; later replays only award the difference for improved stars.
- The result screen supports retry, next level and return to map.
- The versioned local save is written atomically and merges local/cloud progress by best result.

### Verification

- EditMode: 49/49 passed.
- PlayMode: 8/8 passed.
- iPhone 17 Pro level 50 layout: passed.
- iPhone 17 Pro result/reward layout: passed.
- Live guest Authentication and Cloud Save: passed on iPhone 17 Pro.
- Normal iPhone 17 Pro launch now opens the welcome screen instead of being mistaken for an automation run.
- The live Unity Player Accounts portal and Google account handoff open from the iPhone 17 Pro build.
- Player Accounts opens in portrait when the phone is upright, while the Unity game remains landscape-only;
  cancel and successful-dismiss paths restore the game orientation mask.
- Level 50 performance acceptance: 60 FPS median, p95 near 17 ms, no low-memory warning.
- Dense 90-second soak: 60 FPS median, p95 18.2 ms, 0.05 seconds maximum continuous time
  below 30 FPS, 6.6% post-warmup memory growth, no low-memory warning.

Evidence is stored under `Builds/`:

- `iphone17pro-level50-final.png`
- `iphone17pro-result-reward-fixed.png`
- `iphone17pro-ugs-guest-final.png`
- `iphone17pro-ugs-live-guest.png`
- `ios-ugs-live-guest.log`
- `iphone17pro-frontend-normal.png`
- `iphone17pro-google-portal.png`
- `editmode-results-frontend-auth.xml`
- `playmode-results-frontend-auth.xml`
- `ios-performance-acceptance.log`
- `ios-soak-acceptance.log`
- `editmode-results-final-all.xml`
- `playmode-results-final-all.xml`

## External Configuration Still Required

The Unity Cloud project link, Player Accounts provider, live anonymous Authentication, Cloud Save and
the Google browser handoff are complete. The remaining account-owned acceptance steps are:

1. Complete the Google account credential/verification screen and confirm the deep-link return.
2. Run one Apple sign-in and one guest-to-linked-account merge on iPhone 17 Pro.
3. Add public Privacy Policy and Terms of Service links before publishing.

## Later Release Backlog

These items are release work, not blockers for the completed gameplay/application-flow pass:

- Manual balance sessions using archer-heavy, barracks-heavy and mage-heavy strategies across all regions
- Settings, localization beyond Turkish, privacy policy and account-deletion surfaces
- App Store signing, production entitlements, TestFlight and physical-device thermal testing
- Remaining authored character, environment, animation, VFX and audio polish tracked in the visual backlog
