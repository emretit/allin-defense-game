namespace DefenderChronicles.UI
{
    /// <summary>A bilingual label bound explicitly, independent of its Turkish wording.</summary>
    public readonly struct LocalizedCopy
    {
        public string Turkish { get; }
        public string English { get; }
        public bool IsLocalized { get; }
        public string Value => GameLocalization.Text(Turkish, English);

        public LocalizedCopy(string turkish, string english)
        {
            Turkish = turkish ?? string.Empty;
            English = english ?? string.Empty;
            IsLocalized = true;
        }

        private LocalizedCopy(string literal)
        {
            Turkish = English = literal ?? string.Empty;
            IsLocalized = false;
        }

        // Empty, numeric and dynamically refreshed strings remain literal copy.
        public static implicit operator LocalizedCopy(string literal) => new(literal);
    }

    /// <summary>
    /// Stable frontend copy IDs. Change either language here without changing call sites
    /// or test-visible GameObject names; widgets never infer an ID from displayed text.
    /// </summary>
    public static class FrontendCopy
    {
        public static readonly LocalizedCopy CampaignEyebrow = new("GREENWATCH SEFERİ", "THE GREENWATCH CAMPAIGN");
        public static readonly LocalizedCopy BattleResult = new("SAVAŞ SONUCU", "BATTLE RESULT");
        public static readonly LocalizedCopy TryAgain = new("YENİDEN DENE", "TRY AGAIN");
        public static readonly LocalizedCopy NextLevel = new("SONRAKİ BÖLÜM  ›", "NEXT LEVEL  ›");
        public static readonly LocalizedCopy ReturnToMap = new("HARİTAYA DÖN", "RETURN TO MAP");
        public static readonly LocalizedCopy MyHero = new("KAHRAMANIM", "MY HERO");
        public static readonly LocalizedCopy HeroNameDescription = new("Kahramanına bir ad ver. Savaşta bu adla görünecek.", "Name your hero. This name will appear in battle.");
        public static readonly LocalizedCopy Warrior = new("Savaşçı", "Warrior");
        public static readonly LocalizedCopy HeroNameHint = new("En fazla 20 karakter. Boş bırakırsan Savaşçı olur.", "Up to 20 characters. Leave blank to use Warrior.");
        public static readonly LocalizedCopy Cancel = new("İPTAL", "CANCEL");
        public static readonly LocalizedCopy Save = new("KAYDET", "SAVE");
        public static readonly LocalizedCopy SettingsButton = new("AYAR", "SETTINGS");
        public static readonly LocalizedCopy MainMenuCallout = new("SAVUNMA HATTI\nSENİ BEKLİYOR", "THE DEFENCE LINE\nAWAITS");
        public static readonly LocalizedCopy RealmMap = new("KRALLIK HARİTASI", "REALM MAP");
        public static readonly LocalizedCopy StartDefence = new("SAVUNMAYI BAŞLAT", "START DEFENCE");
        public static readonly LocalizedCopy WelcomeEyebrow = new("GREENWATCH  •  SINIR SEFERİ", "GREENWATCH  •  FRONTIER CAMPAIGN");
        public static readonly LocalizedCopy WelcomeDescription = new("Greenwatch Kalesi kuşatma altında.\nSavunma hattını kur. Şafağa kadar dayan.", "Greenwatch Keep is under siege.\nBuild the defence line. Hold until dawn.");
        public static readonly LocalizedCopy LinkAccount = new("HESABINI BAĞLA", "LINK YOUR ACCOUNT");
        public static readonly LocalizedCopy GuestNote = new("Misafir ilerlemen bu cihazda saklanır; hesabını daha sonra bağlayabilirsin.", "Guest progress is saved on this device; you can link an account later.");
        public static readonly LocalizedCopy ProtectProgress = new("İLERLEMENİ KORU", "PROTECT YOUR PROGRESS");
        public static readonly LocalizedCopy AccountDescription = new("Misafir olarak hemen oynayabilir veya ilerlemeni cihazlar arasında taşımak için hesabını bağlayabilirsin.", "Play as a guest now, or link your account to carry progress between devices.");
        public static readonly LocalizedCopy StartAsGuest = new("MİSAFİR OLARAK BAŞLA", "START AS GUEST");
        public static readonly LocalizedCopy PrivacyPolicy = new("GİZLİLİK POLİTİKASI", "PRIVACY POLICY");
        public static readonly LocalizedCopy TermsOfService = new("KULLANIM KOŞULLARI", "TERMS OF SERVICE");
        public static readonly LocalizedCopy SettingsTitle = new("AYARLAR", "SETTINGS");
        public static readonly LocalizedCopy Music = new("MÜZİK", "MUSIC");
        public static readonly LocalizedCopy Effects = new("EFEKTLER", "SFX");
        public static readonly LocalizedCopy Haptics = new("TİTREŞİM", "HAPTICS");
        public static readonly LocalizedCopy CameraShake = new("KAMERA SARSINTISI", "CAMERA SHAKE");
        public static readonly LocalizedCopy ReducedMotion = new("AZALTILMIŞ HAREKET", "REDUCED MOTION");
        public static readonly LocalizedCopy AccountManagement = new("HESAP YÖNETİMİ", "ACCOUNT MANAGEMENT");
        public static readonly LocalizedCopy Done = new("TAMAM", "DONE");
    }
}
