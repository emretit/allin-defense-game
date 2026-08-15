using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DefenderChronicles.Grid;
using DefenderChronicles.Towers;

namespace DefenderChronicles.Heroes
{
    public class HeroCommandController : MonoBehaviour
    {
        [SerializeField] private HeroBase hero;

        private void Awake()
        {
            if (hero == null)
                hero = FindAnyObjectByType<HeroBase>();
        }

        private void Update()
        {
            if (!GetPrimaryPointerDownThisFrame())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (TowerPlacer.Instance != null && TowerPlacer.Instance.IsPlacingTower)
                return;

            if (hero == null || !hero.IsAlive)
                hero = FindAnyObjectByType<HeroBase>();

            if (hero == null)
                return;

            Vector3 world = GetPointerWorldPosition();
            if (CommandPointManager.Instance != null &&
                CommandPointManager.Instance.TryGetNearestPoint(world, out var point))
            {
                hero.CommandTo(point);
            }
        }

        private static Vector3 GetPointerWorldPosition()
        {
            var cam = Camera.main;
            if (cam == null)
                return Vector3.zero;

            Vector2 screen;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null)
                screen = Mouse.current.position.ReadValue();
            else
                screen = Vector2.zero;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, cam.nearClipPlane + 1f));
            world.z = 0f;
            return world;
        }

        private static bool GetPrimaryPointerDownThisFrame()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }
    }
}
