using Listen2MeRefined.Core.DomainObjects;

namespace Listen2MeRefined.Infrastructure.Media;

public interface IOutputDevice
{
    IEnumerable<AudioOutputDevice> EnumerateOutputDevices();
}