using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DigitalLogicLab.Logic;

namespace DigitalLogicLab.Lab
{
    /// <summary>
    /// Procedurally renders distinct, standard IEEE logic gate symbols and signal wires
    /// using LineRenderers and 3D primitives. Updates wire colors based on logic high/low states.
    /// </summary>
    public class CircuitSymbolRenderer : MonoBehaviour
    {
        [Header("Symbol Board Settings")]
        [SerializeField] private Transform symbolRoot;
        [SerializeField] private Material wireMaterial;
        [SerializeField] private Material gateMaterial;

        [Header("Wire Color Scheme")]
        [SerializeField] private Color activeWireColor = new Color(0.0f, 0.95f, 0.85f, 1f); // Neon cyan for 1
        [SerializeField] private Color inactiveWireColor = new Color(0.2f, 0.26f, 0.35f, 0.8f); // Dim slate for 0
        [SerializeField] private Color gateLineColor = new Color(0.85f, 0.92f, 1.0f, 1f); // Crisp glowing white-blue

        private List<LineRenderer> activeInputWires = new List<LineRenderer>();
        private List<LineRenderer> activeOutputWires = new List<LineRenderer>();
        private List<GameObject> spawnedSymbolParts = new List<GameObject>();

        private CircuitType currentCircuit = CircuitType.AND;

        public void Initialize(Transform root, Material wMat, Material gMat)
        {
            symbolRoot = root != null ? root : transform;
            wireMaterial = wMat;
            gateMaterial = gMat;
        }

        public void RenderCircuit(CircuitType circuit, int[] currentInputs, int[] currentOutputs)
        {
            currentCircuit = circuit;
            ClearVisuals();

            switch (circuit)
            {
                case CircuitType.AND:
                    DrawAndGate(false);
                    break;
                case CircuitType.NAND:
                    DrawAndGate(true);
                    break;
                case CircuitType.OR:
                    DrawOrGate(false);
                    break;
                case CircuitType.NOR:
                    DrawOrGate(true);
                    break;
                case CircuitType.NOT:
                    DrawNotGate();
                    break;
                case CircuitType.MUX:
                    DrawMux();
                    break;
                case CircuitType.DEMUX:
                    DrawDemux();
                    break;
            }

            UpdateSignalStates(currentInputs, currentOutputs);
        }

        public void UpdateSignalStates(int[] currentInputs, int[] currentOutputs)
        {
            if (currentInputs != null)
            {
                for (int i = 0; i < activeInputWires.Count; i++)
                {
                    int val = (i < currentInputs.Length) ? currentInputs[i] : 0;
                    SetWireColor(activeInputWires[i], val == 1);
                }
            }

            if (currentOutputs != null)
            {
                for (int i = 0; i < activeOutputWires.Count; i++)
                {
                    int val = (i < currentOutputs.Length) ? currentOutputs[i] : 0;
                    SetWireColor(activeOutputWires[i], val == 1);
                }
            }
        }

        private void SetWireColor(LineRenderer line, bool isHigh)
        {
            if (line == null) return;
            Color col = isHigh ? activeWireColor : inactiveWireColor;
            line.startColor = col;
            line.endColor = col;
            if (line.material != null)
            {
                line.material.color = col;
                if (line.material.HasProperty("_EmissionColor"))
                {
                    line.material.SetColor("_EmissionColor", isHigh ? col * 1.2f : Color.black);
                }
            }
        }

        private void ClearVisuals()
        {
            activeInputWires.Clear();
            activeOutputWires.Clear();

            foreach (var part in spawnedSymbolParts)
            {
                if (part != null) Destroy(part);
            }
            spawnedSymbolParts.Clear();
        }

        // --- Gate Geometries ---

