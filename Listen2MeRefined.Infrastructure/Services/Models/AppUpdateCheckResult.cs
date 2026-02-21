namespace Listen2MeRefined.Infrastructure.Services.Models;

public sealed record AppUpdateCheckResult(bool IsUpdateAvailable, string Message, bool CanOpenUpdateLink);
