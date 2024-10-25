namespace Listen2MeRefined.Infrastructure.SystemOperations.KeyboardHook;
using System.ComponentModel;

public sealed class KeyboardHookEventArgs : HandledEventArgs
{
    public KeyboardState KeyboardState { get; }
    public KeycodeInterpreter KeyboardData { get; }

    public KeyboardHookEventArgs(
        KeycodeInterpreter keyboardData,
        KeyboardState keyboardState)
    {
        KeyboardData = keyboardData;
        KeyboardState = keyboardState;
    }
}