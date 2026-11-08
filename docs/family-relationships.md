# Parentesco e árvore genealógica

Implementação de 05/11/2026. Código em `S:\Projetos Mobile\Frozen Projects\ACS View`.

## Funcionalidades

- Rede familiar independente da família domiciliar e da residência, isolada pelo perfil autenticado.
- Cadastro, edição e remoção de vínculos pela seção **Vínculos familiares** do formulário Android/Windows. As alterações ficam pendentes até salvar o cadastro.
- Seleção de pessoa com busca de até 50 resultados, debounce de 300 ms, nome, identificador, nascimento e referências de residência/família para distinguir homônimos.
- Parentalidade com papéis editáveis e naturezas biológica, adotiva, socioafetiva ou não informada. Não há restrições por sexo nem exigência de um pai e uma mãe.
- Avós, bisavós, irmãos, tios e descendentes; união estável, casamento, companheirismo; afinidades, padrinhos/afilhados e descrições personalizadas recíprocas.
- Prévia do papel principal, reciprocidade, efeitos demonstráveis e alertas cronológicos. Sugestões de compartilhar um ascendente entre irmãos exigem confirmações individuais e são validadas em conjunto.
- Popup do paciente com seção recolhível, contagens de pessoas distintas, procedência declarada/calculada, natureza dos caminhos e consulta de outro paciente pelo mesmo fluxo existente.
- Página Windows com árvore e rede, seleção, resumo, abertura do cadastro, redefinição da pessoa central, zoom pela roda/botões, restauração, centralização, arraste e filtros.

## Entidades e tabelas

`FamilyRelationship` mantém uma única linha para ambos os sentidos: endpoints de paciente, perfil, código do tipo, rótulos dos dois sentidos, natureza, atividade, datas opcionais, motivo de encerramento, criação, atualização e revisão otimista.

`FamilyRelationshipType` fornece catálogo normalizado de códigos, categorias internas, rótulos, simetria e distância de geração. Novos tipos não exigem novas colunas em Patient ou FamilyRelationship. As regras dos tipos predefinidos ficam em KinshipCatalog; novos tipos com regras próprias precisam ser incluídos nesse catálogo de regras.

Não foram adicionadas colunas à entidade Patient. Os campos legados de pai/mãe e os vínculos domiciliares foram preservados. Identificadores explícitos de pai/mãe são conciliados com a rede; nomes isolados nunca criam pessoas ou vínculos. Remover um vínculo parental limpa somente referências de ID correspondentes, preservando o texto legado como evidência.

Os metadados conjugais pertencem à própria relação para manter uma única identidade e fonte de verdade da união. Os campos opcionais não exigem datas conhecidas. Episódios distintos coexistem; registros equivalentes não são duplicados.

## Migração e integridade

Migração **6 — family-relationships**, no SchemaMigrator existente: expansão aditiva, snapshot prévio, transação, ledger, verificação dos fingerprints das tabelas antigas e integridade SQLite. Falhas revertem tabelas novas, ledger e versão. Repetir a inicialização não duplica registros.

Referências legadas são carregadas em lote por perfil. Conflitos antigos ficam preservados nos campos originais e produzem MigrationIssue `KinshipLegacyConflict`, sem inventar gerações ou impedir a migração de dados válidos.

Há FKs compostas para ambos os endpoints/perfil, restrição de autorrelacionamento, proprietário imutável, normalização de relações simétricas/personalizadas, índices nas duas direções e índice de unicidade equivalente. Excluir uma relação nunca exclui pacientes. O arquivamento individual mantém o histórico conforme a política existente; a limpeza explícita de pacientes remove dependências primeiro.

A gravação de Patient/SUS/projeções e do conjunto de parentescos usa a mesma transação por PatientPersistence e KinshipService.SaveRegistrationAsync. Inclui conciliação dos seletores antigos de pai/mãe. Falhas restauram a identidade do paciente novo em memória. Outros passos clínicos do formulário conservam seu fluxo preexistente.

