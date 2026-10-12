using UnityEngine;

namespace AllinDefense.UI
{
    [DisallowMultipleComponent]
    public sealed class UiId : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Id => id;

        public static void Assign(Transform target, string value)
        {
            var identity = target.GetComponent<UiId>();
            if (identity == null) identity = target.gameObject.AddComponent<UiId>();
            identity.id = value;
        }

        public static void AssignHierarchy(Transform root)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.GetComponent<UiId>() == null) Assign(rect, rect.name);
        }
    }
}
