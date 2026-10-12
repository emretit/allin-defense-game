using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public enum GameLanguage
    {
        Turkish,
        English
    }

    public static class GameLocalization
    {
        private const string LanguageKey = "AllinDefense.Frontend.Language";

        private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
        {
            ["Yeşil Nöbet"] = "Greenwatch",
            ["İlk Nöbet"] = "First Watch",
            ["Dar Patika"] = "Narrow Trail",
            ["Çifte Baskı"] = "Double Pressure",
            ["Eski Köprü"] = "Old Bridge",
            ["Sessiz Koruluk"] = "Silent Grove",
            ["Kırık Yol"] = "Broken Road",
            ["Gece Akını"] = "Night Raid",
            ["Taş Çember"] = "Stone Circle",
            ["Kapı Önü"] = "At the Gate",
            ["Yeşil Muhafız"] = "Green Guardian",
            ["Savunmanın temelleri"] = "Defence fundamentals",
            [FrontendCopy.CampaignEyebrow.Turkish] = FrontendCopy.CampaignEyebrow.English,
            [FrontendCopy.WelcomeEyebrow.Turkish] = FrontendCopy.WelcomeEyebrow.English,
            [FrontendCopy.WelcomeDescription.Turkish] = FrontendCopy.WelcomeDescription.English,
            ["KRALLIĞA GİR  ›"] = "ENTER THE REALM  ›",
            [FrontendCopy.LinkAccount.Turkish] = FrontendCopy.LinkAccount.English,
            [FrontendCopy.GuestNote.Turkish] = FrontendCopy.GuestNote.English,
            [FrontendCopy.ProtectProgress.Turkish] = FrontendCopy.ProtectProgress.English,
            [FrontendCopy.AccountDescription.Turkish] = FrontendCopy.AccountDescription.English,
            [FrontendCopy.StartAsGuest.Turkish] = FrontendCopy.StartAsGuest.English,
            [FrontendCopy.PrivacyPolicy.Turkish] = FrontendCopy.PrivacyPolicy.English,
            [FrontendCopy.TermsOfService.Turkish] = FrontendCopy.TermsOfService.English,
            [FrontendCopy.SettingsButton.Turkish] = FrontendCopy.SettingsButton.English,
            [FrontendCopy.MainMenuCallout.Turkish] = FrontendCopy.MainMenuCallout.English,
            ["DEVAM ET  ›"] = "CONTINUE  ›",
            [FrontendCopy.RealmMap.Turkish] = FrontendCopy.RealmMap.English,
            ["HESABI BAĞLA / DEĞİŞTİR"] = "LINK / SWITCH ACCOUNT",
            ["Greenwatch Bölgesi\n10 savunma noktası  ·  1 bölgesel lord"] = "Greenwatch Region\n10 defence points  ·  1 regional lord",
            ["KRALLIK SAVAŞ MASASI"] = "REALM WAR TABLE",
            ["BRİFİNGİ AÇ  ›"] = "OPEN BRIEFING  ›",
            ["SAVAŞ BRİFİNGİ"] = "BATTLE BRIEFING",
            ["Greenwatch ön hattı"] = "Greenwatch front line",
            ["KOMUTAN EMRİ\nDüşman yolunu oku, savunma noktalarını kur ve kaleyi ayakta tut."] = "COMMANDER'S ORDER\nRead the enemy route, build defence points, and keep the fortress standing.",
            [FrontendCopy.StartDefence.Turkish] = FrontendCopy.StartDefence.English,
            [FrontendCopy.BattleResult.Turkish] = FrontendCopy.BattleResult.English,
            [FrontendCopy.TryAgain.Turkish] = FrontendCopy.TryAgain.English,
            [FrontendCopy.NextLevel.Turkish] = FrontendCopy.NextLevel.English,
            [FrontendCopy.ReturnToMap.Turkish] = FrontendCopy.ReturnToMap.English,
            [FrontendCopy.SettingsTitle.Turkish] = FrontendCopy.SettingsTitle.English,
            ["DİL"] = "LANGUAGE",
            [FrontendCopy.Music.Turkish] = FrontendCopy.Music.English,
            [FrontendCopy.Effects.Turkish] = FrontendCopy.Effects.English,
            [FrontendCopy.Haptics.Turkish] = FrontendCopy.Haptics.English,
            [FrontendCopy.CameraShake.Turkish] = FrontendCopy.CameraShake.English,
            [FrontendCopy.ReducedMotion.Turkish] = FrontendCopy.ReducedMotion.English,
            [FrontendCopy.AccountManagement.Turkish] = FrontendCopy.AccountManagement.English,
            [FrontendCopy.MyHero.Turkish] = FrontendCopy.MyHero.English,
            [FrontendCopy.Warrior.Turkish] = FrontendCopy.Warrior.English,
            [FrontendCopy.Save.Turkish] = FrontendCopy.Save.English,
            [FrontendCopy.HeroNameDescription.Turkish] = FrontendCopy.HeroNameDescription.English,
            [FrontendCopy.HeroNameHint.Turkish] = FrontendCopy.HeroNameHint.English,
            [FrontendCopy.Done.Turkish] = FrontendCopy.Done.English,
            ["SAVAŞ ALANI HAZIRLANIYOR\nCİHAZ YATAYA ALINIYOR"] = "PREPARING THE BATTLEFIELD\nROTATING TO LANDSCAPE",
            ["KRALLIK HARİTASI HAZIRLANIYOR\nCİHAZ PORTREYE ALINIYOR"] = "PREPARING THE REALM MAP\nROTATING TO PORTRAIT",
            ["DURAKLAT"] = "PAUSE",
            ["DEVAM"] = "RESUME",
            ["OKÇU  75"] = "ARCHER  75",
            ["KIŞLA  100"] = "BARRACKS  100",
            ["BÜYÜ  125"] = "MAGE  125",
            ["SARSICI DARBE"] = "STUNNING STRIKE",
            [FrontendCopy.Cancel.Turkish] = FrontendCopy.Cancel.English,
            ["İLK DALGAYI BAŞLAT"] = "START FIRST WAVE",
            ["YÜKSELT"] = "UPGRADE",
            ["SAT"] = "SELL",
            ["Bir karakol platformuna dokun ve savunmanı kur."] = "Tap an outpost platform and build your defence.",
            ["Kale muhafızı son savunma hattını otomatik korur."] = "The keep guard automatically protects the final line.",
            ["Sarsıcı Darbe kalabalıkları kısa süre durdurur."] = "Stunning Strike briefly stuns groups of enemies.",
            ["Önce mavi bir karakol platformu seç."] = "Choose a blue build site first.",
            ["Yeterli altın yok."] = "Not enough gold.",
            ["Şövalye henüz savaşa dönemedi."] = "The knight is still recovering.",
            ["Misafir hesabı hazırlanıyor..."] = "Preparing your guest profile...",
            ["Güvenli hesap penceresi hazırlanıyor..."] = "Opening secure account sign-in...",
            ["İlerleme güvenli bulut kaydıyla eşitlendi."] = "Progress synced to the cloud.",
            ["Çevrimdışı ilerleme korunuyor; bağlantı gelince eşitlenecek."] = "Offline progress is saved and will sync when connected.",
            ["Çevrimiçi hesap henüz yapılandırılmadı. Misafir olarak devam edebilirsin."] = "Account sign-in is unavailable. You can continue as a guest.",
            ["Çevrimiçi hesap bağlantısı hazırlanıyor. Şimdilik misafir olarak devam edebilirsin."] = "Account sign-in is being prepared. You can continue as a guest.",
            ["PLATFORM SEÇİLMEDİ"] = "NO PLATFORM SELECTED",
            ["ZAFER\nGREENWATCH KORUNDU"] = "VICTORY\nGREENWATCH DEFENDED",
            ["YENİLGİ\nGREENWATCH DÜŞTÜ"] = "DEFEAT\nGREENWATCH HAS FALLEN",
            ["AÇIK"] = "READY",
            ["KİLİTLİ"] = "LOCKED",
            ["Bağlı hesap"] = "Linked account",
            ["Misafir Muhafız"] = "Guest Defender",
            ["Greenwatch Muhafızı"] = "Greenwatch Defender",
            ["Hızlı yağmacılar"] = "Fast raiders",
            ["Zırhlı öncüler"] = "Armored vanguards",
            ["Yenilenen muhafızlar"] = "Regenerating guards",
            ["Şifacı destek birliği"] = "Healer support unit",
            ["Kuşatma birliği"] = "Siege unit",
            ["Bölge lordu ve seçkin muhafızları"] = "Regional boss and elite guards",
            ["Yağmacılar + zırhlı öncü"] = "Raiders + armored vanguard",
            ["İki koldan karma baskı"] = "Mixed assault from two routes",
            ["Şifacı desteği"] = "Healer support",
            ["Kızıl Muhafız - üç safhalı boss"] = "Crimson Guard - three-phase boss"
        };

        public static event Action LanguageChanged;

        public static GameLanguage CurrentLanguage
        {
            get
            {
                int fallback = Application.systemLanguage == SystemLanguage.Turkish ? 0 : 1;
                return PlayerPrefs.GetInt(LanguageKey, fallback) == 0 ? GameLanguage.Turkish : GameLanguage.English;
            }
        }

        public static bool IsEnglish => CurrentLanguage == GameLanguage.English;

        public static string Text(string turkish, string english) => IsEnglish ? english : turkish;

        public static string Translate(string turkish)
        {
            if (!IsEnglish || string.IsNullOrEmpty(turkish)) return turkish;
            return English.TryGetValue(turkish, out string english) ? english : turkish;
        }

        // Compatibility for campaign data and legacy HUD/status callers only.
        // Frontend labels use explicit FrontendCopy IDs instead.
        internal static string EnglishText(string turkish) =>
            !string.IsNullOrEmpty(turkish) && English.TryGetValue(turkish, out string english) ? english : turkish;

        public static bool HasTranslation(string turkish) =>
            !string.IsNullOrEmpty(turkish) && English.ContainsKey(turkish);

        public static void SetLanguage(GameLanguage language)
        {
            if (CurrentLanguage == language) return;
            PlayerPrefs.SetInt(LanguageKey, language == GameLanguage.Turkish ? 0 : 1);
            PlayerPrefs.Save();
            ApplyUnityLocale(language);
            LanguageChanged?.Invoke();
        }

        public static void ApplyStoredUnityLocale() => ApplyUnityLocale(CurrentLanguage);

        private static void ApplyUnityLocale(GameLanguage language)
        {
            try
            {
                string code = language == GameLanguage.Turkish ? "tr" : "en";
                Locale locale = LocalizationSettings.AvailableLocales?.GetLocale(new LocaleIdentifier(code));
                if (locale != null) LocalizationSettings.SelectedLocale = locale;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Unity locale could not be selected yet: {exception.Message}");
            }
        }
    }

    public sealed class LocalizedTmpText : MonoBehaviour
    {
        private TMP_Text target;
        private LocalizedCopy copy;

        public void Configure(TMP_Text text, string source) =>
            Configure(text, new LocalizedCopy(source, GameLocalization.EnglishText(source)));

        public void Configure(TMP_Text text, LocalizedCopy value)
        {
            target = text;
            copy = value;
            Apply();
        }

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += Apply;
            Apply();
        }

        private void OnDisable() => GameLocalization.LanguageChanged -= Apply;

        private void Apply()
        {
            if (target != null) target.text = copy.Value;
        }
    }

    public sealed class LocalizedLegacyText : MonoBehaviour
    {
        private Text target;
        private string turkish;

        public void Configure(Text text, string source)
        {
            target = text;
            turkish = source ?? string.Empty;
            Apply();
        }

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += Apply;
            Apply();
        }

        private void OnDisable() => GameLocalization.LanguageChanged -= Apply;

        private void Apply()
        {
            if (target != null) target.text = GameLocalization.Translate(turkish);
        }
    }
}
