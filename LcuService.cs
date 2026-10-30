using System.Text.Json;
using PoniLCU;

namespace LOLFriendsCleaner;

/// <summary>Erro amigável ao falar com o cliente do League of Legends.</summary>
public sealed class LcuException : Exception
{
    public LcuException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Encapsula a comunicação com o cliente local (LCU) via PoniLCU.</summary>
public sealed class LcuService
{
    private const string FriendsEndpoint = "/lol-chat/v1/friends";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private LeagueClient? _client;

    /// <summary>Conecta ao cliente aberto e devolve a lista atual de amigos.</summary>
    public async Task<List<Friend>> LoadFriendsAsync()
    {
        string raw;
        try
        {
            // A criação do client procura o processo do LoL; roda fora da thread de UI.
            _client = await Task.Run(() => new LeagueClient(LeagueClient.credentials.cmd));
            raw = await _client.Request(LeagueClient.requestMethod.GET, FriendsEndpoint);
        }
        catch (Exception ex)
        {
            _client = null;
            throw new LcuException(
                "Não foi possível encontrar o cliente do League of Legends.\n" +
                "Abra o cliente, faça login e tente novamente. Se ele estiver rodando como administrador, " +
                "execute este aplicativo também como administrador.", ex);
        }

        if (string.IsNullOrWhiteSpace(raw))
            throw new LcuException(
                "O cliente não respondeu. Aguarde o League of Legends terminar de carregar e tente novamente.");

        try
        {
            var friends = JsonSerializer.Deserialize<List<Friend>>(raw, JsonOptions);
            return friends ?? [];
        }
        catch (JsonException ex)
        {
            throw new LcuException(
                "O cliente ainda não está pronto para listar os amigos (o chat pode estar carregando).\n" +
                "Aguarde alguns segundos e clique em \"Tentar novamente\".", ex);
        }
    }

    /// <summary>Remove um amigo. Retorna true se o cliente aceitou a remoção.</summary>
    public async Task<bool> RemoveFriendAsync(string puuid)
    {
        if (_client is null || string.IsNullOrWhiteSpace(puuid)) return false;

        try
        {
            var response = await _client.Request(
                LeagueClient.requestMethod.DELETE,
                $"{FriendsEndpoint}/{Uri.EscapeDataString(puuid)}");

            // Sucesso = resposta vazia (204). Erros do LCU voltam como JSON com "errorCode"/"httpStatus".
            if (string.IsNullOrWhiteSpace(response)) return true;
            return !response.Contains("errorCode", StringComparison.OrdinalIgnoreCase)
                && !response.Contains("httpStatus", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
