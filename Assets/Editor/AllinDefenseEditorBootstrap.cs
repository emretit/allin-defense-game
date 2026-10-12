#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AllinDefense.Editor
{
    /// <summary>
    /// Proje açıldığında TMP Essential Resources yoksa import menüsünü bir kez dener.
    /// Menü yolu Unity sürümüne göre değişebilir; olmazsa menü: ALLIN: Defense → Import TMP Essential Resources.
    /// </summary>
    [InitializeOnLoad]
    public static class AllinDefenseEditorBootstrap
    {
        private static bool _attemptedImportThisDomain;

        static AllinDefenseEditorBootstrap()
        {
            EditorApplication.delayCall += TryImportTmpEssentialsOnce;
        }

        private static bool TmpSettingsExists()
        {
            return AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null;
        }

        private static void TryImportTmpEssentialsOnce()
        {
            if (TmpSettingsExists())
                return;
            if (_attemptedImportThisDomain)
                return;
            _attemptedImportThisDomain = true;

            try
            {
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            }
            catch
            {
                try
                {
                    EditorApplication.ExecuteMenuItem("Window/Text Mesh Pro/Import TMP Essential Resources");
                }
                catch
                {
                    Debug.LogWarning(
                        "[ALLIN: Defense] TMP Essential Resources bulunamadı. Menü: ALLIN: Defense → Import TMP Essential Resources veya Window → TextMeshPro.");
                }
            }
        }

        [MenuItem("ALLIN: Defense/Import TMP Essential Resources")]
        private static void MenuImportTmp()
        {
            try
            {
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            }
            catch
            {
                EditorApplication.ExecuteMenuItem("Window/Text Mesh Pro/Import TMP Essential Resources");
            }
        }
    }
}
#endif
