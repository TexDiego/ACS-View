# Acompanhamento modular no popup individual

Implementação local em 06/11/2026. Nenhum commit, push ou release realizado.

## Inspeção e integração

O checkout com os fontes é `S:\Projetos Mobile\Frozen Projects\ACS View`.
Foram examinados `PersonsInfo`, `DesktopPersonsInfo`, `PersonsInfoViewModel`, os serviços de paciente, gestação e visitas, as entidades clínicas, o cadastro Windows/Android, a persistência, o migrador, a limpeza de perfis e o backup criptografado.
As alterações anteriores do checkout foram preservadas. Em particular, não foram substituídas as regras de visitas, parentesco, autenticação ou o trabalho em `Phone.cs`.

O popup agora usa uma composição compartilhada. Em Windows com pelo menos 860 DIP disponíveis, o acompanhamento aparece à direita, com um intervalo transparente de 12 DIP. O principal permanece com 460 DIP de largura; a composição pode chegar a 1080 DIP, limitada à janela. As alturas têm limites próprios (440/760 DIP) e cada painel possui sua rolagem. Em telas estreitas e Android, a navegação alterna entre os painéis. Ocultar o acompanhamento preserva suas Views, formulários e dados; fechar o principal encerra a sessão.

As ações de WhatsApp, edição, família, abertura de pais vinculados e visita rápida permanecem acessíveis. Os detalhes clínicos e de visitas ficam nos módulos, enquanto o principal mantém condições resumidas e a última visita.

## Módulos e carregamento

- `DiabetesView`: situação de diabetes/insulina, rotina opcional, método de monitoramento, retiradas habituais, estimativa para 30 dias, observações e histórico de retiradas informadas. Inicialmente mostra cinco retiradas; é possível expandir o histórico.
- `HypertensionView`: condição existente e acesso ao acompanhamento de visitas; nenhum campo clínico ou regra foi inventado.
- `PregnancyHealthView`: gestação/puerpério, idade gestacional, DPP, risco informado, sugestão e motivos já calculados, filhos informados e observações.
- `VisitsView`: próxima visita em destaque, última realizada e total do mês em resumo; indicadores com contagem e barra de progresso. Critérios, fontes e marcos ficam em uma seção expansível. O histórico permite selecionar um mês, consultar seus totais e expandir os registros originais sem perder os demais meses.

Cada módulo tem sua própria ViewModel. O registro `HealthModuleRegistry` define aplicabilidade e construção. O hospedeiro é um `ContentView.Content`; nenhuma View é criada pelo pré-carregamento.

`PatientHealthSession` pertence exclusivamente ao popup/paciente/perfil. Seu DTO `PatientHealthContext` fornece identidade e nome. Na seleção do paciente, os dados clínicos começam a carregar imediatamente; visitas são carregadas independentemente; diabetes é antecipado assim que sua aplicabilidade é conhecida. Hipertensão e gestação reutilizam a leitura clínica compartilhada. A interface principal não aguarda essas operações.

O cache guarda Tasks e resultados. Abertura durante pré-carregamento reutiliza a mesma operação. Um `SemaphoreSlim` limita a preparação dos módulos a uma operação por vez. O acesso usa o `SQLiteAsyncConnection` existente, sem envolver consultas síncronas em `Task.Run`. O cálculo de recomendações continua no serviço de visitas existente.

As ViewModels expõem `NotLoaded`, `Loading`, `Loaded` e `Error`, com indicador e nova tentativa. Falhas clínicas não impedem abrir Visitas. Fechar a sessão cancela carregamentos pendentes, rejeita resultados obsoletos, remove assinaturas e libera os recursos após as operações em andamento terminarem. Uma chamada SQLite já iniciada pode terminar fisicamente, mas seu resultado é descartado. A troca de paciente cria outra sessão; identidade do perfil e versão de login são verificadas antes de publicar resultados. Mudanças visuais ocorrem na thread principal.

## Persistência e estimativas

A migração incremental **7** adiciona:

