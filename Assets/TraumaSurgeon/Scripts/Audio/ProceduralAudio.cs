using UnityEngine;

namespace TraumaSurgeon.Audio
{
    /// <summary>
    /// Generates every placeholder clip in code: tones, noise beds and simple envelopes.
    /// Keeps the repository free of binary audio while still giving each action a distinct cue.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;

        /// <summary>Builds the clip for a given cue.</summary>
        public static AudioClip Create(SoundId id)
        {
            switch (id)
            {
                case SoundId.MonitorBeep:
                    return Tone("beep", 1046f, 0.09f, 0.35f, 0.004f, 0.05f);
                case SoundId.MonitorBeepCritical:
                    return Tone("beep_crit", 1318f, 0.07f, 0.4f, 0.002f, 0.03f);
                case SoundId.Flatline:
                    return Tone("flatline", 880f, 2.2f, 0.32f, 0.01f, 0.4f);
                case SoundId.OxygenAlarm:
                    return Warble("alarm_o2", 700f, 1000f, 0.9f, 0.30f, 6f);
                case SoundId.BloodPressureAlarm:
                    return Warble("alarm_bp", 520f, 780f, 0.9f, 0.28f, 4f);
                case SoundId.Heartbeat:
                    return Heartbeat();
                case SoundId.Incision:
                    return FilteredNoise("incision", 0.30f, 0.22f, 0.55f, 0.02f);
                case SoundId.ScissorsCut:
                    return FilteredNoise("scissors", 0.16f, 0.28f, 0.8f, 0.004f);
                case SoundId.ClampClick:
                    return Click("clamp", 2100f, 0.07f, 0.3f);
                case SoundId.Suction:
                    return FilteredNoise("suction", 1.0f, 0.20f, 0.25f, 0.06f, loop: true);
                case SoundId.Cautery:
                    return Buzz("cautery", 220f, 0.6f, 0.22f, loop: true);
                case SoundId.Drill:
                    return Buzz("drill", 130f, 0.8f, 0.26f, loop: true, noiseMix: 0.45f);
                case SoundId.BoneSaw:
                    return Buzz("saw", 95f, 0.8f, 0.30f, loop: true, noiseMix: 0.7f);
                case SoundId.StaplerFire:
                    return Click("stapler", 1500f, 0.10f, 0.4f);
                case SoundId.SutureStitch:
                    return FilteredNoise("suture", 0.14f, 0.16f, 0.45f, 0.01f);
                case SoundId.DefibCharge:
                    return Sweep("defib_charge", 300f, 1800f, 1.6f, 0.22f);
                case SoundId.DefibShock:
                    return Click("defib_shock", 160f, 0.35f, 0.5f);
                case SoundId.Irrigation:
                    return FilteredNoise("irrigation", 0.9f, 0.18f, 0.35f, 0.05f, loop: true);
                case SoundId.ToolPickup:
                    return Click("pickup", 1800f, 0.06f, 0.22f);
                case SoundId.ToolDrop:
                    return Click("drop", 900f, 0.10f, 0.22f);
                case SoundId.UiClick:
                    return Tone("ui_click", 1400f, 0.05f, 0.18f, 0.002f, 0.03f);
                case SoundId.UiHover:
                    return Tone("ui_hover", 900f, 0.04f, 0.10f, 0.002f, 0.02f);
                case SoundId.ObjectiveComplete:
                    return Arpeggio("objective", new[] { 784f, 988f, 1318f }, 0.10f, 0.26f);
                case SoundId.SuccessChime:
                    return Arpeggio("success", new[] { 523f, 659f, 784f, 1046f }, 0.13f, 0.30f);
                case SoundId.FailureBuzz:
                    return Arpeggio("failure", new[] { 330f, 262f, 196f }, 0.18f, 0.30f);
                case SoundId.Warning:
                    return Warble("warning", 640f, 480f, 0.5f, 0.26f, 8f);
                case SoundId.Ambience:
                    return FilteredNoise("ambience", 4f, 0.05f, 0.08f, 1.5f, loop: true);
                case SoundId.Announcement:
                    return Warble("announce", 440f, 560f, 0.8f, 0.18f, 3f);
                case SoundId.Ventilator:
                    return Ventilator();
                default:
                    return Tone("default", 660f, 0.1f, 0.2f, 0.005f, 0.05f);
            }
        }

        // ---- Generators -------------------------------------------------------

