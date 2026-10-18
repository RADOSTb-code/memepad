namespace MemePad.Core.Model;

/// <summary>A library entry. Audio stays in the original file; only the reference and edits are stored.</summary>
public sealed record Sound(
    Guid Id,
    string Path,
    string Title,
    bool Favorite,
    double GainDb,
    TimeSpan TrimStart,
    TimeSpan? TrimEnd);