- `PatientGlucoseMonitoring`: ID inteiro autoincremental, proprietário, paciente, aferições/dia, frequência variável, fitas por retirada, intervalo em dias, última retirada conhecida, método e observações. Um registro por perfil/paciente.
- `PatientStripPickup`: ID inteiro autoincremental, proprietário, paciente, data, quantidade e observação. Os registros efetivos permanecem separados da rotina declarada.

Não há exclusão ou transformação destrutiva de tabelas antigas. O migrador existente continua criando snapshot, verificando preservação e registrando a migração. As novas tabelas têm vínculo composto ao paciente/proprietário, restrições de quantidade/intervalo positivos e proteção contra troca de proprietário/paciente. Os serviços também validam entrada, data futura e contexto de login.

O formulário `GlucoseMonitoringForm` é compartilhado entre Diabetes e o cadastro individual Windows/Android. Campos vazios permanecem desconhecidos. Salvar a rotina não substitui retiradas anteriores e remover a condição de diabetes não apaga o histórico. A criação/edição valida os novos campos antes de persistir o paciente e impede salvar se o monitoramento existente não tiver sido carregado.

A estimativa usa aferições/dia × 30 e normaliza a retirada habitual para 30 dias quando houver quantidade e intervalo. Frequência variável ou dados insuficientes impedem a estimativa. Um possível déficit produz apenas uma observação informativa sobre acesso aos insumos.

As duas tabelas integram o inventário de exportação/restauração do perfil, com remapeamento de IDs e validação dos relacionamentos. Backups de versões anteriores recebem coleções vazias para as novas tabelas. A limpeza de perfil foi atualizada para respeitar as dependências.

## Invalidação e atualizações

- Rotina ou retirada: mensagem fraca `PatientHealthChanged`, identificada por paciente/perfil, invalida somente `diabetes`. O editor atualiza após salvar; uma View oculta reutiliza ou atualiza a sessão quando selecionada. Visitas e demais dados clínicos permanecem em cache.
- Visita rápida: usa o serviço e as confirmações existentes, invalida somente `visits`, atualiza o resumo e recarrega o módulo já criado. A proteção existente contra acionamentos concorrentes é preservada.
- Edição geral: atualiza o contexto, compara condições/insulina/gestação e idade, reavalia a aplicabilidade e invalida as recomendações quando suas entradas mudam. A leitura de monitoramento permanece no cache se não foi alterada.
- Operações antigas não publicam sobre uma seleção/contexto posterior. Ocultar um painel não cancela o pré-carregamento nem apaga seu estado.

## Arquivos criados

- `Application/Health/PatientHealthSession.cs`, `HealthModuleData.cs`, `PatientHealthChanged.cs`.
- `Application/Interfaces/IGlucoseMonitoringService.cs`, `IPatientHealthDataService.cs`.
- `Domain/Entities/PatientGlucoseMonitoring.cs` (inclui a entidade de retirada).
- `Domain/ValueObjects/GlucoseMonitoringRules.cs`.
- `Infrastructure/Data/Migrations/GlucoseMonitoringSchema.cs`.
- `UseCases/Services/GlucoseMonitoringService.cs`, `PatientHealthDataService.cs`.
- `ViewModels/PersonsInfoHealthViewModel.cs`.
- `ViewModels/HealthViewModels/HealthModuleViewModel.cs`, `GlucoseMonitoringEditorViewModel.cs`, `DiabetesViewModel.cs`, `VisitsViewModel.cs`, `ClinicalViewModels.cs`.
- `Views/HealthViews/HealthModuleRegistry.cs` e os pares XAML/code-behind `GlucoseMonitoringForm`, `DiabetesView`, `HypertensionView`, `PregnancyHealthView`, `VisitsView`.
- `Tests/ACSView.MigrationTests/HealthMonitoringTests.cs`.
- `Tests/ACSView.PatientLoadTests/HealthModuleTests.cs`.
- Este documento.

## Arquivos alterados por esta implementação

