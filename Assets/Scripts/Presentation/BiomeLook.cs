using AllinDefense.Data;
using UnityEngine;

namespace AllinDefense.Presentation
{
    public enum BiomeSilhouette { Forest, AshBranches, FrostPines, SunkenColumns, CitadelBanners }

    public sealed class BiomeLook
    {
        public int Index { get; }
        public BiomeSilhouette Silhouette { get; }
        public Color GroundColor { get; }
        public Color CrownColor { get; }
        public Color CrownMidColor { get; }
        public Color CrownLightColor { get; }
        public Color TrunkColor { get; }
        public Color RockColor { get; }
        public Color UndergrowthColor { get; }
        public Color MossColor { get; }
        public Color AccentColor { get; }
        public Color DecorativeAccentColor { get; }
        public Color KeyLightColor { get; }
        public Color FogColor { get; }
        public Color AmbientGroundColor { get; }
        public float LightBlend { get; }
        public float EquatorBlend { get; }
        public float FogBlend { get; }
        public Color GroundTextureCenter => Index == 0 ? new Color(.79f, .87f, .86f) : new Color(.90f, .91f, .91f);
        public Color GroundTextureEdge => Index == 0 ? new Color(.29f, .39f, .48f) : new Color(.43f, .46f, .49f);

        private static readonly BiomeLook[] Profiles =
        {
            new(0, BiomeSilhouette.Forest, new(.21f, .25f, .25f),
                new(.26f, .43f, .36f), new(.36f, .48f, .34f), new(.40f, .56f, .50f),
                new(.32f, .29f, .24f), new(.44f, .54f, .57f), new(.31f, .46f, .36f),
                new(.46f, .91f, .65f), new(.55f, .66f, .36f), .08f, .12f, .18f),
            new(1, BiomeSilhouette.AshBranches, new(.32f, .29f, .28f),
                new(.31f, .25f, .22f), new(.40f, .31f, .25f), new(.52f, .38f, .27f),
                new(.23f, .21f, .20f), new(.24f, .26f, .28f), new(.48f, .34f, .23f),
                new(.68f, .48f, .32f), new(.85f, .43f, .19f), .14f, .18f, .32f),
            new(2, BiomeSilhouette.FrostPines, new(.25f, .32f, .37f),
                new(.24f, .42f, .49f), new(.40f, .64f, .72f), new(.72f, .85f, .88f),
                new(.32f, .38f, .42f), new(.56f, .80f, .86f), new(.60f, .74f, .78f),
                new(.42f, .54f, .60f), new(.35f, .82f, 1f), .08f, .12f, .18f),
            new(3, BiomeSilhouette.SunkenColumns, new(.12f, .27f, .26f),
                new(.37f, .51f, .48f), new(.51f, .63f, .58f), new(.64f, .68f, .55f),
                new(.29f, .41f, .39f), new(.28f, .45f, .43f), new(.19f, .47f, .36f),
                new(.30f, .74f, .58f), new(.68f, .61f, .38f), .13f, .18f, .34f),
            new(4, BiomeSilhouette.CitadelBanners, new(.29f, .18f, .20f),
                new(.70f, .62f, .48f), new(.80f, .73f, .59f), new(.90f, .84f, .71f),
                new(.32f, .24f, .23f), new(.57f, .52f, .44f), new(.40f, .28f, .26f),
                new(.61f, .40f, .35f), new(.88f, .64f, .24f), .14f, .18f, .32f)
        };

        private BiomeLook(int index, BiomeSilhouette silhouette, Color ground, Color crown,
            Color crownMid, Color crownLight, Color trunk, Color rock, Color undergrowth,
            Color moss, Color decorativeAccent, float lightBlend, float equatorBlend, float fogBlend)
        {
            BiomeDefinition palette = CampaignLevelFactory.Biome(index);
            Index = index;
            Silhouette = silhouette;
            GroundColor = ground;
            CrownColor = crown;
            CrownMidColor = crownMid;
            CrownLightColor = crownLight;
            TrunkColor = trunk;
            RockColor = rock;
            UndergrowthColor = undergrowth;
            MossColor = moss;
            AccentColor = palette.accentColor;
            DecorativeAccentColor = decorativeAccent;
            KeyLightColor = palette.keyLightColor;
            FogColor = palette.fogColor;
            AmbientGroundColor = palette.groundColor;
            LightBlend = lightBlend;
            EquatorBlend = equatorBlend;
            FogBlend = fogBlend;
        }

        public static BiomeLook ForRegion(int index) => Profiles[Mathf.Clamp(index, 0, Profiles.Length - 1)];

        public Color[] SceneryPalette() => new[]
        {
            CrownColor, CrownMidColor, CrownLightColor, TrunkColor, RockColor,
            UndergrowthColor, DecorativeAccentColor, Color.white
        };
    }
}
