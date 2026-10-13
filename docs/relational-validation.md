# Validação da remodelação relacional

Execução local em 08/10/2026, no checkout `S:\Projetos Mobile\Frozen Projects\ACS View`. Todos os bancos utilizados nos testes são fixtures sintéticas em diretórios temporários exclusivos. Nenhum banco real foi aberto para teste, migrado ou apagado.

## Resultados finais

| Verificação | Resultado |
|---|---|
| Restore Android `net10.0-android` | Sucesso |
| Build Android Debug, `--no-restore` | Sucesso; 0 erros, 50 avisos |
| Build Android Release local, `--no-restore` | Sucesso; 0 erros, 232 avisos |
| ACSView.MigrationTests | 97 verificações de migração e integração aprovadas |
| ACSView.AuthTests | 76 verificações aprovadas |
| ACSView.ImportTests | 35 casos PASS; conclusão de importação, persistência e múltiplos CNS aprovada |
| ACSView.BolsaFamiliaTests | 22 verificações aprovadas |
| ACSView.ReminderTests | 32 verificações aprovadas; entrega Android não simulada |
| ACSView.SessionTests | 6 verificações aprovadas |
| ACSView.Tests | 10 grupos aprovados |
| `git diff --check -- . ':!Views/FamiliesPage.xaml'` | Aprovado; avisos de conversão LF/CRLF |

Os avisos de compilação incluem nullability C# e bindings XAML não compilados. A aprovação é de build com avisos, não de build sem avisos. A edição concorrente do usuário em `Views/FamiliesPage.xaml` foi preservada e excluída da checagem de whitespace da tarefa. Os logs locais ficam em `tmp/remodel-*.log`, ignorados pelo Git; esta documentação registra os resultados sem depender desses arquivos.

Não houve commit, push, tag, publicação, atualização de versão pública ou mudança de identificador/assinatura. Release aqui significa somente compilação local.

## Matriz dos casos solicitados

Os casos abaixo são cobertos por `Tests/ACSView.MigrationTests/Program.cs`, salvo indicação adicional.

| Caso | Evidência e limite |
|---|---|
| 1. Instalação limpa | Esquema 3, FKs habilitadas, ledger contíguo, nenhum telefone inventado |
| 2. Banco legado completo | Fixture multientidade com pacientes, casas, família, visitas, doses, gestação, condições, CID e contas |
| 3. Banco parcialmente migrado | Colunas ausentes acrescentadas; proprietário NULL mantido; ConditionId preexistente NULL mantido e sinalizado |
| 4. Repetição | Segunda inicialização não reaplica backfill nem duplica ledger/projeções |
| 5. Interrupção | Falha injetada antes da validação reverte DDL, dados e versão; tentativa seguinte migra |
| 6. Constraint/índice | Duplicidade incompatível em esquema parcial recusa migração e mantém linhas; mudança indevida de linha detectada pelo fingerprint também reverte |
| 7. Sem residência | Sentinelas preservadas; grupo sem casa não é associado a uma casa fictícia |
| 8. Sem família | Ausência preservada; não se cria vínculo por coincidência de nome |
| 9. Mesmo número em casas distintas | Identidades separadas por usuário/casa/número; casa desconhecida separada por origem |
| 10. Mudança de residência | Mantém identidade familiar e membros, encerra e abre episódios de residência |
| 11. Entrada e saída | Saída encerra episódio; reentrada cria outro; datas migradas desconhecidas continuam NULL |
| 12. Múltiplos CNS | Projeção, escrita transacional, busca por CNS secundário, importação e reinício |
| 13. CNS inválido/duplicado | Texto bruto preservado, projeção pelas regras existentes e MigrationIssue para conciliação |
| 14. Telefone compartilhado | Mesmo número permitido em pacientes distintos |
| 15. Múltiplos telefones | Vários contatos, número incompleto preservado, apenas um principal |
| 16. Visitas históricas | Eventos e snapshots permanecem após transferência e arquivamento; consultas não purgam histórico |
| 17. Visitas com referência inválida | Evento órfão e endereço original preservados, referência sinalizada |
| 18. Condições/CIDs | Linhas e IDs preservados; catálogo estável, importação e descrição Unicode sem reescrita; CID órfão permanece para conciliação |
| 19. Doses distintas | Aplicações, lotes e datas distintos não são deduplicados; datas desconhecidas permanecem desconhecidas no editor real |
| 20. Gestação | Episódios distintos mantidos; encerrar um não encerra outro; timestamp TEXT legado lido e preservado nas edições |
| 21. Múltiplas contas | Constraints/guards rejeitam referências e trocas de proprietário entre usuários |
| 22. UserId=0 | Dados mantidos sem claim automático no login; NULL legado também não recebe dono |
| 23. Exclusão seletiva de paciente | Exclusão individual arquiva e mantém clínica; limpeza explicitamente destrutiva remove dependentes em ordem |
| 24. Exclusão seletiva de casa | Arquiva/detacha ocupação e mantém visitas/história; atualização concorrente obsoleta rejeitada |
| 25. Limpeza completa | Remove dados da conta escolhida, incluindo dependentes novos, gestação e notificações |
| 26. Outras contas após limpeza | Pacientes/casas de outra conta e dados sem proprietário continuam presentes e íntegros |
| 27. Importação | Serviços e repositórios SQLite reais, CEP substituído por implementação offline; suíte ImportTests cobre leitura de planilha e regras existentes |
| 28. Backup antigo | Snapshot consistente, cópia para novo caminho, migração e seleção somente depois da validação; original permanece |
| 29. Fechar/reabrir | Conexões fechadas/reabertas e fábrica com seleção restaurada; contatos, registros e ledger persistem |
| 30. Inicialização repetida | Idempotência, abertura concorrente e backfill de vacinas sem duplicação |