- `Infrastructure/Data/Migrations/SchemaMigrator.cs`.
- `Infrastructure/Data/Backups/UserBackupData.cs`.
- `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`.
- `Platforms/Windows/DesktopPopupLayout.cs`.
- `UseCases/Services/UserDataCleanupService.cs`.
- `ViewModels/PersonsInfoViewModel.cs`, `AddRegisterViewModel.cs`.
- `Views/PersonsInfo.xaml`, `PersonsInfo.xaml.cs`, `PersonsInfoPopupService.cs`.
- `Views/AddRegister.xaml`, `AddRegister.xaml.cs`, `Views/Desktop/DesktopAddRegister.xaml`.
- Projetos e programas de testes `ACSView.MigrationTests` e `ACSView.PatientLoadTests`.
- Programas de testes `ACSView.BackupTests` e `ACSView.KinshipTests` (fixture de banco antigo ajustada para remover as migrações posteriores à versão simulada).

O projeto de testes de carregamento usa CommunityToolkit.Mvvm 8.4.2, alinhado ao aplicativo, para compilar as propriedades observáveis parciais. Não foi adicionada biblioteca de UI.

## Validação executada

| Verificação | Resultado |
|---|---|
| Windows Debug, net10.0-windows10.0.19041.0 | 0 erros; 313 avisos |
| Android Debug, net10.0-android | 0 erros; 49 avisos |
| Migração e integração relacional | 361 verificações aprovadas, incluindo atualização 6 → 7 e 20 verificações de monitoramento/sessão |
| Backup | 102 verificações aprovadas, incluindo conteúdo clínico, remapeamento, isolamento e compatibilidade anterior |
| Módulos e editor | 14 verificações aprovadas após a reformulação de Visitas |
| Cadastro/carregamento | 31 verificações aprovadas, incluindo salvar/reabrir monitoramento e preservar retiradas |
| Autenticação | 166 verificações aprovadas |
| Parentesco | 85 verificações aprovadas |
| Regressões gerais | 79 verificações aprovadas |
| Bolsa Família | 22 verificações aprovadas |
| Métricas | 32 verificações aprovadas |
| Lembretes | 32 verificações aprovadas |
| Sessão | 6 verificações aprovadas |
| Snackbar | 14 verificações aprovadas |
| Atualização | 31 verificações aprovadas |
| Domínio e importação | Suítes aprovadas; domínio inclui 71 verificações de acompanhamento de visitas |
| `git diff --check` | Sem erros de whitespace |

Foram executados os 14 projetos de teste disponíveis. Logs locais: `tmp-health-*.log`. Alguns testes de restauração/autenticação precisaram ser reexecutados fora do sandbox porque a troca atômica de arquivos temporários foi bloqueada pelo ambiente. A fixture antiga de parentesco precisou considerar a nova versão do banco; a validação de integridade em produção foi preservada.

## Limitações e validação manual restante

Os builds e os testes não substituem a inspeção visual do novo popup com uma sessão autenticada. Ainda é necessário validar visualmente no Windows/Android: abertura com painel oculto, alternância, resize, rolagens, teclado, temas claro/escuro e o fluxo com dados reais. Não foi realizado teste em aparelho físico nem compilação/execução iOS neste ambiente Windows.

Não foi medido um tempo real de abertura do popup nem um orçamento de memória em dispositivo. Os testes comprovam compartilhamento de operações e cache, não uma latência específica. Os avisos de compilação existentes permanecem. Nenhum dado real de paciente foi usado nas fixtures.

### Reformulação visual de Visitas

O texto corrido foi substituído por uma hierarquia de leitura: próxima data, resumo recente, indicadores e histórico mensal. Os critérios e registros ficam recolhidos inicialmente. Alterar o mês não consulta o banco e uma atualização preserva o mês selecionado quando disponível. As recomendações, quantidades, fontes, marcos e registros vêm dos mesmos resultados do serviço; nenhuma regra de visita ou tabela foi modificada. Verificações direcionadas: `tmp-visits-design-tests.log`; builds: `tmp-visits-design-windows.log` e `tmp-visits-design-android.log`.

A reformulação de Visitas foi inspecionada no Windows com sessão autenticada e tema escuro: resumo, indicadores, abertura/fechamento dos critérios e estado vazio do histórico. Capturas `09-visitas-reformuladas-resumo.jpg` e `10-visitas-reformuladas-criterios.jpg` foram salvas na pasta de visualizações desta conversa. Windows e Android: builds aprovados com 0 erros; testes direcionados: 14 verificações de módulos/editor e 31 de cadastro aprovadas. A seleção de meses com registros foi validada por testes automatizados; o paciente usado na inspeção visual não possui visitas registradas.

