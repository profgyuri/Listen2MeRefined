using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Messages;

public class CurrentSongChangedMessage(AudioModel song) : ValueChangedMessage<AudioModel>(song);