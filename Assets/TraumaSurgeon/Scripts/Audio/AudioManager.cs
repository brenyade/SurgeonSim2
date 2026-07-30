using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.Patient;
using UnityEngine;

namespace TraumaSurgeon.Audio
{
    /// <summary>
    /// Central sound service. Lazily builds procedural clips (or loads a user-supplied .wav of the
    /// same name from Resources), plays one-shots on a small voice pool, manages looping beds and
    /// drives the heart monitor / alarm logic from the patient's vitals.
    /// </summary>
    public class AudioManager : MonoSingleton<AudioManager>
    {
        private const int VoiceCount = 8;

        private readonly Dictionary<SoundId, AudioClip> _clips = new Dictionary<SoundId, AudioClip>();
        private readonly Dictionary<SoundId, AudioSource> _loops = new Dictionary<SoundId, AudioSource>();
        private AudioSource[] _voices;
        private int _voiceIndex;
        private AudioSource _uiSource;

        private PatientController _patient;
        private float _beepTimer;
        private float _alarmTimer;
        private bool _monitorEnabled;

        public float SfxVolume { get; set; } = 0.9f;
        public float AmbienceVolume { get; set; } = 0.6f;

        protected override void OnSingletonAwake()
        {
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var go = new GameObject($"Voice{i}");
                go.transform.SetParent(transform, false);
                _voices[i] = go.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
                _voices[i].spatialBlend = 0f;
            }

            var uiGo = new GameObject("UISource");
            uiGo.transform.SetParent(transform, false);
            _uiSource = uiGo.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;

            SettingsManager.SettingsChanged += OnSettingsChanged;
            if (SettingsManager.Exists)
            {
                OnSettingsChanged(SettingsManager.Instance.Data);
            }
        }

        protected override void OnDestroy()
        {
            SettingsManager.SettingsChanged -= OnSettingsChanged;
            base.OnDestroy();
        }

        private void OnSettingsChanged(SettingsData data)
        {
            SfxVolume = data.sfxVolume;
            AmbienceVolume = data.ambienceVolume;
            foreach (KeyValuePair<SoundId, AudioSource> pair in _loops)
            {
                if (pair.Value != null)
                {
                    pair.Value.volume = pair.Key == SoundId.Ambience ? AmbienceVolume * 0.5f : SfxVolume * 0.5f;
                }
            }
        }

        public AudioClip GetClip(SoundId id)
        {
            if (_clips.TryGetValue(id, out AudioClip clip) && clip != null)
            {
                return clip;
            }

            // Allow drop-in replacement with real audio assets.
            AudioClip loaded = Resources.Load<AudioClip>("TraumaSurgeonAudio/" + id);
            clip = loaded != null ? loaded : ProceduralAudio.Create(id);
            _clips[id] = clip;
            return clip;
        }

        public void Play(SoundId id, float volumeScale = 1f, float pitch = 1f)
        {
            AudioClip clip = GetClip(id);
            if (clip == null || _voices == null)
            {
                return;
            }

            AudioSource source = _voices[_voiceIndex];
            _voiceIndex = (_voiceIndex + 1) % VoiceCount;
            source.clip = clip;
            source.volume = SfxVolume * volumeScale;
            source.pitch = pitch;
            source.spatialBlend = 0f;
            source.loop = false;
            source.Play();
        }

        public void PlayUI(SoundId id, float volumeScale = 1f)
        {
            AudioClip clip = GetClip(id);
            if (clip == null || _uiSource == null)
            {
                return;
            }

            _uiSource.pitch = 1f;
            _uiSource.PlayOneShot(clip, SfxVolume * volumeScale);
        }

        /// <summary>Plays a positioned one-shot (instrument sounds at the surgical field).</summary>
        public void PlayAt(SoundId id, Vector3 position, float volumeScale = 1f, float pitch = 1f)
        {
            AudioClip clip = GetClip(id);
            if (clip == null)
            {
                return;
            }

            AudioSource source = _voices[_voiceIndex];
            _voiceIndex = (_voiceIndex + 1) % VoiceCount;
            source.transform.position = position;
            source.clip = clip;
            source.spatialBlend = 0.6f;
            source.volume = SfxVolume * volumeScale;
            source.pitch = pitch;
            source.loop = false;
            source.Play();
        }

        public void StartLoop(SoundId id, float volumeScale = 1f)
        {
            if (_loops.TryGetValue(id, out AudioSource existing) && existing != null)
            {
                if (!existing.isPlaying)
                {
                    existing.Play();
                }

                return;
            }

            var go = new GameObject("Loop_" + id);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = GetClip(id);
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = (id == SoundId.Ambience ? AmbienceVolume : SfxVolume) * volumeScale;
            source.Play();
            _loops[id] = source;
        }

        public void StopLoop(SoundId id)
        {
            if (_loops.TryGetValue(id, out AudioSource source) && source != null)
            {
                source.Stop();
            }
        }

        public bool IsLooping(SoundId id)
        {
            return _loops.TryGetValue(id, out AudioSource source) && source != null && source.isPlaying;
        }

        // ---- Monitor driving --------------------------------------------------

        /// <summary>Hooks the heart monitor and alarms up to a patient.</summary>
        public void AttachMonitor(PatientController patient)
        {
            _patient = patient;
            _monitorEnabled = patient != null;
            _beepTimer = 0f;
            if (_monitorEnabled)
            {
                StartLoop(SoundId.Ventilator, 0.35f);
            }
        }

        public void DetachMonitor()
        {
            _monitorEnabled = false;
            _patient = null;
            StopLoop(SoundId.Ventilator);
            StopLoop(SoundId.Suction);
            StopLoop(SoundId.Cautery);
            StopLoop(SoundId.Drill);
            StopLoop(SoundId.BoneSaw);
            StopLoop(SoundId.Irrigation);
        }

        private void Update()
        {
            if (!_monitorEnabled || _patient == null)
            {
                return;
            }

            VitalSigns v = _patient.Vitals;

            // Heart monitor beep, rate driven by the actual heart rate.
            if (v.rhythm == CardiacRhythm.Asystole || !v.isAlive)
            {
                if (!IsLooping(SoundId.Flatline))
                {
                    StartLoop(SoundId.Flatline, 0.5f);
                }
            }
            else
            {
                StopLoop(SoundId.Flatline);
                float interval = 60f / Mathf.Max(20f, v.heartRate);
                _beepTimer += Time.deltaTime;
                if (_beepTimer >= interval)
                {
                    _beepTimer = 0f;
                    bool critical = v.oxygenSaturation < 90f || v.MeanArterialPressure < 60f;
                    Play(critical ? SoundId.MonitorBeepCritical : SoundId.MonitorBeep, 0.35f);
                }
            }

            // Alarms, rate-limited so they nag rather than scream.
            _alarmTimer -= Time.deltaTime;
            if (_alarmTimer <= 0f)
            {
                if (v.oxygenSaturation < 90f)
                {
                    Play(SoundId.OxygenAlarm, 0.45f);
                    _alarmTimer = 4f;
                }
                else if (v.MeanArterialPressure < 60f)
                {
                    Play(SoundId.BloodPressureAlarm, 0.45f);
                    _alarmTimer = 5f;
                }
            }
        }
    }
}
