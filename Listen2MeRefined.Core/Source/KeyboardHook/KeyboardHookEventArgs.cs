using System.ComponentModel;

namespace Listen2MeRefined.Core.Source.KeyboardHook;

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