using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Patient;
using UnityEngine;

namespace TraumaSurgeon.Visuals
{
    /// <summary>
    /// Dynamic blood and fluid effects, kept deliberately clinical: a pooling film over the
    /// surgical field that grows with the blood in the wound, and a small particle emitter that
    /// pulses at whichever bleeders are currently uncontrolled.
    ///
    /// Everything is driven from <see cref="BloodLossSystem.PooledBloodMl"/> so the visuals and the
    /// simulation can never disagree.
    /// </summary>
    public class BloodEffectSystem : MonoBehaviour
    {
        private const float FullPoolMl = 420f;

        private PatientController _patient;
        private Transform _pool;
        private Material _poolMaterial;
        private ParticleSystem _spray;
        private float _sprayTimer;

        private readonly List<BleedingPoint> _uncontrolled = new List<BleedingPoint>();
        private float _rescanTimer;

        public static BloodEffectSystem Attach(PatientController patient)
        {
            if (patient == null)
            {
                return null;
            }

            var go = new GameObject("BloodEffects");
            go.transform.SetParent(patient.transform, false);
            var system = go.AddComponent<BloodEffectSystem>();
            system.Initialise(patient);
            return system;
        }

        private void Initialise(PatientController patient)
        {
            _patient = patient;
            BuildPool();
            BuildSpray();
        }

        /// <summary>Thin disc lying in the wound; alpha and radius track the pooled volume.</summary>
        private void BuildPool()
        {
            _poolMaterial = MaterialLibrary.CreateInstance(new Color(0.42f, 0.04f, 0.06f, 0f), 0f, 0.75f);
            MakeTransparent(_poolMaterial);

            GameObject pool = PrimitiveFactory.Create(
                PrimitiveType.Cylinder,
                "BloodPool",
                transform,
                new Vector3(0f, 0.155f, 0.24f),
                new Vector3(0.30f, 0.002f, 0.30f),
                _poolMaterial,
                false);

            _pool = pool.transform;
            _pool.gameObject.SetActive(false);
        }

        private void BuildSpray()
        {
            var go = new GameObject("BloodSpray");
            go.transform.SetParent(transform, false);

            _spray = go.AddComponent<ParticleSystem>();
            _spray.Stop();

            ParticleSystem.MainModule main = _spray.main;
            main.startLifetime = 0.45f;
            main.startSpeed = 0.35f;
            main.startSize = 0.008f;
            main.startColor = new Color(0.52f, 0.05f, 0.07f);
            main.gravityModifier = 1.1f;
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = _spray.emission;
            emission.enabled = false;   // emitted manually in bursts

            ParticleSystem.ShapeModule shape = _spray.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f;
            shape.radius = 0.004f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = MaterialLibrary.GetEmissive("bloodparticle", new Color(0.52f, 0.05f, 0.07f));
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void Update()
        {
            if (_patient == null || _patient.BloodLoss == null)
            {
                return;
            }

            UpdatePool();
            UpdateSpray();
        }

        private void UpdatePool()
        {
            float fill = Mathf.Clamp01(_patient.BloodLoss.PooledBloodMl / FullPoolMl);

            if (fill <= 0.01f)
            {
                if (_pool.gameObject.activeSelf)
                {
                    _pool.gameObject.SetActive(false);
                }

                return;
            }

            if (!_pool.gameObject.activeSelf)
            {
                _pool.gameObject.SetActive(true);
            }

            float radius = Mathf.Lerp(0.06f, 0.30f, fill);
            _pool.localScale = new Vector3(radius, 0.002f, radius);
            MaterialLibrary.SetColor(_poolMaterial,
                new Color(0.42f, 0.04f, 0.06f, Mathf.Lerp(0.15f, 0.85f, fill)));
        }

        private void UpdateSpray()
        {
            _rescanTimer -= Time.deltaTime;
            if (_rescanTimer <= 0f)
            {
                _rescanTimer = 0.75f;
                RescanBleeders();
            }

            if (_uncontrolled.Count == 0 || _spray == null)
            {
                return;
            }

            _sprayTimer -= Time.deltaTime;
            if (_sprayTimer > 0f)
            {
                return;
            }

            _sprayTimer = 0.12f;

            BleedingPoint source = _uncontrolled[Random.Range(0, _uncontrolled.Count)];
            if (source == null)
            {
                return;
            }

            // A brisk bleeder throws more particles than a slow ooze.
            int count = Mathf.Clamp(Mathf.RoundToInt(source.rate * 1.5f), 1, 8);
            _spray.transform.position = source.transform.position;
            _spray.transform.rotation = Quaternion.LookRotation(Vector3.up);
            _spray.Emit(count);
        }

        private void RescanBleeders()
        {
            _uncontrolled.Clear();
            if (_patient.Body == null)
            {
                return;
            }

            foreach (AnatomyPart part in _patient.Body.AllParts)
            {
                if (part.isRemoved)
                {
                    continue;
                }

                foreach (BleedingPoint bp in part.BleedingPoints)
                {
                    if (bp != null && !bp.IsControlled)
                    {
                        _uncontrolled.Add(bp);
                    }
                }
            }
        }

        private static void MakeTransparent(Material mat)
        {
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3f);
            }

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
            }

            if (mat.HasProperty("_SrcBlend"))
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (mat.HasProperty("_DstBlend"))
            {
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (mat.HasProperty("_ZWrite"))
            {
                mat.SetInt("_ZWrite", 0);
            }

            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
