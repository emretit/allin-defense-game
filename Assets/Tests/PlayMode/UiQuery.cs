using System.Linq;
using AllinDefense.UI;
using UnityEngine;

namespace AllinDefense.Tests
{
    internal static class UiQuery
    {
        public static Transform FindById(Component root, string id) => FindById(root.transform, id);

        public static Transform FindById(Transform root, string id)
        {
            foreach (string segment in id.Split('/'))
            {
                root = root.GetComponentsInChildren<UiId>(true).FirstOrDefault(item => item.Id == segment)?.transform;
                if (root == null) return null;
            }
            return root;
        }

        public static T FindById<T>(Component root, string id) where T : Component =>
            FindById(root, id)?.GetComponent<T>();
    }
}
