<p align="center">
  <img src="Assets/icon.png" width="120" alt="Ícone do LoL Friends Cleaner"/>
</p>

<h1 align="center">LoL Friends Cleaner</h1>

<p align="center">
  Limpe sua lista de amigos do League of Legends escolhendo quem sai e quem fica.<br/>
  <strong>Criado por Matheus Leandro</strong>
</p>

Baixe o aplicativo aqui: https://github.com/M-Minkov/LeagueFriendsCleaner/releases/tag/Release

## Recursos

- Interface em **modo escuro** que lista **todos os seus amigos**.
- Todos vêm **marcados para exclusão por padrão**; desmarque quem você quer manter (clique na linha inteira).
- **Pesquisa** por nome ou tag (ignora maiúsculas e acentos) com botões para **marcar / desmarcar os resultados**.
- **Confirmação clara** antes de excluir: quantos serão removidos, quantos ficam, tempo estimado e caixa de ciência de que a ação é irreversível.
- **Progresso em tempo real**, com botão para **cancelar** a qualquer momento.
- Resumo final do que foi removido, mantido ou falhou.

## Como usar

1. Abra o cliente do League of Legends e faça login.
2. Execute o `LOLFriendsCleaner.exe`. Ele se conecta ao cliente e carrega sua lista automaticamente.
3. Desmarque os amigos que devem ser **mantidos** (use a pesquisa para achar alguém rápido).
4. Clique em **Excluir N amigos**, leia a confirmação e marque a caixa de ciência.
5. Aguarde o fim e **reinicie o cliente do LoL** para garantir que a lista seja atualizada corretamente.

> Se o cliente do LoL estiver rodando como administrador, execute o aplicativo também como administrador.

### `keep_friends.txt` (opcional)

O arquivo da versão anterior continua funcionando: se existir na mesma pasta do `.exe`, os amigos listados já começam **desmarcados** (serão mantidos). Ele não é mais criado automaticamente.

| Formato    | Exemplo     | Efeito                                       |
|------------|-------------|----------------------------------------------|
| `Nome#TAG` | `Faker#KR1` | Mantém exatamente esse jogador               |
| `Nome`     | `Faker`     | Mantém o jogador com esse nome, qualquer tag |
| `puuid`    | `a1b2c3...` | Mantém o jogador com esse identificador      |

Linhas em branco ou iniciadas com `#` são ignoradas.

## Proteções do aplicativo

- Pausa curta e aleatória entre as remoções + pausa de 1 s a cada 3 remoções (para não sobrecarregar o cliente).
- Espera extra após qualquer falha e **interrupção automática após 5 falhas seguidas**.
- Confirmação obrigatória (com caixa de ciência) e botão de cancelar durante a execução.
- Ele só lista amigos e remove os que você marcou. Não toca no jogo, não lê a memória do jogo e não dá vantagem em partidas.

## ⚠️ Riscos e aviso legal

**Este é um aplicativo independente e não oficial. Não nos responsabilizamos por eventuais banimentos, suspensões ou outros problemas na sua conta. Use por sua conta e risco.**

O que apuramos sobre risco de banimento:

- O app usa a **API local do cliente (LCU)**. A Riot aceita ferramentas que usem essa API, mas exige que elas sejam registradas e usem **apenas endpoints da lista aprovada** ([política de 2019](https://www.riotgames.com/en/DevRel/changes-to-the-lcu-api-policy)). Este app **não foi registrado nem aprovado** pela Riot, e não conseguimos confirmar se os endpoints de amigos estão na lista aprovada.
- A [página oficial de apps de terceiros](https://support-leagueoflegends.riotgames.com/hc/en-us/articles/225266848) cita suspensões para scripts, bots e ferramentas que comprometem a integridade do jogo. **Não encontramos registro de punição por remover amigos**, mas isso não é garantia: o risco é baixo, não zero.
- Em 2019 a Riot proibiu, "por ora", apps que usam a API do cliente para jogadores **na Coreia**. Não confirmamos se isso segue valendo; se você joga lá, não use.
- Em outubro/2026 a Riot passou a bloquear o acesso de apps não autorizados à memória do jogo. Segundo a própria Riot, [isso não gera banimentos por si só](https://support.riotgames.com/ar-ae/riot/performance/game-memory-access-removed-for-third-party-apps), mas ferramentas não autorizadas podem deixar de funcionar.

*League of Legends e Riot Games são marcas registradas da Riot Games, Inc. Este projeto não é endossado pela Riot Games e não reflete as opiniões da Riot Games nem de quem produz ou gerencia o League of Legends.*

## Compilar

Requer o [.NET 9 SDK](https://dotnet.microsoft.com/download) e Windows (a interface usa WPF).

```bash
dotnet run
# ou, para gerar o .exe único:
dotnet publish LOLFriendsCleaner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O workflow `.github/workflows/build.yml` faz esse build automaticamente (manual ou ao criar uma tag `v*`). O executável fica em `bin/Release/net9.0-windows/win-x64/publish/`.

## Créditos

Criado e mantido por **Matheus Leandro**.

Fork de https://github.com/Sharaf-Mansour/LeagueFriendsCleaner, atualizado e com versão `.exe` para facilitar o uso.
Baseado em [PoniLCU](https://github.com/Ponita0/PoniLCU).
