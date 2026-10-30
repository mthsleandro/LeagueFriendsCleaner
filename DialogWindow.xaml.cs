using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LOLFriendsCleaner;

public enum DialogKind { Info, Success, Warning, Danger }

public sealed class DialogOptions
{
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    /// <summary>Caixa de destaque (ex.: aviso de ação irreversível).</summary>
    public string? Warning { get; init; }
    /// <summary>Texto menor, em cinza, abaixo da mensagem.</summary>
    public string? Detail { get; init; }
    /// <summary>Se informado, o botão principal só habilita depois de marcar esta caixa.</summary>
    public string? RequireCheckText { get; init; }
    public string PrimaryText { get; init; } = "OK";
    public string? SecondaryText { get; init; }
    public DialogKind Kind { get; init; } = DialogKind.Info;
}

public partial class DialogWindow : Window
{
    private readonly DialogOptions _options;

    public DialogWindow(DialogOptions options)
    {
        InitializeComponent();
        _options = options;

        var (accent, soft, line, glyph) = options.Kind switch
        {
            DialogKind.Success => (Res("SuccessBrush"), Res("SuccessSoftBrush"), Res("SuccessBrush"), "✓"),
            DialogKind.Warning => (Res("WarningBrush"), Res("WarningSoftBrush"), Res("WarningBrush"), "!"),
            DialogKind.Danger => (Res("DangerBrush"), Res("DangerSoftBrush"), Res("DangerLineBrush"), "!"),
            _ => (Res("AccentBrush"), Res("Surface2Brush"), Res("LineStrongBrush"), "i"),
        };

        IconCircle.Fill = accent;
        IconGlyph.Text = glyph;
        IconGlyph.Foreground = accent;

        TitleText.Text = options.Title;
        MessageText.Text = options.Message;
        MessageText.Visibility = string.IsNullOrWhiteSpace(options.Message) ? Visibility.Collapsed : Visibility.Visible;

        if (!string.IsNullOrWhiteSpace(options.Warning))
        {
            WarningText.Text = options.Warning;
            WarningText.Foreground = accent;
            WarningBox.Background = soft;
            WarningBox.BorderBrush = line;
            WarningBox.Visibility = Visibility.Visible;
        }

        if (!string.IsNullOrWhiteSpace(options.Detail))
        {
            DetailText.Text = options.Detail;
            DetailText.Visibility = Visibility.Visible;
        }

        PrimaryButton.Content = options.PrimaryText;
        PrimaryButton.Style = (Style)FindResource(options.Kind == DialogKind.Danger ? "DangerButton" : "PrimaryButton");

        if (options.SecondaryText is null)
        {
            SecondaryButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            SecondaryButton.Content = options.SecondaryText;
        }

        if (!string.IsNullOrWhiteSpace(options.RequireCheckText))
        {
            ConfirmCheckText.Text = options.RequireCheckText;
            ConfirmCheck.Visibility = Visibility.Visible;
            PrimaryButton.IsEnabled = false;
            ConfirmCheck.Click += (_, _) => PrimaryButton.IsEnabled = ConfirmCheck.IsChecked == true;
        }

        // O foco inicial vai para a opção segura (cancelar) quando existir.
        Loaded += (_, _) => (options.SecondaryText is not null ? SecondaryButton : PrimaryButton).Focus();
    }

    private Brush Res(string key) => (Brush)FindResource(key);

    private void Primary_Click(object sender, RoutedEventArgs e)
    {
        if (_options.RequireCheckText is not null && ConfirmCheck.IsChecked != true) return;
        DialogResult = true;
    }

    private void Secondary_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try { DragMove(); } catch { /* ignorado: botão já foi solto */ }
    }
}
