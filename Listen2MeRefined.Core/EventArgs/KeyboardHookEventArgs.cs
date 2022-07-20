namespace Listen2MeRefined.Core.EventArgs;

using Listen2MeRefined.Core.Enums;
using System.ComponentModel;

public class KeyboardHookEventArgs : HandledEventArgs
{
    public KeyboardState KeyboardState { get; }
    public KeycodeInterpreter KeyboardData { get; }

    public KeyboardHookEventArgs(KeycodeInterpreter keyboardData, KeyboardState keyboardState)
    {
        KeyboardData = keyboardData;
        KeyboardState = keyboardState;
    }
}