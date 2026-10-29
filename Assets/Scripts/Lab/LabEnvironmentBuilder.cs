using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DigitalLogicLab.Lab
{
    /// <summary>
    /// Procedurally constructs the complete 3D virtual electronics lab environment
    /// using Unity primitives, materials, and lighting without external assets.
    /// </summary>
    public static class LabEnvironmentBuilder
    {
        public class LabStructure
        {
            public GameObject Root;
            public Transform BoardTransform;
            public InputSwitch SwitchA;
            public InputSwitch SwitchB;
            public InputSwitch SwitchC; // Used as S for MUX
            public OutputLamp LampY;
            public OutputLamp LampY0;
            public OutputLamp LampY1;
            public CircuitSymbolRenderer SymbolRenderer;
        }

        public static LabStructure BuildLab(Material defaultLitMat)
        {
            GameObject labRoot = new GameObject("Lab_3D_Environment");
            LabStructure result = new LabStructure();
            result.Root = labRoot;

            Shader baseShader = defaultLitMat != null ? defaultLitMat.shader : Shader.Find("Standard");
            if (baseShader == null) baseShader = Shader.Find("Sprites/Default");
            if (baseShader == null) baseShader = Shader.Find("Unlit/Color");

            // Common Materials
            Material floorMat = CreateMat(baseShader, new Color(0.08f, 0.1f, 0.14f), 0.2f, 0.8f);
            Material wallMat = CreateMat(baseShader, new Color(0.12f, 0.15f, 0.22f), 0.1f, 0.9f);
            Material benchMat = CreateMat(baseShader, new Color(0.18f, 0.22f, 0.28f), 0.4f, 0.6f);
            Material metalTrimMat = CreateMat(baseShader, new Color(0.35f, 0.42f, 0.52f), 0.8f, 0.3f);
            Material boardMat = CreateMat(baseShader, new Color(0.04f, 0.06f, 0.1f), 0.1f, 0.95f);
            Material accentMat = CreateMat(baseShader, new Color(0.0f, 0.7f, 0.9f), 0.5f, 0.5f);
            accentMat.EnableKeyword("_EMISSION");
            accentMat.SetColor("_EmissionColor", new Color(0.0f, 0.5f, 0.8f));

            Material switchBaseMat = CreateMat(baseShader, new Color(0.15f, 0.18f, 0.22f));
            Material switchHandleMat = CreateMat(baseShader, new Color(0.3f, 0.35f, 0.4f));

            // 1. Room Geometry: Floor, Back wall, Side walls
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(labRoot.transform);
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.1f, 12f);
            floor.GetComponent<MeshRenderer>().material = floorMat;

            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.SetParent(labRoot.transform);
            backWall.transform.position = new Vector3(0f, 2.5f, 4f);
            backWall.transform.localScale = new Vector3(12f, 5f, 0.2f);
            backWall.GetComponent<MeshRenderer>().material = wallMat;

            // 2. Central Workbench
            GameObject benchTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benchTop.name = "Workbench_Top";
            benchTop.transform.SetParent(labRoot.transform);
            benchTop.transform.position = new Vector3(0f, 0.8f, 0.5f);
            benchTop.transform.localScale = new Vector3(5.5f, 0.12f, 2.4f);
            benchTop.GetComponent<MeshRenderer>().material = benchMat;

            // Workbench legs & trim
            float[] legX = new float[] { -2.6f, 2.6f };
            float[] legZ = new float[] { -0.5f, 1.5f };
            foreach (float lx in legX)
            {
                foreach (float lz in legZ)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.name = "BenchLeg";
                    leg.transform.SetParent(labRoot.transform);
                    leg.transform.position = new Vector3(lx, 0.4f, lz);
                    leg.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
                    leg.GetComponent<MeshRenderer>().material = metalTrimMat;
                }
            }

            // Glowing neon accent edge along bench front
            GameObject benchGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benchGlow.name = "BenchNeonAccent";
            benchGlow.transform.SetParent(labRoot.transform);
            benchGlow.transform.position = new Vector3(0f, 0.84f, -0.65f);
            benchGlow.transform.localScale = new Vector3(5.4f, 0.03f, 0.04f);
            benchGlow.GetComponent<MeshRenderer>().material = accentMat;

            // 3. Central Circuit Display Board (Vertical mounted right behind switch table)
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "CircuitDisplayBoard";
            board.transform.SetParent(labRoot.transform);
            board.transform.position = new Vector3(0f, 1.65f, 0.85f);
            board.transform.localScale = new Vector3(3.2f, 1.6f, 0.08f);
            board.GetComponent<MeshRenderer>().material = boardMat;
            result.BoardTransform = board.transform;

            // Board metal frame
            GameObject boardFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardFrame.name = "BoardFrame";
            boardFrame.transform.SetParent(board.transform);
            boardFrame.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            boardFrame.transform.localScale = new Vector3(1.04f, 1.06f, 0.8f);
            boardFrame.GetComponent<MeshRenderer>().material = metalTrimMat;

            // Add CircuitSymbolRenderer - in front of board with clean unscaled transform
            GameObject symbolObj = new GameObject("SymbolRenderer");
            symbolObj.transform.SetParent(labRoot.transform);
            symbolObj.transform.position = new Vector3(0f, 1.65f, 0.78f);
            symbolObj.transform.rotation = Quaternion.identity;
            symbolObj.transform.localScale = Vector3.one;
            var symbolRenderer = symbolObj.AddComponent<CircuitSymbolRenderer>();
            Material lineMat = new Material(baseShader);
            lineMat.color = Color.white;
            symbolRenderer.Initialize(symbolObj.transform, lineMat, lineMat);
            result.SymbolRenderer = symbolRenderer;

            // 4. Lab Props: Oscilloscope / Test Equipment & Shelves
            BuildLabProps(labRoot.transform, baseShader, benchMat, metalTrimMat, accentMat);

            // 5. Switches on the left of bench in one horizontal row (same z = -0.20f)
            // Switch A (default for AND gate at x = -1.25f, z = -0.20f)
            result.SwitchA = CreatePhysicalSwitch("Switch_A", "A", new Vector3(-1.25f, 0.88f, -0.20f), labRoot.transform, baseShader, switchBaseMat, switchHandleMat);
            // Switch B (default for AND gate at x = -0.85f, z = -0.20f)
            result.SwitchB = CreatePhysicalSwitch("Switch_B", "B", new Vector3(-0.85f, 0.88f, -0.20f), labRoot.transform, baseShader, switchBaseMat, switchHandleMat);
            // Switch C (select line S at x = -0.45f, z = -0.20f)
            result.SwitchC = CreatePhysicalSwitch("Switch_S", "S", new Vector3(-0.45f, 0.88f, -0.20f), labRoot.transform, baseShader, switchBaseMat, switchHandleMat);

            // 6. Output Lamps on the right of bench in one horizontal row (same z = -0.20f, y = 1.00f)
            // Lamp Y (single output for AND, OR, NOT, NAND, NOR, MUX)
            result.LampY = CreatePhysicalLamp("Lamp_Y", "Output", new Vector3(1.05f, 1.00f, -0.20f), labRoot.transform, baseShader);
            // Lamp Y0 (Output 1 Y1 for DEMUX at x = 0.65f, z = -0.20f)
            result.LampY0 = CreatePhysicalLamp("Lamp_Y1", "Y1", new Vector3(0.65f, 1.00f, -0.20f), labRoot.transform, baseShader);
            // Lamp Y1 (Output 2 Y2 for DEMUX at x = 1.05f, z = -0.20f)
            result.LampY1 = CreatePhysicalLamp("Lamp_Y2", "Y2", new Vector3(1.05f, 1.00f, -0.20f), labRoot.transform, baseShader);

            // 7. Lab Lighting: Overhead Spotlight & Board Fill Light
            GameObject spotObj = new GameObject("Lab_Workbench_Spot");
            spotObj.transform.SetParent(labRoot.transform);
            spotObj.transform.position = new Vector3(0f, 3.2f, 0f);
            Light spot = spotObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 75f;
            spot.range = 7f;
            spot.intensity = 1.6f;
            spot.color = new Color(0.92f, 0.96f, 1.0f);
            spotObj.transform.rotation = Quaternion.Euler(70f, 0f, 0f);

            return result;
        }

        private static void BuildLabProps(Transform parent, Shader shader, Material benchMat, Material trimMat, Material accentMat)
        {
            // Oscilloscope Monitor on left bench edge
            GameObject scope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scope.name = "Oscilloscope";
            scope.transform.SetParent(parent);
            scope.transform.position = new Vector3(-2.2f, 1.15f, 1.1f);
            scope.transform.localScale = new Vector3(0.6f, 0.5f, 0.5f);
            scope.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            scope.GetComponent<MeshRenderer>().material = trimMat;

            // Oscilloscope Screen
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "ScopeScreen";
            screen.transform.SetParent(scope.transform);
            screen.transform.localPosition = new Vector3(0f, 0.05f, -0.51f);
            screen.transform.localScale = new Vector3(0.75f, 0.65f, 0.04f);
            Material screenMat = CreateMat(shader, new Color(0.02f, 0.15f, 0.18f));
            screenMat.EnableKeyword("_EMISSION");
            screenMat.SetColor("_EmissionColor", new Color(0.0f, 0.4f, 0.35f));
            screen.GetComponent<MeshRenderer>().material = screenMat;

            // Power Supply Box on right bench edge
            GameObject psu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            psu.name = "PowerSupply";
            psu.transform.SetParent(parent);
            psu.transform.position = new Vector3(2.3f, 1.1f, 1.1f);
            psu.transform.localScale = new Vector3(0.55f, 0.45f, 0.45f);
            psu.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
            psu.GetComponent<MeshRenderer>().material = trimMat;

            // PSU status LED
            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "PSU_LED";
            led.transform.SetParent(psu.transform);
            led.transform.localPosition = new Vector3(0.3f, 0.2f, -0.52f);
            led.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            led.GetComponent<MeshRenderer>().material = accentMat;

            // Shelf behind workbench on back wall
            GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "WallShelf";
            shelf.transform.SetParent(parent);
            shelf.transform.position = new Vector3(0f, 3.2f, 3.8f);
            shelf.transform.localScale = new Vector3(6f, 0.08f, 0.6f);
            shelf.GetComponent<MeshRenderer>().material = trimMat;
        }

        private static InputSwitch CreatePhysicalSwitch(string name, string id, Vector3 pos, Transform parent, Shader shader, Material baseMat, Material handleMat)
        {
            GameObject switchRoot = new GameObject(name);
            switchRoot.transform.SetParent(parent);
            switchRoot.transform.position = pos;

            // Switch Mounting Base (Sleek dark metallic electronics chassis)
            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "Chassis";
            baseBox.transform.SetParent(switchRoot.transform);
            baseBox.transform.localPosition = Vector3.zero;
            baseBox.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);
            baseBox.GetComponent<MeshRenderer>().material = baseMat;

            // Chassis top border accent plate
            GameObject topPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topPlate.name = "TopPlate";
            topPlate.transform.SetParent(switchRoot.transform);
            topPlate.transform.localPosition = new Vector3(0f, 0.041f, 0f);
            topPlate.transform.localScale = new Vector3(0.29f, 0.005f, 0.29f);
            var plateCol = topPlate.GetComponent<Collider>();
            if (plateCol != null) Object.Destroy(plateCol);
            Material plateMat = CreateMat(shader, new Color(0.1f, 0.14f, 0.2f), 0.5f, 0.7f);
            topPlate.GetComponent<MeshRenderer>().material = plateMat;

            // Lever collar ring (chrome bezel)
            GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collar.name = "Collar";
            collar.transform.SetParent(switchRoot.transform);
            collar.transform.localPosition = new Vector3(0f, 0.045f, 0.01f);
            collar.transform.localScale = new Vector3(0.09f, 0.015f, 0.09f);
            var collarCol = collar.GetComponent<Collider>();
            if (collarCol != null) Object.Destroy(collarCol);
            Material collarMat = CreateMat(shader, new Color(0.7f, 0.75f, 0.85f), 0.8f, 0.9f);
            collar.GetComponent<MeshRenderer>().material = collarMat;

            // Add BoxCollider to switchRoot for raycast clicks
            BoxCollider col = switchRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.12f, 0f);
            col.size = new Vector3(0.42f, 0.32f, 0.42f);

            // Switch Lever / Handle Pivot
            GameObject leverPivot = new GameObject("LeverPivot");
            leverPivot.transform.SetParent(switchRoot.transform);
            leverPivot.transform.localPosition = new Vector3(0f, 0.05f, 0.01f);

            GameObject lever = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lever.name = "Lever";
            lever.transform.SetParent(leverPivot.transform);
            lever.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            lever.transform.localScale = new Vector3(0.03f, 0.09f, 0.03f);
            var hRenderer = lever.GetComponent<MeshRenderer>();
            hRenderer.material = new Material(handleMat);

            // Chrome tip knob on switch lever
            GameObject tipKnob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipKnob.name = "TipKnob";
            tipKnob.transform.SetParent(leverPivot.transform);
            tipKnob.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            tipKnob.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);
            var knobCol = tipKnob.GetComponent<Collider>();
            if (knobCol != null) Object.Destroy(knobCol);
            tipKnob.GetComponent<MeshRenderer>().material = collarMat;

            // Status LED dot on switch base
            GameObject ledObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ledObj.name = "StatusLED";
            ledObj.transform.SetParent(switchRoot.transform);
            ledObj.transform.localPosition = new Vector3(0.09f, 0.046f, 0.09f);
            ledObj.transform.localScale = new Vector3(0.04f, 0.02f, 0.04f);
            var ledCol = ledObj.GetComponent<Collider>();
            if (ledCol != null) Object.Destroy(ledCol);
            var ledRenderer = ledObj.GetComponent<MeshRenderer>();
            Material ledMat = CreateMat(shader, new Color(0.18f, 0.22f, 0.28f));
            ledRenderer.material = ledMat;

            // Front Nameplate facing camera - Upright / Standing
            GameObject nameplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nameplate.name = "Nameplate";
            nameplate.transform.SetParent(switchRoot.transform);
            nameplate.transform.localPosition = new Vector3(0f, 0.045f, -0.17f);
            nameplate.transform.localRotation = Quaternion.identity;
            nameplate.transform.localScale = new Vector3(0.28f, 0.082f, 0.012f);
            var npCol = nameplate.GetComponent<Collider>();
            if (npCol != null) Object.Destroy(npCol);
            nameplate.GetComponent<MeshRenderer>().material = plateMat;

            // Label TMP on front nameplate - Upright, consistent font, easy to read without table overlap
            GameObject labelObj = new GameObject("LabelTMP");
            labelObj.transform.SetParent(nameplate.transform);
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            labelObj.transform.localRotation = Quaternion.identity;
            TextMeshPro labelTMP = labelObj.AddComponent<TextMeshPro>();
            var defaultFont = DigitalLogicLab.Core.AppBootstrap.GetDefaultFont();
            if (defaultFont != null) labelTMP.font = defaultFont;
            labelTMP.text = id;
            labelTMP.fontSize = 1.8f;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = Color.white;
            labelTMP.rectTransform.sizeDelta = new Vector2(0.80f, 0.22f);

            // State TMP on top
            GameObject stateObj = new GameObject("StateTMP");
            stateObj.transform.SetParent(switchRoot.transform);
            stateObj.transform.localPosition = new Vector3(-0.06f, 0.045f, 0.09f);
            stateObj.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            TextMeshPro stateTMP = stateObj.AddComponent<TextMeshPro>();
            if (defaultFont != null) stateTMP.font = defaultFont;
            stateTMP.text = "0";
            stateTMP.fontSize = 1.5f;
            stateTMP.fontStyle = FontStyles.Bold;
            stateTMP.alignment = TextAlignmentOptions.Center;
            stateTMP.color = new Color(0.6f, 0.7f, 0.8f);
            stateTMP.rectTransform.sizeDelta = new Vector2(0.50f, 0.25f);

            InputSwitch sw = switchRoot.AddComponent<InputSwitch>();
            sw.Initialize(id, leverPivot.transform, hRenderer, labelTMP, stateTMP, ledRenderer);
            return sw;
        }

        private static OutputLamp CreatePhysicalLamp(string name, string id, Vector3 pos, Transform parent, Shader shader)
        {
            GameObject lampRoot = new GameObject(name);
            lampRoot.transform.SetParent(parent);
            lampRoot.transform.position = pos;

            // 1. Lamp Pedestal Base (tiered heavy base)
            GameObject baseTier = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseTier.name = "BasePlate";
            baseTier.transform.SetParent(lampRoot.transform);
            baseTier.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            baseTier.transform.localScale = new Vector3(0.36f, 0.04f, 0.36f);
            baseTier.GetComponent<MeshRenderer>().material = CreateMat(shader, new Color(0.12f, 0.16f, 0.22f), 0.6f, 0.7f);

            // 2. Lamp Neck Column
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "NeckPillar";
            pillar.transform.SetParent(lampRoot.transform);
            pillar.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            pillar.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            pillar.GetComponent<MeshRenderer>().material = CreateMat(shader, new Color(0.7f, 0.75f, 0.85f), 0.85f, 0.9f);

            // 3. Socket Collar Ring (Chrome/Gold bezel)
            GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collar.name = "SocketCollar";
            collar.transform.SetParent(lampRoot.transform);
            collar.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            collar.transform.localScale = new Vector3(0.28f, 0.035f, 0.28f);
            collar.GetComponent<MeshRenderer>().material = CreateMat(shader, new Color(0.85f, 0.75f, 0.3f), 0.9f, 0.85f);

            // 4. Glowing Sphere Bulb
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lampRoot.transform);
            bulb.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            bulb.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            var bulbRenderer = bulb.GetComponent<MeshRenderer>();
            Material bulbMat = CreateMat(shader, new Color(0.12f, 0.16f, 0.22f));
            bulbRenderer.material = bulbMat;

            // 5. Outer Glow Halo (activates on logic 1)
            GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "GlowHalo";
            halo.transform.SetParent(bulb.transform);
            halo.transform.localPosition = Vector3.zero;
            halo.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
            var haloCol = halo.GetComponent<Collider>();
            if (haloCol != null) Object.Destroy(haloCol);
            Material haloMat = CreateMat(shader, new Color(0.0f, 1.0f, 0.5f, 0.35f));
            haloMat.EnableKeyword("_EMISSION");
            haloMat.SetColor("_EmissionColor", new Color(0.1f, 1.8f, 0.9f));
            halo.GetComponent<MeshRenderer>().material = haloMat;
            halo.SetActive(false);

            // 6. Point Light
            GameObject lightObj = new GameObject("PointLight");
            lightObj.transform.SetParent(bulb.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light pl = lightObj.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.range = 5.5f;
            pl.intensity = 0f;

            // 7. Front Badge on Pedestal - Upright / Standing
            GameObject badgePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badgePlate.name = "BadgePlate";
            badgePlate.transform.SetParent(lampRoot.transform);
            badgePlate.transform.localPosition = new Vector3(0f, -0.075f, -0.20f);
            badgePlate.transform.localRotation = Quaternion.identity;
            badgePlate.transform.localScale = new Vector3(0.30f, 0.082f, 0.012f);
            var bCol = badgePlate.GetComponent<Collider>();
            if (bCol != null) Object.Destroy(bCol);
            badgePlate.GetComponent<MeshRenderer>().material = CreateMat(shader, new Color(0.08f, 0.12f, 0.18f), 0.5f, 0.6f);

            // Label TMP on the badge - Upright, consistent font, easy to read without table overlap
            GameObject labelObj = new GameObject("LabelTMP");
            labelObj.transform.SetParent(badgePlate.transform);
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            labelObj.transform.localRotation = Quaternion.identity;
            TextMeshPro labelTMP = labelObj.AddComponent<TextMeshPro>();
            var defaultFont = DigitalLogicLab.Core.AppBootstrap.GetDefaultFont();
            if (defaultFont != null) labelTMP.font = defaultFont;
            labelTMP.text = id;
            labelTMP.fontSize = 1.75f;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = new Color(0.0f, 0.95f, 1f);
            labelTMP.rectTransform.sizeDelta = new Vector2(0.80f, 0.22f);

            // State TMP on socket collar
            GameObject stateObj = new GameObject("StateTMP");
            stateObj.transform.SetParent(lampRoot.transform);
            stateObj.transform.localPosition = new Vector3(0f, 0.02f, -0.15f);
            stateObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            TextMeshPro stateTMP = stateObj.AddComponent<TextMeshPro>();
            if (defaultFont != null) stateTMP.font = defaultFont;
            stateTMP.text = "0";
            stateTMP.fontSize = 1.45f;
            stateTMP.fontStyle = FontStyles.Bold;
            stateTMP.alignment = TextAlignmentOptions.Center;
            stateTMP.color = Color.white;
            stateTMP.rectTransform.sizeDelta = new Vector2(0.55f, 0.20f);

            OutputLamp lamp = lampRoot.AddComponent<OutputLamp>();
            lamp.Initialize(id, bulbRenderer, pl, labelTMP, stateTMP, halo);
            return lamp;
        }

        private static Material CreateMat(Shader shader, Color color, float metallic = 0f, float smoothness = 0.5f)
        {
            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
    }
}