## Preservação e reconciliação

O migrador captura contagens, nomes das colunas e fingerprints SHA-256 das ocorrências originais, incluindo tipo SQLite e conteúdo hexadecimal. Confere a presença de todas essas ocorrências após cada etapa, permitindo somente expansões aditivas. `SchemaPreservation` registra contagens e digests aprovados; `SchemaMigration` registra as versões concluídas. `MigrationIssue` registra referências inválidas, propriedade desconhecida, CNS e vínculos ambíguos, sem descartar a fonte.

As fixtures incluem observação com NUL, datas SQL TEXT, data de nascimento NULL, CNS bruto inválido/repetido, IDs existentes e vínculos inconsistentes. Uma adulteração proposital de nome durante a migração é detectada e revertida. `integrity_check` e `foreign_key_check` passam após migração e limpeza. Uma falha de constraint também não recebe versão concluída.

A preservação demonstrada é a das fixtures suportadas. Não há afirmação de perda zero para qualquer banco real, nem recuperação de registros apagados por versões anteriores. Metadados futuros, corrupção e estados parciais conflitantes devem recusar a abertura conservadoramente, mantendo o original e o snapshot para análise. Campos clínicos já presentes em bases parciais não são substituídos apenas para completar uma projeção.

## Limites e requisitos antes da distribuição

- Interrupção é uma falha injetada em transação; não foi executado desligamento físico, falha de armazenamento ou morte abrupta do processo Android.
- Autenticação, isolamento da sessão restaurada e grants biométricos foram testados com AuthService/SQLite reais e vault/biometria de SO simulados. Sensor, Android Keystore e ciclo de vida em aparelho não foram exercitados.
- Lembretes e recuperação foram testados com scheduler simulado. Entrega sob Doze, reboot, tela bloqueada e restrições do fabricante continua dependente de aparelho.
- A persistência foi verificada por reabertura SQLite, não por reinício manual do APK. Renderização/interação nativa, F5, acessibilidade e demais plataformas não foram validadas.
- Não foram usadas planilhas pessoais, bancos de produção ou credenciais/secrets de distribuição. O fluxo relacional de importação foi exercitado offline com linhas sintéticas; conectividade externa não é prova necessária para migração local.
- UserId=0/NULL e relações ambíguas exigem conciliação explícita; não há editor visual novo para resolver propriedade ou conflitos. Os serviços e relatórios técnicos disponibilizam os dados sem decisões destrutivas.
- O migrador atual recusa versões futuras; APKs antigos já distribuídos não podem receber retroativamente esse guard. Definir bloqueio de downgrade antes da distribuição.
- Snapshots integrais preservam cópias de dados posteriormente excluídos do banco ativo. Definir retenção e descarte de backups, considerando outras contas, antes de distribuir.

Arquitetura, tabelas, mapeamento, estruturas legadas e recuperação: [relational-remodeling.md](relational-remodeling.md). Inventário anterior: [relational-audit-before.md](relational-audit-before.md).
