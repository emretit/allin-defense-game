using System;
using System.Collections;
using System.Threading.Tasks;
using AllinDefense.Core;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public sealed partial class AppFlowController
    {
        public Func<bool, Task> AccountDeletion { get; set; }
        private GameObject deleteAccountModal;
        private bool deletingAccount;
        private TMP_Text deletionWelcomeStatus;

        private Button AccountDeleteButton(Transform parent, string id, LocalizedCopy copy, Action action)
        {
            var button = menuKit.Button(parent, id, copy, UiButtonVariant.Danger, clicked: () => MenuClick(action));
            button.GetComponentInChildren<TMP_Text>().fontSize = 30;
            button.GetComponent<UiAccessibleButton>().Configure(copy);
            UiKit.Size((RectTransform)button.transform, height: TouchHeight, flex: 1);
            return button;
        }

        private void ConfirmAccountDeletion()
        {
            if (deletingAccount || deleteAccountModal != null) return;
            deleteAccountModal = UiKit.Node(canvas.transform, "Confirm Account Deletion").gameObject;
            UiKit.Stretch((RectTransform)deleteAccountModal.transform);
            var scrim = CreateImage("Delete Account Scrim", deleteAccountModal.transform, UiTokens.HeroNameScrim);
            Stretch((RectTransform)scrim.transform);
            var card = menuKit.Card(deleteAccountModal.transform, "Delete Account Chronicle");
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            deleteAccountModal.AddComponent<FrontendResponsiveLayout>().Configure(size =>
                SetRect(card, Vector2.zero, new Vector2(Mathf.Min(size.x - 88, 1000), card.sizeDelta.y), Center(), Center()));
            MenuText(card, "Delete Account Heading", FrontendCopy.DeleteAccountTitle, 40, true);
            var hint = MenuText(card, "Delete Account Explanation", FrontendCopy.DeleteAccountHint, 30);
            hint.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(hint.rectTransform, height: 180);
            var cancel = MenuButton(card, "Cancel Account Deletion", FrontendCopy.DeleteAccountCancel, null, CloseAccountDeletion, true);
            cancel.GetComponent<UiAccessibleButton>().Configure(FrontendCopy.DeleteAccountCancel);
            AccountDeleteButton(card, "Confirm Account Deletion Button", FrontendCopy.DeleteAccountConfirm, () => _ = DeleteAccountConfirmedAsync());
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cancel.gameObject);
            foreach (var button in screens[AppScreen.Login].GetComponentsInChildren<Button>(true)) button.interactable = false;
        }

        private void CloseAccountDeletion()
        {
            if (deletingAccount) return;
            if (deleteAccountModal != null) { deleteAccountModal.SetActive(false); Destroy(deleteAccountModal); }
            deleteAccountModal = null;
            foreach (var button in screens[AppScreen.Login].GetComponentsInChildren<Button>(true)) button.interactable = true;
        }

        private IEnumerator ClearAccountDeletedStatus()
        {
            yield return new WaitForSecondsRealtime(4f);
            if (deletionWelcomeStatus != null) deletionWelcomeStatus.text = string.Empty;
        }

        private async Task DeleteAccountConfirmedAsync()
        {
            if (deletingAccount) return;
            deletingAccount = true;
            foreach (var button in deleteAccountModal.GetComponentsInChildren<Button>()) button.interactable = false;
            loginStatus.text = FrontendCopy.DeletingAccount.Value;
            deleteAccountModal.GetComponentInChildren<TMP_Text>().text = FrontendCopy.DeletingAccount.Value;
            bool localOnly = progress.Data.playerId == "local-guest" || string.IsNullOrWhiteSpace(Application.cloudProjectId);
            try
            {
                if (AccountDeletion != null) await AccountDeletion(localOnly);
                else if (!localOnly) throw new InvalidOperationException("Account deletion service is unavailable.");
            }
            catch (Exception error)
            {
                Debug.LogWarning("Account deletion failed: " + error.Message);
                deletingAccount = false;
                CloseAccountDeletion();
                loginStatus.text = FrontendCopy.AccountDeleteFailed.Value;
                return;
            }
            progress.DeleteLocalAccount();
            PlayerProgress.Reset();
            navigationState = new FrontendNavigationState();
            selectedLevel = knownUnlockedLevel = 1;
            pendingLevel = 0;
            pendingNavigation = null;
            deletingAccount = false;
            CloseAccountDeletion();
            ShowScreen(AppScreen.Welcome);
            deletionWelcomeStatus.text = FrontendCopy.AccountDeleted.Value;
            StartCoroutine(ClearAccountDeletedStatus());
        }
    }
}