Limites ativos contam pessoas distintas: 2 ascendentes diretos, 4 avós, 8 bisavós, combinando declarações e caminhos demonstráveis. A mensagem identifica o paciente e os parentes envolvidos. Ciclos ancestrais são detectados por ordenação topológica, incluindo declarações de gerações intermediárias desconhecidas. Datas suspeitas geram alertas, sem bloqueio rígido por idade. Edições concorrentes verificam revisão de paciente e relação dentro da transação.

## Inferência

KinshipEngine é puro, compartilhado e independente de páginas/banco. Mantém somente declarações persistidas. Percorre ascendência/descendência até bisavós/bisnetos, usa ascendentes diretos compartilhados para identificar irmãos e compõe irmandade/parentalidade para tios e sobrinhos. Declarações de avô/bisavô podem contribuir com sua distância conhecida sem criar indivíduos intermediários.

Resultados deduplicam pessoa/tipo e agregam declarações e caminhos, conservando IDs das evidências e naturezas parentais. Dois ascendentes conhecidos compartilhados identificam irmãos completos na parentalidade cadastrada. A classificação de meio-irmãos biológicos exige dois ascendentes conhecidos de cada pessoa, exatamente um compartilhado e naturezas biológicas em todos os vínculos considerados. Dados incompletos permanecem explicitamente incompletos.

Não há inferência de parentalidade por casamento, afinidade, padrinhos ou vínculos personalizados. Não há cache persistente de inferências: remoções e edições aparecem na próxima construção, sem dados derivados obsoletos. A análise e as prévias são executadas fora da thread de interface. A carga da rede usa consultas em lote, sem uma consulta por nó.

## Gráficos Windows

FamilyGraphBuilder compartilha os dados e regras com o cadastro. Usa busca em largura com profundidade limitada e uma identidade visual por paciente. O layout separa gerações nos dois modos, agrupa casais e coparentes conhecidos e mantém blocos de irmãos contíguos. Varreduras e compactação isotônica alinham ascendentes com as subárvores dos filhos, sem sobrepor cartões. A rede usa essa mesma base familiar, preservando também todos os outros vínculos; o layout circular anterior foi substituído.

GraphicsView/IDrawable desenha conexões e cartões sem criar uma árvore de controles por paciente. Há descarte de nós fora do viewport durante o desenho. Filhos com os mesmos pais, natureza e situação compartilham ramais de filiação. Corredores sem sobreposição horizontal reutilizam faixas; os demais usam faixas distintas. Conexões laterais longas passam fora dos cartões. Trechos colineares equivalentes são unificados. Vínculos parentais de naturezas diferentes entram em pontos distintos do cartão do filho, evitando sobreposição até a extremidade da conexão. A árvore recolhe declarações redundantes de avós/irmãos e múltiplos episódios da mesma união, conservando os registros e o resumo da pessoa. A rede oferece Mostrar todas as conexões e, por padrão, destaca somente os vínculos da pessoa selecionada, sem ocultar os cadastros. Ligações parentais têm direção, padrões para as naturezas e legenda; conjugais têm traço duplo e distinguem atividade/encerramento. Declarações sem geração intermediária conhecida são rotuladas; quando há um caminho demonstrado, essa indicação é reconciliada.

Inicial: 3 gerações ascendentes, 2 descendentes. A interface permite 0–12 ascendentes/descendentes e 1–12 níveis na rede. O limite visual padrão é 1.000 pessoas, com aviso quando atingido e filtros para reduzir a rede. O construtor permite configurar até 5.000 nós. O algoritmo não promete um ótimo global de cruzamentos em redes muito complexas.

A página, VM gráfico, rota e eventos WinUI são condicionados a WINDOWS. Nenhum controle ou evento WinUI é exigido no Android. Eventos nativos são desconectados ao sair da página.

## Backup

