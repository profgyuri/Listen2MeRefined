using Listen2MeRefined.Core.DomainObjects;

namespace Listen2MeRefined.Application.Settings;

public interface IOutputDevice
{
    IEnumerable<AudioOutputDevice> EnumerateOutputDevices();
}