# CraftFromChests

Mod de cliente para **Valheim** que deixa você craftar, melhorar e construir usando
os itens dos baús que estão por perto, sem precisar tirar nada deles antes.

A lista de receitas, os números de "tenho / preciso" e o botão de craft passam a
contar o conteúdo dos baús próximos. Ao craftar, os itens saem primeiro da sua
mochila e só depois dos baús, do mais perto para o mais longe.

## Versão do jogo analisada

| Item | Valor |
| --- | --- |
| Versão do jogo | 1.0.7 |
| Build Steam | 25185596 |
| Motor | Unity 6000.0.75f1, backend Mono |
| Código do jogo | `valheim_Data/Managed/assembly_valheim.dll` |

Como o backend é Mono (e não IL2CPP), o caminho normal de modding continua valendo:
BepInEx 5 + Harmony, com patches em memória. Nenhum arquivo do jogo é alterado.

## Instalação manual

1. Instale o **BepInEx 5** para Valheim na pasta do jogo, o pacote
   `denikson-BepInExPack_Valheim`. Rode o jogo uma vez para ele gerar as pastas.
2. Copie `CraftFromChests.dll` para `Valheim/BepInEx/plugins/CraftFromChests/`,
   ou extraia o zip `-nexus.zip` por cima da pasta do jogo.
3. Suba o jogo. O log em `Valheim/BepInEx/LogOutput.log` deve trazer uma linha
   `CraftFromChests 1.0.0 loaded`.

O `dotnet build` já copia a DLL para `BepInEx/plugins/CraftFromChests/`
automaticamente, se essa pasta existir.

## Instalação pelo r2modman

O r2modman e o Thunderstore Mod Manager mantêm o BepInEx dentro do perfil deles,
então **não misture com uma instalação manual na raiz do jogo**. Se você já tem
uma, apague de `Valheim/` o `winhttp.dll`, o `doorstop_config.ini`, o
`.doorstop_version`, a pasta `doorstop_libs` e a pasta `BepInEx` antes de migrar.
Dois BepInEx no mesmo jogo é a causa clássica de mod carregando duas vezes ou
não carregando.

Três formas de gerenciar este mod por lá, da mais rápida para a mais completa:

| Forma | Como | O que você ganha e o que perde |
| --- | --- | --- |
| DLL solta | `Import local mod` apontando para `CraftFromChests.dll` | Funciona na hora. Você digita nome e versão na mão, sem ícone e sem dependência automática do BepInEx. |
| Zip local | `Import local mod` apontando para `dist/CraftFromChests-<versão>-thunderstore.zip` | Ícone, versão, descrição e o BepInEx instalado como dependência. Continua privado, nada é publicado. |
| Thunderstore | Publicar o mesmo zip em thunderstore.io | Instalação e atualização pela busca do próprio r2modman, para você e para qualquer outra pessoa. |

A Nexus não entra nessa lista. O r2modman não instala mod da Nexus, e o
gerenciador da Nexus é o Vortex. Publicar nos dois é comum, mas quem baixa da
Nexus instala manualmente ou pelo Vortex.

No r2modman o arquivo de configuração fica dentro do perfil, e não em
`Valheim/BepInEx/config`. O próprio gerenciador tem editor de config.

## Configuração

O arquivo `BepInEx/config/dev.trentini.craftfromchests.cfg` é criado no primeiro
boot e pode ser editado com o jogo fechado.

| Opção | Padrão | O que faz |
| --- | --- | --- |
| `Enabled` | `true` | Desliga o mod inteiro sem removê-lo. |
| `Range` | `20` | Raio em metros de onde os baús são lidos. |
| `UseForBuilding` | `true` | Também paga construções (martelo, enxada) com os baús. |
| `IncludeVehicleContainers` | `true` | Inclui baús de carroças e barcos. |
| `Verbose` | `false` | Loga cada retirada de baú e cada redirecionamento de chamada. |

## Quais baús entram na conta

Um baú só é usado se passar pelas mesmas regras que o jogo aplica quando você
tenta abri-lo:

- está dentro do raio configurado;
- privacidade `Public`, ou `Private` com você como criador da peça (baú de grupo
  fica fora, igual ao vanilla);
- se o baú checa guard stone, você precisa ter acesso ao ward;
- ninguém mais está com ele aberto (baú em uso por outro jogador é ignorado).

Antes de tirar qualquer item o mod reivindica a posse de rede do baú
(`ZNetView.ClaimOwnership`), que é o que autoriza a gravar o inventário de volta
no ZDO. É o mesmo mecanismo que o jogo usa em "pegar tudo", então funciona em
servidor dedicado sem mod do lado do servidor. É um mod **só de cliente**.

