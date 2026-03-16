using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class ExternalAudioFilesOpenedMessage(IReadOnlyList<string> paths)
    : ValueChangedMessage<IReadOnlyList<string>>(paths);
