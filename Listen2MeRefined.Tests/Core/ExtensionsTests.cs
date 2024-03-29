﻿using Listen2MeRefined.Core.Source.Extensions;
using NUnit.Framework;

namespace Listen2MeRefined.Tests.Core;

[TestFixture]
public class ExtensionsTests
{
    [Test]
    public void Shuffle_RandomizesListCorrectly()
    {
        var list = new[] {1, 2, 3, 4, 5};
        var shuffled = new int[5];
        Array.Copy(list, shuffled, 5);

        shuffled.Shuffle();

        Assert.That(shuffled, Is.Not.EqualTo(list));
        Assert.That(shuffled, Is.EquivalentTo(list));
    }
}