## Como funciona por dentro

Os métodos do jogo leem o inventário do jogador por campo (`m_inventory`), sem
nenhum ponto de extensão. Em vez de injetar itens falsos na mochila, o mod
reescreve, por transpiler, apenas as chamadas de inventário dentro de sete
métodos. Uma chamada de instância já tem o `this` como primeiro argumento na
pilha, então um método estático cujo primeiro parâmetro é o `Inventory` é
substituição direta e o resto do corpo do método fica intacto.

Chamadas redirecionadas para `InventoryBridge`:

| Método do jogo | Chamada trocada | Para quê |
| --- | --- | --- |
| `Player.HaveRequirementItems` | `CountItems` | libera a receita e o botão de craft |
| `Player.GetFirstRequiredItem` | `CountItems`, `GetItem` | receitas de ingrediente único (hidromel, banquetes) |
| `Player.ConsumeResources` | `RemoveItem` | cobra o craft e a construção |
| `Player.HaveRequirements(Piece)` | `HaveItem`, `CountItems` | libera a peça de construção |
| `InventoryGui.DoCrafting` | `RemoveItem` | cobra o ingrediente único |
| `InventoryGui.SetupRequirement` | `CountItems` | números de "tenho / preciso" na UI |

Mais três patches de apoio: `Container.Awake` registra os baús que aparecem no
mundo (nada de `FindObjectsOfType`), e `Player.UpdatePlacement` e
`Hud.SetupPieceInfo` marcam o contexto de construção para a opção
`UseForBuilding` poder valer.

Se um dia o jogo mudar e um desses pontos deixar de existir, o transpiler grava
um `LogError` no log do BepInEx dizendo qual chamada não foi encontrada, em vez
de falhar em silêncio.

## Build

```
dotnet build -c Release
```

Se o Valheim estiver em outro caminho:

```
dotnet build -c Release -p:ValheimDir="D:\Steam\steamapps\common\Valheim"
```

As referências do jogo vêm direto de `valheim_Data/Managed`, e o BepInEx/Harmony
vêm do feed NuGet do próprio BepInEx, já configurado em `nuget.config`.

## Empacotar e publicar

```
powershell -ExecutionPolicy Bypass -File packaging\build-package.ps1
```

Gera em `dist/` os dois zips de release:

| Zip | Conteúdo | Para onde vai |
| --- | --- | --- |
| `-thunderstore.zip` | `manifest.json`, `icon.png`, README, changelog e a DLL na raiz | Thunderstore, ou `Import local mod` do r2modman |
| `-nexus.zip` | `BepInEx/plugins/CraftFromChests/CraftFromChests.dll` mais README e changelog | Nexus Mods, o jogador extrai por cima da pasta do jogo |

O script valida o que costuma reprovar upload antes de gerar o zip: versão em
`x.y.z`, ícone exatamente 256 por 256, nome do manifest só com letras, dígitos e
sublinhado, e descrição dentro do limite de 250 caracteres. Ele também confere
que a versão da DLL compilada bate com a do csproj.

A versão vive **só** em `<Version>` no `CraftFromChests.csproj`. O alvo
`GenerateBuildInfo` gera de lá a constante que o atributo `BepInPlugin` usa, e o
script de pacote lê a mesma propriedade para preencher o `manifest.json`. Para
lançar uma versão nova: mexa no csproj, escreva o changelog, rode o script.

O ícone sai de `packaging/make-icon.ps1`, que desenha o PNG por código. Mude a
paleta no topo do arquivo e rode de novo.

Para a página da Nexus, `docs/nexus-description.bbcode` já está em BBCode,
pronto para colar no campo de descrição.

## Checagem de compatibilidade depois de um update

`tools/PatchCheck` abre o `assembly_valheim.dll` com Mono.Cecil e confere que os
nove métodos alvo continuam existindo com a mesma assinatura, que cada chamada de
inventário que os transpilers procuram ainda está lá, e que as assinaturas do
`InventoryBridge` batem com as do jogo.

```
cd tools/PatchCheck
dotnet run
```

Saída esperada: `ALL CHECKS PASSED`. Rode isso primeiro sempre que o Valheim
atualizar, antes de abrir o jogo.

## Limitações conhecidas

- Reparo não custa material no Valheim, então não há nada a fazer ali.
- Fornalhas, fogueiras e outras estações que consomem minério/lenha por interação
  direta não são alimentadas pelos baús. O mod cobre craft, upgrade e construção.
- Um baú que outro jogador tem aberto naquele instante é ignorado até ele fechar.