        private void DrawAndGate(bool hasBubble)
        {
            // Input Wires (A at y = +0.25, B at y = -0.25)
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, 0.25f, 0f), new Vector3(-0.4f, 0.25f, 0f)));
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, -0.25f, 0f), new Vector3(-0.4f, -0.25f, 0f)));

            // Gate D-shape
            List<Vector3> points = new List<Vector3>();
            // Left vertical line
            points.Add(new Vector3(-0.4f, -0.5f, 0f));
            points.Add(new Vector3(-0.4f, 0.5f, 0f));
            // Top flat line
            points.Add(new Vector3(0.0f, 0.5f, 0f));
            // Semi-circle from +0.5 to -0.5
            int arcSegments = 16;
            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = Mathf.PI * 0.5f - (Mathf.PI * (float)i / arcSegments);
                float x = Mathf.Cos(angle) * 0.5f;
                float y = Mathf.Sin(angle) * 0.5f;
                points.Add(new Vector3(x, y, 0f));
            }
            // Bottom flat line back to vertical start
            points.Add(new Vector3(-0.4f, -0.5f, 0f));

            CreateOutline(points.ToArray(), true);

            float outputStartX = 0.5f;
            if (hasBubble)
            {
                CreateInversionBubble(new Vector3(0.58f, 0f, 0f), 0.08f);
                outputStartX = 0.66f;
            }

            // Output Wire
            activeOutputWires.Add(CreateWire(new Vector3(outputStartX, 0f, 0f), new Vector3(1.0f, 0f, 0f)));
        }

        private void DrawOrGate(bool hasBubble)
        {
            // Input Wires
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, 0.25f, 0f), new Vector3(-0.35f, 0.25f, 0f)));
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, -0.25f, 0f), new Vector3(-0.35f, -0.25f, 0f)));

            // Curved shield:
            // 1. Inward rear curve (from -0.5, 0.5 to -0.5, -0.5 with center at -0.7)
            // 2. Bottom curve towards tip at (0.55, 0)
            // 3. Top curve back to (-0.5, 0.5)
            List<Vector3> points = new List<Vector3>();
            int arcSegs = 12;

            // Rear concave curve
            for (int i = 0; i <= arcSegs; i++)
            {
                float t = (float)i / arcSegs;
                float y = Mathf.Lerp(0.5f, -0.5f, t);
                float x = -0.45f + (1f - (y * y / 0.25f)) * 0.15f;
                points.Add(new Vector3(x, y, 0f));
            }

            // Bottom to tip curve
            for (int i = 0; i <= arcSegs; i++)
            {
                float t = (float)i / arcSegs;
                float x = Mathf.Lerp(-0.45f, 0.55f, t);
                float y = Mathf.Lerp(-0.5f, 0f, Mathf.Sin(t * Mathf.PI * 0.5f));
                points.Add(new Vector3(x, y, 0f));
            }

            // Tip to top curve
            for (int i = 0; i <= arcSegs; i++)
            {
                float t = (float)i / arcSegs;
                float x = Mathf.Lerp(0.55f, -0.45f, t);
                float y = Mathf.Lerp(0f, 0.5f, 1f - Mathf.Cos(t * Mathf.PI * 0.5f));
                points.Add(new Vector3(x, y, 0f));
            }

            CreateOutline(points.ToArray(), true);

            float outputStartX = 0.55f;
            if (hasBubble)
            {
                CreateInversionBubble(new Vector3(0.63f, 0f, 0f), 0.08f);
                outputStartX = 0.71f;
            }

            activeOutputWires.Add(CreateWire(new Vector3(outputStartX, 0f, 0f), new Vector3(1.0f, 0f, 0f)));
        }

        private void DrawNotGate()
        {
            // Single Input Wire A
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, 0f, 0f), new Vector3(-0.4f, 0f, 0f)));

            // Triangle
            Vector3[] triangle = new Vector3[]
            {
                new Vector3(-0.4f, -0.45f, 0f),
                new Vector3(-0.4f, 0.45f, 0f),
                new Vector3(0.35f, 0f, 0f),
                new Vector3(-0.4f, -0.45f, 0f)
            };
            CreateOutline(triangle, true);

            // Inversion Bubble
            CreateInversionBubble(new Vector3(0.44f, 0f, 0f), 0.09f);

            // Output Wire
            activeOutputWires.Add(CreateWire(new Vector3(0.53f, 0f, 0f), new Vector3(1.0f, 0f, 0f)));
        }

        private void DrawMux()
        {
            // Inputs: I0 (top), I1 (bottom), S (bottom-center)
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, 0.35f, 0f), new Vector3(-0.45f, 0.35f, 0f)));
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, -0.35f, 0f), new Vector3(-0.45f, -0.35f, 0f)));
            // Select S line entering from bottom
            activeInputWires.Add(CreateWire(new Vector3(0f, -1.0f, 0f), new Vector3(0f, -0.55f, 0f)));

            // Trapezoid: taller on left (x=-0.45, y=±0.65), narrower on right (x=0.45, y=±0.45)
            Vector3[] trap = new Vector3[]
            {
                new Vector3(-0.45f, -0.65f, 0f),
                new Vector3(-0.45f, 0.65f, 0f),
                new Vector3(0.45f, 0.45f, 0f),
                new Vector3(0.45f, -0.45f, 0f),
                new Vector3(-0.45f, -0.65f, 0f)
            };
            CreateOutline(trap, true);

            // Text Labels inside MUX
            CreateTextLabel("L1", new Vector3(-0.3f, 0.35f, 0f), 1.0f, new Color(0.8f, 0.9f, 1f));
            CreateTextLabel("L2", new Vector3(-0.3f, -0.35f, 0f), 1.0f, new Color(0.8f, 0.9f, 1f));
            CreateTextLabel("S", new Vector3(0f, -0.4f, 0f), 1.0f, new Color(1f, 0.85f, 0.3f));
            // Output label "Y" inside trapezoid and along the output line
            CreateTextLabel("Y", new Vector3(0.28f, 0f, 0f), 1.15f, new Color(0.0f, 0.95f, 1.0f));
            CreateTextLabel("Y", new Vector3(0.72f, 0.16f, 0f), 1.15f, new Color(0.0f, 0.95f, 1.0f));

            // Output Wire Y
            activeOutputWires.Add(CreateWire(new Vector3(0.45f, 0f, 0f), new Vector3(1.0f, 0f, 0f)));
        }

        private void DrawDemux()
        {
            // Inputs: Data D (center-left), S (bottom-center)
            activeInputWires.Add(CreateWire(new Vector3(-1.0f, 0f, 0f), new Vector3(-0.45f, 0f, 0f)));
            activeInputWires.Add(CreateWire(new Vector3(0f, -1.0f, 0f), new Vector3(0f, -0.65f, 0f)));

            // Inverted Trapezoid: narrower on left (x=-0.45, y=±0.45), taller on right (x=0.45, y=±0.65)
            Vector3[] trap = new Vector3[]
            {
                new Vector3(-0.45f, -0.45f, 0f),
                new Vector3(-0.45f, 0.45f, 0f),
                new Vector3(0.45f, 0.65f, 0f),
                new Vector3(0.45f, -0.65f, 0f),
                new Vector3(-0.45f, -0.45f, 0f)
            };
            CreateOutline(trap, true);

            // Text Labels inside DEMUX (Symbol text removed per request)
            CreateTextLabel("D", new Vector3(-0.3f, 0f, 0f), 1.0f, new Color(0.4f, 1f, 0.8f));
            CreateTextLabel("S", new Vector3(0f, -0.45f, 0f), 1.0f, new Color(1f, 0.85f, 0.3f));
            CreateTextLabel("Y1", new Vector3(0.3f, 0.35f, 0f), 1.0f, new Color(0.8f, 0.9f, 1f));
            CreateTextLabel("Y2", new Vector3(0.3f, -0.35f, 0f), 1.0f, new Color(0.8f, 0.9f, 1f));

            // Outputs: Y0 (top-right), Y1 (bottom-right)
            activeOutputWires.Add(CreateWire(new Vector3(0.45f, 0.35f, 0f), new Vector3(1.0f, 0.35f, 0f)));
            activeOutputWires.Add(CreateWire(new Vector3(0.45f, -0.35f, 0f), new Vector3(1.0f, -0.35f, 0f)));
        }

        // --- Helper Rendering Utilities ---

        private LineRenderer CreateWire(Vector3 start, Vector3 end)
        {
            CreateTerminalPad(start);
            CreateTerminalPad(end);

            GameObject obj = new GameObject("Wire");
            obj.transform.SetParent(symbolRoot, false);
            spawnedSymbolParts.Add(obj);

            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.045f;
            line.endWidth = 0.045f;

            if (wireMaterial != null)
            {
                line.material = new Material(wireMaterial);
            }
            SetWireColor(line, false);
            return line;
        }

        private LineRenderer CreateOutline(Vector3[] pts, bool loop)
        {
            GameObject obj = new GameObject("GateOutline");
            obj.transform.SetParent(symbolRoot, false);
            spawnedSymbolParts.Add(obj);

            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = pts.Length;
            line.SetPositions(pts);
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;

            if (gateMaterial != null)
            {
                line.material = new Material(gateMaterial);
                line.material.color = gateLineColor;
                if (line.material.HasProperty("_EmissionColor"))
                {
                    line.material.EnableKeyword("_EMISSION");
                    line.material.SetColor("_EmissionColor", gateLineColor * 0.85f);
                }
            }
            line.startColor = gateLineColor;
            line.endColor = gateLineColor;
            return line;
        }

        private void CreateTerminalPad(Vector3 pos)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "TerminalPad";
            pad.transform.SetParent(symbolRoot, false);
            pad.transform.localPosition = pos;
            pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pad.transform.localScale = new Vector3(0.075f, 0.008f, 0.075f);
            var col = pad.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            if (gateMaterial != null)
            {
                var padMat = new Material(gateMaterial);
                padMat.color = new Color(0.3f, 0.75f, 0.95f);
                pad.GetComponent<MeshRenderer>().material = padMat;
            }
            spawnedSymbolParts.Add(pad);
        }

        private void CreateInversionBubble(Vector3 center, float radius)
        {
            int segments = 16;
            Vector3[] pts = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float ang = (float)i / segments * Mathf.PI * 2f;
                pts[i] = center + new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f);
            }
            CreateOutline(pts, true);
        }

        private void CreateTextLabel(string text, Vector3 localPos, float size, Color color)
        {
            GameObject textObj = new GameObject($"Label_{text}");
            textObj.transform.SetParent(symbolRoot, false);
            textObj.transform.localRotation = Quaternion.identity;
            spawnedSymbolParts.Add(textObj);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            var font = DigitalLogicLab.Core.AppBootstrap.GetDefaultFont();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size * 2.9f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.rectTransform.localPosition = localPos + new Vector3(0f, 0f, -0.015f);
            tmp.rectTransform.sizeDelta = new Vector2(1.2f, 0.6f);
        }
    }
}
