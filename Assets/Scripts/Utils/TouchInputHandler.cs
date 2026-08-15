using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using DefenderChronicles.Towers;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using InputTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DefenderChronicles.Utils
{
    // Mobile touch input handler - supports tap, drag, pinch-to-zoom
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 10f;
        [SerializeField] private float zoomSpeed = 0.05f;

        [Header("Input")]
        [Tooltip("Scales mouse wheel delta (Input System) to match previous zoom feel.")]
        [SerializeField] private float mouseScrollZoomScale = 0.01f;

        [Header("Events")]
        public UnityEvent<Vector3> OnTap;
        public UnityEvent<Vector3> OnLongPress;

        private float longPressTime = 0.5f;
        private float touchHoldTime = 0f;
        private bool isLongPress = false;

        private void Awake()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (TowerPlacer.Instance != null && TowerPlacer.Instance.IsPlacingTower)
            {
                if (gameCamera == null) return;
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    float scrollY = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scrollY) > 0.01f)
                        ZoomCamera(-scrollY * mouseScrollZoomScale * 3f);
                }
                return;
            }

#if UNITY_EDITOR
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        private void HandleMouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null || gameCamera == null)
                return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                touchHoldTime = 0f;
                isLongPress = false;
            }

            if (mouse.leftButton.isPressed)
            {
                touchHoldTime += Time.deltaTime;
                if (!isLongPress && touchHoldTime >= longPressTime)
                {
                    isLongPress = true;
                    Vector2 p = mouse.position.ReadValue();
                    Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(p.x, p.y, gameCamera.nearClipPlane + 1f));
                    worldPos.z = 0;
                    OnLongPress?.Invoke(worldPos);
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame && !isLongPress)
            {
                Vector2 p = mouse.position.ReadValue();
                Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(p.x, p.y, gameCamera.nearClipPlane + 1f));
                worldPos.z = 0;
                OnTap?.Invoke(worldPos);
            }

            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
                ZoomCamera(-scrollY * mouseScrollZoomScale * 3f);
        }

        private void HandleTouchInput()
        {
            if (gameCamera == null)
                return;

            var touches = InputTouch.activeTouches;
            if (touches.Count == 1)
            {
                var touch = touches[0];
                if (touch.phase == TouchPhase.Began)
                {
                    touchHoldTime = 0f;
                    isLongPress = false;
                }

                if (touch.phase == TouchPhase.Stationary)
                {
                    touchHoldTime += Time.deltaTime;
                    if (!isLongPress && touchHoldTime >= longPressTime)
                    {
                        isLongPress = true;
                        Vector2 p = touch.screenPosition;
                        Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(p.x, p.y, gameCamera.nearClipPlane + 1f));
                        worldPos.z = 0;
                        OnLongPress?.Invoke(worldPos);
                    }
                }

                if (touch.phase == TouchPhase.Ended && !isLongPress)
                {
                    Vector2 p = touch.screenPosition;
                    Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(p.x, p.y, gameCamera.nearClipPlane + 1f));
                    worldPos.z = 0;
                    OnTap?.Invoke(worldPos);
                }
            }
            else if (touches.Count >= 2)
            {
                HandlePinchZoom(touches[0], touches[1]);
            }
        }

        private void HandlePinchZoom(InputTouch t0, InputTouch t1)
        {
            if (TowerPlacer.Instance != null && TowerPlacer.Instance.IsPlacingTower)
                return;

            Vector2 prevT0 = t0.screenPosition - t0.delta;
            Vector2 prevT1 = t1.screenPosition - t1.delta;

            float prevDistance = Vector2.Distance(prevT0, prevT1);
            float currDistance = Vector2.Distance(t0.screenPosition, t1.screenPosition);

            float delta = prevDistance - currDistance;
            ZoomCamera(delta * zoomSpeed);
        }

        private void ZoomCamera(float amount)
        {
            if (gameCamera == null) return;
            gameCamera.orthographicSize = Mathf.Clamp(
                gameCamera.orthographicSize + amount,
                minZoom,
                maxZoom);
        }
    }
}
