using System;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.Presentation
{
    public sealed class DefenderHudController : MonoBehaviour
    {
        private UiCopyCatalog copy;
        private TMP_Text goldText;
        private TMP_Text livesText;
        private TMP_Text waveText;
        private TMP_Text phaseText;
        private TMP_Text previewText;
        private TMP_Text messageText;
        private TMP_Text heroText;
        private TMP_Text selectedText;
        private TMP_Text resultTitle;
        private TMP_Text nextWaveLabel;
        private TMP_Text pauseLabel;
        private Button archerButton;
        private Button barracksButton;
        private Button mageButton;
        private Button abilityButton;
        private Button cancelButton;
        private Button nextWaveButton;
        private Button pauseButton;
        private Button upgradeButton;
        private Button sellButton;
        private Button restartButton;
        private GameObject selectedPanel;
        private GameObject resultPanel;

        public void Initialize(DefenderPresentationCatalog catalog, Action<OutpostKind> build, Action ability, Action cancel,
            Action nextWave, Action pause, Action upgrade, Action sell, Action restart, Action uiClick)
        {
            copy = catalog.uiCopy;
            CacheReferences();
            Bind(archerButton, () => build(OutpostKind.Archer), uiClick);
            Bind(barracksButton, () => build(OutpostKind.Barracks), uiClick);
            Bind(mageButton, () => build(OutpostKind.Mage), uiClick);
            Bind(abilityButton, ability, uiClick);
            Bind(cancelButton, cancel, uiClick);
            Bind(nextWaveButton, nextWave, uiClick);
            Bind(pauseButton, pause, uiClick);
            Bind(upgradeButton, upgrade, uiClick);
            Bind(sellButton, sell, uiClick);
            Bind(restartButton, restart, uiClick);
            AddTextIcon(goldText, catalog.goldIcon);
            AddTextIcon(livesText, catalog.keepIcon);
            AddTextIcon(waveText, catalog.waveIcon);
            AddButtonIcon(archerButton, catalog.archerIcon);
            AddButtonIcon(barracksButton, catalog.barracksIcon);
            AddButtonIcon(mageButton, catalog.mageIcon);
            AddButtonIcon(abilityButton, catalog.abilityIcon);
            ApplyStaticCopy();
            selectedPanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        public void Refresh(GameSessionController session)
        {
            if (session == null || goldText == null) return;
            goldText.text = $"{copy.Get(UiTextKey.Gold)}  {session.Gold}";
            livesText.text = $"{copy.Get(UiTextKey.Keep)}  {session.Lives}";
            livesText.color = session.Lives <= 5 ? new Color(1f, 0.36f, 0.22f) : Color.white;
            int displayedWave = Mathf.Min(session.CurrentWaveIndex + (session.Phase == GamePhase.Wave ? 1 : 0), session.WaveCount);
            waveText.text = $"{copy.Get(UiTextKey.Wave)}  {displayedWave} / {session.WaveCount}";
            phaseText.text = copy.Get(session.Phase switch
            {
                GamePhase.Preparing => UiTextKey.Preparing,
                GamePhase.Wave => UiTextKey.Combat,
                GamePhase.Intermission => UiTextKey.Intermission,
                GamePhase.Paused => UiTextKey.Paused,
                GamePhase.Victory => UiTextKey.VictoryPhase,
                GamePhase.Defeat => UiTextKey.DefeatPhase,
                _ => UiTextKey.Loading
            });
            pauseLabel.text = session.Phase == GamePhase.Paused ? copy.Get(UiTextKey.Continue) : copy.Get(UiTextKey.Pause);
            bool canCallWave = session.Phase is GamePhase.Preparing or GamePhase.Intermission;
            nextWaveButton.gameObject.SetActive(canCallWave);
            nextWaveButton.interactable = canCallWave;

            GreyboxHero hero = session.Hero;
            if (hero != null)
            {
                heroText.text = hero.IsAlive
                    ? $"{copy.Get(UiTextKey.Knight)}  {Mathf.CeilToInt(hero.HealthFraction * 100f)}%  •  " +
                      (hero.AbilityRemaining <= 0f ? copy.Get(UiTextKey.AbilityReady) : $"{Mathf.CeilToInt(hero.AbilityRemaining)} sn")
                    : $"{copy.Get(UiTextKey.KnightReturning)}  {Mathf.CeilToInt(hero.RespawnRemaining)} sn";
                abilityButton.interactable = hero.IsAlive && hero.AbilityRemaining <= 0f && session.Phase != GamePhase.Paused;
            }
        }

        public void SetPreview(string value) => previewText.text = value ?? string.Empty;
        public void SetMessage(string value) => messageText.text = value ?? string.Empty;
        public void SetNextWaveLabel(string value) => nextWaveLabel.text = value ?? string.Empty;

        public void SetSelected(string value, bool canUpgrade, bool canSell, bool visible)
        {
            selectedText.text = value ?? string.Empty;
            upgradeButton.interactable = canUpgrade;
            sellButton.interactable = canSell;
            selectedPanel.SetActive(visible);
        }

        public void ShowResult(bool victory)
        {
            resultPanel.SetActive(true);
            resultTitle.text = copy.Get(victory ? UiTextKey.Victory : UiTextKey.Defeat);
            resultTitle.color = victory ? new Color(1f, 0.74f, 0.18f) : new Color(1f, 0.32f, 0.22f);
        }

        public void SetBuildInteractable(bool archer, bool barracks, bool mage)
        {
            archerButton.interactable = archer;
            barracksButton.interactable = barracks;
            mageButton.interactable = mage;
        }

        private void ApplyStaticCopy()
        {
            SetButtonLabel(archerButton, $"{copy.Get(UiTextKey.Archer)}\n75");
            SetButtonLabel(barracksButton, $"{copy.Get(UiTextKey.Barracks)}\n100");
            SetButtonLabel(mageButton, $"{copy.Get(UiTextKey.Mage)}\n125");
            SetButtonLabel(abilityButton, copy.Get(UiTextKey.ShieldBash));
            SetButtonLabel(cancelButton, copy.Get(UiTextKey.Cancel));
            SetButtonLabel(upgradeButton, copy.Get(UiTextKey.Upgrade));
            SetButtonLabel(sellButton, copy.Get(UiTextKey.Sell));
            SetButtonLabel(restartButton, copy.Get(UiTextKey.Restart));
            nextWaveLabel.text = copy.Get(UiTextKey.FirstWave);
            pauseLabel.text = copy.Get(UiTextKey.Pause);
        }

        private void CacheReferences()
        {
            goldText = Find<TMP_Text>("TopBar/Gold");
            livesText = Find<TMP_Text>("TopBar/Lives");
            waveText = Find<TMP_Text>("TopBar/Wave");
            phaseText = Find<TMP_Text>("TopBar/Phase");
            pauseButton = Find<Button>("TopBar/Pause");
            pauseLabel = Find<TMP_Text>("TopBar/Pause/Label");
            previewText = Find<TMP_Text>("Preview");
            messageText = Find<TMP_Text>("Message");
            archerButton = Find<Button>("BottomBar/Archer");
            barracksButton = Find<Button>("BottomBar/Barracks");
            mageButton = Find<Button>("BottomBar/Mage");
            abilityButton = Find<Button>("BottomBar/Ability");
            cancelButton = Find<Button>("BottomBar/Cancel");
            heroText = Find<TMP_Text>("BottomBar/Hero");
            nextWaveButton = Find<Button>("BottomBar/NextWave");
            nextWaveLabel = Find<TMP_Text>("BottomBar/NextWave/Label");
            selectedPanel = transform.Find("SelectedPanel").gameObject;
            selectedText = Find<TMP_Text>("SelectedPanel/Description");
            upgradeButton = Find<Button>("SelectedPanel/Upgrade");
            sellButton = Find<Button>("SelectedPanel/Sell");
            resultPanel = transform.Find("ResultPanel").gameObject;
            resultTitle = Find<TMP_Text>("ResultPanel/Title");
            restartButton = Find<Button>("ResultPanel/Restart");
        }

        private T Find<T>(string path) where T : Component
        {
            Transform item = transform.Find(path);
            if (item == null || !item.TryGetComponent(out T component))
                throw new InvalidOperationException($"HUD prefab is missing {typeof(T).Name} at {path}.");
            return component;
        }

        private static void Bind(Button button, Action action, Action click)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                click?.Invoke();
                action?.Invoke();
            });
        }

        private static void SetButtonLabel(Button button, string value)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = value;
        }

        private static void AddTextIcon(TMP_Text text, Sprite sprite)
        {
            if (text == null || sprite == null || text.transform.Find("Icon") != null) return;
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(text.transform, false);
            var rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(2f, 0f);
            rect.sizeDelta = new Vector2(34f, 34f);
            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            text.margin = new Vector4(text.margin.x + 42f, text.margin.y, text.margin.z, text.margin.w);
        }

        private static void AddButtonIcon(Button button, Sprite sprite)
        {
            if (button == null || sprite == null || button.transform.Find("Icon") != null) return;
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            var rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);
            rect.sizeDelta = new Vector2(42f, 42f);
            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.offsetMin = new Vector2(Mathf.Max(labelRect.offsetMin.x, 48f), labelRect.offsetMin.y);
            }
        }
    }
}
