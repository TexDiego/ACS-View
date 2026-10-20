# Controle Vetorial — implementação e validação

Data: 15/10/2026. Alterações locais, sem commit, push, publicação ou release.

## Estrutura encontrada e decisões

O projeto usa .NET 10 / MAUI 10.0.110, CommunityToolkit.Mvvm e SQLiteAsyncConnection
(sqlite-net-base, a mesma API sqlite-net já adotada no aplicativo).
As páginas `VetorialControl` e `VetorialControlOptions`, suas ViewModels, o DI,
as rotas `vetorialcontrol` / `vetorialcontroloptions` e os ícones de toolbar já
existiam no checkout e foram aproveitados. Não foram duplicados registros ou rotas.
As alterações locais anteriores em csproj, DI, MauiProgram, Shell e listagem foram preservadas.

O caminho implementado é página → ViewModel → IHouseService / HouseService →
IHouseRepository / SQLiteHouseRepository → banco existente. Dialogs e navegação
continuam usando BaseViewModel, IDialogService e INavigationService.

A residência real é `House`, identificada por `CasaId`, isolada por `UserId` e
protegida por `RowRevision`. O número do endereço já era textual. Não havia
contrato que restringisse quadras a números; portanto, `SetorCensitario` e
`NumeroQuadra` são `string?`, com trim e sem conversão numérica, preservando
`001`, códigos longos e quadras como `12A`, `12-B` e `A`.

Foi encontrado também `TerritorialAssignment`, usado pelo serviço relacional
para histórico, com códigos censitários e códigos operacionais distintos.
Os campos de House representam a classificação atual; o histórico existente
continua registrando episódios. Cadastro individual, lote e o método anterior
AssignTerritoryAsync mantêm ambos coerentes na mesma transação. AreaCode e
SectorCode existentes são preservados. Não foi criada outra tabela de residências.

## Persistência e migração

A migração 5 (`house-territory`) adiciona colunas TEXT NULL e índice por
usuário/ativo/setor/quadra, sem recriar House ou alterar IDs.
Residências sem classificação permanecem null. Quando há histórico territorial
ativo anterior, a migração recupera os códigos para as colunas recém-introduzidas.
Ela preserva os campos originais, inclusive RowRevision: o trigger de revisão é
suspenso e restaurado dentro da transação de migração, sem janela de escrita externa.

Mantidos backup pré-migração, fingerprints de preservação, ledger SchemaMigration,
PRAGMA user_version, integrity_check e foreign_key_check. Uma falha na migração 5
reverte também a alteração de schema; há teste específico para isso.

Na criação, a residência e seu histórico são transacionais. Se ocorrer rollback,
o ID gerado por sqlite-net é restaurado no objeto para impedir uma tentativa
posterior de edição usando uma identidade cuja criação falhou.

## Cadastro individual

`AddHouse.xaml` ganhou a seção discreta Organização territorial, com dois campos
opcionais ligados diretamente ao HouseModel persistido. A consulta de CEP mantém
setor, quadra, revisão e estado ativo. Em falha de gravação, o formulário permanece
preenchido, permitindo correção ou recarga em vez de perder a edição.

## Hierarquia visual e atualização

Uma única CollectionView, dentro de uma linha com altura limitada pelo Grid,
virtualiza cabeçalhos de setor, cabeçalhos de quadra e residências, com três templates.
Não há listas scrolláveis aninhadas. Os modelos de apresentação não são entidades SQLite.

A ordem é setor → quadra → rua → número → complemento → ID. O comparador natural
compara sequências de dígitos por magnitude, sem conversão para inteiro e sem
remover zeros dos códigos. Códigos distintos como `01` e `1` continuam distintos.

Residências sem um dos campos ficam em Não classificados. Classificações parciais
são exibidas com os valores existentes, para que a informação não desapareça.
Tocar uma residência abre a rota de edição existente, `addhouse`, com `houseId`.
O único ícone na toolbar do Controle Vetorial abre as opções, com tooltip
Configurar residências em lote. A toolbar de residências mantém os dois ícones.

OnAppearing compara HousesVersion e SessionVersion. Atualizações individuais,
em lote e pelo serviço territorial anterior invalidam HousesVersion. Assim, voltar
à tela atualiza a hierarquia, e reaparecer sem alterações reutiliza a lista.
A troca de sessão limpa os dados da configuração e impede aplicar um lote antigo.

## Busca, parsing e seleção acumulativa

A busca é explícita pelo botão Buscar: não consulta a base a cada tecla.
SearchTextNormalizer normaliza a comparação para ignorar acentos e caixa,
reutilizando SearchRua sem alterar os textos de endereço exibidos.
Os termos da rua são combinados; o usuário confere os endereços retornados antes
de adicioná-los ao lote. A busca aceita limites opcionais e paridade Todos/Pares/Ímpares.

