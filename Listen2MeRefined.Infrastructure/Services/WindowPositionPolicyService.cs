namespace Listen2MeRefined.Infrastructure.Services;

using Contracts;

public sealed class WindowPositionPolicyService : IWindowPositionPolicyService
{
    public bool IsTopmost(string? windowPosition)
    {
        return string.Equals(windowPosition, "Always on top", StringComparison.OrdinalIgnoreCase);
    }
}
