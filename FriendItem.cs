using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;

namespace LOLFriendsCleaner;

/// <summary>Linha da lista: um amigo + se ele está marcado para exclusão.</summary>
public sealed class FriendItem : INotifyPropertyChanged
{
    private static readonly Brush Online = Frozen("#3DD68C");
    private static readonly Brush Away = Frozen("#F5A524");
    private static readonly Brush InGame = Frozen("#4C8DFF");
    private static readonly Brush Mobile = Frozen("#A78BFA");
    private static readonly Brush Offline = Frozen("#5B6372");

    private bool _toDelete;

    public FriendItem(Friend source, bool toDelete)
    {
        Source = source;
        _toDelete = toDelete;
    }

    public Friend Source { get; }

    public string Puuid => Source.puuid ?? "";

    public string Name => !string.IsNullOrWhiteSpace(Source.gameName)
        ? Source.gameName!
        : (!string.IsNullOrWhiteSpace(Source.name) ? Source.name! : "(sem nome)");

    public string TagText => string.IsNullOrWhiteSpace(Source.gameTag) ? "" : "#" + Source.gameTag;

    public string FullName => Name + TagText;

    public string Initial => Name.Length > 0 ? char.ToUpperInvariant(Name[0]).ToString() : "?";

    public string StatusText => Source.availability?.ToLowerInvariant() switch
    {
        "chat" => "Online",
        "away" => "Ausente",
        "dnd" => "Em jogo",
        "mobile" => "Mobile",
        _ => "Offline",
    };

    public Brush StatusBrush => Source.availability?.ToLowerInvariant() switch
    {
        "chat" => Online,
        "away" => Away,
        "dnd" => InGame,
        "mobile" => Mobile,
        _ => Offline,
    };

    /// <summary>Ex.: "Offline · visto há 3 meses".</summary>
    public string StatusLine
    {
        get
        {
            var seen = LastSeenText();
            return seen.Length == 0 ? StatusText : $"{StatusText} · {seen}";
        }
    }

    public bool ToDelete
    {
        get => _toDelete;
        set
        {
            if (_toDelete == value) return;
            _toDelete = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToDelete)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string LastSeenText()
    {
        // Só faz sentido para quem não está online; ignora valores fora do formato esperado (epoch em ms).
        if (StatusText != "Offline") return "";
        if (!long.TryParse(Source.lastSeenOnlineTimestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms)) return "";
        if (ms < 946_684_800_000L) return "";

        TimeSpan ago;
        try { ago = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(ms); }
        catch { return ""; }
        if (ago < TimeSpan.Zero) return "";

        var days = (int)ago.TotalDays;
        if (days < 1) return "visto hoje";
        if (days < 30) return $"visto há {days} {(days == 1 ? "dia" : "dias")}";
        if (days < 365) { var m = days / 30; return $"visto há {m} {(m == 1 ? "mês" : "meses")}"; }
        var y = days / 365;
        return $"visto há {y} {(y == 1 ? "ano" : "anos")}";
    }

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
