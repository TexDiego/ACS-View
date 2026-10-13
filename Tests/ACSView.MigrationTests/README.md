# Migração e integração relacional

Executar na raiz do checkout:

```powershell
dotnet run --project Tests/ACSView.MigrationTests/ACSView.MigrationTests.csproj
```

A suíte cria apenas fixtures sintéticas em um diretório temporário exclusivo. Exercita o migrador, SQLite, repositórios, importação, família, contatos, território, visitas, vacinação, gestação, limpeza e recuperação reais. O leitor de linhas/CEP externo e o scheduler são substituídos; o ViewModel real do editor de vacina usa uma base de plataforma mínima para executar fora de MAUI.

O teste de restauração precisa de permissão para criar e renomear o arquivo de seleção dentro do diretório temporário. Não executar sobre bancos reais.

Resultados, matriz e limites: [relational-validation.md](../../docs/relational-validation.md).

Os testes de telefone exercitam os comandos reais do `PhoneEditorViewModel` e o serviço/repositório SQLite: adicionar, editar, cancelar, excluir, seleção exclusiva do principal, duplicados com máscara, preservação de metadados, troca de números, rollback com falha de inserção e isolamento entre contas. A entidade `PatientPhone` inclui os campos obrigatórios `Type` e `HasWhatsApp` do esquema.

Os testes de SUS exercitam os comandos reais do `SusNumberEditorViewModel`: entrada individual, edição, cancelamento, exclusão, duplicados, escolha do principal, preservação do texto legado não alterado e sincronização com `Patient.SusNumbers` e `PatientSusNumber`. A integração verifica também a busca depois de excluir um número. Novos números e alterações exigem 15 dígitos; números antigos incompletos são preservados ao carregar.

A renderização dos controles compactos, o foco da Entry e a reciclagem dos RadioButtons nas CollectionViews precisam de verificação no aplicativo em dispositivo.
