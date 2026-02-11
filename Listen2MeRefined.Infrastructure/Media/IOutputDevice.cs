namespace Listen2MeRefined.Infrastructure.Media;

public interface IOutputDevice
{
    IEnumerable<AudioOutputDevice> EnumerateOutputDevices();
}