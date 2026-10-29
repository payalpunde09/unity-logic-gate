using System;
using UnityEngine;
using TMPro;
using DigitalLogicLab.Input;

namespace DigitalLogicLab.Lab
{
    /// <summary>
    /// Interactive physical switch with toggle animation and IInteractable implementation.
    /// Supports desktop mouse raycast and VR controller interaction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InputSwitch : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        [SerializeField] private string switchId = "A";

        [Header("Visual Parts")]
        [SerializeField] private Transform handleTransform;
        [SerializeField] private MeshRenderer handleRenderer;
        [SerializeField] private TextMeshPro labelText;
        [SerializeField] private TextMeshPro stateText;

        [Header("Configuration")]
        [SerializeField] private Vector3 offRotationEuler = new Vector3(-25f, 0f, 0f);
        [SerializeField] private Vector3 onRotationEuler = new Vector3(25f, 0f, 0f);
        [SerializeField] private Color offHandleColor = new Color(0.35f, 0.4f, 0.45f);
        [SerializeField] private Color onHandleColor = new Color(0.1f, 0.8f, 1.0f);

        [SerializeField] private MeshRenderer statusLedRenderer;

        private int currentState = 0;
        private bool isEnabled = true;

        public event Action<InputSwitch, int> OnValueChanged;

        public string SwitchId => switchId;
        public int CurrentState => currentState;
        public bool IsEnabled => isEnabled;

        public void Initialize(string id, Transform handle, MeshRenderer hRenderer, TextMeshPro label, TextMeshPro stateDisplay, MeshRenderer ledRenderer = null)
        {
            switchId = id;
            handleTransform = handle;
            handleRenderer = hRenderer;
            labelText = label;
            stateText = stateDisplay;
            statusLedRenderer = ledRenderer;

            if (labelText != null)
            {
                labelText.text = switchId;
            }

            SetState(0, false);
        }

        public void SetLabel(string label)
        {
            switchId = label;
            if (labelText != null)
            {
                labelText.text = switchId;
            }
        }

        public void SetState(int newState, bool notify = true)
        {
            currentState = newState == 1 ? 1 : 0;
            UpdateVisuals();

            if (notify)
            {
                OnValueChanged?.Invoke(this, currentState);
            }
        }

        private float lastToggleTime = 0f;
        private bool isHovered = false;

        public void Toggle()
        {
            if (!isEnabled || !gameObject.activeInHierarchy) return;
            if (Time.unscaledTime - lastToggleTime < 0.15f) return; // Debounce
            lastToggleTime = Time.unscaledTime;
            SetState(currentState == 1 ? 0 : 1, true);
        }

        public void ResetToZero()
        {
            SetState(0, true);
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = enabled;
        }

        private void UpdateVisuals()
        {
            bool isOn = currentState == 1;

            if (handleTransform != null)
            {
                handleTransform.localRotation = Quaternion.Euler(isOn ? onRotationEuler : offRotationEuler);
            }

            if (handleRenderer != null)
            {
                handleRenderer.material.color = isOn ? onHandleColor : offHandleColor;
                if (handleRenderer.material.HasProperty("_EmissionColor"))
                {
                    if (isOn)
                    {
                        handleRenderer.material.EnableKeyword("_EMISSION");
                        handleRenderer.material.SetColor("_EmissionColor", onHandleColor * 0.7f);
                    }
                    else
                    {
                        handleRenderer.material.DisableKeyword("_EMISSION");
                        handleRenderer.material.SetColor("_EmissionColor", Color.black);
                    }
                }
            }

            if (statusLedRenderer != null)
            {
                statusLedRenderer.material.color = isOn ? onHandleColor : new Color(0.18f, 0.22f, 0.28f);
                if (statusLedRenderer.material.HasProperty("_EmissionColor"))
                {
                    if (isOn)
                    {
                        statusLedRenderer.material.EnableKeyword("_EMISSION");
                        statusLedRenderer.material.SetColor("_EmissionColor", onHandleColor * 1.8f);
                    }
                    else
                    {
                        statusLedRenderer.material.DisableKeyword("_EMISSION");
                        statusLedRenderer.material.SetColor("_EmissionColor", Color.black);
                    }
                }
            }

            if (stateText != null)
            {
                stateText.text = isOn ? "<color=#00E5FF><b>1</b></color>" : "<color=#778899>0</color>";
            }
        }

        public void SetHover(bool hover)
        {
            if (isHovered == hover) return;
            isHovered = hover;

            if (handleRenderer != null && handleRenderer.material.HasProperty("_EmissionColor"))
            {
                if (isHovered)
                {
                    handleRenderer.material.EnableKeyword("_EMISSION");
                    Color hoverGlow = currentState == 1 ? onHandleColor * 1.2f : new Color(0.15f, 0.45f, 0.7f);
                    handleRenderer.material.SetColor("_EmissionColor", hoverGlow);
                }
                else
                {
                    UpdateVisuals();
                }
            }
        }

        // --- IInteractable Implementation ---
        public void Interact()
        {
            Toggle();
        }

        public string GetInteractPrompt()
        {
            return $"Toggle Input {switchId} ({currentState} -> {(currentState == 1 ? 0 : 1)})";
        }

        public bool IsInteractable()
        {
            return isEnabled && gameObject.activeInHierarchy;
        }

        // Support direct clicking via Unity's OnMouseDown in addition to raycasts
        private void OnMouseDown()
        {
            Interact();
        }
    }
}
