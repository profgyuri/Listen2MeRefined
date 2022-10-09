namespace Listen2MeRefined.Infrastructure;

internal static class GlobalConstants
{
    public static string[] SupportedExtensions { get; } =
    {
        ".aa", ".aax", ".aac", ".aiff", ".ape", ".dsf",
        ".flac", ".m4a", ".m4b", ".m4p", ".mp3", ".mpc",
        ".mpp", ".ogg", ".oga",
        ".wav", ".wma", ".wv", ".webm"
    };
    
    public const string ParentPathItem = "..";
}