TerritorialRules.NumericPart concentra o parsing do prefixo decimal:
450 → 450; 450A → 450; 450-B → 450; S/N e Fundos → null.
Valores sem prefixo ou com overflow ficam fora de filtros numéricos/paridade.
Sem esses filtros, continuam disponíveis na busca por rua e na seleção individual.
Os limites são inclusivos; limites negativos, não inteiros ou invertidos são rejeitados.

TerritorialSelection usa HashSet para IDs, trechos com IDs de origem, seleções
individuais e exclusões explícitas. Buscar outra rua não limpa o lote. Repetir o
mesmo trecho não duplica a seleção. Remover um trecho preserva IDs ainda cobertos
por outro trecho ou seleção individual. Desmarcar um resultado o exclui do lote;
selecionar novamente todos os resultados permite incluí-lo novamente.

O total do lote permanece visível. Os trechos exibem os critérios e os IDs ainda
selecionados por aquele critério. Trechos sobrepostos podem contar o mesmo endereço
em seus resumos; o total global e a atualização contam cada residência uma única vez.
Os trechos são exibidos em BindableLayout no cabeçalho da lista, sem outro scrolling;
as residências de resultado usam os itens virtualizados da CollectionView.

## Revisão, sobrescrita e gravação

O botão Revisar e aplicar lote primeiro valida seleção, setor e quadra, relê os
registros selecionados e apresenta os trechos, o destino e a quantidade afetada.
Quadra é um identificador textual: exige conteúdo no lote e admite letras e hífens.
Nenhum dos campos é obrigatório no cadastro individual.

Se qualquer campo territorial já estiver preenchido, são oferecidas as opções
Alterar todas as selecionadas ou Alterar apenas residências não classificadas.
Esta última preserva também classificações parciais. Uma confirmação final informa
quantos registros serão alterados; cancelar mantém banco e seleção.

O repositório verifica novamente usuário, atividade, presença dos IDs e RowRevision
contra a revisão. Se algo mudou, rejeita o lote e pede nova revisão. As leituras por
IDs são divididas em grupos de 400 parâmetros para respeitar limites SQLite.
Updates e histórico são executados em uma transação; qualquer erro reverte tudo.
Após sucesso, a seleção é limpa, há feedback e a versão de residências é invalidada.

## Arquivos da implementação

- Domain/Entities/House.cs
- Domain/ValueObjects/TerritorialRules.cs (novo)
- Application/DTOs/TerritorialSelection.cs (novo)
- Application/Interfaces/IHouseRepository.cs
- Application/Interfaces/IHouseService.cs
- Infrastructure/Data/Migrations/SchemaMigrator.cs
- Infrastructure/Data/Migrations/RelationalStore.cs
- Infrastructure/Data/SQLite/SQLiteHouseRepository.cs
- UseCases/Services/HouseService.cs
- UseCases/Services/RelationalDataService.cs
- ViewModels/AddHouseViewModel.cs
- ViewModels/VetorialControlViewModel.cs (esboço existente)
- ViewModels/VetorialControlOptionsViewModel.cs (esboço existente)
- Controls/VectorControlTemplateSelector.cs (novo)
- Views/AddHouse.xaml
- Views/HousesPage.xaml (tooltips dos ícones)
- Views/VetorialControl.xaml e .xaml.cs (esboços existentes)
- Views/VetorialControlOptions.xaml e .xaml.cs (esboços existentes)
- Tests/ACSView.MigrationTests/VectorControlTests.cs (novo)
- Tests/ACSView.MigrationTests/Program.cs
- Tests/ACSView.MigrationTests/PopupViewModelPlatform.cs
- Tests/ACSView.MigrationTests/ACSView.MigrationTests.csproj
- Tests/ACSView.MigrationTests/SusEntityTests.cs (fixture de downgrade remove versões posteriores)
- Tests/ACSView.AuthTests/ACSView.AuthTests.csproj
- Tests/ACSView.BolsaFamiliaTests/ACSView.BolsaFamiliaTests.csproj
- Tests/ACSView.ImportTests/ACSView.ImportTests.csproj
- docs/vector-control.md (este relatório)

Os três projetos de teste acima receberam apenas a referência explícita ao novo
arquivo de regras de domínio, pois suas compilações usam fontes vinculadas.

## Testes e resultados

A suíte MigrationTests passou 285 verificações, incluindo 64 verificações
específicas do Controle Vetorial. Executa serviços/repositórios reais
sobre SQLite e as ViewModels de produção, substituindo somente a infraestrutura MAUI.
Inclui ordenação natural, zeros/códigos longos, parsing, intervalo, paridade,
acúmulo/duplicidade/sobreposição, desmarcação, cancelamento, sobrescrita em ambos
os modos, classificação parcial, usuário incorreto, revisão obsoleta, rollback de
lote/criação/migração, cadastro/edição/CEP, rotas, invalidação de tela e fechar/reabrir banco.

As outras dez suítes também passaram:

