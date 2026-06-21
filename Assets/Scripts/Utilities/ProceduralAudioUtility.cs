using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임에 절차적으로 합성한 효과음 클립을 제공하는 유틸리티.
/// 외부 오디오 에셋 없이 코드로 파형을 생성하여 캐시한다.
/// 지원 key: "merge_success","coin","serve_fail","customer_arrive","customer_leave","drink_complete"
/// </summary>
public static class ProceduralAudioUtility
{
    const int SampleRate = 44100;

    // key별 합성 클립 캐시 (중복 생성 금지)
    static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

    /// <summary>
    /// 알려진 key는 캐시된 합성 클립을 반환, 모르는 key는 null.
    /// </summary>
    public static AudioClip Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (_cache.TryGetValue(key, out AudioClip cached))
            return cached;

        AudioClip clip = Build(key);
        // 모르는 key는 캐시하지 않고 null 반환
        if (clip != null)
            _cache[key] = clip;

        return clip;
    }

    /// <summary>
    /// key에 해당하는 파형을 합성해 AudioClip 생성. 모르는 key는 null.
    /// </summary>
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
            default:
                return null;
        }
    }

    // ── 개별 효과음 합성 ─────────────────────────────────────────────

    /// <summary>coin: 상승 2음(밝게). 짧은 두 사인음을 위로 올려 동전 줍는 느낌.</summary>
    static AudioClip BuildCoin()
    {
        int total = SecondsToSamples(0.16f);
        float[] data = new float[total];

        int note0 = SecondsToSamples(0.07f);
        // 첫 음 (낮음), 둘째 음 (높음) — 밝은 상승
        WriteTone(data, 0, note0, 988f, 0.5f, WaveType.Sine);              // B5
        WriteTone(data, note0, total - note0, 1319f, 0.55f, WaveType.Sine); // E6

        ApplyEnvelope(data, 0.005f, 0.04f);
        return MakeClip("sfx_coin", data);
    }

    /// <summary>merge_success: 밝은 차임(3음 아르페지오). 메이저 코드 상승.</summary>
    static AudioClip BuildMergeSuccess()
    {
        int total = SecondsToSamples(0.42f);
        float[] data = new float[total];

        int seg = SecondsToSamples(0.12f);
        // C6 - E6 - G6 아르페지오 (밝은 차임). 사인 + 옥타브 살짝 섞어 반짝임 부여.
        WriteChime(data, 0, seg, 1047f, 0.45f);
        WriteChime(data, seg, seg, 1319f, 0.45f);
        WriteChime(data, seg * 2, total - seg * 2, 1568f, 0.5f);

        ApplyEnvelope(data, 0.004f, 0.12f);
        return MakeClip("sfx_merge_success", data);
    }

    /// <summary>serve_fail: 하강 부저(톱니파). 실패를 알리는 거친 하강음.</summary>
    static AudioClip BuildServeFail()
    {
        int total = SecondsToSamples(0.34f);
        float[] data = new float[total];

        // 톱니파로 거칠게, 주파수를 220 → 110으로 하강시켜 부저 느낌
        float startFreq = 220f;
        float endFreq = 110f;
        float phase = 0f;
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)total;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += freq / SampleRate;
            if (phase >= 1f) phase -= 1f;
            // 톱니파: -1 ~ 1
            float saw = (phase * 2f) - 1f;
            data[i] = saw * 0.4f;
        }

        ApplyEnvelope(data, 0.006f, 0.08f);
        return MakeClip("sfx_serve_fail", data);
    }

    /// <summary>customer_arrive: 딩동(2음). 초인종 같은 하강 2음.</summary>
    static AudioClip BuildCustomerArrive()
    {
        int total = SecondsToSamples(0.40f);
        float[] data = new float[total];

        int note0 = SecondsToSamples(0.18f);
        // "딩"(높음) → "동"(낮음) 전형적인 도어벨
        WriteTone(data, 0, note0, 1319f, 0.5f, WaveType.Sine);            // E6
        WriteTone(data, note0, total - note0, 988f, 0.5f, WaveType.Sine); // B5

        ApplyEnvelope(data, 0.005f, 0.10f);
        return MakeClip("sfx_customer_arrive", data);
    }

    /// <summary>customer_leave: 부드러운 하강음. 사인파 글라이드 다운.</summary>
    static AudioClip BuildCustomerLeave()
    {
        int total = SecondsToSamples(0.36f);
        float[] data = new float[total];

        // 부드럽게 784 → 392 으로 미끄러지는 사인음
        float startFreq = 784f; // G5
        float endFreq = 392f;   // G4
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

    /// <summary>drink_complete: 짧은 팝(노이즈 + 짧은 톤). 완성 알림.</summary>
    static AudioClip BuildDrinkComplete()
    {
        int total = SecondsToSamples(0.12f);
        float[] data = new float[total];

        // 앞부분 짧은 노이즈 버스트(팝) + 짧은 고음 톤이 함께
        int noiseLen = SecondsToSamples(0.025f);
        uint seed = 1234567u;
        for (int i = 0; i < total; i++)
        {
            float sample = 0f;

            // 짧은 노이즈 버스트 (앞쪽만, 빠르게 감쇠)
            if (i < noiseLen)
            {
                float nEnv = 1f - (i / (float)noiseLen);
                sample += NextNoise(ref seed) * 0.5f * nEnv;
            }

            // 짧은 고음 톤 (전체 길이, 가벼운 사인)
            float tone = Mathf.Sin((i / (float)SampleRate) * 1568f * 2f * Mathf.PI); // G6
            sample += tone * 0.35f;

            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        ApplyEnvelope(data, 0.002f, 0.05f);
        return MakeClip("sfx_drink_complete", data);
    }

    // ── 파형 헬퍼 ────────────────────────────────────────────────────

    enum WaveType
    {
        Sine,
        Saw
    }

    /// <summary>지정 구간에 단일 톤을 기록한다.</summary>
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

    /// <summary>차임용 톤: 기본음 + 옥타브 배음을 섞어 반짝이는 종소리 느낌.</summary>
    static void WriteChime(float[] data, int start, int length, float freq, float amp)
    {
        int end = Mathf.Min(data.Length, start + length);
        for (int i = start; i < end; i++)
        {
            float t = (i - start) / (float)SampleRate;
            // 구간 내 자체 감쇠로 종소리처럼 떨어지게
            float decay = Mathf.Exp(-6f * (t / (length / (float)SampleRate)));
            float fundamental = Mathf.Sin(t * freq * 2f * Mathf.PI);
            float octave = Mathf.Sin(t * freq * 2f * 2f * Mathf.PI) * 0.3f;
            data[i] += (fundamental + octave) * amp * decay;
        }
    }

    /// <summary>간단한 결정론적 화이트 노이즈 (-1~1). xorshift 기반.</summary>
    static float NextNoise(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        // 0~1 정규화 후 -1~1로 변환
        return ((seed & 0xFFFFFF) / (float)0xFFFFFF) * 2f - 1f;
    }

    // ── 공통 처리 ────────────────────────────────────────────────────

    /// <summary>앞(attack)/뒤(decay) 페이드를 적용해 클릭 노이즈를 방지한다.</summary>
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

        // 안전: 최종 클리핑 방지
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