        private static AudioClip Tone(string name, float frequency, float duration, float volume,
            float attack, float release)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Envelope(t, duration, attack, release);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * volume;
            }

            return Build(name, data, false);
        }

        private static AudioClip Warble(string name, float freqA, float freqB, float duration, float volume,
            float rate)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float f = Mathf.Lerp(freqA, freqB, Mathf.Repeat(t * rate, 1f) > 0.5f ? 1f : 0f);
                float env = Envelope(t, duration, 0.01f, 0.05f);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * volume;
            }

            return Build(name, data, false);
        }

        private static AudioClip Sweep(string name, float startFreq, float endFreq, float duration, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                float f = Mathf.Lerp(startFreq, endFreq, progress * progress);
                phase += 2f * Mathf.PI * f / SampleRate;
                float env = Envelope(t, duration, 0.05f, 0.1f);
                data[i] = Mathf.Sin(phase) * env * volume;
            }

            return Build(name, data, false);
        }

        private static AudioClip Buzz(string name, float frequency, float duration, float volume,
            bool loop = false, float noiseMix = 0.2f)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            var rng = new System.Random(name.GetHashCode());
            float last = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                // Square-ish tone plus filtered noise for grit.
                float saw = Mathf.Repeat(t * frequency, 1f) * 2f - 1f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, noise, 0.35f);
                float env = loop ? 1f : Envelope(t, duration, 0.02f, 0.05f);
                data[i] = (saw * (1f - noiseMix) + last * noiseMix) * env * volume;
            }

            if (loop)
            {
                CrossfadeLoop(data);
            }

            return Build(name, data, loop);
        }

        private static AudioClip FilteredNoise(string name, float duration, float volume, float brightness,
            float attack, bool loop = false)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            var rng = new System.Random(name.GetHashCode());
            float last = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, noise, Mathf.Clamp01(brightness));
                float env = loop ? 1f : Envelope(t, duration, attack, duration * 0.4f);
                data[i] = last * env * volume;
            }

            if (loop)
            {
                CrossfadeLoop(data);
            }

            return Build(name, data, loop);
        }

        private static AudioClip Click(string name, float frequency, float duration, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float decay = Mathf.Exp(-t * 45f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * decay * volume;
            }

            return Build(name, data, false);
        }

        private static AudioClip Arpeggio(string name, float[] frequencies, float noteDuration, float volume)
        {
            int noteSamples = Mathf.RoundToInt(SampleRate * noteDuration);
            var data = new float[noteSamples * frequencies.Length];
            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Envelope(t, noteDuration, 0.005f, noteDuration * 0.6f);
                    data[n * noteSamples + i] = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t) * env * volume;
                }
            }

            return Build(name, data, false);
        }

        private static AudioClip Heartbeat()
        {
            float duration = 0.55f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float lub = Mathf.Exp(-Mathf.Pow((t - 0.05f) * 26f, 2f));
                float dub = Mathf.Exp(-Mathf.Pow((t - 0.24f) * 22f, 2f)) * 0.7f;
                float tone = Mathf.Sin(2f * Mathf.PI * 58f * t);
                data[i] = tone * (lub + dub) * 0.4f;
            }

            return Build("heartbeat", data, false);
        }

        private static AudioClip Ventilator()
        {
            float duration = 4f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            var data = new float[samples];
            var rng = new System.Random(7);
            float last = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float cycle = Mathf.Repeat(t, 4f) / 4f;
                float breath = cycle < 0.35f
                    ? Mathf.Sin(cycle / 0.35f * Mathf.PI)
                    : (cycle < 0.7f ? Mathf.Sin((cycle - 0.35f) / 0.35f * Mathf.PI) * 0.6f : 0f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, noise, 0.06f);
                data[i] = last * breath * 0.35f;
            }

            CrossfadeLoop(data);
            return Build("ventilator", data, true);
        }

        // ---- Helpers ----------------------------------------------------------

        private static float Envelope(float t, float duration, float attack, float release)
        {
            if (attack > 0f && t < attack)
            {
                return t / attack;
            }

            float releaseStart = Mathf.Max(0f, duration - release);
            if (release > 0f && t > releaseStart)
            {
                return Mathf.Clamp01(1f - (t - releaseStart) / release);
            }

            return 1f;
        }

        /// <summary>Smooths the seam so looping beds do not click.</summary>
        private static void CrossfadeLoop(float[] data)
        {
            int fade = Mathf.Min(2000, data.Length / 4);
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                data[i] = Mathf.Lerp(data[data.Length - fade + i], data[i], k);
            }
        }

        private static AudioClip Build(string name, float[] data, bool loop)
        {
            AudioClip clip = AudioClip.Create("TS_" + name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
