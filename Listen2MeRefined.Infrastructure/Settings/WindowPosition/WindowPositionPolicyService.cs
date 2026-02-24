namespace Listen2MeRefined.Infrastructure.Settings.WindowPosition;

public sealed class WindowPositionPolicyService : IWindowPositionPolicyService
{
    public bool IsTopmost(string? windowPosition)
    {
        return string.Equals(windowPosition, "Always on top", StringComparison.OrdinalIgnoreCase);
    }
}
