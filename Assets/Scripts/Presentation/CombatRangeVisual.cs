using UnityEngine;
using UnityEngine.EventSystems;

namespace DefenderChronicles.Presentation
{
    public sealed class CombatRangeVisual : MonoBehaviour
    {
        private LineRenderer ring;
        private static Material lineMaterial;
        public static CombatRangeVisual Create(Transform parent, string label, Color color)
        {
            var go = new GameObject(label); go.transform.SetParent(parent, false);
            var visual = go.AddComponent<CombatRangeVisual>();
            visual.ring = go.AddComponent<LineRenderer>();
            if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));
            visual.ring.sharedMaterial = lineMaterial;
            visual.ring.useWorldSpace = true; visual.ring.loop = true; visual.ring.positionCount = 48;
            visual.ring.startWidth = visual.ring.endWidth = .085f;
            visual.ring.startColor = visual.ring.endColor = color;
            visual.ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return visual;
        }
        public void Show(Vector3 center, float radius, bool visible)
        {
            ring.enabled = visible;
            if (!visible) return;
            center.y = .24f;
            for (int i=0;i<48;i++)
            {
                float angle=i*Mathf.PI*2/48;
                ring.SetPosition(i,center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
            }
        }
    }
    public sealed class AbilityRangePreview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private CombatRangeVisual ring;
        public void OnPointerDown(PointerEventData data)
        {
            var session = Core.GameSessionController.Instance;
            if (session?.Hero == null) return;
            if (ring == null) ring = CombatRangeVisual.Create(session.transform,"Shield Bash preview",new Color(.3f,.8f,1));
            ring.gameObject.SetActive(true);
            ring.Show(session.Hero.Position,3.2f,true);
        }
        public void OnPointerUp(PointerEventData data) { if (ring != null) ring.gameObject.SetActive(false); }
        public void OnPointerExit(PointerEventData data) => OnPointerUp(data);
        private void OnDisable() { if (ring != null) ring.gameObject.SetActive(false); }
    }
}
