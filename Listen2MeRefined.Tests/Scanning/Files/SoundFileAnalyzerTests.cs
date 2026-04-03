using Listen2MeRefined.Infrastructure.Scanning.Files;

namespace Listen2MeRefined.Tests.Scanning.Files;

public class SoundFileAnalyzerTests
{
    [Theory]
    [InlineData(0u, 0u)]
    [InlineData(120u, 120u)]
    [InlineData(150u, 150u)]
    [InlineData(999u, 999u)]
    [InlineData(150_000_000u, 150u)]
    [InlineData(155_000_000u, 155u)]
    [InlineData(154_990_000u, 155u)]
    [InlineData(139_510_000u, 140u)]
    [InlineData(160_000_00u, 160u)]
    [InlineData(1500u, 150u)]
    [InlineData(15_000u, 150u)]
    [InlineData(1001u, 100u)]
    [InlineData(3_000_000u, 300u)]
    [InlineData(174_600_000u, 175u)]
    public void SanitizeBpm_ValidOrInflated_ReturnsExpected(uint input, uint expected)
    {
        var result = SoundFileAnalyzer.SanitizeBpm(input);

        Assert.Equal(expected, result);
    }
}
