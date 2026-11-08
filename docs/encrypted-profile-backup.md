# Backup individual criptografado (.acs)

## Uso e ativação

Em **Perfil → Backup e restauração**, confirme a senha atual da conta, escolha uma
senha independente (12–128 caracteres), confirme-a e salve o `.acs` pelo seletor
nativo. O arquivo pode ser renomeado. Não há serviço remoto, sincronização,
agendamento ou dependência de SecureStorage para abrir o arquivo.

**Restaurar backup** também está disponível no login, antes da autenticação e
independentemente da verificação de atualização. Selecione o arquivo, informe a
senha do backup, valide o resumo e confirme a restauração. Um nome já existente
não é sobrescrito: entre naquela conta, marque a substituição e confirme a senha
local atual e a confirmação específica. Não há mesclagem incremental.

A ativação exige **fechar completamente e abrir novamente o aplicativo**. Este é
o protocolo de ativação da infraestrutura existente; nenhuma conexão SQLite ativa
é substituída. A tela retorna ao login, que permanece bloqueado até reiniciar.
O banco anterior fica somente para leitura após a preparação. No próximo início,
o perfil pode entrar com a senha original; não há login automático. A saída para
um seletor de arquivos preserva o ciclo de vida normal da sessão.

## Responsabilidades e inventário real

`BackupDocument` é uma representação relacional de valores SQLite (inteiros de
64 bits, inclusive ticks de datas, texto e nulos). O documento nunca contém SQL,
DDL, triggers ou caminhos necessários à recuperação. `UserBackupData` concentra
a lista explícita de entidades, validação de propriedade e remapeamento.
`EncryptedBackupService` confirma a senha pelo `AuthService`, coordena snapshots,
preparação e ativação. `BackupEncryptionService` cuida apenas do contêiner e da
criptografia. O ViewModel usa FilePicker e FileSaver do toolkit MAUI existente.

| Categoria | Tabelas/dados | Regra |
|---|---|---|
| Conta | User | Exatamente uma linha; hashes, salts, versão e recuperação preservados; Password e BiometricTokenHash vazios |
| Cadastros | Patient, House | UserId positivo do contexto autenticado, inclusive inativos |
| Famílias | Family, FamilyRecord, FamilyMember, FamilyResidenceHistory, LegacyFamilyMap | Inclui vínculos encerrados e histórico de residências |
| Contatos e CNS | PatientPhone, PatientSusNumber | Propriedade e paciente validados, ordem e principal preservados |
| Clínica | PatientConditions, PatientCID, PatientBolsaFamilia, PatientInsulinDependency, PatientPregnancy | Valida pacientes e responsáveis indiretos |
| Vacinas | Vaccines, PatientVaccineDose | Evidência legada e aplicações históricas; Vaccines.Id referencia Patient.Id |
| Visitas | Visits, VisitBatch, VisitCareLine | Inclui datas mensais, notas, status, lotes e linhas de cuidado |
| Notas e notificações | Note, CareNotification | Inclui lembretes futuros, estados de notificações e chaves derivadas |
| Território/vetores | House, TerritorialAssignment | Classificação atual e histórico territorial; não existe outra tabela de controle vetorial |
| Auditoria | ImportHistory, MigrationIssue | Inclui relatórios, evidências classificadas e referências de origem |
| Referências públicas | CidSubcategory, HealthConditionDefinition | Catálogo público explícito; IDs remapeados por Code, nunca sobrescrevendo entradas locais |
| Referências regeneráveis | CidCategory, CidChapter, CidGroup, CommonConditions | Não exportadas; seeds existentes as reconstroem |
| Cache global | CepAddressCache | Não exportado; cache regenerável, sem propriedade individual |
| Metadados locais | SchemaMigration, SchemaPreservation, LocalAuthIdentity, sqlite_sequence | Nunca transportados do perfil; o banco consolidado conserva seu próprio histórico de migração |
| Preferências do perfil | DashboardMetricPreferences.{UserId} | Única preferência persistente individual identificada; sem IDs de cadastros no DTO |
| Dispositivo/sessão | SecureStorage, biometria Android, Windows Hello, contas lembradas, AuthToken/AuthUserId antigos, permissões, cache de alarmes exatos | Não exportados |

