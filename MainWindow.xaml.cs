using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LOLFriendsCleaner;

public partial class MainWindow : Window
{
    private const int BatchSize = 3;                    // pausa longa a cada N remoções
    private const int BatchPauseMs = 1000;
    private const int MinStepDelayMs = 150;             // pausa curta e variável entre remoções
    private const int MaxStepDelayMs = 350;
    private const int FailurePauseMs = 3000;            // espera extra após uma falha
    private const int MaxConsecutiveFailures = 5;       // aborta se o cliente parar de responder
    private const double EstimatedSecondsPerFriend = 0.75;

    private enum UiState { Loading, Ready, Disconnected, Empty }

    private readonly LcuService _lcu = new();
    private readonly ObservableCollection<FriendItem> _items = new();
    private readonly ICollectionView _view;

    private UiState _state = UiState.Loading;
    private int _toDeleteCount;
    private int _keptByFileCount;
    private bool _suppressCount;
    private bool _busy;
    private bool _closeRequested;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();

        _view = CollectionViewSource.GetDefaultView(_items);
        _view.Filter = FilterItem;
        FriendsList.ItemsSource = _view;

        VersionRun.Text = $" · v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";
        ApplyState(UiState.Loading);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) => DarkTitleBar.Apply(this);

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        if (_busy) return;
        ApplyState(UiState.Loading);

        try
        {
            var friends = await _lcu.LoadFriendsAsync();
            var keep = KeepList.Load();

            var loaded = friends
                .Where(f => !string.IsNullOrEmpty(f.puuid))
                .Select(f => new FriendItem(f, toDelete: !keep.Matches(f)))
                .OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(i => i.TagText, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _suppressCount = true;
            foreach (var old in _items) old.PropertyChanged -= OnItemChanged;
            _items.Clear();
            foreach (var item in loaded)
            {
                item.PropertyChanged += OnItemChanged;
                _items.Add(item);
            }
            _suppressCount = false;

            _toDeleteCount = loaded.Count(i => i.ToDelete);
            _keptByFileCount = loaded.Count - _toDeleteCount;

            ApplyState(_items.Count == 0 ? UiState.Empty : UiState.Ready);
        }
        catch (LcuException ex)
        {
            DisconnectedMessage.Text = ex.Message;
            ApplyState(UiState.Disconnected);
        }
    }

    private async void Reload_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private bool FilterItem(object obj)
    {
        var query = SearchBox.Text.Trim();
        if (query.Length == 0) return true;
        if (obj is not FriendItem item) return false;

        // Ignora maiúsculas/minúsculas e acentos.
        return CultureInfo.CurrentCulture.CompareInfo.IndexOf(
            item.FullName, query, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_view is null) return;
        _view.Refresh();
        RefreshTexts();
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        SearchBox.Focus();
    }

    private void Mark_Click(object sender, RoutedEventArgs e) => SetVisible(true);

    private void Unmark_Click(object sender, RoutedEventArgs e) => SetVisible(false);

    private void SetVisible(bool toDelete)
    {
        var delta = 0;
        _suppressCount = true;
        foreach (var item in _view.Cast<FriendItem>())
        {
            if (item.ToDelete == toDelete) continue;
            item.ToDelete = toDelete;
            delta += toDelete ? 1 : -1;
        }
        _suppressCount = false;

        _toDeleteCount += delta;
        RefreshTexts();
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressCount || sender is not FriendItem item || e.PropertyName != nameof(FriendItem.ToDelete)) return;
        _toDeleteCount += item.ToDelete ? 1 : -1;
        RefreshTexts();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        var targets = _items.Where(i => i.ToDelete).ToList();
        if (targets.Count == 0) return;

        var total = _items.Count;
        var keep = total - targets.Count;
        var all = keep == 0;

        var confirmed = ShowDialogBox(new DialogOptions
        {
            Kind = DialogKind.Danger,
            Title = $"Excluir {targets.Count} {Plural(targets.Count, "amigo", "amigos")}?",
            Message = $"Você tem {total} {Plural(total, "amigo", "amigos")} na lista. " +
                      $"{targets.Count} {Plural(targets.Count, "será removido", "serão removidos")} " +
                      $"e {keep} {Plural(keep, "será mantido", "serão mantidos")}.",
            Warning = all
                ? "Você marcou TODOS os amigos: sua lista ficará completamente vazia. Essa ação não pode ser desfeita por este aplicativo."
                : "Essa ação não pode ser desfeita por este aplicativo. Para reaver alguém, será preciso enviar um novo pedido de amizade.",
            Detail = $"Tempo estimado: {FormatDuration(EstimatedSecondsPerFriend * targets.Count)}, com pausas para respeitar os limites do cliente. " +
                     "Mantenha o League of Legends aberto até o fim.\n" +
                     "Aplicativo não oficial: não nos responsabilizamos por eventuais banimentos.",
            RequireCheckText = "Entendi que a exclusão é permanente e que uso este aplicativo por minha conta e risco.",
            PrimaryText = $"Excluir {targets.Count} {Plural(targets.Count, "amigo", "amigos")}",
            SecondaryText = "Cancelar",
        });

        if (!confirmed) return;
        await RunDeletionAsync(targets, keep);
    }

    private async Task RunDeletionAsync(List<FriendItem> targets, int keep)
    {
        _busy = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        RefreshTexts();

        ProgressBarMain.Maximum = targets.Count;
        ProgressBarMain.Value = 0;
        ProgressName.Text = "Preparando…";
        ProgressCount.Text = $"0 de {targets.Count}";
        ProgressEta.Text = "";
        CancelButton.IsEnabled = true;
        CancelButton.Content = "Cancelar";
        ProgressLayer.Visibility = Visibility.Visible;

        int removed = 0, failed = 0, streak = 0;
        bool cancelled = false, aborted = false;
        var clock = Stopwatch.StartNew();

        try
        {
            for (var i = 0; i < targets.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var friend = targets[i];
                ProgressName.Text = friend.FullName;

                var ok = await _lcu.RemoveFriendAsync(friend.Puuid);
                if (ok) { removed++; streak = 0; }
                else { failed++; streak++; }

                var processed = removed + failed;
                ProgressBarMain.Value = processed;
                ProgressCount.Text = $"{processed} de {targets.Count}";
                var remaining = targets.Count - processed;
                ProgressEta.Text = remaining == 0
                    ? ""
                    : $"faltam {FormatDuration(clock.Elapsed.TotalSeconds / processed * remaining)}";

                if (streak >= MaxConsecutiveFailures) { aborted = true; break; }
                if (remaining == 0) break;

                await Task.Delay(Random.Shared.Next(MinStepDelayMs, MaxStepDelayMs + 1), ct);
                if (!ok) await Task.Delay(FailurePauseMs, ct);
                if (processed % BatchSize == 0) await Task.Delay(BatchPauseMs, ct);
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        finally
        {
            clock.Stop();
            ProgressLayer.Visibility = Visibility.Collapsed;
            _cts?.Dispose();
            _cts = null;
            _busy = false;
        }

        if (_closeRequested)
        {
            Close();
            return;
        }

        var notProcessed = targets.Count - removed - failed;
        ShowResult(removed, failed, notProcessed, keep, cancelled, aborted);

        await LoadAsync(); // atualiza a lista com o que realmente restou
    }

    private void ShowResult(int removed, int failed, int notProcessed, int keep, bool cancelled, bool aborted)
    {
        const string restart =
            "Reinicie o cliente do League of Legends para garantir que a lista de amigos seja atualizada corretamente.";

        DialogOptions options;
        if (cancelled)
        {
            options = new DialogOptions
            {
                Kind = DialogKind.Warning,
                Title = "Operação cancelada",
                Message = $"{removed} {Plural(removed, "amigo removido", "amigos removidos")} antes do cancelamento. " +
                          $"{notProcessed + failed} {Plural(notProcessed + failed, "continua", "continuam")} na lista.",
                Detail = restart,
                PrimaryText = "Fechar",
            };
        }
        else if (aborted)
        {
            options = new DialogOptions
            {
                Kind = DialogKind.Danger,
                Title = "Operação interrompida",
                Message = $"O cliente falhou {MaxConsecutiveFailures} vezes seguidas, então paramos por segurança. " +
                          $"{removed} {Plural(removed, "amigo removido", "amigos removidos")}; " +
                          $"{notProcessed + failed} {Plural(notProcessed + failed, "continua", "continuam")} na lista.",
                Detail = "Verifique se o League of Legends continua aberto e logado, aguarde alguns minutos e tente de novo. " + restart,
                PrimaryText = "Fechar",
            };
        }
        else if (failed > 0)
        {
            options = new DialogOptions
            {
                Kind = DialogKind.Warning,
                Title = "Concluído, com algumas falhas",
                Message = $"{removed} {Plural(removed, "amigo removido", "amigos removidos")}, " +
                          $"{failed} {Plural(failed, "não pôde ser removido", "não puderam ser removidos")} e {keep} {Plural(keep, "mantido", "mantidos")}.",
                Detail = "Os que falharam continuam na lista e podem ser tentados novamente. " + restart,
                PrimaryText = "Fechar",
            };
        }
        else
        {
            options = new DialogOptions
            {
                Kind = DialogKind.Success,
                Title = "Tudo pronto!",
                Message = $"{removed} {Plural(removed, "amigo removido", "amigos removidos")} e {keep} {Plural(keep, "mantido", "mantidos")}.",
                Detail = restart,
                PrimaryText = "Fechar",
            };
        }

        ShowDialogBox(options);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        CancelButton.Content = "Cancelando…";
        _cts?.Cancel();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_busy)
        {
            e.Cancel = true;
            _closeRequested = true;
            CancelButton.IsEnabled = false;
            CancelButton.Content = "Cancelando…";
            _cts?.Cancel();
        }
        base.OnClosing(e);
    }

    private void Risks_Click(object sender, RoutedEventArgs e)
    {
        ShowDialogBox(new DialogOptions
        {
            Kind = DialogKind.Warning,
            Title = "Riscos e como o app funciona",
            Message =
                "• O app conversa com o cliente do League of Legends pela API local (LCU), apenas para listar e remover amigos. Ele não mexe no jogo, não lê a memória do jogo e não dá vantagem em partidas.\n\n" +
                "• A Riot permite ferramentas que usam a API do cliente, mas exige que sejam registradas e usem somente endpoints aprovados. Este app é independente e não foi registrado nem aprovado pela Riot.\n\n" +
                "• Não encontramos registro de punição por remover amigos. A Riot costuma punir scripts, bots e ferramentas que dão vantagem injusta. Mesmo assim, o risco não é zero.\n\n" +
                "• Em 2019 a Riot proibiu temporariamente apps que usam a API do cliente para jogadores na Coreia. Se você joga lá, não use.\n\n" +
                "• Proteções do app: pausas entre as remoções, interrupção após falhas seguidas, confirmação obrigatória e botão de cancelar.",
            Warning = "Não nos responsabilizamos por eventuais banimentos, suspensões ou outros problemas na sua conta. Use por sua conta e risco.",
            Detail = "Criado por Matheus Leandro. Baseado em PoniLCU e no projeto original de Sharaf-Mansour.",
            PrimaryText = "Entendi",
        });
    }

    // =====================================================================
    //  Estado da interface
    // =====================================================================

    private void ApplyState(UiState state)
    {
        _state = state;
        LoadingPanel.Visibility = state == UiState.Loading ? Visibility.Visible : Visibility.Collapsed;
        DisconnectedPanel.Visibility = state == UiState.Disconnected ? Visibility.Visible : Visibility.Collapsed;
        EmptyPanel.Visibility = state == UiState.Empty ? Visibility.Visible : Visibility.Collapsed;
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        var ready = _state == UiState.Ready;
        var total = _items.Count;
        var searching = SearchBox.Text.Trim().Length > 0;
        var visible = ready ? _view.Cast<object>().Count() : 0;
        var noResults = ready && visible == 0;

        FriendsList.Visibility = ready && !noResults ? Visibility.Visible : Visibility.Collapsed;
        NoResultsPanel.Visibility = noResults ? Visibility.Visible : Visibility.Collapsed;

        switch (_state)
        {
            case UiState.Loading:
                StatusDot.Fill = Res("WarningBrush");
                StatusText.Text = "Conectando…";
                break;
            case UiState.Disconnected:
                StatusDot.Fill = Res("DangerBrush");
                StatusText.Text = "Cliente não encontrado";
                break;
            default:
                StatusDot.Fill = Res("SuccessBrush");
                StatusText.Text = $"Conectado · {total} {Plural(total, "amigo", "amigos")}";
                break;
        }

        ShowingText.Text = !ready ? "" : searching ? $"Mostrando {visible} de {total}" : $"{total} {Plural(total, "amigo", "amigos")}";
        CounterText.Text = !ready ? "" : $"{_toDeleteCount} para excluir  ·  {total - _toDeleteCount} para manter";
        CounterText.Foreground = Res(_toDeleteCount > 0 ? "DangerBrush" : "SuccessBrush");

        MarkButton.Content = searching ? "Marcar resultados" : "Marcar todos";
        UnmarkButton.Content = searching ? "Desmarcar resultados" : "Desmarcar todos";
        var canEdit = ready && !_busy;
        SearchBox.IsEnabled = canEdit;
        MarkButton.IsEnabled = canEdit && visible > 0;
        UnmarkButton.IsEnabled = canEdit && visible > 0;
        ReloadButton.IsEnabled = !_busy;

        var keep = total - _toDeleteCount;
        if (!ready)
        {
            SummaryText.Text = "Aguardando o cliente…";
            SummaryHint.Text = "Abra o League of Legends, faça login e clique em Recarregar.";
        }
        else if (_toDeleteCount == 0)
        {
            SummaryText.Text = "Nenhum amigo marcado para exclusão";
            SummaryHint.Text = "Marque na lista quem você quer remover. Por padrão, todos vêm marcados.";
        }
        else
        {
            SummaryText.Text = $"{_toDeleteCount} {Plural(_toDeleteCount, "amigo será excluído", "amigos serão excluídos")}";
            SummaryHint.Text = $"{keep} {Plural(keep, "será mantido", "serão mantidos")} · tempo estimado: {FormatDuration(EstimatedSecondsPerFriend * _toDeleteCount)}" +
                               (_keptByFileCount > 0 ? $" · {_keptByFileCount} já desmarcados por keep_friends.txt" : "");
        }

        DeleteButton.Content = _toDeleteCount > 0
            ? $"Excluir {_toDeleteCount} {Plural(_toDeleteCount, "amigo", "amigos")}"
            : "Excluir";
        DeleteButton.IsEnabled = canEdit && _toDeleteCount > 0;
    }

    private bool ShowDialogBox(DialogOptions options)
    {
        DimLayer.Visibility = Visibility.Visible;
        try
        {
            var dialog = new DialogWindow(options) { Owner = this };
            return dialog.ShowDialog() == true;
        }
        finally
        {
            DimLayer.Visibility = Visibility.Collapsed;
        }
    }

    private Brush Res(string key) => (Brush)FindResource(key);

    private static string Plural(int n, string singular, string plural) => n == 1 ? singular : plural;

    private static string FormatDuration(double seconds)
    {
        if (seconds < 60) return "menos de 1 min";
        var minutes = (int)Math.Ceiling(seconds / 60);
        if (minutes < 60) return $"{minutes} min";
        var h = minutes / 60;
        var m = minutes % 60;
        return m == 0 ? $"{h} h" : $"{h} h {m} min";
    }
}