| Suíte | Resultado |
| --- | --- |
| AuthTests | 76 verificações |
| BolsaFamiliaTests | 22 verificações |
| ImportTests | Todas as verificações de importação/persistência/CNS |
| MetricTests | 32 verificações |
| PatientLoadTests | 20 verificações |
| ReminderTests | 32 verificações |
| SessionTests | 6 verificações |
| SnackbarTests | 14 verificações |
| ACSView.Tests | 10 grupos de testes |
| UpdateTests | 26 verificações |

Medição sintética da última execução: hierarquia de 10.000 residências em 12 ms;
transação de 2.000 residências e histórico em 360 ms. Esses valores medem lógica
em desktop, sem representar renderização/rolagem ou armazenamento de um celular.

Comandos de reprodução na raiz do checkout:

```powershell
dotnet run --project Tests/ACSView.MigrationTests --no-restore
dotnet build 'ACS View.csproj' -f net10.0-android -c Debug --no-restore
dotnet build 'ACS View.csproj' -f net10.0-android -c Release --no-restore
Get-ChildItem Tests -Directory | ForEach-Object { dotnet run --project $_.FullName --no-restore }
```

Logs locais: tmp/vector-migration.log, tmp/vector-debug.log,
tmp/vector-release.log e tmp/vector-ACSView.*.log. Não são artefatos publicados.
O sandbox inicialmente bloqueou acesso ao registro do SDK Android e a movimentação
do arquivo temporário de restauração; as verificações completas foram executadas
com acesso apropriado. Não houve alteração no banco real do usuário.

## Builds e warnings

Os builds finais Android Debug e Release terminaram com código 0 e zero erros.
Debug registrou 49 warnings e Release, 245. Os logs incluem warnings já existentes
em modelos/serviços (nullability, async sem await, parâmetro não utilizado),
bindings XAML de outras telas e NU1900 por indisponibilidade do serviço de auditoria NuGet.
Não se trata de um build sem warnings.

As novas páginas acrescentam três XC0025 no Release: o comando de edição da
residência e os comandos de remoção de trecho e seleção por toque usam bindings
com Source/RelativeSource, seguindo o padrão já usado no aplicativo. A compilação
global desses bindings não está habilitada no projeto; eles são resolvidos em runtime.
Não foi habilitada essa opção global para evitar mudar bindings de todas as telas.
Esses três bindings precisam fazer parte da checagem visual em dispositivo.

## Limitações e evolução

ADB não encontrou dispositivo/emulador conectado. Os comandos, as rotas e os XAML
foram verificados por testes e compilação; toque, teclado, tamanhos de tela e
rolagem ainda precisam de validação visual em Android. Não foi instalada uma APK.
Somente builds Android foram solicitados para esta validação local; iOS e Windows
não foram compilados. A migração foi testada em bancos existentes sintéticos,
não em uma cópia de dados reais ou em falha física de armazenamento.

A hierarquia é carregada em memória uma vez por mudança, com virtualização visual.
Para bases muito maiores que as testadas, pode ser útil acrescentar paginação de
projeções territoriais. O lote é transitório: não persiste após encerrar a página/app.
Busca de rua usa termos parciais; o resultado deve ser conferido antes de selecionar.


## Atualização de 16/10/2026 — busca no Controle Vetorial

A página agora contém uma SearchBar no mesmo SearchPanel da página de residências,
com o seletor explícito Pesquisar por: Endereço ou paciente / Setor censitário / Quadra.
O placeholder muda para Buscar rua, número ou paciente, Buscar setor censitário ou
Buscar quadra, respectivamente. Assim, um número digitado é interpretado somente
no contexto escolhido.

O modo padrão reutiliza BuildSearchCriteria do SQLiteHouseRepository, exatamente
como a listagem de residências: rua, endereço combinado, complemento e nome do
paciente ativo, com isolamento por conta e normalização já existente.
A hierarquia é reconstruída somente com as residências encontradas, sem cabeçalhos
vazios e sem mudar a ordenação natural. Limpar a busca restaura a listagem completa.

Setor e quadra usam comparação textual parcial normalizada, sem converter códigos
para número. A lista base desses modos é reutilizada enquanto HousesVersion e
SessionVersion forem as mesmas. A digitação usa debounce de 300 ms; mudanças de
texto/modo invalidam requisições anteriores e sair da página cancela buscas pendentes.

Arquivos ajustados nesta atualização: IHouseRepository, IHouseService,
SQLiteHouseRepository, HouseService, VetorialControlViewModel,
VetorialControl.xaml/.xaml.cs, VectorControlTests e este relatório.

A suíte de migração/integração passou 296 verificações, incluindo 11 novas para
busca: nome normalizado de paciente, equivalência com a página de residências por
rua/número/complemento, separação dos códigos de setor e quadra em relação ao
número da residência, placeholders, troca de modo, limpar busca e debounce.
Logs desta atualização: tmp/vector-search-tests.log, tmp/vector-search-debug.log
e tmp/vector-search-release.log. A validação visual em dispositivo continua pendente.

Os builds Android Debug e Release desta atualização terminaram com código 0 e zero erros.
Os warnings existentes permanecem; não foram introduzidos novos tipos de warnings pela busca.
