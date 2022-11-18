using Listen2MeRefined.Core.Models;
using NUnit.Framework;

namespace Listen2MeRefined.Tests.Infrastructure;

[TestFixture]
public class ExtensionTests
{
    [Test]
    public void AudioUpdate_UpdatesValuesCorrectly_WhenNewValuesAreSet()
    {
        var audio = new AudioModel(){Id = 1, Artist = "Test", Genre = "Test", Path = "C:\\Test\\Test.mp3"};
        var newAudio = new AudioModel(){Id = 2, Artist = "Test2", Genre = "Test2", Path = "C:\\Test\\Test2.mp3"};
        
        audio.Update(newAudio);
        Assert.Multiple(() =>
        {
            Assert.That(audio.Id, Is.Not.EqualTo(2));
            Assert.That(audio.Artist, Is.EqualTo("Test2"));
            Assert.That(audio.Path, Is.Not.EqualTo("C:\\Test\\Test2.mp3"));
            Assert.That(audio.Genre, Is.EqualTo("Test2"));
        });
    }
}