namespace Listen2MeRefined.Core.Models;

using System.Text.RegularExpressions;

/// <summary>
/// This class is used to represent software versions. It can be used to compare versions and to create
/// versions from a string or from three numbers.<para/> The version is represented by three numbers: major, minor
/// and patch. The major number is used to indicate breaking changes, the minor number is used to indicate
/// new features or changes that are not breaking and the patch number is used to indicate bug fixes.
/// </summary>
public partial class Version : IComparable<Version>
{
    /// <summary>
    /// The major version number. This number is used to indicate breaking changes.
    /// </summary>
    public int Major { get; set; }

    /// <summary>
    /// The minor version number. This number is used to indicate new features or changes that are not breaking.
    /// </summary>
    public int Minor { get; set; }

    /// <summary>
    /// The patch version number. This number is used to indicate bug fixes.
    /// </summary>
    public int Patch { get; set; }

    private Version(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    private Version(string version)
    {
        var versionParts = version.Split('.');
        Major = Convert.ToInt32(versionParts[0]);
        Minor = Convert.ToInt32(versionParts[1]);
        Patch = Convert.ToInt32(versionParts[2]);
    }

    /// <summary>
    /// Create a version from three numbers.
    /// </summary>
    /// <param name="major">/// The major version number. This number is used to indicate breaking changes.</param>
    /// <param name="minor">The minor version number. This number is used to indicate new features or changes that
    /// are not breaking.</param>
    /// <param name="patch">The patch version number. This number is used to indicate bug fixes.</param>
    /// <returns></returns>
    public static Version FromVersionNumbers(int major, int minor, int patch)
    {
        return new Version(major, minor, patch);
    }

    /// <summary>
    /// Create a version from a string. The string can start with or without 'v'.
    /// </summary>
    /// <param name="version">The string that represents the version. It can start with or without 'v'.</param>
    /// <returns>A new instance of <see cref="Version"/>. If the string is invalid for versioning, a version with all
    /// values set to 0 will be returned. </returns>
    public static Version FromString(string version)
    {
        if (!string.IsNullOrEmpty(version))
        {
            version = version
                .Replace("-beta", "")
                .Replace("-alpha", "");
        }

        if (!VersionRegex().IsMatch(version))
        {
            return new Version(0, 0, 0);
        }

        version = version.StartsWith('v') ? version[1..] : version;
        return new Version(version);
    }

    [GeneratedRegex("^(v)?\\d+\\.\\d+\\.\\d+$")]
    private static partial Regex VersionRegex();

    /// <summary>
    /// Compare this version to another version.
    /// </summary>
    /// <param name="other">The other version to compare to.</param>
    /// <returns>A negative number if this version is less than the other version, 0 if they are equal and a
    /// positive number if this version is greater than the other version.</returns>
    public int CompareTo(Version? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        if (Patch != other.Patch)
        {
            return Patch.CompareTo(other.Patch);
        }

        return 0;
    }

    public override string ToString()
    {
        return $"v{Major}.{Minor}.{Patch}";
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not Version version)
        {
            return false;
        }

        return Major == version.Major && Minor == version.Minor && Patch == version.Patch;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch);
    }

    public static bool operator ==(Version left, Version right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Version left, Version right)
    {
        return !(left == right);
    }

    public static bool operator <(Version left, Version right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    public static bool operator <=(Version left, Version right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    public static bool operator >(Version left, Version right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    public static bool operator >=(Version left, Version right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }
}