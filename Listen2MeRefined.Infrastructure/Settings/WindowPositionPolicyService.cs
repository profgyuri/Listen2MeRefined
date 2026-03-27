using Listen2MeRefined.Application.Settings;

namespace Listen2MeRefined.Infrastructure.Settings;

public sealed class WindowPositionPolicyService : IWindowPositionPolicyService
{
    public bool IsTopmost(string? windowPosition)
    {
        return string.Equals(windowPosition, "Always on top", StringComparison.OrdinalIgnoreCase);
    }
}