### Ações na borda do popup principal

WhatsApp, edição, visita rápida, acompanhamento e fechamento usam botões de ícone de 44 DIP sobre a borda superior, com tooltip e descrição de acessibilidade. As ações duplicadas foram removidas do conteúdo. Condições de saúde com módulo disponível abrem o acompanhamento ao tocar no resumo; o texto da última visita abre Visitas. O registro de módulos define quais ícones correspondem a cada módulo, sem adicionar uma sequência de condicionais ao popup. Vínculos familiares têm cabeçalho discreto e resumo recolhido no popup; a apresentação dos formulários de cadastro permanece preservada. A altura máxima do principal é 440 DIP, mantendo rolagem independente. Builds: `tmp-popup-edge-windows.log` e `tmp-popup-edge-android.log`.

### Árvore na borda e sexo abreviado

A seção de vínculos familiares foi removida do popup principal. Um botão com ícone de pessoas, tooltip “Abrir árvore genealógica” e descrição acessível usa o comando de navegação da árvore já existente, fechando o popup e mantendo o paciente como centro do grafo. Como a árvore atual é exclusiva do Windows, esse botão segue a disponibilidade `CanDraw` existente e não é exibido no Android. O sexo aparece no canto do cabeçalho como F, M ou I; o texto completo permanece no tooltip e na descrição acessível. O limite de altura do painel principal foi reduzido para 440 DIP, preservando rolagem para cadastros extensos. Builds: `tmp-popup-compact-windows.log` e `tmp-popup-compact-android.log`.

### Fechamento na borda dos módulos adjacentes

O hospedeiro compartilhado dos módulos usa o mesmo botão de ícone na borda do principal, com tamanho, cantos, cores e posição equivalentes. O botão textual Voltar foi removido. O novo controle mantém `CloseHealthCommand`: oculta somente o acompanhamento e preserva a sessão, os módulos já criados e seus dados. No Android retorna ao principal. Builds: `tmp-adjacent-edge-windows.log` e `tmp-adjacent-edge-android.log`.

### Tuberculose, HIV e limpeza

Os módulos TuberculosisView e HivView usam acompanhamentos opcionais persistidos por paciente/perfil/condição na migração 8. O cadastro agrupa visualmente Imunodeficiência / HIV com marcações independentes, preservando o significado dos dados legados. Os painéis foram centralizados verticalmente e a versão desktop antiga foi removida. A lista de exclusões e a validação estão em `docs/cleanup-and-infectious-follow-up.md`. Logs antigos referenciados nas seções históricas deste documento foram removidos na limpeza; os resultados atuais estão em `tmp-infectious-*.log`.

### Cadastro compacto e gestação em seções

Diabetes/insulina, tuberculose e Imunodeficiência / HIV usam resumo e botão Ver detalhes no cadastro mobile/desktop. Selecionar uma dessas condições abre a janela de detalhes, seguindo a interação de gestação. O editor permanece no rascunho até salvar o cadastro; os dados não são apagados ao fechar a janela. A janela tem rolagem própria e Concluir fixo, limitada à área disponível no Windows.

O módulo de gestação agora separa idade gestacional/tempo pós-parto, data de referência e risco informado. A sugestão de risco e seus motivos, assim como dados adicionais e observações, ficam em seções recolhidas inicialmente. Não houve alteração nas regras clínicas ou na persistência. Dados desconhecidos continuam explícitos, sem datas inventadas.

Validação: builds Windows/Android sem erros; 34 verificações de cadastro e 17 de módulos/editor aprovadas. Logs tmp-compact-registration-*.log. O fluxo de diabetes/insulina e tuberculose foi inspecionado no Windows, em rascunho descartado sem salvar.
A sessão Diego exibiu 0 gestantes cadastradas e 0 puérperas ativas. Por isso, a nova gestação foi verificada por build e testes de projeção/expansão; a inspeção visual com dados gestacionais persistidos permanece pendente. Nenhum cadastro foi criado ou alterado durante essa revisão.
