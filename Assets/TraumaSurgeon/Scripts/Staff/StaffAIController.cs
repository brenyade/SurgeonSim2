using TraumaSurgeon.Core;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Staff
{
    /// <summary>
    /// One member of the operating room team. The figure is built by
    /// <see cref="HumanoidFigureBuilder"/> - a proportioned humanoid assembled from primitives, or
    /// a supplied model prefab when one is present - and animated at the joints here.
    ///
    /// Skill level (from hired/trained staff in career mode) affects how quickly and how well they
    /// carry out orders.
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

        private FigureRig _rig;
        private float _idlePhase;
        private float _breathPhase;
        private float _busyTimer;
        private float _compressionBlend;
        private Quaternion _chestRest;
        private Quaternion _headRest;

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
            staff.BuildFigure();
            return staff;
        }

        private void BuildFigure()
        {
            HumanoidStyle style = StyleForRole(Role);
            _rig = HumanoidFigureBuilder.Build(transform, "Staff_" + Role, style);

            if (_rig.Chest != null)
            {
                _chestRest = _rig.Chest.localRotation;
            }

            if (_rig.Head != null)
            {
                _headRest = _rig.Head.localRotation;
            }

            _idlePhase = Random.value * 10f;
            _breathPhase = Random.value * 10f;
        }

        private static HumanoidStyle StyleForRole(StaffRole role)
        {
            HumanoidStyle style;

            switch (role)
            {
                case StaffRole.Anesthesiologist:
                    style = HumanoidStyle.Default(new Color(0.30f, 0.45f, 0.62f));
                    style.Cap = new Color(0.22f, 0.34f, 0.48f);
                    break;
                case StaffRole.ScrubNurse:
                    style = HumanoidStyle.Default(new Color(0.24f, 0.52f, 0.48f));
                    style.Cap = new Color(0.18f, 0.40f, 0.37f);
                    break;
                case StaffRole.CirculatingNurse:
                    style = HumanoidStyle.Default(new Color(0.42f, 0.48f, 0.58f));
                    style.Cap = new Color(0.32f, 0.37f, 0.46f);
                    break;
                case StaffRole.SurgicalAssistant:
                    style = HumanoidStyle.Default(new Color(0.26f, 0.42f, 0.52f));
                    style.Cap = new Color(0.20f, 0.33f, 0.41f);
                    break;
                default:
                    style = HumanoidStyle.Default(new Color(0.35f, 0.38f, 0.48f));
                    style.Cap = new Color(0.27f, 0.29f, 0.38f);
                    break;
            }

            // A little variation so the team does not look like five copies of one person.
            style.Height = Random.Range(1.66f, 1.84f);
            float tone = Random.Range(0.55f, 0.86f);
            style.Skin = new Color(tone, tone * 0.78f, tone * 0.66f);
            return style;
        }

        private void Update()
        {
            Animate();

            if (_busyTimer > 0f)
            {
                _busyTimer -= Time.deltaTime;
                if (_busyTimer <= 0f)
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Joint-level idle: a breathing chest, a slow weight shift, and a bow into the patient
        /// with pumping arms during compressions.
        /// </summary>
        private void Animate()
        {
            if (_rig == null || _rig.Root == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            _compressionBlend = Mathf.MoveTowards(_compressionBlend, PerformingCompressions ? 1f : 0f, dt * 4f);

            // A supplied model may carry its own Animator; leave it alone beyond the weight shift.
            if (_rig.IsExternalModel)
            {
                _idlePhase += dt * 1.2f;
                _rig.Root.localRotation = Quaternion.Euler(0f, Mathf.Sin(_idlePhase) * 1.5f, 0f);
                return;
            }

            _breathPhase += dt * (PerformingCompressions ? 3.2f : 1.1f);
            _idlePhase += dt * 1.2f;

            if (_rig.Chest != null)
            {
                // Breathing: a small vertical swell rather than the whole body bobbing.
                float breath = Mathf.Sin(_breathPhase) * 0.012f;
                _rig.Chest.localScale = new Vector3(1f, 1f + breath, 1f + breath * 0.6f);

                // Bow forward over the patient while compressing.
                float bow = Mathf.Lerp(0f, 26f, _compressionBlend);
                float pump = _compressionBlend * Mathf.Sin(Time.time * 11f) * 7f;
                _rig.Chest.localRotation = _chestRest * Quaternion.Euler(bow + pump, 0f, 0f);
            }

            if (_rig.Head != null)
            {
                // Look down at the field, more so when working.
                float lookDown = Mathf.Lerp(12f, 34f, _compressionBlend);
                float glance = Mathf.Sin(_idlePhase * 0.7f) * 3f * (1f - _compressionBlend);
                _rig.Head.localRotation = _headRest * Quaternion.Euler(lookDown, glance, 0f);
            }

            // Arms drive down into the chest on each compression.
            float armPump = _compressionBlend * (Mathf.Sin(Time.time * 11f) * 0.5f + 0.5f) * 22f;
            AnimateArm(_rig.ShoulderL, _rig.ForearmL, armPump);
            AnimateArm(_rig.ShoulderR, _rig.ForearmR, armPump);

            // Subtle weight shift so nobody stands perfectly frozen.
            float sway = Mathf.Sin(_idlePhase) * 1.4f * (1f - _compressionBlend);
            _rig.Root.localRotation = Quaternion.Euler(0f, sway, 0f);
        }

        private void AnimateArm(Transform shoulder, Transform forearm, float pump)
        {
            if (shoulder == null)
            {
                return;
            }

            Vector3 euler = shoulder.localEulerAngles;
            shoulder.localRotation = Quaternion.Euler(28f + pump, euler.y, euler.z);

            if (forearm != null)
            {
                forearm.localRotation = Quaternion.Euler(-62f + pump * 0.4f, 0f, 0f);
            }
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
