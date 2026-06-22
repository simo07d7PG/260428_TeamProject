using System.Collections.Generic;
using UnityEngine;

/// <summary>런타임에 절차적으로 합성한 효과음 클립을 제공하는 유틸리티.</summary>
public static class ProceduralAudioUtility
{
    const int SampleRate = 44100;

    static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

    public static AudioClip Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (_cache.TryGetValue(key, out AudioClip cached))
            return cached;

        AudioClip clip = Build(key);
        if (clip != null)
            _cache[key] = clip;

        return clip;
    }

    static AudioClip Build(string key)
    {
        switch (key)
        {
            case "coin":
                return BuildCoin();
            case "merge_success":
                return BuildMergeSuccess();
            case "serve_fail":
                return BuildServeFail();
            case "customer_arrive":
                return BuildCustomerArrive();
            case "customer_leave":
                return BuildCustomerLeave();
            case "drink_complete":
                return BuildDrinkComplete();
            case "cup_take":
                return BuildTick(880f, 0.06f);
            case "ice_add":
                return BuildTick(1500f, 0.05f);
            case "topping_add":
                return BuildTick(520f, 0.08f);
            case "lid_close":
                return BuildNoise(0.07f, 0.4f);
            case "shot_extract":
                return BuildNoise(0.45f, 0.22f);
            case "milk_pour":
                return BuildNoise(0.30f, 0.18f);
            case "syrup_drop":
                return BuildBlip(900f, 520f, 0.09f);
            default:
                return null;
        }
    }

    static AudioClip BuildTick(float freq, float duration)
    {
        int total = SecondsToSamples(duration);
        float[] data = new float[total];
        WriteTone(data, 0, total, freq, 0.5f, WaveType.Sine);
        ApplyEnvelope(data, 0.003f, duration * 0.6f);
        return MakeClip("sfx_tick", data);
    }

    static AudioClip BuildNoise(float duration, float amp)
    {
        int total = SecondsToSamples(duration);
        float[] data = new float[total];
        uint seed = 987654u;
        for (int i = 0; i < total; i++)
            data[i] = NextNoise(ref seed) * amp;
        ApplyEnvelope(data, 0.01f, duration * 0.5f);
        return MakeClip("sfx_noise", data);
    }

    static AudioClip BuildBlip(float startFreq, float endFreq, float duration)
    {
        int total = SecondsToSamples(duration);
        float[] data = new float[total];
        float phase = 0f;
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)total;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += freq / SampleRate;
            if (phase >= 1f) phase -= 1f;
            data[i] = Mathf.Sin(phase * 2f * Mathf.PI) * 0.45f;
        }
        ApplyEnvelope(data, 0.004f, duration * 0.5f);
        return MakeClip("sfx_blip", data);
    }

    static AudioClip BuildCoin()
    {
        int total = SecondsToSamples(0.16f);
        float[] data = new float[total];

        int note0 = SecondsToSamples(0.07f);
        WriteTone(data, 0, note0, 988f, 0.5f, WaveType.Sine);
        WriteTone(data, note0, total - note0, 1319f, 0.55f, WaveType.Sine);

        ApplyEnvelope(data, 0.005f, 0.04f);
        return MakeClip("sfx_coin", data);
    }

    static AudioClip BuildMergeSuccess()
    {
        int total = SecondsToSamples(0.42f);
        float[] data = new float[total];

        int seg = SecondsToSamples(0.12f);
        WriteChime(data, 0, seg, 1047f, 0.45f);
        WriteChime(data, seg, seg, 1319f, 0.45f);
        WriteChime(data, seg * 2, total - seg * 2, 1568f, 0.5f);

        ApplyEnvelope(data, 0.004f, 0.12f);
        return MakeClip("sfx_merge_success", data);
    }

    static AudioClip BuildServeFail()
    {
        int total = SecondsToSamples(0.34f);
        float[] data = new float[total];

        float startFreq = 220f;
        float endFreq = 110f;
        float phase = 0f;
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)total;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += freq / SampleRate;
            if (phase >= 1f) phase -= 1f;
            float saw = (phase * 2f) - 1f;
            data[i] = saw * 0.4f;
        }

        ApplyEnvelope(data, 0.006f, 0.08f);
        return MakeClip("sfx_serve_fail", data);
    }

    static AudioClip BuildCustomerArrive()
    {
        int total = SecondsToSamples(0.40f);
        float[] data = new float[total];

        int note0 = SecondsToSamples(0.18f);
        WriteTone(data, 0, note0, 1319f, 0.5f, WaveType.Sine);
        WriteTone(data, note0, total - note0, 988f, 0.5f, WaveType.Sine);

        ApplyEnvelope(data, 0.005f, 0.10f);
        return MakeClip("sfx_customer_arrive", data);
    }

    static AudioClip BuildCustomerLeave()
    {
        int total = SecondsToSamples(0.36f);
        float[] data = new float[total];

        float startFreq = 784f;
        float endFreq = 392f;
        float phase = 0f;
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)total;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += freq / SampleRate;
            if (phase >= 1f) phase -= 1f;
            data[i] = Mathf.Sin(phase * 2f * Mathf.PI) * 0.42f;
        }

        ApplyEnvelope(data, 0.01f, 0.16f);
        return MakeClip("sfx_customer_leave", data);
    }

    static AudioClip BuildDrinkComplete()
    {
        int total = SecondsToSamples(0.12f);
        float[] data = new float[total];

        int noiseLen = SecondsToSamples(0.025f);
        uint seed = 1234567u;
        for (int i = 0; i < total; i++)
        {
            float sample = 0f;

            if (i < noiseLen)
            {
                float nEnv = 1f - (i / (float)noiseLen);
                sample += NextNoise(ref seed) * 0.5f * nEnv;
            }

            float tone = Mathf.Sin((i / (float)SampleRate) * 1568f * 2f * Mathf.PI);
            sample += tone * 0.35f;

            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        ApplyEnvelope(data, 0.002f, 0.05f);
        return MakeClip("sfx_drink_complete", data);
    }

    enum WaveType
    {
        Sine,
        Saw
    }

    static void WriteTone(float[] data, int start, int length, float freq, float amp, WaveType wave)
    {
        int end = Mathf.Min(data.Length, start + length);
        for (int i = start; i < end; i++)
        {
            float t = (i - start) / (float)SampleRate;
            float v;
            if (wave == WaveType.Saw)
            {
                float phase = (freq * t) % 1f;
                v = (phase * 2f) - 1f;
            }
            else
            {
                v = Mathf.Sin(t * freq * 2f * Mathf.PI);
            }
            data[i] += v * amp;
        }
    }

    static void WriteChime(float[] data, int start, int length, float freq, float amp)
    {
        int end = Mathf.Min(data.Length, start + length);
        for (int i = start; i < end; i++)
        {
            float t = (i - start) / (float)SampleRate;
            float decay = Mathf.Exp(-6f * (t / (length / (float)SampleRate)));
            float fundamental = Mathf.Sin(t * freq * 2f * Mathf.PI);
            float octave = Mathf.Sin(t * freq * 2f * 2f * Mathf.PI) * 0.3f;
            data[i] += (fundamental + octave) * amp * decay;
        }
    }

    static float NextNoise(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return ((seed & 0xFFFFFF) / (float)0xFFFFFF) * 2f - 1f;
    }

    static void ApplyEnvelope(float[] data, float attackSeconds, float decaySeconds)
    {
        int len = data.Length;
        int attack = Mathf.Clamp(SecondsToSamples(attackSeconds), 1, len);
        int decay = Mathf.Clamp(SecondsToSamples(decaySeconds), 1, len);

        for (int i = 0; i < attack; i++)
        {
            float g = i / (float)attack;
            data[i] *= g;
        }
        for (int i = 0; i < decay; i++)
        {
            int idx = len - 1 - i;
            if (idx < 0) break;
            float g = i / (float)decay;
            data[idx] *= g;
        }

        for (int i = 0; i < len; i++)
            data[i] = Mathf.Clamp(data[i], -1f, 1f);
    }

    static int SecondsToSamples(float seconds)
    {
        return Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
    }

    static AudioClip MakeClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
