using Listen2MeRefined.Core.Interfaces;
using Listen2MeRefined.Infrastructure.SystemOperations;
using NUnit.Framework;

namespace Listen2MeRefined.Tests.Infrastructure.SystemOperations;

[TestFixture]
public class FileEnumeratorTests
{
    [Test]
    public void EnumerateFiles_ReutrnsAList_WhenFolderIsGivenAsPath()
    {
        IFileEnumerator fileEnumerator = new FileEnumerator();

        var files = fileEnumerator.EnumerateFiles(@"e:\zene\bounce");

        Assert.That(files.Any);
    }

    [Test]
    public void EnumerateFiles_ReutrnsAList_OnlyWithExistingFiles()
    {
        IFileEnumerator fileEnumerator = new FileEnumerator();

        var files = fileEnumerator.EnumerateFiles(@"e:\zene\bounce");

        foreach (var file in files)
        {
            Assert.That(File.Exists(file));
        }
    }

    [Test]
    public void EnumerateFiles_ThrowsArguementException_WhenFolderDoesNotExist()
    {
        IFileEnumerator fileEnumerator = new FileEnumerator();

        Assert.Throws<ArgumentException>(() => fileEnumerator.EnumerateFiles(@"e:\zene\bounce\doesnotexist"));
    }

    [Test]
    public void EnumerateFiles_ThrowsArguementException_WhenPathIsNotFolder()
    {
        IFileEnumerator fileEnumerator = new FileEnumerator();

        Assert.Throws<ArgumentException>(() =>
            fileEnumerator.EnumerateFiles(@"e:\zene\bounce\Mike Candys - Like That.mp3"));
    }
}