Não foram identificados anexos privados persistentes associados aos cadastros.
Planilhas são entradas de importação, não arquivos necessários ao perfil.
Tabelas novas desconhecidas impedem a exportação até serem classificadas. A coluna
legada escalar `Patient.BolsaFamilia` é preservada e, se necessário, acrescentada
somente na cópia de destino. Outras colunas desconhecidas no destino são
recusadas, evitando transportar referências que não sabemos remapear.

## Isolamento

O serviço de exportação não recebe UserId. Obtém-no de `ICurrentUserContext` e
confirma a senha atual usando o serviço de autenticação (incluindo bloqueio por
tentativas incorretas). A infraestrutura produz um snapshot consistente no
armazenamento privado. Só então são selecionadas explicitamente as linhas do
perfil e os dois catálogos públicos. O snapshot integral nunca é parte do `.acs`.
Snapshots internos de recuperação são mantidos.

Todas as relações conhecidas, inclusive as que não possuem FK SQLite, devem
apontar para registros exportados do mesmo perfil. Não há reivindicação de
UserId=0. Uma relação positiva ausente/externa faz a operação falhar, sem apagar
ou omitir silenciosamente o registro problemático. Nulos e sentinelas não
positivas existentes permanecem; FKs e projeções também são verificadas no merge.

Relatórios de importação remapeiam Entries[].RecordId conforme EntityType
(`Paciente`/`Residência`). Nomes, observações e texto histórico de uma linha do
perfil são evidência daquele perfil, não joins globais. MigrationIssue.SourceId
deve pertencer a SourceTable do mesmo perfil. Evidências compostas reconhecidas
de famílias são validadas e remapeadas. Evidências desconhecidas com conteúdo,
metadados JSON opacos de notificações e chaves de notificação não reconhecidas
são recusados, em vez de assumir propriedade ou semântica. Atualmente o serviço
de notificações não preenche MetadataJson e gera apenas as chaves gestacionais
implementadas. Dados legados inconsistentes precisam ser conciliados para
exportar; a funcionalidade não inventa vínculos nem elimina históricos.

## Contêiner v1 — especificação binária

Todos os inteiros são little-endian, exceto o contador no nonce.

| Offset | Bytes | Conteúdo |
|---|---:|---|
| 0 | 8 | ASCII `ACSVIEW` seguido de NUL |
| 8 | 1 | Versão do contêiner: 1 |
| 9 | 1 | Conjunto criptográfico: 1 (AES-256-GCM / PBKDF2-HMAC-SHA256) |
| 10 | 2 | Reservados, zero; outros valores são recusados |
| 12 | 4 | Iterações PBKDF2: 600000 |
| 16 | 16 | Salt aleatório por arquivo |
| 32 | 8 | Prefixo aleatório do nonce |
| 40 | 8 | Comprimento total do conteúdo descriptografado |
| 48 | 4 | Tamanho nominal do bloco: 65536 |
| 52 | variável | Blocos de ciphertext, cada um seguido de tag GCM de 16 bytes |

O conteúdo descriptografado é `SHA256(JSON) [32 bytes] || JSON UTF-8`.
O JSON contém FormatVersion=1, SchemaVersion, AppVersion, CreatedUtc,
ProfileIdentity (identificador opaco do perfil dentro deste arquivo), Tables e
DashboardPreferences. A identidade e o checksum permanecem criptografados.

A chave de 32 bytes vem de `Rfc2898DeriveBytes.Pbkdf2` usando a senha exata (sem
trim/normalização), salt e HMAC-SHA256. O nonce de cada bloco é o prefixo de 8
bytes seguido do índice de 4 bytes **big-endian**, começando em zero. O AAD é o
cabeçalho completo (52 bytes) seguido do índice de 4 bytes little-endian.
A quantidade/tamanho dos blocos é derivada do comprimento autenticado. Cada
posição, o cabeçalho e o tamanho total são autenticados; truncamento, troca,
reordenação, remoção e bytes finais adicionais são recusados. Não se utiliza
criptografia própria, DPAPI ou Keystore para o contêiner.

