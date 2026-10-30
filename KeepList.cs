using System.IO;

namespace LOLFriendsCleaner;

internal sealed class KeepList
{
    private readonly HashSet<string> _entries;

    private KeepList(HashSet<string> entries) => _entries = entries;

    public static KeepList Load()
    {
        var entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "keep_friends.txt");
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path).Select(l => l.Trim()))
                {
                    if (line.Length > 0 && !line.StartsWith('#')) entries.Add(line);
                }
            }
        }
        catch
        {
        }
        return new KeepList(entries);
    }

    public bool Matches(Friend f) =>
        _entries.Count > 0 &&
        (_entries.Contains($"{f.gameName}#{f.gameTag}") ||
         (f.gameName is not null && _entries.Contains(f.gameName)) ||
         (f.puuid is not null && _entries.Contains(f.puuid)));
}