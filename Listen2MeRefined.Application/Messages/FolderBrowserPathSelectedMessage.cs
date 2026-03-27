using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class FolderBrowserPathSelectedMessage(string path)
    : ValueChangedMessage<string>(path);
