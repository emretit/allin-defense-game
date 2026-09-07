using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    public enum GameLanguage
    {
        Turkish,
        English
    }

    public static class GameLocalization
    {
        private const string LanguageKey = "DefenderChronicles.Frontend.Language";

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
            ["GREENWATCH SEFERİ"] = "THE GREENWATCH CAMPAIGN",
            ["GREENWATCH  •  SINIR SEFERİ"] = "GREENWATCH  •  FRONTIER CAMPAIGN",
            ["Greenwatch Kalesi kuşatma altında.\nSavunma hattını kur. Şafağa kadar dayan."] = "Greenwatch Keep is under siege.\nBuild the defence line. Hold until dawn.",
            ["KRALLIĞA GİR  ›"] = "ENTER THE REALM  ›",
            ["HESABINI BAĞLA"] = "LINK YOUR ACCOUNT",
            ["Misafir ilerlemen bu cihazda saklanır; hesabını daha sonra bağlayabilirsin."] = "Guest progress is saved on this device; you can link an account later.",
            ["İLERLEMENİ KORU"] = "PROTECT YOUR PROGRESS",
            ["Misafir olarak hemen oynayabilir veya ilerlemeni cihazlar arasında taşımak için hesabını bağlayabilirsin."] = "Play as a guest now, or link your account to carry progress between devices.",
            ["MİSAFİR OLARAK BAŞLA"] = "START AS GUEST",
            ["GİZLİLİK POLİTİKASI"] = "PRIVACY POLICY",
            ["KULLANIM KOŞULLARI"] = "TERMS OF SERVICE",
            ["AYAR"] = "SETTINGS",
            ["SAVUNMA HATTI\nSENİ BEKLİYOR"] = "THE DEFENCE LINE\nAWAITS",
            ["DEVAM ET  ›"] = "CONTINUE  ›",
            ["KRALLIK HARİTASI"] = "REALM MAP",
            ["HESABI BAĞLA / DEĞİŞTİR"] = "LINK / SWITCH ACCOUNT",
            ["Greenwatch Bölgesi\n10 savunma noktası  ·  1 bölgesel lord"] = "Greenwatch Region\n10 defence points  ·  1 regional lord",
            ["KRALLIK SAVAŞ MASASI"] = "REALM WAR TABLE",
            ["BRİFİNGİ AÇ  ›"] = "OPEN BRIEFING  ›",
            ["SAVAŞ BRİFİNGİ"] = "BATTLE BRIEFING",
            ["Greenwatch ön hattı"] = "Greenwatch front line",
            ["KOMUTAN EMRİ\nDüşman yolunu oku, savunma noktalarını kur ve kaleyi ayakta tut."] = "COMMANDER'S ORDER\nRead the enemy route, build defence points, and keep the fortress standing.",
            ["SAVUNMAYI BAŞLAT"] = "START DEFENCE",
            ["SAVAŞ SONUCU"] = "BATTLE RESULT",
            ["YENİDEN DENE"] = "TRY AGAIN",
            ["SONRAKİ BÖLÜM  ›"] = "NEXT LEVEL  ›",
            ["HARİTAYA DÖN"] = "RETURN TO MAP",
            ["AYARLAR"] = "SETTINGS",
            ["DİL"] = "LANGUAGE",
            ["MÜZİK"] = "MUSIC",
            ["EFEKTLER"] = "SFX",
            ["TİTREŞİM"] = "HAPTICS",
            ["KAMERA SARSINTISI"] = "CAMERA SHAKE",
            ["AZALTILMIŞ HAREKET"] = "REDUCED MOTION",
            ["HESAP YÖNETİMİ"] = "ACCOUNT MANAGEMENT",
            ["KAHRAMANIM"] = "MY HERO",
            ["Savaşçı"] = "Warrior",
            ["KAYDET"] = "SAVE",
            ["Kahramanına bir ad ver. Savaşta bu adla görünecek."] = "Name your hero. This name will appear in battle.",
            ["En fazla 20 karakter. Boş bırakırsan Savaşçı olur."] = "Up to 20 characters. Leave blank to use Warrior.",
            ["TAMAM"] = "DONE",
            ["SAVAŞ ALANI HAZIRLANIYOR\nCİHAZ YATAYA ALINIYOR"] = "PREPARING THE BATTLEFIELD\nROTATING TO LANDSCAPE",
            ["KRALLIK HARİTASI HAZIRLANIYOR\nCİHAZ PORTREYE ALINIYOR"] = "PREPARING THE REALM MAP\nROTATING TO PORTRAIT",
            ["DURAKLAT"] = "PAUSE",
            ["DEVAM"] = "RESUME",
            ["OKÇU  75"] = "ARCHER  75",
            ["KIŞLA  100"] = "BARRACKS  100",
            ["BÜYÜ  125"] = "MAGE  125",
            ["SARSICI DARBE"] = "STUNNING STRIKE",
            ["İPTAL"] = "CANCEL",
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
            ["Greenwatch Muhafızı"] = "Greenwatch Defender"
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
        private string turkish;

        public void Configure(TMP_Text text, string source)
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
