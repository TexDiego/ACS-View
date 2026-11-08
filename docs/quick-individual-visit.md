# Visita rápida individual

Implementação no checkout `S:\Projetos Mobile\Frozen Projects\ACS View`.

## Arquivos desta implementação

- `Application/Interfaces/IVisitsService.cs`: contrato `RegisterQuickVisitAsync`.
- `UseCases/Services/VisitsService.cs`: confirmação, proteção contra concorrência, validação do perfil e reutilização da persistência convencional por `RegisterVisitCoreAsync`.
- `Application/DTOs/PatientVisitSummaryDto.cs`: quantidade de visitas concluídas no mês de referência no texto do acompanhamento.
- `ViewModels/PersonsInfoViewModel.cs`: comando assíncrono, diálogos existentes, atualização localizada e snackbar; respostas antigas de acompanhamento não substituem uma leitura posterior.
- `Views/PersonsInfo.xaml` e `Views/Desktop/DesktopPersonsInfo.xaml`: ação com CalendarCheckmark24, tooltip e descrição acessível, cores do ThemedIcon existentes.
- `Tests/ACSView.MigrationTests/QuickVisitTests.cs`: 15 verificações novas na infraestrutura SQLite existente.
- `Tests/ACSView.MigrationTests/Program.cs`: execução dos novos testes.
- Este documento.

Os arquivos já tinham alterações de outras tarefas; elas foram preservadas.

## Fluxo e persistência

O comando abre “Registrar visita rápida”, com a pergunta solicitada, nome, data e horário. Cancelar não grava nada. Ao confirmar, captura o horário local e consulta visitas Completed da pessoa e do perfil naquele dia. Se houver, solicita “Visita já registrada”, permitindo cancelar ou “Registrar outra visita”.

O serviço existente recebe somente um PatientId. HouseId e FamilyId são contexto do registro; BatchId fica nulo, e nenhum lote ou registro para outros moradores é criado. O perfil é obtido de ICurrentUserContext, nunca do objeto exibido. Alterações de perfil durante o diálogo são rejeitadas. Um SemaphoreSlim no serviço singleton ignora chamadas simultâneas enquanto o fluxo está aberto; o comando assíncrono também não permite execução concorrente.

RegisterVisitCoreAsync normaliza o registro e reutiliza GetCareLinesAsync e a transação SQLite existente. Visita e snapshots VisitCareLine são gravados atomicamente. A transação revalida pessoa, perfil e quantidade de visitas concluídas observada antes da confirmação: se outro fluxo mudar esse histórico, a operação falha sem gravar, permitindo tentar novamente. O bloqueio de duplicidade do cadastro convencional permanece como estava.

DataChangeTracker.MarkVisitsChanged somente é chamado após commit. O popup permanece aberto e relê GetPatientVisitSummaryAsync; mostra última visita, total realizado no mês e acompanhamento recalculado. O snackbar informa “Visita registrada com sucesso.” As telas de histórico, métricas e sugestões usam os mecanismos de atualização existentes ao serem apresentadas novamente. Erros de persistência apresentam aviso e permitem nova tentativa.

## Histórico e indicadores

São reutilizados Visits, VisitCareLine, GetMonthlyOverviewAsync, GetVisitRecordGroupsAsync, BuildSummary e VisitFollowUpCalculator. Não há histórico paralelo, nova categoria, pacote adicional ou migração de banco.

As regras existentes continuam exigindo elegibilidade, episódio, janela temporal e espaçamento. Duas visitas reais no mesmo dia aparecem como dois registros no histórico e no total mensal, mas não contam como dois dias distintos para indicadores. Os snapshots de linhas de cuidado seguem exatamente a classificação já usada pelo registro convencional; não são inventadas medições, procedimentos ou comprovações clínicas. A modelagem atual comprova as regras de visitas implementadas, não requisitos clínicos externos que ela não armazena.

## Validação

- ACSView.MigrationTests: 341 verificações passaram, incluindo 15 de visita rápida (primeiro registro, cancelamentos, repetição consentida, histórico mensal, outro morador, última visita, próxima sugestão, espaçamento dos indicadores, preservação, concorrência, perfil, troca de perfil, rollback e nova tentativa).
- ACSView.Tests: execução concluída com sucesso, incluindo regras de acompanhamento e agrupamento de registros.
- Windows Debug, net10.0-windows10.0.19041.0: 0 erros, 315 avisos.
- Android Debug, net10.0-android: 0 erros, 50 avisos.
- git diff --check: sem erros de whitespace.

A execução completa de integração precisou das permissões normais do usuário para o teste preexistente de backup, que usa a pasta local do aplicativo. A primeira execução restrita parou nesse teste; a execução posterior completa passou.

Pendente: validar visualmente o popup, sobreposição do diálogo nativo, tooltip, cores, disposição dos três botões e atualização sem fechar o popup em Android e Windows, nos temas claro e escuro. Não foram realizados testes de interface autenticada, emulador ou dispositivo físico. Não houve commit, push ou publicação.

