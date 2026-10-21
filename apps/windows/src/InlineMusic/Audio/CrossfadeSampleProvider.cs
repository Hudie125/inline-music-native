using System;
using NAudio.Wave;

namespace InlineMusic.Audio;

/// <summary>
/// Provides smooth sample-level crossfading between an active audio stream and a newly enqueued stream.
/// Employs equal-power sin/cos crossfade curve to prevent perception of volume loss during transition.
/// </summary>
public class CrossfadeSampleProvider : ISampleProvider
{
    private ISampleProvider? _currentProvider;
    private ISampleProvider? _fadingOutProvider;
    private readonly WaveFormat _waveFormat;
    private readonly object _lock = new();

    private long _fadeTotalSamples;
    private long _fadeSamplesRemaining;
    private float[]? _fadeBuffer;

    public WaveFormat WaveFormat => _waveFormat;
    public double CrossfadeDurationSeconds { get; set; } = 3.0;
    public bool IsCrossfading => _fadeSamplesRemaining > 0;

    public CrossfadeSampleProvider(WaveFormat waveFormat)
    {
        _waveFormat = waveFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    }

    public void TransitionTo(ISampleProvider newProvider, bool enableCrossfade = true)
    {
        lock (_lock)
        {
            if (_currentProvider == null || !enableCrossfade || CrossfadeDurationSeconds <= 0.05)
            {
                _fadingOutProvider = null;
                _currentProvider = newProvider;
                _fadeSamplesRemaining = 0;
                return;
            }

            _fadingOutProvider = _currentProvider;
            _currentProvider = newProvider;

            int channels = _waveFormat.Channels;
            int sampleRate = _waveFormat.SampleRate;
            _fadeTotalSamples = (long)(CrossfadeDurationSeconds * sampleRate * channels);
            _fadeSamplesRemaining = _fadeTotalSamples;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _currentProvider = null;
            _fadingOutProvider = null;
            _fadeSamplesRemaining = 0;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);

        lock (_lock)
        {
            if (_currentProvider == null && _fadingOutProvider == null)
            {
                return 0;
            }

            // Normal playback without active crossfade
            if (_fadingOutProvider == null || _fadeSamplesRemaining <= 0)
            {
                return _currentProvider?.Read(buffer, offset, count) ?? 0;
            }

            // Active crossfade in progress
            if (_fadeBuffer == null || _fadeBuffer.Length < count)
            {
                _fadeBuffer = new float[count];
            }

            int currentRead = _currentProvider?.Read(buffer, offset, count) ?? 0;
            int fadeOutRead = _fadingOutProvider.Read(_fadeBuffer, 0, count);

            int samplesToMix = Math.Max(currentRead, fadeOutRead);
            if (samplesToMix == 0)
            {
                _fadingOutProvider = null;
                _fadeSamplesRemaining = 0;
                return 0;
            }

            for (int i = 0; i < samplesToMix; i++)
            {
                double progress = 1.0 - ((double)_fadeSamplesRemaining / _fadeTotalSamples);
                progress = Math.Clamp(progress, 0.0, 1.0);

                // Equal-power crossfade curve
                float gainIn = (float)Math.Sin(progress * Math.PI * 0.5);
                float gainOut = (float)Math.Cos(progress * Math.PI * 0.5);

                float currentSample = (i < currentRead) ? buffer[offset + i] : 0f;
                float fadeOutSample = (i < fadeOutRead) ? _fadeBuffer[i] : 0f;

                buffer[offset + i] = (currentSample * gainIn) + (fadeOutSample * gainOut);

                if (_fadeSamplesRemaining > 0)
                {
                    _fadeSamplesRemaining--;
                }
            }

            if (_fadeSamplesRemaining <= 0)
            {
                _fadingOutProvider = null;
            }

            return samplesToMix;
        }
    }
}
