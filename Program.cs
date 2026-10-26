using PoniLCU;
using System.Text.Json;
using LOLFriendsCleaner;
using static PoniLCU.LeagueClient;

var leagueClient = new LeagueClient(credentials.cmd);

var data = await leagueClient.Request(requestMethod.GET, "/lol-chat/v1/friends");
var allFriends = JsonSerializer.Deserialize<List<Friend>>(data) ?? [];

// Amizades em keep_friends.txt são mantidas (Um por linha: "Name#TAG", "Name" or Tag).
var keepFilePath = Path.Combine(AppContext.BaseDirectory, "keep_friends.txt");
if (!File.Exists(keepFilePath))
{
    File.WriteAllText(keepFilePath, """
                                    # Amizades para manter (uma por linha). Linhas que começam com # são ignoradas.
                                    # Formatos aceitos: Name#TAG, Name (qualquer tag) ou puuid. Case-insensitive.
                                    # Exemplo:
                                    # Faker#KR1

                                    """);
}

var keepEntries = File.ReadAllLines(keepFilePath)
    .Select(line => line.Trim())
    .Where(line => line.Length > 0 && !line.StartsWith('#'))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

bool ShouldKeep(Friend friend) =>
    keepEntries.Contains($"{friend.gameName}#{friend.gameTag}") ||
    (friend.gameName is not null && keepEntries.Contains(friend.gameName)) ||
    (friend.puuid is not null && keepEntries.Contains(friend.puuid));

var keptFriends = allFriends.Where(ShouldKeep).ToList();
var friends = allFriends.Except(keptFriends).ToList();

Console.WriteLine($"Mantidos: {keepFilePath}");
foreach (var kept in keptFriends)
{
    Console.WriteLine($"Mantendo {kept.gameName}#{kept.gameTag}");
}

Console.WriteLine($$"""
                    Tem certeza que deseja deletar seus amigos? 
                    Você tem {{allFriends.Count}} amigos no jogo, {{keptFriends.Count}} serão mantidos e {{friends.Count}} removidos!
                    Digite 1 para sim, 2 para não
                    """);

int.TryParse(Console.ReadLine(), out var input);
if (input is 1)
{
    var friendIndex = 0;
    foreach (var friend in friends)
    {
        friendIndex++;
        await leagueClient.Request(requestMethod.DELETE, $"/lol-chat/v1/friends/{friend.puuid}");
        Console.WriteLine($"Removed {friend.gameName}");
        if (friendIndex % 3 is 0)
        {
            // o CLient tem um rate limit, o que pode acionar um aviso para a Riot Games (causando um banimento). Evita isso com um atraso.
            await Task.Delay(1000);
        }
    }
    Console.WriteLine($$"""
                        {{friends.Count}} amigos removidos! {{keptFriends.Count}} mantidos.
                        Reinicie o seu jogo para ter certeza que não está bugado!
                        """);
}
else
{
    Console.WriteLine("Exiting...");
    Thread.Sleep(1000);
}
