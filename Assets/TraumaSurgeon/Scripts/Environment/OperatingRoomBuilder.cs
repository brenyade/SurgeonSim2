using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.Player;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Environment
{
    /// <summary>
    /// Builds the operating theatre out of primitives: room shell, OR table, overhead lights,
    /// anaesthesia machine, monitors, instrument tables, imaging displays and supply carts.
    ///
    /// Every prop is created through <see cref="PrimitiveFactory"/> so a real art pass can replace
    /// each call with a prefab instantiation without touching gameplay code.
    /// </summary>
    public class OperatingRoomBuilder : MonoBehaviour
    {
        public const float TableHeight = 0.95f;

        /// <summary>Where the patient's body root should sit.</summary>
        public Transform PatientAnchor { get; private set; }

        /// <summary>Where the player spawns (surgeon's position at the patient's right).</summary>
        public Vector3 SurgeonSpawn { get; private set; }

        public Transform MonitorAnchor { get; private set; }
        public Transform ImagingAnchor { get; private set; }
        public Transform ScopeMonitorAnchor { get; private set; }

        private readonly List<Light> _surgicalLights = new List<Light>();
        private readonly List<Transform> _staffStations = new List<Transform>();

        public IReadOnlyList<Transform> StaffStations => _staffStations;

        public static OperatingRoomBuilder Build(Transform parent = null)
        {
            var go = new GameObject("OperatingRoom");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var builder = go.AddComponent<OperatingRoomBuilder>();
            builder.Construct();
            return builder;
        }

        public void Construct()
        {
            BuildShell();
            BuildTable();
            BuildLights();
            BuildAnesthesiaMachine();
            BuildMonitors();
            BuildInstrumentTables();
            BuildSupplies();
            BuildStaffStations();

            // Just outside the drape edge (x = 0.44) plus the player's 0.28 m radius, so the
            // surgeon starts within arm's reach of the abdomen rather than across the room.
            SurgeonSpawn = new Vector3(0.88f, 0f, 0.15f);
        }

        // ---- Room -------------------------------------------------------------

        private void BuildShell()
        {
            Transform root = PrimitiveFactory.Empty("Shell", transform).transform;

            PrimitiveFactory.Panel("Floor", root, new Vector3(0f, -0.05f, 0f),
                new Vector3(10f, 0.1f, 12f), MaterialLibrary.Floor);

            PrimitiveFactory.Panel("Ceiling", root, new Vector3(0f, 3.2f, 0f),
                new Vector3(10f, 0.1f, 12f), MaterialLibrary.Get("ceiling", new Color(0.88f, 0.90f, 0.91f)));

            Material wall = MaterialLibrary.Wall;
            PrimitiveFactory.Panel("WallN", root, new Vector3(0f, 1.6f, 6f), new Vector3(10f, 3.3f, 0.15f), wall);
            PrimitiveFactory.Panel("WallS", root, new Vector3(0f, 1.6f, -6f), new Vector3(10f, 3.3f, 0.15f), wall);
            PrimitiveFactory.Panel("WallE", root, new Vector3(5f, 1.6f, 0f), new Vector3(0.15f, 3.3f, 12f), wall);
            PrimitiveFactory.Panel("WallW", root, new Vector3(-5f, 1.6f, 0f), new Vector3(0.15f, 3.3f, 12f), wall);

            // Tiled accent strip so the room reads as a clinical space.
            Material accent = MaterialLibrary.Get("accent", new Color(0.42f, 0.58f, 0.64f));
            PrimitiveFactory.Panel("AccentN", root, new Vector3(0f, 1.1f, 5.9f), new Vector3(9.6f, 0.12f, 0.02f), accent);
            PrimitiveFactory.Panel("AccentS", root, new Vector3(0f, 1.1f, -5.9f), new Vector3(9.6f, 0.12f, 0.02f), accent);

            // Ambient fill so the room is never pitch black.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.33f, 0.35f, 0.38f);
        }

        private void BuildTable()
        {
            Transform root = PrimitiveFactory.Empty("OperatingTable", transform).transform;

            PrimitiveFactory.Create(PrimitiveType.Cube, "Top", root,
                new Vector3(0f, TableHeight, 0f), new Vector3(0.68f, 0.08f, 2.1f),
                MaterialLibrary.Get("tabletop", new Color(0.30f, 0.42f, 0.48f)));

            PrimitiveFactory.Create(PrimitiveType.Cylinder, "Column", root,
                new Vector3(0f, TableHeight * 0.5f, 0f), new Vector3(0.16f, TableHeight * 0.5f, 0.16f),
                MaterialLibrary.Steel);

            PrimitiveFactory.Create(PrimitiveType.Cube, "Base", root,
                new Vector3(0f, 0.05f, 0f), new Vector3(0.7f, 0.1f, 1.1f), MaterialLibrary.DarkSteel);

            // Sterile drape covering everything except the working window.
            Material drape = MaterialLibrary.Drape;
            PrimitiveFactory.Create(PrimitiveType.Cube, "DrapeHead", root,
                new Vector3(0f, TableHeight + 0.05f, 1.35f), new Vector3(0.72f, 0.02f, 0.9f), drape, false);
            PrimitiveFactory.Create(PrimitiveType.Cube, "DrapeFeet", root,
                new Vector3(0f, TableHeight + 0.05f, -0.95f), new Vector3(0.72f, 0.02f, 0.9f), drape, false);
            PrimitiveFactory.Create(PrimitiveType.Cube, "DrapeSideL", root,
                new Vector3(-0.44f, TableHeight - 0.16f, 0f), new Vector3(0.02f, 0.5f, 2.1f), drape, false);
            PrimitiveFactory.Create(PrimitiveType.Cube, "DrapeSideR", root,
                new Vector3(0.44f, TableHeight - 0.16f, 0f), new Vector3(0.02f, 0.5f, 2.1f), drape, false);

            GameObject anchor = PrimitiveFactory.Empty("PatientAnchor", root,
                new Vector3(0f, TableHeight + 0.04f, 0f));
            PatientAnchor = anchor.transform;
        }

        private void BuildLights()
        {
            Transform root = PrimitiveFactory.Empty("SurgicalLights", transform).transform;

            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? 0.35f : -0.35f;
                Transform head = PrimitiveFactory.Empty("LightHead" + i, root, new Vector3(0f, 2.35f, z)).transform;

                PrimitiveFactory.Create(PrimitiveType.Cylinder, "Dish", head,
                    Vector3.zero, new Vector3(0.55f, 0.05f, 0.55f), MaterialLibrary.Steel, false);
                PrimitiveFactory.Create(PrimitiveType.Cylinder, "Lens", head,
                    new Vector3(0f, -0.05f, 0f), new Vector3(0.48f, 0.01f, 0.48f),
                    MaterialLibrary.GetEmissive("lens", new Color(1f, 0.98f, 0.93f)), false);
                PrimitiveFactory.Create(PrimitiveType.Cylinder, "Arm", head,
                    new Vector3(0f, 0.42f, 0f), new Vector3(0.05f, 0.42f, 0.05f), MaterialLibrary.Steel, false);

                var lightGo = new GameObject("Spot");
                lightGo.transform.SetParent(head, false);
                lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Spot;
                light.range = 5f;
                light.spotAngle = 62f;
                light.intensity = 4.2f;
                light.color = new Color(1f, 0.98f, 0.94f);
                light.shadows = LightShadows.Soft;
                _surgicalLights.Add(light);
            }

            // Room fill light.
            var fillGo = new GameObject("RoomFill");
            fillGo.transform.SetParent(transform, false);
            fillGo.transform.position = new Vector3(0f, 3f, 0f);
            fillGo.transform.rotation = Quaternion.Euler(60f, 25f, 0f);
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.85f, 0.89f, 0.95f);
            fill.shadows = LightShadows.None;
        }

        private void BuildAnesthesiaMachine()
        {
            Transform root = PrimitiveFactory.Empty("AnesthesiaMachine", transform,
                new Vector3(-0.95f, 0f, 1.5f)).transform;

            PrimitiveFactory.Create(PrimitiveType.Cube, "Cart", root,
                new Vector3(0f, 0.55f, 0f), new Vector3(0.6f, 1.1f, 0.5f), MaterialLibrary.Plastic);
            PrimitiveFactory.Create(PrimitiveType.Cube, "Screen", root,
                new Vector3(0f, 1.25f, -0.2f), new Vector3(0.45f, 0.32f, 0.05f),
                MaterialLibrary.GetEmissive("anes_screen", new Color(0.10f, 0.35f, 0.30f)));

            for (int i = 0; i < 3; i++)
            {
                PrimitiveFactory.Create(PrimitiveType.Cylinder, "Vaporiser" + i, root,
                    new Vector3(-0.18f + i * 0.18f, 1.02f, 0f), new Vector3(0.12f, 0.09f, 0.12f),
                    MaterialLibrary.Get("vaporiser", new Color(0.75f, 0.55f, 0.2f)), false);
            }

            PrimitiveFactory.Create(PrimitiveType.Cylinder, "O2Tank", root,
                new Vector3(0.38f, 0.35f, 0.2f), new Vector3(0.16f, 0.35f, 0.16f),
                MaterialLibrary.Get("o2tank", new Color(0.15f, 0.55f, 0.35f)));
        }

        private void BuildMonitors()
        {
            Transform root = PrimitiveFactory.Empty("Monitors", transform).transform;

            // Vitals monitor on a boom over the head of the table.
            Transform boom = PrimitiveFactory.Empty("VitalsBoom", root, new Vector3(-0.9f, 0f, 0.75f)).transform;
            PrimitiveFactory.Create(PrimitiveType.Cylinder, "Pole", boom,
                new Vector3(0f, 0.85f, 0f), new Vector3(0.06f, 0.85f, 0.06f), MaterialLibrary.Steel);
            GameObject screen = PrimitiveFactory.Create(PrimitiveType.Cube, "Screen", boom,
                new Vector3(0.12f, 1.55f, 0f), Quaternion.Euler(0f, -65f, 0f),
                new Vector3(0.62f, 0.42f, 0.04f), MaterialLibrary.Screen);
            MonitorAnchor = screen.transform;

            // Imaging display on the wall.
            Transform wall = PrimitiveFactory.Empty("ImagingWall", root, new Vector3(-2.4f, 0f, 2.6f)).transform;
            PrimitiveFactory.Create(PrimitiveType.Cube, "Frame", wall,
                new Vector3(0f, 1.7f, 0f), Quaternion.Euler(0f, -35f, 0f),
                new Vector3(1.1f, 0.78f, 0.06f), MaterialLibrary.DarkSteel);
            GameObject imaging = PrimitiveFactory.Create(PrimitiveType.Cube, "Display", wall,
                new Vector3(0f, 1.7f, -0.04f), Quaternion.Euler(0f, -35f, 0f),
                new Vector3(1.02f, 0.70f, 0.02f), MaterialLibrary.GetEmissive("imaging", new Color(0.06f, 0.09f, 0.14f)));
            ImagingAnchor = imaging.transform;

            // Laparoscopy stack.
            Transform stack = PrimitiveFactory.Empty("ScopeStack", root, new Vector3(1.9f, 0f, 1.6f)).transform;
            PrimitiveFactory.Create(PrimitiveType.Cube, "Cart", stack,
                new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1f, 0.45f), MaterialLibrary.DarkSteel);
            GameObject scopeScreen = PrimitiveFactory.Create(PrimitiveType.Cube, "Screen", stack,
                new Vector3(0f, 1.35f, 0f), Quaternion.Euler(0f, 200f, 0f),
                new Vector3(0.62f, 0.42f, 0.04f), MaterialLibrary.GetEmissive("scope", new Color(0.04f, 0.05f, 0.06f)));
            ScopeMonitorAnchor = scopeScreen.transform;
        }

        private void BuildInstrumentTables()
        {
            Transform root = PrimitiveFactory.Empty("InstrumentTables", transform,
                new Vector3(1.25f, 0f, -0.9f)).transform;

            PrimitiveFactory.Create(PrimitiveType.Cube, "MayoTop", root,
                new Vector3(0f, 0.98f, 0f), new Vector3(0.75f, 0.05f, 0.45f), MaterialLibrary.Steel);
            PrimitiveFactory.Create(PrimitiveType.Cylinder, "MayoPost", root,
                new Vector3(0f, 0.49f, 0f), new Vector3(0.07f, 0.49f, 0.07f), MaterialLibrary.Steel);

            // A few instrument silhouettes laid out on the tray.
            for (int i = 0; i < 6; i++)
            {
                PrimitiveFactory.Create(PrimitiveType.Cube, "Instrument" + i, root,
                    new Vector3(-0.28f + i * 0.11f, 1.02f, 0f),
                    Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f),
                    new Vector3(0.02f, 0.012f, 0.22f), MaterialLibrary.Steel, false);
            }

            // The back table.
            Transform back = PrimitiveFactory.Empty("BackTable", transform, new Vector3(2.6f, 0f, -0.2f)).transform;
            PrimitiveFactory.Create(PrimitiveType.Cube, "Top", back,
                new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.05f, 1.6f), MaterialLibrary.Steel);
            PrimitiveFactory.Create(PrimitiveType.Cube, "Skirt", back,
                new Vector3(0f, 0.45f, 0f), new Vector3(0.66f, 0.85f, 1.55f), MaterialLibrary.Drape, false);
        }

        private void BuildSupplies()
        {
            // Blood fridge / bag hanger.
            Transform hanger = PrimitiveFactory.Empty("BloodHanger", transform,
                new Vector3(-1.5f, 0f, -0.4f)).transform;
            PrimitiveFactory.Create(PrimitiveType.Cylinder, "Pole", hanger,
                new Vector3(0f, 0.9f, 0f), new Vector3(0.05f, 0.9f, 0.05f), MaterialLibrary.Steel);
            for (int i = 0; i < 3; i++)
            {
                PrimitiveFactory.Create(PrimitiveType.Cube, "BloodBag" + i, hanger,
                    new Vector3(-0.12f + i * 0.12f, 1.62f, 0f), new Vector3(0.10f, 0.18f, 0.04f),
                    MaterialLibrary.Get("bloodbag", new Color(0.42f, 0.05f, 0.08f)), false);
            }

            // Medication tray.
            Transform meds = PrimitiveFactory.Empty("MedicationTray", transform,
                new Vector3(-1.85f, 0f, 0.6f)).transform;
            PrimitiveFactory.Create(PrimitiveType.Cube, "Cart", meds,
                new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.9f, 0.4f), MaterialLibrary.Plastic);
            PrimitiveFactory.Create(PrimitiveType.Cube, "Tray", meds,
                new Vector3(0f, 0.93f, 0f), new Vector3(0.42f, 0.04f, 0.36f), MaterialLibrary.Steel);
            for (int i = 0; i < 5; i++)
            {
                PrimitiveFactory.Create(PrimitiveType.Cylinder, "Vial" + i, meds,
                    new Vector3(-0.14f + i * 0.07f, 0.98f, 0.06f), new Vector3(0.025f, 0.03f, 0.025f),
                    MaterialLibrary.Get("vial", new Color(0.85f, 0.88f, 0.9f)), false);
            }

            // Scrub sink by the door.
            Transform sink = PrimitiveFactory.Empty("ScrubSink", transform, new Vector3(3.2f, 0f, 2.6f)).transform;
            PrimitiveFactory.Create(PrimitiveType.Cube, "Basin", sink,
                new Vector3(0f, 0.85f, 0f), new Vector3(0.8f, 0.25f, 0.5f), MaterialLibrary.Steel);
            PrimitiveFactory.Create(PrimitiveType.Cylinder, "Tap", sink,
                new Vector3(0f, 1.1f, 0.18f), new Vector3(0.04f, 0.18f, 0.04f), MaterialLibrary.Steel);

            // Sharps/waste bins for set dressing.
            PrimitiveFactory.Create(PrimitiveType.Cube, "SharpsBin", transform,
                new Vector3(3.4f, 0.25f, 1.2f), new Vector3(0.3f, 0.5f, 0.3f),
                MaterialLibrary.Get("sharps", new Color(0.85f, 0.72f, 0.15f)));
        }

        private void BuildStaffStations()
        {
            // Positions where AI staff stand around the table.
            AddStation("Station_Anesthesiologist", new Vector3(-0.85f, 0f, 1.15f), 120f);
            AddStation("Station_ScrubNurse", new Vector3(0.95f, 0f, -0.75f), 250f);
            AddStation("Station_CirculatingNurse", new Vector3(-1.6f, 0f, -0.9f), 60f);
            AddStation("Station_SurgicalAssistant", new Vector3(-0.95f, 0f, 0.1f), 90f);
            AddStation("Station_Consultant", new Vector3(1.7f, 0f, 0.9f), 240f);
        }

        private void AddStation(string name, Vector3 position, float yaw)
        {
            GameObject go = PrimitiveFactory.Empty(name, transform, position);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _staffStations.Add(go.transform);
        }

        // ---- Runtime control --------------------------------------------------

        /// <summary>Power failure challenge: kills the surgical lights.</summary>
        public void SetLightsEnabled(bool enabled)
        {
            foreach (Light light in _surgicalLights)
            {
                if (light != null)
                {
                    light.enabled = enabled;
                }
            }

            RenderSettings.ambientLight = enabled
                ? new Color(0.33f, 0.35f, 0.38f)
                : new Color(0.06f, 0.07f, 0.09f);
        }

        /// <summary>Flickers the theatre lights (equipment malfunction / power failure).</summary>
        public void FlickerLights(float seconds)
        {
            StartCoroutine(FlickerRoutine(seconds));
        }

        private System.Collections.IEnumerator FlickerRoutine(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                SetLightsEnabled(Random.value > 0.45f);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.22f));
            }

            SetLightsEnabled(true);
        }

        /// <summary>
        /// Adds an interactable station: a trigger volume the player can walk through, marked by a
        /// translucent panel and a floor pad so it is findable across the room rather than being an
        /// invisible spot you have to know about.
        /// </summary>
        public InteractableStation AddInteractable(string name, Vector3 position, Vector3 size,
            string prompt, System.Action<GameObject> onInteract)
        {
            GameObject go = PrimitiveFactory.Create(PrimitiveType.Cube, name, transform, position, size,
                null);

            // The volume itself is invisible. An earlier version drew it as a translucent box with
            // a glowing band, which just put unexplained floating squares in the middle of the
            // theatre; the HUD now projects a marker onto whichever station the objective needs,
            // so the geometry only has to be a trigger.
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            // A flat pad on the floor is the only world-space cue: it reads as a place to stand
            // rather than as an object hanging in mid-air.
            // Kept as a sibling: parenting it under the non-uniformly scaled volume would skew it.
            PrimitiveFactory.Create(PrimitiveType.Cylinder, name + "_Pad", transform,
                new Vector3(position.x, 0.008f, position.z), new Vector3(0.55f, 0.004f, 0.55f),
                MaterialLibrary.GetTransparent("station_pad", new Color(0.25f, 0.8f, 0.9f, 0.22f)),
                false);

            var station = go.AddComponent<InteractableStation>();
            station.prompt = prompt;
            station.OnInteract = onInteract;
            return station;
        }
    }
}
