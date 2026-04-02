using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using NAudio;
using NAudio.Wave;
using System.Runtime.InteropServices;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class NAudioTrackLoader : ITrackLoader
{
    private static readonly Guid PcmSubTypeGuid = new("00000001-0000-0010-8000-00AA00389B71");
    private static readonly Guid IeeeFloatSubTypeGuid = new("00000003-0000-0010-8000-00AA00389B71");

    public TrackLoadResult Load(AudioModel track)
    {
        if (string.IsNullOrWhiteSpace(track.Path) || !File.Exists(track.Path))
        {
            return new TrackLoadResult(TrackLoadStatus.MissingFile, Reason: "File does not exist");
        }

        try
        {
            WaveStream reader = track.Path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                ? CreatePlayableWavReader(track.Path)
                : new AudioFileReader(track.Path);

            return TrackLoadResult.Success(reader);
        }
        catch (Exception e) 
            when (e is FormatException or MmException or InvalidDataException
                      or COMException or ArgumentException or InvalidOperationException)
        {
            return new TrackLoadResult(TrackLoadStatus.CorruptFile, Reason: e.Message);
        }
    }

    private static WaveStream CreatePlayableWavReader(string path)
    {
        var ownedStreams = new List<IDisposable>();

        try
        {
            WaveStream playableStream;
            var reader = new WaveFileReader(path);
            ownedStreams.Add(reader);

            if (reader.WaveFormat.Encoding is WaveFormatEncoding.Extensible)
            {
                if (TryCreateStandardFormatForExtensible(reader.WaveFormat, out var standardFormat))
                {
                    reader.Position = 0;
                    var normalizedExtensibleStream = new RawSourceWaveStream(reader, standardFormat);
                    ownedStreams.Add(normalizedExtensibleStream);
                    playableStream = normalizedExtensibleStream;
                }
                else
                {
                    DisposeOwned(ownedStreams);
                    ownedStreams.Clear();

                    var mediaFoundationReader = new MediaFoundationReader(path);
                    ownedStreams.Add(mediaFoundationReader);
                    playableStream = mediaFoundationReader;
                }
            }
            else if (reader.WaveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat)
            {
                playableStream = reader;
            }
            else
            {
                var conversionStream = WaveFormatConversionStream.CreatePcmStream(reader);
                ownedStreams.Add(conversionStream);
                playableStream = conversionStream;
            }

            return new OwnedWaveStream(playableStream, ownedStreams);
        }
        catch
        {
            DisposeOwned(ownedStreams);
            throw;
        }
    }

    private static bool TryCreateStandardFormatForExtensible(WaveFormat waveFormat, out WaveFormat standardFormat)
    {
        if (waveFormat is not WaveFormatExtensible extensibleFormat)
        {
            standardFormat = null!;
            return false;
        }

        if (extensibleFormat.SubFormat == PcmSubTypeGuid)
        {
            standardFormat = new WaveFormat(
                extensibleFormat.SampleRate,
                extensibleFormat.BitsPerSample,
                extensibleFormat.Channels);
            return true;
        }

        if (extensibleFormat.SubFormat == IeeeFloatSubTypeGuid && extensibleFormat.BitsPerSample == 32)
        {
            standardFormat = WaveFormat.CreateIeeeFloatWaveFormat(extensibleFormat.SampleRate, extensibleFormat.Channels);
            return true;
        }

        standardFormat = null!;
        return false;
    }

    private static void DisposeOwned(IReadOnlyList<IDisposable> disposables)
    {
        for (var i = disposables.Count - 1; i >= 0; i--)
        {
            try
            {
                disposables[i].Dispose();
            }
            catch
            {
                // Ignore cleanup exceptions so the original failure can bubble up.
            }
        }
    }

    private sealed class OwnedWaveStream : WaveStream
    {
        private readonly WaveStream _inner;
        private readonly IReadOnlyList<IDisposable> _ownedDisposables;
        private bool _disposed;

        public OwnedWaveStream(WaveStream inner, IReadOnlyList<IDisposable> ownedDisposables)
        {
            _inner = inner;

            var uniqueOwned = new List<IDisposable>();
            foreach (var disposable in ownedDisposables)
            {
                if (!ContainsReference(uniqueOwned, disposable))
                {
                    uniqueOwned.Add(disposable);
                }
            }

            if (!ContainsReference(uniqueOwned, inner))
            {
                uniqueOwned.Add(inner);
            }

            _ownedDisposables = uniqueOwned;
        }

        public override WaveFormat WaveFormat => _inner.WaveFormat;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                DisposeOwned(_ownedDisposables);
            }

            base.Dispose(disposing);
        }

        private static bool ContainsReference(IEnumerable<IDisposable> disposables, IDisposable candidate)
        {
            foreach (var disposable in disposables)
            {
                if (ReferenceEquals(disposable, candidate))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
