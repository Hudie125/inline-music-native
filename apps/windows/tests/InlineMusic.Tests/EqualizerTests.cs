using System;
using InlineMusic.Audio;
using NAudio.Wave;
using Xunit;

namespace InlineMusic.Tests;

public class DummySampleProvider : ISampleProvider
{
    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    public int Read(float[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = 0.5f; // Constant DC test signal
        }
        return count;
    }
}

public class EqualizerTests
{
    [Fact]
    public void BiquadPeakingFilter_ZeroGain_DoesNotAlterSignal()
    {
        var filter = new BiquadPeakingFilter(1000f, 44100f, 1.414f, 0f);
        float input = 0.75f;
        float output = filter.ProcessSampleLeft(input);
        Assert.Equal(input, output, precision: 4);
    }

    [Fact]
    public void BiquadPeakingFilter_BoostGain_AffectsSignal()
    {
        var filter = new BiquadPeakingFilter(1000f, 44100f, 1.414f, 6.0f);
        float input = 0.5f;
        float output = filter.ProcessSampleLeft(input);
        Assert.NotEqual(0f, output);
    }

    [Fact]
    public void TenBandEqualizer_ProcessesSamplesWithoutClipping()
    {
        var dummy = new DummySampleProvider();
        var eq = new TenBandEqualizerSampleProvider(dummy);

        // Boost all bands by +6dB
        for (int i = 0; i < 10; i++)
        {
            eq.SetBandGain(i, 6.0f);
            Assert.Equal(6.0f, eq.GetBandGain(i));
        }

        float[] buffer = new float[256];
        int read = eq.Read(buffer, 0, buffer.Length);

        Assert.Equal(256, read);
        foreach (float sample in buffer)
        {
            // Verify clamping safety guarantees: [-1.0, 1.0]
            Assert.True(sample >= -1.0f && sample <= 1.0f, $"Sample {sample} exceeded allowable range");
        }
    }
}
