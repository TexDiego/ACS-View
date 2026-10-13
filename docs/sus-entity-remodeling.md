# Remodelação SUS — 10/10/2026

Cada paciente possui zero ou vários registros `PatientSusNumber`, com ID próprio, proprietário, ordem e indicação de principal. `IPatientSusService`/repositório permitem consultar, adicionar, editar, remover e gravar a lista em transação. O editor carrega as entidades e conserva os IDs ao editar. O cadastro exige nome, mas não exige SUS. O popup consulta somente o principal e oculta CNS quando não há principal.

A versão 4 do esquema executa backfill aditivo para bancos da versão 3: acrescenta números presentes em `Patient.SusNumber` ainda ausentes na tabela, sem excluir registros previamente existentes nem modificar o texto legado ou seu principal. Bancos anteriores continuam pelo fluxo de expansão/backfill existente. Snapshot, fingerprints, ledger e verificações de integridade seguem ativos. `Patient.SusNumber` permanece na base para compatibilidade. Alterações explícitas mantêm a representação compatível, e o texto anterior fica registrado como `LegacyCnsEvidence` em `MigrationIssue`. Dados inválidos ou compartilhados legados permanecem sujeitos a conciliação.

As consultas de paciente carregam números da tabela própria. A importação mantém o reconhecimento por qualquer CNS e por identidade segura: coluna/célula ausente ou vazia não remove números; uma célula com vários números separados por ponto e vírgula acrescenta números distintos. A gravação paciente/números ocorre na mesma transação. Texto legado prévio é preservado quando a importação acrescenta números. Linhas sem CNS e sem identidade suficiente continuam relatadas para revisão para evitar mesclas incorretas.

Validação local: 169 verificações de migração/integração SQLite; suíte de importação; suíte geral; compilação Android Debug. Os cenários incluem banco legado, atualização da versão 3, preservação do texto original e IDs, ausência de CNS, várias entradas, principal, isolamento de conta/paciente, reversão em falha, troca de números, reimportação e integridade SQLite. Logs em `tmp/sus-*-validation.log` e `tmp/sus-android-build.log`.

Não foi realizada validação visual em dispositivo físico nem migração de banco real de pacientes.
