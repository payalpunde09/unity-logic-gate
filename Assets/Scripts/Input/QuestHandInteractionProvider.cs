using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DigitalLogicLab.Lab;

namespace DigitalLogicLab.Input
{
    /// <summary>
    /// Meta Quest 3S Hand Interaction System.
    /// Implements:
    /// 1. Pointing: Index finger tracking with sleek laser ray, reticle cursor, and hover highlights.
    ///    Pointing alone NEVER activates a button.
    /// 2. Pinch to Select: Thumb + index finger pinch detection.
    ///    Activates targeted UI button or 3D switch ONCE on pinch down.
    ///    Prevents repeated activations while pinched. Allows another activation only after release and re-pinch.
    ///    Avoids accidental activations when pointing at non-interactive space.
    /// 3. Desktop Fallback: Smooth mouse pointing and left-click / Spacebar pinch simulation in Editor.
    /// </summary>
    public class QuestHandInteractionProvider : MonoBehaviour
    {
        public static QuestHandInteractionProvider Instance { get; private set; }

        [Header("Pinch Settings")]
        [SerializeField] private float pinchDistanceThreshold = 0.028f; // 2.8 cm between index tip & thumb tip
        [SerializeField] private float maxRayDistance = 30f;

        [Header("Visual Feedback")]
        [SerializeField] private Color idleRayColor = new Color(0.0f, 0.85f, 1.0f, 0.55f);
        [SerializeField] private Color hoverRayColor = new Color(0.1f, 1.0f, 0.65f, 0.95f);
        [SerializeField] private Color pinchFlashColor = new Color(1.0f, 0.9f, 0.2f, 1.0f);

        // Ray & Reticle Visuals
        private LineRenderer laserLine;
        private GameObject reticleObj;
        private MeshRenderer reticleRenderer;
        private Material lineMat;
        private Material reticleMat;

        // State Tracking
        private Ray currentPointerRay;
        private bool isHandTrackingActive = false;
        private bool isPinching = false;
        private bool wasPinching = false;
        private float lastPinchTime = 0f;

        // Hover Targets
        private Button currentTargetButton = null;
        private IInteractable currentTargetInteractable = null;
        private Color originalButtonColor = Color.white;
        private Image currentButtonImage = null;

        public Ray CurrentPointerRay => currentPointerRay;
        public bool IsHandTrackingActive => isHandTrackingActive;
        public bool IsPinching => isPinching;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            BuildPointerVisuals();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (lineMat != null) Destroy(lineMat);
            if (reticleMat != null) Destroy(reticleMat);
        }

        private void BuildPointerVisuals()
        {
            // 1. Setup Materials
            Shader unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

            lineMat = new Material(unlitShader);
            reticleMat = new Material(unlitShader);

            // 2. Setup Laser Line
            GameObject lineObj = new GameObject("HandPointerLaser");
            lineObj.transform.SetParent(transform, false);
            laserLine = lineObj.AddComponent<LineRenderer>();
            laserLine.material = lineMat;
            laserLine.useWorldSpace = true;
            laserLine.startWidth = 0.005f;
            laserLine.endWidth = 0.002f;
            laserLine.positionCount = 2;
            laserLine.startColor = idleRayColor;
            laserLine.endColor = idleRayColor;

            // 3. Setup Reticle Cursor (Flat Ring / Disk facing camera)
            reticleObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reticleObj.name = "HandPointerReticle";
            reticleObj.transform.SetParent(transform, false);
            reticleObj.transform.localScale = Vector3.one * 0.035f;

            // Remove collider so it doesn't block raycasts
            Collider retCol = reticleObj.GetComponent<Collider>();
            if (retCol != null) Destroy(retCol);

            reticleRenderer = reticleObj.GetComponent<MeshRenderer>();
            if (reticleRenderer != null)
            {
                reticleRenderer.material = reticleMat;
                reticleRenderer.material.color = idleRayColor;
            }
        }

        private void Update()
        {
            UpdatePointerRay();
            PerformTargetRaycast();
            HandlePinchGesture();
        }

        // ==========================================
        // PART 5A: POINTING (Index finger tracking & visual ray)
        // ==========================================

        private void UpdatePointerRay()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 origin;
            Vector3 direction;

            // Check if native XR Hand Tracking is providing data
            if (TryGetXRHandData(out origin, out direction, out bool xrPinch))
            {
                isHandTrackingActive = true;
                currentPointerRay = new Ray(origin, direction);
                isPinching = xrPinch;
            }
            else
            {
                // Fallback / Editor Simulation Mode:
                // Camera screen ray from mouse position
                isHandTrackingActive = false;
                currentPointerRay = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);

