using System.Collections;
using UnityEngine;
using TMPro;

namespace DefenderChronicles.UI
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float fadeTime = 0.8f;

        public static FloatingText Create(Vector3 position, string text, Color color, float fontSize = 2f)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = position;
            var ft = go.AddComponent<FloatingText>();
            ft.textMesh = go.AddComponent<TextMeshPro>();
            ft.textMesh.text = text;
            ft.textMesh.color = color;
            ft.textMesh.fontSize = fontSize;
            ft.textMesh.alignment = TextAlignmentOptions.Center;
            ft.textMesh.sortingOrder = 10;
            ft.StartCoroutine(ft.FloatAndFade());
            return ft;
        }

        private IEnumerator FloatAndFade()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Color startColor = textMesh.color;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                transform.position = startPos + Vector3.up * floatSpeed * t;
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
