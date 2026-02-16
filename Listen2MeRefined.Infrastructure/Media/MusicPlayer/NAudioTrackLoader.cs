using NAudio;
using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class NAudioTrackLoader : ITrackLoader
{
    public TrackLoadResult Load(AudioModel track)
    {
        if (string.IsNullOrWhiteSpace(track.Path) || !File.Exists(track.Path))
        {
            return new TrackLoadResult(TrackLoadStatus.MissingFile, Reason: "File does not exist");
        }

        try
        {
            WaveStream reader = track.Path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                ? new WaveFileReader(track.Path)
                : new AudioFileReader(track.Path);

            if (reader.WaveFormat.Encoding is WaveFormatEncoding.Extensible)
            {
                reader.Dispose();
                return new TrackLoadResult(TrackLoadStatus.UnsupportedFormat, Reason: "WaveFormatEncoding.Extensible");
            }

            return TrackLoadResult.Success(reader);
        }
        catch (FormatException e)
        {
            return new TrackLoadResult(TrackLoadStatus.UnsupportedFormat, Reason: e.Message);
        }
        catch (MmException e)
        {
            return new TrackLoadResult(TrackLoadStatus.CorruptFile, Reason: e.Message);
        }
        catch (InvalidDataException e)
        {
            return new TrackLoadResult(TrackLoadStatus.CorruptFile, Reason: e.Message);
        }
    }
}