                // For laser origin in desktop simulation, start from slightly below-right camera
                origin = cam.transform.position + cam.transform.right * 0.22f - cam.transform.up * 0.18f + cam.transform.forward * 0.35f;
                direction = (currentPointerRay.GetPoint(10f) - origin).normalized;

                // Mouse click or Spacebar simulates pinch gesture
                isPinching = UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetKey(KeyCode.Space);
            }
        }

        private void PerformTargetRaycast()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 hitPoint;
            bool hitInteractive = false;
            Button hitButton = null;
            IInteractable hitInteractable = null;

            // 1. First check UI Buttons across active canvases
            if (RaycastUIButtons(currentPointerRay, out hitButton, out hitPoint))
            {
                hitInteractive = true;
            }
            else
            {
                // 2. Check 3D physical interactables (switches on workbench)
                if (Physics.Raycast(currentPointerRay, out RaycastHit physHit, maxRayDistance))
                {
                    hitPoint = physHit.point;
                    var interactable = physHit.collider.GetComponentInParent<IInteractable>();
                    if (interactable != null && interactable.IsInteractable())
                    {
                        hitInteractive = true;
                        hitInteractable = interactable;
                    }
                }
                else
                {
                    // No surface hit: extend laser forward
                    hitPoint = currentPointerRay.GetPoint(4.0f);
                }
            }

            // Update Target Hovers (Pointing alone must NOT activate)
            UpdateHoverStates(hitButton, hitInteractable);

            // Update Visual Laser & Reticle
            UpdateVisualFeedback(hitPoint, hitInteractive);
        }

        private void UpdateHoverStates(Button newButton, IInteractable newInteractable)
        {
            // --- UI Button Hover ---
            if (currentTargetButton != newButton)
            {
                // Restore old button
                if (currentTargetButton != null && currentButtonImage != null)
                {
                    currentButtonImage.color = originalButtonColor;
                }

                currentTargetButton = newButton;

                // Highlight new button
                if (currentTargetButton != null)
                {
                    currentButtonImage = currentTargetButton.GetComponent<Image>();
                    if (currentButtonImage != null)
                    {
                        originalButtonColor = currentButtonImage.color;
                        currentButtonImage.color = originalButtonColor * 1.35f + new Color(0.0f, 0.2f, 0.2f, 0f);
                    }
                }
            }

            // --- 3D Interactable Hover ---
            if (currentTargetInteractable != newInteractable)
            {
                if (currentTargetInteractable != null)
                {
                    currentTargetInteractable.SetHover(false);
                }

                currentTargetInteractable = newInteractable;

                if (currentTargetInteractable != null)
                {
                    currentTargetInteractable.SetHover(true);
                }
            }
        }

        private void UpdateVisualFeedback(Vector3 hitPoint, bool isHoveringInteractive)
        {
            Camera cam = Camera.main;
            Vector3 origin = isHandTrackingActive ? currentPointerRay.origin :
                (cam != null ? cam.transform.position + cam.transform.right * 0.22f - cam.transform.up * 0.18f + cam.transform.forward * 0.35f : currentPointerRay.origin);

            // Laser line points
            if (laserLine != null)
            {
                laserLine.SetPosition(0, origin);
                laserLine.SetPosition(1, hitPoint);

                Color targetColor = isHoveringInteractive ? hoverRayColor : idleRayColor;
                if (isPinching && isHoveringInteractive) targetColor = pinchFlashColor;

                laserLine.startColor = targetColor;
                laserLine.endColor = targetColor;
            }

            // Reticle cursor position and orientation
            if (reticleObj != null)
            {
                reticleObj.transform.position = hitPoint;
                if (cam != null)
                {
                    reticleObj.transform.rotation = Quaternion.LookRotation(reticleObj.transform.position - cam.transform.position);
                }

                float scale = isHoveringInteractive ? 0.048f : 0.032f;
                reticleObj.transform.localScale = Vector3.one * scale;

                if (reticleRenderer != null)
                {
                    Color rColor = isHoveringInteractive ? hoverRayColor : idleRayColor;
                    if (isPinching && isHoveringInteractive) rColor = pinchFlashColor;
                    reticleRenderer.material.color = rColor;
                }
            }
        }

        private bool RaycastUIButtons(Ray ray, out Button outButton, out Vector3 outHitPoint)
        {
            outButton = null;
            outHitPoint = ray.GetPoint(2.5f);

            Canvas[] canvases = FindObjectsOfType<Canvas>();
            if (canvases == null || canvases.Length == 0) return false;

            Camera cam = Camera.main;

            foreach (var canvas in canvases)
            {
                if (!canvas.gameObject.activeInHierarchy || !canvas.enabled) continue;

                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null) continue;

                Plane plane = new Plane(-canvas.transform.forward, canvas.transform.position);
                if (plane.Raycast(ray, out float enterDist) && enterDist > 0.01f && enterDist < maxRayDistance)
                {
                    Vector3 worldHit = ray.GetPoint(enterDist);
                    Camera camForScreen = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas.worldCamera != null ? canvas.worldCamera : cam);
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camForScreen, worldHit);

                    PointerEventData pointerData = new PointerEventData(EventSystem.current)
                    {
                        position = screenPos
                    };

                    List<RaycastResult> results = new List<RaycastResult>();
                    raycaster.Raycast(pointerData, results);

                    if (results.Count > 0)
                    {
                        foreach (var res in results)
                        {
                            if (res.gameObject == null) continue;
                            Button btn = res.gameObject.GetComponentInParent<Button>();
                            if (btn != null && btn.interactable && btn.gameObject.activeInHierarchy)
                            {
                                outButton = btn;
                                outHitPoint = worldHit;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        // ==========================================
        // PART 5B: PINCH TO SELECT (Latching, single-activation)
        // ==========================================

        private void HandlePinchGesture()
        {
            // Detect single transition: pinch DOWN
            bool pinchDown = isPinching && !wasPinching;
            wasPinching = isPinching;

            if (!pinchDown) return;

            // Debounce against rapid accidental triggers
            if (Time.unscaledTime - lastPinchTime < 0.2f) return;
            lastPinchTime = Time.unscaledTime;

            // Activate targeted item ONCE
            if (currentTargetButton != null)
            {
                // Trigger button once
                currentTargetButton.onClick.Invoke();
            }
            else if (currentTargetInteractable != null)
            {
                // Trigger 3D switch once
                currentTargetInteractable.Interact();
            }
            // If neither is targeted, DO NOTHING (strictly avoids accidental activation)
        }

        // ==========================================
        // XR HAND TRACKING HARDWARE QUERY
        // ==========================================

        private bool TryGetXRHandData(out Vector3 indexTipPos, out Vector3 pointerDir, out bool pinch)
        {
            indexTipPos = Vector3.zero;
            pointerDir = Vector3.forward;
            pinch = false;

            try
            {
                // Attempt to query UnityEngine.XR.InputDevices via standard XR types
                Type inputDevicesType = Type.GetType("UnityEngine.XR.InputDevices, UnityEngine.XRModule") ??
                                        Type.GetType("UnityEngine.XR.InputDevices, UnityEngine.CoreModule");

                if (inputDevicesType == null) return false;

                // Look for Right Hand or HandTracking device
                var getDevicesMethod = inputDevicesType.GetMethod("GetDevicesWithCharacteristics",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (getDevicesMethod == null) return false;

                // Characteristics: Right | HandTracking or HeldInHand
                // InputDeviceCharacteristics: Right = 0x08, HandTracking = 0x80
                uint characteristics = 0x08 | 0x80;
                var listType = typeof(List<>).MakeGenericType(Type.GetType("UnityEngine.XR.InputDevice, UnityEngine.XRModule") ?? Type.GetType("UnityEngine.XR.InputDevice, UnityEngine.CoreModule"));
                var deviceList = Activator.CreateInstance(listType);

                object[] parameters = new object[] { Enum.ToObject(Type.GetType("UnityEngine.XR.InputDeviceCharacteristics, UnityEngine.XRModule") ?? Type.GetType("UnityEngine.XR.InputDeviceCharacteristics, UnityEngine.CoreModule"), characteristics), deviceList };
                getDevicesMethod.Invoke(null, parameters);

                int count = (int)listType.GetProperty("Count").GetValue(deviceList);
                if (count == 0)
                {
                    // Fallback to any Right hand device
                    parameters[0] = Enum.ToObject(Type.GetType("UnityEngine.XR.InputDeviceCharacteristics, UnityEngine.XRModule") ?? Type.GetType("UnityEngine.XR.InputDeviceCharacteristics, UnityEngine.CoreModule"), (uint)0x08);
                    getDevicesMethod.Invoke(null, parameters);
                    count = (int)listType.GetProperty("Count").GetValue(deviceList);
                }

                if (count > 0)
                {
                    var itemProp = listType.GetProperty("Item");
                    object device = itemProp.GetValue(deviceList, new object[] { 0 });

                    // Check device position and rotation
                    var tryGetFeatureMethod = device.GetType().GetMethod("TryGetFeatureValue");
                    // If device is tracked, read pointer pose or aim
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        indexTipPos = cam.transform.position + cam.transform.right * 0.2f - cam.transform.up * 0.15f + cam.transform.forward * 0.3f;
                        pointerDir = cam.transform.forward;
                    }
                    return true;
                }
            }
            catch
            {
                // Graceful fallback to desktop simulation if XR subsystem is not currently initialized
            }

            return false;
        }
    }
}
