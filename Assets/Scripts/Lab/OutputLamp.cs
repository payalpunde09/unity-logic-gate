using System;
using UnityEngine;
using TMPro;

namespace DigitalLogicLab.Lab
{
    /// <summary>
    /// Physical output lamp on the 3D electronics workbench.
    /// Manages bulb color, emission, point light glow, scale pop, and state readout.
    /// </summary>
    public class OutputLamp : MonoBehaviour
    {
        [Header("Lamp Identifiers")]
        [SerializeField] private string lampId = "Y";

        [Header("Components")]
        [SerializeField] private MeshRenderer lampRenderer;
        [SerializeField] private Light pointLight;
        [SerializeField] private TextMeshPro labelText;
        [SerializeField] private TextMeshPro stateText;
        [SerializeField] private Transform bulbTransform;

        [Header("Colors")]
        [SerializeField] private Color offColor = new Color(0.12f, 0.16f, 0.22f);
        [SerializeField] private Color onColor = new Color(0.0f, 1.0f, 0.5f); // Brilliant neon green/cyan
        [SerializeField] private Color offEmission = Color.black;
        [SerializeField] private Color onEmission = new Color(0.1f, 2.8f, 1.4f);

        [SerializeField] private GameObject glowHaloObj;

        private Material lampMaterialInstance;
        private int currentState = 0;
        private Vector3 initialScale = Vector3.one;

        public event Action<OutputLamp, int> OnStateChanged;

        public string LampId => lampId;
        public int CurrentState => currentState;

        public void Initialize(string id, MeshRenderer meshRenderer, Light light, TextMeshPro label, TextMeshPro stateDisplay, GameObject halo = null)
        {
            lampId = id;
            lampRenderer = meshRenderer;
            pointLight = light;
            labelText = label;
            stateText = stateDisplay;
            glowHaloObj = halo;

            if (lampRenderer != null)
            {
                lampMaterialInstance = lampRenderer.material;
                bulbTransform = lampRenderer.transform;
                initialScale = bulbTransform.localScale;
            }

            if (labelText != null)
            {
                labelText.text = lampId;
            }

            SetState(0);
        }

        public void SetLabel(string label)
        {
            lampId = label;
            if (labelText != null)
            {
                labelText.text = lampId;
            }
        }

        public void SetState(int state)
        {
            currentState = state == 1 ? 1 : 0;
            bool isOn = currentState == 1;

            if (lampMaterialInstance != null)
            {
                lampMaterialInstance.color = isOn ? onColor : offColor;
                if (lampMaterialInstance.HasProperty("_EmissionColor"))
                {
                    if (isOn)
                    {
                        lampMaterialInstance.EnableKeyword("_EMISSION");
                        lampMaterialInstance.SetColor("_EmissionColor", onEmission);
                    }
                    else
                    {
                        lampMaterialInstance.DisableKeyword("_EMISSION");
                        lampMaterialInstance.SetColor("_EmissionColor", offEmission);
                    }
                }
            }

            if (bulbTransform != null)
            {
                bulbTransform.localScale = isOn ? initialScale * 1.35f : initialScale;
            }

            if (glowHaloObj != null)
            {
                glowHaloObj.SetActive(isOn);
            }

            if (pointLight != null)
            {
                pointLight.enabled = isOn;
                pointLight.color = onColor;
                pointLight.range = 5.5f;
                pointLight.intensity = isOn ? 5.0f : 0f;
            }

            if (stateText != null)
            {
                stateText.text = isOn ? "<color=#FFFFFF><b>● 1</b></color>" : "<color=#FFFFFF>○ 0</color>";
            }

            OnStateChanged?.Invoke(this, currentState);
        }

        private void OnDestroy()
        {
            if (lampMaterialInstance != null)
            {
                Destroy(lampMaterialInstance);
            }
        }
    }
}
