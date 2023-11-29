namespace Listen2MeRefined.Infrastructure.SystemOperations.KeyboardHook;

public record struct KeycodeInterpreter(
    int VirtualCode,
    int HardwareScanCode,
    int Flags,
    int TimeStamp,
    IntPtr AdditionalInformation)
{
    /// <summary>
    ///     Gets the actual key for the virtual keycode
    /// </summary>
    public ConsoleKey Key => (ConsoleKey)VirtualCode;
}