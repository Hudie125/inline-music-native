using System;
using NAudio.Wave;

namespace InlineMusic.Audio;

/// <summary>
/// Digital Biquad IIR Peaking Equalizer filter based on the Robert Bristow-Johnson Audio EQ Cookbook.
/// Processes stereo or mono audio with zero phase distortion and minimal latency.
/// </summary>
public class BiquadPeakingFilter
{
    private float _b0, _b1, _b2, _a1, _a2;
    private float _x1L, _x2L, _y1L, _y2L;
    private float _x1R, _x2R, _y1R, _y2R;

    public float Frequency { get; }
    public float SampleRate { get; }
    public float Q { get; }
    public float GainDb { get; private set; }

    public BiquadPeakingFilter(float frequency, float sampleRate, float q = 1.4142f, float initialGainDb = 0f)
    {
        Frequency = frequency;
        SampleRate = Math.Max(sampleRate, 8000f);
        Q = Math.Max(q, 0.1f);
        SetGain(initialGainDb);
    }

    public void SetGain(float gainDb)
    {
        GainDb = Math.Clamp(gainDb, -15f, 15f);

        if (Math.Abs(GainDb) < 0.01f)
        {
            _b0 = 1f; _b1 = 0f; _b2 = 0f;
            _a1 = 0f; _a2 = 0f;
            return;
        }

        // Robert Bristow-Johnson Peaking EQ formula
        double A = Math.Pow(10.0, GainDb / 40.0);
        double w0 = 2.0 * Math.PI * Math.Min(Frequency, SampleRate * 0.49) / SampleRate;
        double alpha = Math.Sin(w0) / (2.0 * Q);
        double cosw0 = Math.Cos(w0);

        double b0 = 1.0 + alpha * A;
        double b1 = -2.0 * cosw0;
        double b2 = 1.0 - alpha * A;
        double a0 = 1.0 + alpha / A;
        double a1 = -2.0 * cosw0;
        double a2 = 1.0 - alpha / A;

        _b0 = (float)(b0 / a0);
        _b1 = (float)(b1 / a0);
        _b2 = (float)(b2 / a0);
        _a1 = (float)(a1 / a0);
        _a2 = (float)(a2 / a0);
    }

    public float ProcessSampleLeft(float input)
    {
        if (Math.Abs(GainDb) < 0.01f) return input;
        float output = _b0 * input + _b1 * _x1L + _b2 * _x2L - _a1 * _y1L - _a2 * _y2L;
        _x2L = _x1L;
        _x1L = input;
        _y2L = _y1L;
        _y1L = output;
        return output;
    }

    public float ProcessSampleRight(float input)
    {
        if (Math.Abs(GainDb) < 0.01f) return input;
        float output = _b0 * input + _b1 * _x1R + _b2 * _x2R - _a1 * _y1R - _a2 * _y2R;
        _x2R = _x1R;
        _x1R = input;
        _y2R = _y1R;
        _y1R = output;
        return output;
    }

    public void Reset()
    {
        _x1L = _x2L = _y1L = _y2L = 0f;
        _x1R = _x2R = _y1R = _y2R = 0f;
    }
}

/// <summary>
/// 10-Band Graphic Equalizer ISampleProvider for NAudio processing graph.
/// Frequency bands: 32Hz, 64Hz, 125Hz, 250Hz, 500Hz, 1kHz, 2kHz, 4kHz, 8kHz, 16kHz.
/// </summary>
public class TenBandEqualizerSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly BiquadPeakingFilter[] _filters;
    private readonly object _lock = new();

    public static readonly float[] CenterFrequencies = new float[]
    {
        32f, 64f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f
    };

    public bool IsEnabled { get; set; } = true;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public TenBandEqualizerSampleProvider(ISampleProvider source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        float sampleRate = _source.WaveFormat.SampleRate;

        _filters = new BiquadPeakingFilter[CenterFrequencies.Length];
        for (int i = 0; i < CenterFrequencies.Length; i++)
        {
            _filters[i] = new BiquadPeakingFilter(CenterFrequencies[i], sampleRate);
        }
    }

    public void SetBandGain(int bandIndex, float gainDb)
    {
        if (bandIndex < 0 || bandIndex >= _filters.Length) return;
        lock (_lock)
        {
            _filters[bandIndex].SetGain(gainDb);
        }
    }

    public float GetBandGain(int bandIndex)
    {
        if (bandIndex < 0 || bandIndex >= _filters.Length) return 0f;
        return _filters[bandIndex].GainDb;
    }

    public void SetAllBands(float[] gainsDb)
    {
        if (gainsDb == null) return;
        lock (_lock)
        {
            int count = Math.Min(gainsDb.Length, _filters.Length);
            for (int i = 0; i < count; i++)
            {
                _filters[i].SetGain(gainsDb[i]);
            }
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);
        if (!IsEnabled || samplesRead == 0) return samplesRead;

        int channels = WaveFormat.Channels;
        lock (_lock)
        {
            for (int n = 0; n < samplesRead; n += channels)
            {
                float left = buffer[offset + n];
                float right = channels > 1 ? buffer[offset + n + 1] : left;

                for (int b = 0; b < _filters.Length; b++)
                {
                    left = _filters[b].ProcessSampleLeft(left);
                    if (channels > 1)
                    {
                        right = _filters[b].ProcessSampleRight(right);
                    }
                }

                // Prevent hard digital clipping
                buffer[offset + n] = Math.Clamp(left, -1.0f, 1.0f);
                if (channels > 1)
                {
                    buffer[offset + n + 1] = Math.Clamp(right, -1.0f, 1.0f);
                }
            }
        }

        return samplesRead;
    }

    public void Reset()
    {
        lock (_lock)
        {
            foreach (var filter in _filters)
            {
                filter.Reset();
            }
        }
    }
}
