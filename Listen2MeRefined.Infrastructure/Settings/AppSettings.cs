using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Settings;

public sealed class AppSettings : Application.Settings.Settings
{
    [Key] public int Id { get; set; }

    public string FontFamily { get; set; } = "";
    public string NewSongWindowPosition { get; set; } = "";
    public string AudioOutputDeviceName { get; set; } = "";
    public List<MusicFolderModel> MusicFolders { get; set; } = new();
    public bool ScanOnStartup { get; set; } = true;
    public bool EnableGlobalMediaKeys { get; set; } = true;
    public bool EnableCornerNowPlayingPopup { get; set; } = true;
    public short CornerTriggerSizePx { get; set; } = 10;
    public short CornerTriggerDebounceMs { get; set; } = 10;
    public float StartupVolume { get; set; } = 0.7f;
    public bool StartMuted { get; set; }
    public bool AutoCheckUpdatesOnStartup { get; set; } = true;
    public bool UseCompactPlaylistView { get; set; }
    public bool AutoScanOnFolderAdd { get; set; } = true;
    public bool ShowTaskPercentage { get; set; } = true;
    public short TaskPercentageReportInterval { get; set; } = 1;
    public bool ShowScanMilestoneCount { get; set; }
    public short ScanMilestoneInterval { get; set; } = 25;
    public TaskStatusCountBasis ScanMilestoneBasis { get; set; } = TaskStatusCountBasis.Processed;
    public string LastBrowsedFolder { get; set; } = "";
    public bool FolderBrowserStartAtLastLocation { get; set; } = true;
    public string PinnedFoldersJson { get; set; } = "[]";
    public SearchResultsTransferMode SearchResultsTransferMode { get; set; } = SearchResultsTransferMode.Move;
    public string MutedDroppedSongFoldersJson { get; set; } = "[]";
    public string ThemeMode { get; set; } = "Dark";
    public string AccentColor { get; set; } = "Orange";
    public bool AutoFlowTrackText { get; set; }

    [NotMapped]
    public List<string> PinnedFolders
    {
        get => Deserialize(PinnedFoldersJson);
        set => PinnedFoldersJson = Serialize(value);
    }

    [NotMapped]
    public List<string> MutedDroppedSongFolders
    {
        get => Deserialize(MutedDroppedSongFoldersJson);
        set => MutedDroppedSongFoldersJson = Serialize(value);
    }

    private static string Serialize(List<string>? paths)
    {
        return JsonSerializer.Serialize(paths ?? new List<string>());
    }

    private static List<string> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }
}
