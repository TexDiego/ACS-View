# Evolução do acompanhamento de visitas

Implementação em 05/11/2026. Catálogo APS-2026.2; versão anterior APS-2026.1.

## Auditoria do checkout

Checkout real: `S:\Projetos Mobile\Frozen Projects\ACS View`. A alteração local preexistente em `Domain/ValueObjects/Phone.cs` foi preservada.

Confirmados: contador mensal compartilhado indevidamente entre indicadores; consultas repetidas por paciente e condição; consolidação por máximos de contagens incompatíveis; ausência de resumo individual; cache das telas sem passagem do dia; contadores mensais de linhas de cuidado incluindo todos os resultados. `PurgeExpiredVisitsAsync` já era uma operação sem exclusão e continua assim.

Foram inspecionados serviços, interfaces, DTOs, entidades, calculadores, telas/ViewModels de histórico, sugestões, lote familiar e informações pessoais; DI em `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, migrações relacionais, projeção de datas legadas, DataChangeTracker, consumidores familiares e harnesses de teste.

## Arquivos e responsabilidades

| Arquivos | Responsabilidade |
|---|---|
| `Domain/ValueObjects/VisitScoringRuleCatalog.cs` | Versão, períodos explícitos, quantidades, intervalo, fonte e elegibilidade; preservação dos pontos locais |
| `Domain/ValueObjects/VisitFollowUpCalculator.cs` (novo) | Avaliação pura, seleção de dias espaçados, expiração, marcos infantis estruturados, episódios e estados |
| `Application/DTOs/PatientVisitSummaryDto.cs` (novo) | Histórico individual, avaliações independentes, regra principal e apresentação compacta |
| `Application/DTOs/VisitSuggestionDto.cs` | Resumo estruturado e texto de acompanhamento; campos antigos representam a regra principal, não soma física |
| `Application/Interfaces/IVisitsService.cs` | Consulta individual com paciente e data de referência |
| `UseCases/Services/VisitsService.cs` | Pipeline compartilhado individual/coletivo, consultas em lote, evidência histórica e consolidação |
| `ViewModels/PersonsInfoViewModel.cs` | Carregamento independente da seção, falha isolada e proteção por versão/paciente dentro do despacho de UI |
| `Views/PersonsInfo.xaml`, `Views/Desktop/DesktopPersonsInfo.xaml` | Card de acompanhamento com estilos compartilhados; desktop ganha linha própria |
| `ViewModels/AllVisitsViewModel.cs`, `ViewModels/VisitSuggestionsViewModel.cs` | Reconsulta ao carregar; eliminação do cache temporalmente obsoleto; rótulos mensais explícitos |
| `Views/AllVisits.xaml`, `Views/VisitSuggestionsPage.xaml` | Estado, progresso, planejamento e prazo; prévia conserva pontuação e fatores |
| `Tests/ACSView.Tests/VisitFollowUpTests.cs` (novo), projeto e `Program.cs` | Regressões unitárias do motor e ligação ao harness existente |
| `Tests/ACSView.MigrationTests/VisitFollowUpIntegrationTests.cs` (novo), `Program.cs` | SQLite real sintético, migração, isolamento, evidências, histórico, lote misto e atualização |

## Regras e datas

O PDF anexado, com 23 páginas, foi conferido por extração e renderização nas páginas 11, 14, 16, 18 e 20. Somente acompanhamento domiciliar entra neste cálculo; visitas não comprovam consultas, exames, imunizações ou procedimentos.

- **C2:** população com menos de dois anos. Primeira visita entre nascimento e nascimento +30 dias, inclusive. Segundo marco exige dois dias distintos entre nascimento e nascimento +6 meses, inclusive. Os marcos têm resultados e prazos independentes: duas visitas tardias podem atender ao segundo marco sem reparar o primeiro. A elegibilidade termina no segundo aniversário. Nascimento nulo, mínimo ou futuro não gera classificação etária; a insuficiência é informada. A projeção SQLite evita que NULL conserve o valor padrão de hoje da entidade, sem regravar dados.
- **C3:** três dias de visitas com evidência Pregnancy no episódio ativo. Início pela DUM; na ausência, pela DPP menos 280 dias. Sem início confiável, resultado insuficiente e sem data inventada. DPP não vira prazo normativo. Planejamento distribui a próxima visita no tempo restante: hoje + dias até a DPP / (quantidade faltante +1), arredondado em dias; DUM permite estimar DPP em 280 dias. DPP ultrapassada recomenda planejamento imediato, sem declarar violação de um prazo normativo para visitas. Mais de um episódio ativo gera insuficiência cadastral.
- **C4–C6:** janela por datas de calendário **[referência -12 meses, referência]**, ambos os extremos inclusivos. Somente Completed, dias distintos e intervalos de pelo menos 30 dias entre todos os dias selecionados. Seleção gulosa cronológica maximiza a quantidade válida. Nenhum reinício mensal ou anual. Para um conjunto satisfeito, cada expiração é reavaliada considerando também visitas redundantes; o prazo exibido é o último dia de manutenção do conjunto atual e o planejamento recomenda nova visita até esse dia. O dia seguinte remove a evidência expirada. A inversão do limite usa AddMonths e trata 29 de fevereiro explicitamente. Se já há déficit, recomenda-se hoje ou o primeiro dia que respeita o intervalo disponível; sem data normativa absoluta inventada.
- **Puerpério:** regra local de uma visita no episódio encerrado por parto, entre EndedAt e EndedAt +42 dias, inclusive, conforme PregnancyCalculator. Não usa pré-natal como evidência puerperal nem inventa parto a partir de DPP. Ao sair do período puerperal, a linha deixa de ser elegível.
- **Rotinas locais:** Bolsa Família/BPC mantêm catálogo mensal de duas visitas; rotina sem vulnerabilidades mantém uma visita mensal. Bolsa Família exige vínculo relacional; não há fonte confiável de BPC no cadastro e não se infere elegibilidade. A rotina genérica aceita os dias Completed sem exigir uma condição específica. Essas rotinas não são apresentadas como indicadores federais.

A data de referência é avaliada por dia de calendário, incluindo os horários daquele dia; dias posteriores são excluídos. Não há persistência de sugestão como agenda ou evento realizado.

## Evidência e consolidação

VisitCareLine é uma fotografia da elegibilidade cadastral ao registrar a visita. Para linhas específicas, exige-se esse vínculo histórico além de Completed e da janela/episódio. Não são criados vínculos retroativos para preencher lacunas. Registros legados sem linhas permanecem na última visita e no histórico, mas não comprovam indicadores específicos; o resumo sinaliza incompletude.

O mesmo dia pode contribuir para diabetes, hipertensão e pessoa idosa quando há os três vínculos. Cada linha conserva avaliação própria. Há uma sugestão por paciente, ordenada por urgência temporal, prazo, fator de vulnerabilidade e pontos. Quantidades de regras incompatíveis não são somadas nem consolidadas por máximos. Pontos são critérios operacionais do aplicativo, não escore clínico validado.

## Compatibilidade e desempenho

Nenhuma tabela, coluna, índice, formato de data ou migração foi alterado. Nenhum registro foi regravado para ajustar indicadores. Mantidos registros, IDs int, relacionamentos, episódios, lote transacional, prevenção de duplicidade, exclusão individual confirmada, filtros, agrupamento por data/família e navegação mensal existente. O limite de navegação de aproximadamente um ano que já existe na ViewModel histórica não foi ampliado; os registros mais antigos continuam armazenados e consultáveis pelo serviço.

Contadores mensais de linhas de cuidado conservam a semântica anterior de **registros de todos os resultados**, agora com rótulo `Registros:`. Total, realizadas, ausentes e recusadas continuam separados. Não foi silenciosamente adicionado filtro Completed aos contadores históricos.

As sugestões usam seis consultas por operação, independentemente de quantos pacientes/condições existam: pacientes, condições, benefícios, episódios, visitas e linhas. Todas filtram usuário; no popup também filtram paciente no SQL. Agrupamento por lookups e avaliação em memória fora da thread de UI. São reutilizados índices existentes, incluindo Visits(UserId,PatientId,VisitDate). O histórico necessário é lido sem expurgo, sem N+1 no cálculo coletivo. Ainda não há benchmark com território de grande volume.

Não há cache de recomendações. As telas reconsultam ao carregar, capturando mudança de usuário, condições, benefícios, gestação, visitas e data mesmo sem alteração do DataChangeTracker. Os caminhos existentes de registro/exclusão continuam marcando VisitsVersion. O popup carrega a seção em paralelo às demais informações, com proteção de versão/paciente e falha isolada; não há timer de atualização enquanto o popup permanece aberto.

## Validação

- Harness de domínio `dotnet run --project Tests/ACSView.Tests`: passou, incluindo **71 novas verificações** de acompanhamento e os testes existentes.
- SQLite/migrações `dotnet run --project Tests/ACSView.MigrationTests`: **326 verificações passaram**, com fixtures exclusivamente sintéticas. Inclui banco legado migrado e resumo individual com visita antiga, reinicialização, lote misto, preservação do histórico, exclusão, futuro, dois usuários e nascimento NULL.
- Regressões `dotnet run --project Tests/ACSView.BugRegressionTests`: **79 verificações passaram**.
- Windows Debug: compilou com **0 erros / 291 avisos** na compilação final. A nova propriedade observável usa partial property para evitar introduzir aviso MVVMTK0045.
- Android Debug: compilou com **0 erros / 50 avisos**. O primeiro --no-restore não tinha assets Android; a compilação com restauração e TargetFrameworks explícito passou.
- Os builds compilam XAML/bindings. Diff verificado por `git diff --check`. Aviso NU1900 ocorreu em teste por indisponibilidade da consulta de vulnerabilidades no NuGet; execução dos testes passou.
- Logs locais: `tmp-visits-domain.log`, `tmp-visits-migration.log`, `tmp-visits-regression.log`, `tmp-visits-android.log`, `tmp-visits-windows.log`.

Os builds foram por destino; não se afirma compilação integral da solução com iOS. Não houve execução em Android físico, inspeção visual do popup em dispositivo/Windows, teste automatizado da corrida de troca de paciente na ViewModel ou teste de falha visual da seção. A proteção foi revisada no código e o XAML compilado. Não houve publicação, assinatura, alteração de identidade, commit ou push.

## Pendências de domínio e validação funcional

Validar visualmente os dois temas, textos e altura do popup, especialmente pacientes com muitos indicadores. Revisar política municipal de Bolsa Família/BPC/rotina mensal e a distribuição de planejamento gestacional; nenhuma obrigação federal é atribuída a essas políticas.

O modelo não vincula VisitCareLine a um PregnancyId nem armazena procedimentos ou início temporal de cada condição. Episódios são delimitados com os dados existentes; episódios inconsistentes/sobrepostos e fotografias históricas cadastrais incorretas exigem reconciliação humana, não reclassificação automática. Uma data de nascimento aparentemente válida, mas incorreta no cadastro, não pode ser identificada automaticamente. Ausência de registro no aplicativo não prova ausência de atendimento externo.