Integração ao backup individual criptografado existente, sem outro mecanismo de backup. O manifesto passa a versão 2 e inclui FamilyRelationship como tabela pertencente ao perfil. O catálogo de tipos é reconstruído da migração confiável e não importado como código/regra externo.

Exportação/restauração validam referências, perfil e regras genealógicas. Os dois endpoints são remapeados junto com os IDs dos pacientes. Backups v1 de schemas anteriores continuam aceitos; referências parentais legadas são conciliadas após importar. O índice equivalente impede duplicação no destino.

A regra genérica Backup*/ do .gitignore ocultava código-fonte de Application/Backups e Infrastructure/Data/Backups. Foram incluídas exceções somente para os arquivos C# dessas pastas.

## Principais arquivos

- Domain/Entities/FamilyRelationship.cs
- Domain/ValueObjects/KinshipEngine.cs e FamilyGraph.cs
- Application/Interfaces/IKinshipService.cs
- Infrastructure/Data/Migrations/KinshipSchema.cs, PatientPersistence.cs e SchemaMigrator.cs
- Infrastructure/Data/Backups/UserBackupData.cs
- UseCases/Services/KinshipService.cs e UserDataCleanupService.cs
- Infrastructure/Data/SQLite/SQLitePatientRepository.cs
- ViewModels/KinshipEditorViewModel.cs, FamilyGraphViewModel.cs, AddRegisterViewModel.cs e PersonsInfoViewModel.cs
- Views/KinshipSection.cs, KinshipRelationshipPage.cs, Desktop/FamilyGraphPage.cs, formulários/popup XAML das duas plataformas e AppShell.xaml.cs
- Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs

## Validação

- ACSView.KinshipTests: 85 verificações de regras, reciprocidade, naturezas, inferência, histórico, limites mistos, perfil, busca, rollback, edição concorrente, grafo e migração de banco v5 com falha injetada/idempotência.
- ACSView.MigrationTests: 326 verificações.
- ACSView.BackupTests: 96 verificações, incluindo exportação/restauração dos vínculos, remapeamento dos dois endpoints, descrições recíprocas, manifesto v1 e referência externa rejeitada.
- ACSView.PatientLoadTests: 29 verificações, incluindo VM real de parentesco, alterações pendentes, gravação conjunta e resumo do popup.
- ACSView.AuthTests: 166 verificações.
- ACSView.BugRegressionTests: 79; ReminderTests: 32; MetricTests: 32; BolsaFamiliaTests: 22; SessionTests: 6; SnackbarTests: 14; UpdateTests: 31.
- ACSView.Tests e ACSView.ImportTests concluíram sem falhas.
- Builds Debug Windows e Android concluídos sem erros; avisos de nulabilidade e MVVM/WinRT permanecem (os novos VMs seguem o padrão ObservableProperty do projeto). Os logs locais tmp-kinship-*.log registram as execuções.
- A abertura do executável Windows foi inspecionada e chegou ao login.

## Limitações de validação e melhorias futuras

A operação visual autenticada das telas novas, o comportamento real dos gestos e o cadastro em dispositivo Android não foram validados manualmente nesta sessão. Os testes dos ViewModels não substituem esses testes de interface. A implementação não deve ser considerada homologada em dispositivos apenas por compilar.

Melhorias opcionais fora do escopo: exportação de imagens/PDF do grafo, preferências persistidas de filtros e otimização global dos cruzamentos para redes excepcionalmente grandes. Não houve commit, push ou publicação de release. Alterações anteriores de autenticação, visitas, backup e Phone.cs foram preservadas.

## Comandos principais

```powershell
dotnet run --project Tests/ACSView.KinshipTests/ACSView.KinshipTests.csproj
dotnet run --project Tests/ACSView.PatientLoadTests/ACSView.PatientLoadTests.csproj
dotnet run --project Tests/ACSView.MigrationTests/ACSView.MigrationTests.csproj
dotnet run --project Tests/ACSView.BackupTests/ACSView.BackupTests.csproj
dotnet build 'ACS View.csproj' -f net10.0-android -p:TargetFrameworks=net10.0-android
dotnet build 'ACS View.csproj' -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0
```