As chaves derivadas e buffers de plaintext são zerados em finally. Senhas não são
persistidas; strings do runtime não permitem garantia de apagamento físico em
memória. Não há logs de senhas, dados ou plaintext. O arquivo temporário de
exportação contém apenas ciphertext e é removido no finally da interface. Antes
de abrir o seletor de salvamento, ele é relido e autenticado por completo. O
FileSaver só confirma sucesso quando o provedor de armazenamento confirma a
gravação. O aplicativo não pode garantir a durabilidade futura de um provedor
externo ou mídia removida depois da gravação.

## Limites e compatibilidade

Limite de plaintext de **64 MiB**, incluindo checksum; até **200000 linhas** no
documento; profundidade JSON 32, preferências até 256 KiB e profundidade 16.
Comprimento e parâmetros KDF são validados antes de derivar a chave. Não há ZIP,
descompressão, nomes internos de arquivos ou extração para caminhos externos.
A criptografia usa blocos de 64 KiB; a representação relacional é materializada
em memória com limites explícitos, não é um formato de tamanho ilimitado.
As estimativas de tamanho/quantidade também são verificadas antes da extração.

Os bytes não dependem de plataforma, endianess do processador ou caminhos; o
mesmo serviço e formato são compilados para Android e Windows. v1 aceita esquemas
4 e 5. O destino é inicializado por SchemaMigrator. Dados de esquema 4 recebem a
mesma transformação territorial da migração 5, aplicada somente às casas
importadas. Versões anteriores a 4 e versões futuras são recusadas explicitamente.
Não existiam `.acs` publicados anteriores a este formato. Novos leitores devem
manter a implementação v1 e criar outro decoder/versionamento para evoluções;
não alterar offsets, KDF, AAD ou significado de referências de v1.

600000 iterações são executadas fora da thread principal. O custo em aparelhos
Android físicos ainda precisa de medição; ele não é reduzido automaticamente.

## Merge, remapeamento e preservação

1. Descriptografar e validar sem escrever no banco ativo; mostrar resumo.
2. Após confirmação, criar snapshot de segurança pelo DatabaseRecovery.
3. Copiar o snapshot para **outro caminho privado**, checar espaço e inicializar
   pela infraestrutura de migração.
4. Alocar novos IDs acima do máximo de cada tabela e da sequência autoincremental,
   respeitando também IDs órfãos da tabela de vacinas legadas; importar User com novo ID.
   FamilyRecord é uma identidade relacional. Patient.FamilyId e FamilyId de
   visitas/lotes são **números locais** e permanecem iguais. Remapear casas,
   pacientes, familiares, vínculos, lotes, visitas, notas, clínica e demais PKs.
   Vaccines.Id acompanha o novo Patient.Id. Os catálogos são resolvidos por Code.
5. Remapear também LegacyFamilyMap, SourceId de auditoria de migração, evidências
   compostas reconhecidas, IDs em relatórios JSON e UniqueKey gestacional.
6. Na substituição autorizada, remover somente as linhas do perfil confirmado,
   com senha local atual. Nenhum INSERT OR REPLACE é usado para entidades
   importadas ou contas existentes. Não são mesclados históricos de duas contas.
7. Executar tudo em transação **na cópia privada**. FKs permanecem ON e são
   adiadas até o commit para relações cíclicas. Os triggers confiáveis do destino
   são temporariamente suspensos somente nessa transação e recriados antes da
   validação. Nunca se executa SQL fornecido pelo `.acs`.
8. Validar integrity_check, foreign_key_check, projeções e fingerprints de todas
   as linhas preexistentes que deveriam permanecer. Na substituição, capturar as
   linhas dos demais perfis por savepoint, sem alterar o banco ativo.
