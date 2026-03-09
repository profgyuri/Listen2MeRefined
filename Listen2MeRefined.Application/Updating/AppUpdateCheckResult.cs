namespace Listen2MeRefined.Application.Updating;

public sealed record AppUpdateCheckResult(bool IsUpdateAvailable, string Message, bool CanOpenUpdateLink);
