# Importação complementar

Na tela de importação, selecione a planilha de pacientes. Os nomes equivalentes das colunas são reconhecidos automaticamente, sem alterar o mapeamento na tela. A seção de pacientes não possui campos de configuração; somente a importação de residências permite editar o mapeamento. São aceitos `.xlsx` e `.xlsm`, com leitura da primeira aba; arquivos antigos `.xls` precisam ser convertidos.

## Regras

- As colunas da relação fornecida pelo usuário estão no catálogo `ImportColumnAliases`. A comparação ignora acentos, caixa, espaços e pontuação, sem procurar por fragmentos de nomes.
- Colunas sem mapeamento são ignoradas silenciosamente e não geram registros por linha no histórico.
- Havendo várias colunas equivalentes na mesma aba, utiliza-se o primeiro valor preenchido na ordem das colunas. Valores divergentes ficam registrados no relatório.
- A identidade é procurada por CNS e também por nome + nascimento + nome da mãe. Linhas repetidas complementam o mesmo cadastro. A ausência de CNS não impede a importação quando a identidade completa é válida.
- Valores vazios e campos ausentes preservam os dados existentes. Valores válidos preenchidos atualizam o cadastro; observações distintas são concatenadas. Condições aceitam sim/não, s/n, verdadeiro/falso, true/false, 1/0 e x. Um não explícito remove a condição correspondente; valores desconhecidos preservam o dado e geram pendência.
- CNS divergentes para a mesma identidade são registrados e o CNS existente é preservado. Quando a linha corresponde a vários cadastros já existentes, ela exige revisão manual; o importador não exclui esses cadastros e seus históricos.
- Residências podem ser criadas usando CEP e número quando a consulta de CEP fornece um endereço completo. Complementos distinguem residências. Endereços incompletos, divergentes ou ambíguos geram pendências.
- Famílias são agrupadas pelo responsável familiar e sua residência. Mães e pais são associados por nome, com verificação de idade e unicidade. Vínculos pendentes são conferidos novamente quando outros familiares chegam em importações posteriores. O pai só pode ser associado quando seu nome já existe no cadastro ou na planilha; não é inferido a partir do responsável familiar.
- Vínculos manuais existentes são preservados. Conflitos de responsável familiar são registrados para revisão. Campos que não constam nas planilhas continuam disponíveis para edição manual.

## Histórico

Abra **Histórico de importações** na tela de importação e selecione o arquivo. O relatório inclui início e fim, status, contagens, colunas reconhecidas, mesclagens, alterações de campos, vínculos e pendências por linha física do Excel. A busca permite localizar nome, linha, campo ou pendência.

O histórico fica no SQLite, separado por usuário. A limpeza de pacientes também remove os relatórios que contêm seus dados. Cancelamento não desfaz as linhas já gravadas e essa informação aparece no histórico.

## Validação em 05/10/2026

Execute:

```powershell
dotnet run --project Tests/ACSView.ImportTests/ACSView.ImportTests.csproj
dotnet build 'ACS View.csproj' -f net10.0-android --no-restore
```

Resultado: 22 verificações passaram. Os testes usam arquivos Excel Open XML gerados e os serviços reais de leitura, mapeamento, importação e resolução familiar, com repositórios em memória e consulta de CEP simulada. A compilação Android final passou com 0 erros e 51 avisos.

A incompatibilidade anterior da suíte geral `ACSView.Tests` (expectativa de 10 pontos para `NoVulnerability`) foi corrigida na preparação Android v1.0.0: o teste agora verifica os 5 pontos definidos no catálogo desde o commit `79e6bf7`, sem alterar a regra do aplicativo.

Ainda não houve validação visual em aparelho, importação de arquivos reais do usuário ou teste da consulta de CEP e do histórico persistido durante uma sessão real do aplicativo.
