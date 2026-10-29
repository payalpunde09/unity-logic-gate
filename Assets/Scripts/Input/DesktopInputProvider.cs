using UnityEngine;

namespace DigitalLogicLab.Input
{
    /// <summary>
    /// Desktop keyboard & mouse provider.
    /// In VR, this is replaced by an XR Controller / Ray Interactor provider.
    /// </summary>
    public class DesktopInputProvider : MonoBehaviour, IInputProvider
    {
        public static DesktopInputProvider Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public bool GetSelectDown()
        {
            return UnityEngine.Input.GetMouseButtonDown(0);
        }

        public Vector2 GetPointerPosition()
        {
            return UnityEngine.Input.mousePosition;
        }

        public Ray GetPointerRay(Camera cam)
        {
            if (QuestHandInteractionProvider.Instance != null && QuestHandInteractionProvider.Instance.IsHandTrackingActive)
            {
                return QuestHandInteractionProvider.Instance.CurrentPointerRay;
            }
            if (cam == null) cam = Camera.main;
            if (cam == null) return new Ray(Vector3.zero, Vector3.forward);
            return cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
        }

        public int GetNumericShortcut()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad1)) return 1;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad2)) return 2;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad3)) return 3;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad4)) return 4;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad5)) return 5;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha6) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad6)) return 6;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha7) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad7)) return 7;
            return 0;
        }

        public bool GetMenuRequested()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        }
    }
}
