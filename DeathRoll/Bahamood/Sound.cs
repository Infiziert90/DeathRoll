using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DeathRoll.Bahamood;

// Adjusted From: https://markheath.net/post/fire-and-forget-audio-playback-with
internal class AudioPlaybackEngine : IDisposable
{
    private readonly IWavePlayer OutputDevice;
    private readonly MixingSampleProvider Mixer;
    private readonly VolumeSampleProvider VolumeProvider;
    private readonly FadeInOutSampleProvider FadeProvider;

    public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
    {
        OutputDevice = new WaveOutEvent();
        Mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        VolumeProvider = new VolumeSampleProvider(Mixer)
        {
            Volume = 0.1f
        };

        FadeProvider = new FadeInOutSampleProvider(VolumeProvider, true);

        OutputDevice.Init(FadeProvider);
        OutputDevice.Play();
    }

    public void PlaySound(string fileName)
    {
        var input = new VorbisWaveReader(fileName);
        AddMixerInput(new AutoDisposeFileReader(input));
    }

    private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
    {
        if (input.WaveFormat.Channels == Mixer.WaveFormat.Channels)
        {
            return input;
        }
        if (input.WaveFormat.Channels == 1 && Mixer.WaveFormat.Channels == 2)
        {
            return new MonoToStereoSampleProvider(input);
        }

        throw new NotImplementedException("Not yet implemented this channel count conversion");
    }

    public void PlaySound(CachedSound sound)
    {
        if (OutputDevice.PlaybackState == PlaybackState.Stopped)
            OutputDevice.Play();

        AddMixerInput(new CachedSoundSampleProvider(sound));
    }

    private void AddMixerInput(ISampleProvider input)
    {
        Mixer.AddMixerInput(ConvertToRightChannelCount(input));
    }

    public void FadeIn()
    {
        FadeProvider.BeginFadeIn(2000);
    }

    public void FadeOut()
    {
        FadeProvider.BeginFadeOut(2000);
    }

    public void Stop()
    {
        Mixer.RemoveAllMixerInputs();
    }

    public void Dispose()
    {
        Stop();
        OutputDevice.Dispose();
    }

    public static readonly AudioPlaybackEngine Instance = new();
}

internal class CachedSound
{
    public float[] AudioData { get; private set; }
    public WaveFormat WaveFormat { get; private set; }

    public CachedSound(string audioFileName)
    {
        using var audioFileReader = new VorbisWaveReader(audioFileName);

        // TODO: could add resampling in here if required
        WaveFormat = audioFileReader.WaveFormat;
        var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
        var readBuffer= new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
        int samplesRead;
        while((samplesRead = audioFileReader.Read(readBuffer,0,readBuffer.Length)) > 0)
        {
            wholeFile.AddRange(readBuffer.Take(samplesRead));
        }
        AudioData = wholeFile.ToArray();
    }
}

internal class CachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound CachedSound;
    private long Position;

    public CachedSoundSampleProvider(CachedSound cachedSound)
    {
        CachedSound = cachedSound;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = CachedSound.AudioData.Length - Position;
        var samplesToCopy = Math.Min(availableSamples, count);
        Array.Copy(CachedSound.AudioData, Position, buffer, offset, samplesToCopy);
        Position += samplesToCopy;
        return (int) samplesToCopy;
    }

    public WaveFormat WaveFormat => CachedSound.WaveFormat;
}

internal class AutoDisposeFileReader : ISampleProvider
{
    public WaveFormat WaveFormat { get; private set; }

    private readonly VorbisWaveReader Reader;
    private bool IsDisposed;

    public AutoDisposeFileReader(VorbisWaveReader reader)
    {
        Reader = reader;
        WaveFormat = reader.WaveFormat;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (IsDisposed)
            return 0;

        var read = Reader.Read(buffer, offset, count);
        if (read != 0)
            return read;

        Reader.Dispose();
        IsDisposed = true;
        return read;
    }
}