9. Revalidar o snapshot contra o banco ativo sob a transação serializadora de
   SQLite. Se houve qualquer alteração concorrente, recusar e pedir nova tentativa.
10. Tornar a conexão atual somente leitura e selecionar atomicamente o caminho
    consolidado para o próximo início. Não sobrescrever SQLite ativo, WAL ou SHM.

As configurações restauradas ficam em `<novo-banco>.profile-settings`, dentro do
armazenamento privado. São aplicadas após inicializar **aquele banco selecionado**
e o arquivo é removido. Perfis alheios mantêm seus IDs e preferências. O documento
inspecionado é exclusivo da instância da página; um semáforo de processo impede
operações simultâneas de backup incompatíveis.

O perfil importado perde BiometricTokenHash e ganha nova CredentialRevision.
O novo caminho e a identidade local renovada invalidam autorizações do banco
anterior. Nenhuma inscrição de biometria/Hello é transportada ou criada. Por esse
motivo, desbloqueios locais de contas preexistentes também podem exigir nova
configuração após a troca do banco; suas senhas e dados permanecem preservados.
Não se cancela nem agenda alarmes durante a preparação. No próximo início,
NoteReminderService reconcilia todas as notas com os IDs finais, sem cancelamento
global; solicitações existentes das outras contas mantêm seus IDs. O sistema
operacional pode exigir permissão para reagendar futuras notificações.

## Recuperação de falhas

Antes da seleção grava-se `profile-backup-activation.json` com os nomes privados
do banco anterior e candidato. A seleção é feita pelo mesmo mecanismo de rename
atômico de `database-selection.txt` da recuperação existente. No próximo início,
DatabaseRecovery valida o candidato, projeções e sequência das migrações antes
de abri-lo como banco do aplicativo. Se estiver danificado, seleciona novamente o
banco anterior intacto e mantém o plano com sufixo `.failed`. Depois da
inicialização bem-sucedida, remove o marcador de ativação.

Cancelamento, senha errada, arquivo inválido, colisão sem autorização, mudança
concorrente e falta de espaço não alteram registros ativos. A cópia incompleta e
seu sidecar são removidos em finally. Uma interrupção de processo antes da
seleção pode deixar uma cópia privada órfã; o banco original continua selecionado.
Snapshots em migration-backups nunca são apagados pelo backup individual.

Para recuperação administrativa excepcional, feche o processo, preserve todos
os bancos e snapshots, examine o plano `.failed` e a seleção. Se o anterior era
health_app.db, remover somente database-selection.txt faz a infraestrutura voltar
ao caminho padrão; se era health_app-restored-*.db, selecione aquele nome válido.
Não copie arquivos sobre bancos ativos e não apague WAL/SHM de um banco aberto.
Falha de acesso ao armazenamento ou corrupção simultânea do banco anterior exige
recuperação por snapshot, com o aplicativo fechado.

## Validação

`dotnet run --project Tests/ACSView.BackupTests` executa testes com SQLite real,
criptografia real, AuthService real e armazenamento de plataforma substituído por
memória. Abrange isolamento de A/B/C e legado, recuperação limpa, colisões de IDs
e catálogos, preservação de outras contas, histórico relacional/JSON, substituição
autenticada, falhas de segurança/armazenamento, blocos reordenados, esquema 4 e
rollback de ativação. Os testes usam somente dados sintéticos.

Também devem passar as suítes de autenticação, migração e lembretes, além dos
builds Windows e Android. Build multiplataforma não substitui o teste físico do
seletor Android, troca de arquivo entre dispositivos, desempenho do KDF e entrega
de notificações pelo sistema operacional; esses testes manuais permanecem como
validação de dispositivo.

Validação local em 05/11/2026: 89 verificações de backup, 166 de autenticação,
326 de migração/relacionamentos e 32 de lembretes. Builds Windows e Android com
zero erros (avisos de nulabilidade/MVVM permanecem). O acesso público
à página de restauração e seu layout em tema escuro foram inspecionados no
executável Windows. Nenhuma conta real foi restaurada durante a inspeção visual.
