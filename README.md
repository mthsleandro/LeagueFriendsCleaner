# League Of Legends Friends Cleaner/Purger/Remover

Baixe o aplicativo aqui: https://github.com/M-Minkov/LeagueFriendsCleaner/releases/tag/Release

Pequeno aplicativo que remove os amigos da sua lista no League of Legends, mantendo apenas os que você escolher.

## Como usar

1. Abra o cliente do League of Legends e faça login.
2. Execute o aplicativo uma vez e digite `2` para sair. Isso cria o arquivo `keep_friends.txt` na mesma pasta do executável.
3. Edite o `keep_friends.txt` com os amigos que devem ser mantidos (veja abaixo).
4. Execute o aplicativo novamente. Ele mostra quem será mantido e quantos amigos serão removidos.
5. Digite `1` para confirmar a remoção ou `2` para cancelar.
6. Reinicie o cliente ao final para garantir que a lista seja atualizada corretamente.

## Lista de amigos mantidos (`keep_friends.txt`)

Informe um amigo por linha, em qualquer um dos formatos abaixo (maiúsculas e minúsculas são ignoradas):

| Formato    | Exemplo     | Efeito                                   |
|------------|-------------|------------------------------------------|
| `Nome#TAG` | `Faker#KR1` | Mantém exatamente esse jogador           |
| `Nome`     | `Faker`     | Mantém o jogador com esse nome, qualquer tag |
| `puuid`    | `a1b2c3...` | Mantém o jogador com esse identificador  |

Linhas em branco ou iniciadas com `#` são ignoradas.

Exemplo:

```text
# Amigos que não devem ser removidos
Faker#KR1
MeuDuo#BR1
```

Se o arquivo estiver vazio, todos os amigos serão removidos.

## Observações

- O cliente possui limite de requisições; o aplicativo aguarda 1 segundo a cada 3 remoções para evitar problemas com a Riot Games.

## Créditos

Fork de https://github.com/Sharaf-Mansour/LeagueFriendsCleaner, atualizado e com versão `.exe` para facilitar o uso.
Baseado em [PoniLCU](https://github.com/Ponita0/PoniLCU).