Executar os builds MAUI de plataformas diferentes sequencialmente, pois compartilham obj/project.assets.json. Se o servidor de compilação ficar sem resposta, usar `-p:UseSharedCompilation=false`.

## Correção de cônjuges e revisão do layout em 05/11/2026

O usuário relatou um encerramento ao tentar cadastrar cônjuge. A consulta dos eventos Windows não identificou uma exceção do ACSView que confirmasse a causa exata. O bloqueio de autorrelacionamento já existia no motor/SQLite; foram acrescentadas verificações imediatas no formulário e no VM, proteção contra confirmações duplicadas e tratamento de exceções dos comandos de abertura/navegação.

As confirmações são apresentadas pela página modal ativa, quando houver, em vez de serem hospedadas pelo Shell atrás do editor.

A escolha da pessoa usa botões explícitos em uma CollectionView sem seleção nativa. Isso elimina a dependência do ciclo SelectionChanged do WinUI. A atualização dos campos após mudar o tipo é despachada para depois do evento do Picker. Respostas de buscas antigas ou de páginas em fechamento são descartadas. Um erro mantém o formulário aberto com uma mensagem e permite tentar um vínculo válido em seguida.

O novo teste de layout usa 166 pessoas, quarenta filhos com seus próprios cônjuges e filhos, avós, união histórica, declaração redundante de avô e irmandade. Verifica casais adjacentes, ausência de cartões sobrepostos, ausência de linhas atravessando cartões, coordenadas finitas, composição estável apesar da ordem do banco, preservação de todos os vínculos na rede, natureza parental e limite de nós. Também verifica que um avô redundante não afasta seus ascendentes do filho correspondente. Renderizações geométricas dessa família sintética foram inspecionadas; isso não substitui a confirmação do caso original na interface autenticada.

Os testes de formulário cobrem tentativa de autocônjuge sem alterar os vínculos pendentes, falha ao abrir o editor convertida em mensagem e inclusão posterior de um cônjuge válido. Logs desta revisão: tmp-family-layout-tests.log, tmp-family-layout-patientload.log, tmp-family-layout-windows.log e tmp-family-layout-android.log. Não houve alteração de esquema ou dos relacionamentos salvos nesta revisão.

## Revisão visual da página em 06/11/2026

Cabeçalho reorganizado em alternância de modo, barra de zoom com percentual/restauração/centralização e filtros em colunas alinhadas. Os controles de gerações e os filtros de rede aparecem somente no modo correspondente. Em janelas menores, as barras se reorganizam em linhas sem misturar rótulos e controles.

O rodapé mantém o nome, a referência do cadastro e as ações principais. A lista de vínculos fica recolhida por padrão e pode ser expandida. A legenda e as instruções extensas saíram do rodapé e passaram para um painel sob demanda sobre o gráfico. As cores de cartões, seleção, agrupamentos, textos e linhas usam os recursos existentes de tema, com atualização ao alternar claro/escuro.

Foi usada uma prévia MAUI isolada com sete pessoas fictícias e os mesmos arquivos de página/VM/grafo e dicionários de cores/estilos do projeto. Foram conferidos os dois temas, árvore/rede, legenda, abertura de filtros, detalhes recolhíveis e janela compacta de 840×780. Essa prévia não abre bancos nem persiste registros. O seletor recebeu correção de altura/título interno para evitar recorte do valor no Windows.

O build Windows concluiu com zero erros e avisos remanescentes, com saída em tmp/family-design-build para não interromper o ACS View aberto. Logs: tmp-family-design-windows.log e tmp-family-design-preview.log. Esta revisão altera somente a apresentação Windows; o algoritmo e a persistência dos relacionamentos permanecem os mesmos.
