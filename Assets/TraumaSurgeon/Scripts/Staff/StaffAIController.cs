using TraumaSurgeon.Core;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Staff
{
    /// <summary>
    /// One member of the operating room team. Placeholder body built from primitives, with a
    /// simple idle animation, a station to stand at and a small set of behaviours it can be
    /// commanded into. Skill level (from hired/trained staff in career mode) affects how quickly
    /// and how well they carry out orders.
    /// </summary>
    public class StaffAIController : MonoBehaviour
    {
        public StaffRole Role = StaffRole.ScrubNurse;
        public string StaffName = "Staff";

        [Range(1, 10)] public int Skill = 3;

        /// <summary>Where this member normally stands.</summary>
        public Transform Station;

        public bool IsBusy { get; private set; }

        /// <summary>Set while this member is actively performing compressions.</summary>
        public bool PerformingCompressions { get; private set; }

        /// <summary>Set while this member is holding suction or retraction for the surgeon.</summary>
        public bool HoldingSuction { get; private set; }
        public bool HoldingRetraction { get; private set; }

        private Transform _body;
        private float _idlePhase;
        private float _busyTimer;

        /// <summary>Seconds a task takes, scaled by skill.</summary>
        public float TaskTime => Mathf.Lerp(3.2f, 0.9f, (Skill - 1) / 9f);

        public static StaffAIController Spawn(StaffRole role, string name, int skill, Transform station,
            Transform parent)
        {
            var go = new GameObject("Staff_" + role);
            go.transform.SetParent(parent, false);
            if (station != null)
            {
                go.transform.position = station.position;
                go.transform.rotation = station.rotation;
            }

            var staff = go.AddComponent<StaffAIController>();
            staff.Role = role;
            staff.StaffName = name;
            staff.Skill = Mathf.Clamp(skill, 1, 10);
            staff.Station = station;
            staff.BuildBody();
            return staff;
        }

        private void BuildBody()
        {
            Color gown = ColorForRole(Role);
            _body = PrimitiveFactory.Empty("Body", transform).transform;

            PrimitiveFactory.Create(PrimitiveType.Capsule, "Torso", _body,
                new Vector3(0f, 1.05f, 0f), new Vector3(0.42f, 0.52f, 0.28f),
                MaterialLibrary.Get("gown_" + Role, gown), false);

            PrimitiveFactory.Create(PrimitiveType.Sphere, "Head", _body,
                new Vector3(0f, 1.62f, 0f), new Vector3(0.20f, 0.24f, 0.21f),
                MaterialLibrary.Get("cap_" + Role, gown * 0.85f), false);

            PrimitiveFactory.Create(PrimitiveType.Cube, "Mask", _body,
                new Vector3(0f, 1.56f, 0.09f), new Vector3(0.15f, 0.08f, 0.04f),
                MaterialLibrary.Get("mask", new Color(0.85f, 0.88f, 0.9f)), false);

            PrimitiveFactory.Create(PrimitiveType.Capsule, "LegL", _body,
                new Vector3(-0.10f, 0.42f, 0f), new Vector3(0.16f, 0.42f, 0.16f),
                MaterialLibrary.Get("scrubs_" + Role, gown * 0.9f), false);
            PrimitiveFactory.Create(PrimitiveType.Capsule, "LegR", _body,
                new Vector3(0.10f, 0.42f, 0f), new Vector3(0.16f, 0.42f, 0.16f),
                MaterialLibrary.Get("scrubs_" + Role, gown * 0.9f), false);

            PrimitiveFactory.Create(PrimitiveType.Capsule, "ArmL", _body,
                new Vector3(-0.26f, 1.12f, 0.08f), Quaternion.Euler(-40f, 0f, -12f),
                new Vector3(0.13f, 0.30f, 0.13f), MaterialLibrary.Get("gown_" + Role, gown), false);
            PrimitiveFactory.Create(PrimitiveType.Capsule, "ArmR", _body,
                new Vector3(0.26f, 1.12f, 0.08f), Quaternion.Euler(-40f, 0f, 12f),
                new Vector3(0.13f, 0.30f, 0.13f), MaterialLibrary.Get("gown_" + Role, gown), false);

            _idlePhase = Random.value * 10f;
        }

        private static Color ColorForRole(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Anesthesiologist: return new Color(0.30f, 0.45f, 0.62f);
                case StaffRole.ScrubNurse: return new Color(0.24f, 0.52f, 0.48f);
                case StaffRole.CirculatingNurse: return new Color(0.42f, 0.48f, 0.58f);
                case StaffRole.SurgicalAssistant: return new Color(0.26f, 0.42f, 0.52f);
                default: return new Color(0.35f, 0.38f, 0.48f);
            }
        }

        private void Update()
        {
            AnimateIdle();

            if (_busyTimer > 0f)
            {
                _busyTimer -= Time.deltaTime;
                if (_busyTimer <= 0f)
                {
                    IsBusy = false;
                }
            }
        }

        private void AnimateIdle()
        {
            if (_body == null)
            {
                return;
            }

            _idlePhase += Time.deltaTime * (PerformingCompressions ? 9f : 1.4f);
            float amplitude = PerformingCompressions ? 0.05f : 0.012f;
            float bob = Mathf.Sin(_idlePhase) * amplitude;
            _body.localPosition = new Vector3(0f, bob, PerformingCompressions ? 0.12f : 0f);
            _body.localRotation = Quaternion.Euler(PerformingCompressions ? 18f : Mathf.Sin(_idlePhase * 0.5f) * 2f,
                0f, 0f);
        }

        /// <summary>Speaks a line and marks the member busy for a skill-scaled interval.</summary>
        public void Say(string line, bool busy = false)
        {
            GameEvents.RaiseStaffSpeech(Role, line);
            if (busy)
            {
                IsBusy = true;
                _busyTimer = TaskTime;
            }
        }

        public void SetCompressions(bool active)
        {
            PerformingCompressions = active;
        }

        public void SetSuction(bool active)
        {
            HoldingSuction = active;
        }

        public void SetRetraction(bool active)
        {
            HoldingRetraction = active;
        }

        /// <summary>Chance this member performs a task cleanly, based on skill.</summary>
        public float Competence => Mathf.Lerp(0.45f, 1f, (Skill - 1) / 9f);
    }
}
