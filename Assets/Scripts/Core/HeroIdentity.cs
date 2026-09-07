using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DefenderChronicles.UI;

namespace DefenderChronicles.Core
{
    public static class HeroIdentity
    {
        public const int MaxNameLength = 20;
        public static string DefaultName => GameLocalization.Text("Savaşçı", "Warrior");
        public static string DisplayName(string customName) =>
            string.IsNullOrWhiteSpace(customName) ? DefaultName : customName;

        public static bool TryNormalize(string value, out string name)
        {
            name = string.Empty;
            try { name = (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormC); }
            catch (ArgumentException) { return false; }
            return new StringInfo(name).LengthInTextElements <= MaxNameLength &&
                   !name.Any(c => char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format || c is '<' or '>');
        }
    }
}
