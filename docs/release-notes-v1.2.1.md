# ACS View 1.2.1

Esta versão melhora a auditoria das importações e o acompanhamento da atualização na tela de login. Mantém o identificador Android `com.texdiego.acsview`, a assinatura de distribuição e a atualização opcional (`minimumVersionCode: 1`). O código interno Android passa de 4 para 5.

- Histórico com filtros por resultado, busca por cadastro, linha ou motivo, identificação dos registros e detalhes das alterações efetivas, inclusive gravações parciais.
- Pacientes e residências já cadastrados sem informações novas são classificados como ignorados. A comparação dos pacientes considera os campos utilizados e preenchidos na planilha, respeitando a padronização de nomes, datas e CNS; campos ausentes ou colunas não utilizadas não geram mesclagem.
- Importação de várias planilhas de pacientes em uma seleção, processadas em sequência para complementar os cadastros, com progresso e histórico por arquivo.
- Endereços consultados primeiro no cache de CEP do app. Quando necessário, uma consulta à API por CEP no lote, com armazenamento dos resultados válidos; em caso de falha, utilização das informações disponíveis na planilha.
- Associação do responsável familiar por CNS principal ou secundário, incluindo vários números na mesma célula, com preservação dos vínculos existentes e tratamento explícito de ambiguidades.
- Preenchimento da residência ausente do responsável somente quando há uma indicação única e segura. Conflitos exibem os endereços do paciente e do responsável no histórico.
- Ajustes automáticos de vínculos com informações já existentes são auditados separadamente, sem transformar uma linha sem novidades em mesclagem.
- Atualização no login com barra de progresso, volume baixado e descrição das etapas de download, verificação e abertura do instalador. O acesso permanece bloqueado durante o processo.

Os relatórios anteriores permanecem preservados. Para tentar novamente associações pendentes e gerar um relatório atualizado, reimporte as planilhas na nova versão.

Os testes automatizados usam dados sintéticos. A interação visual, a instalação e a atualização em aparelho real, incluindo a preservação dos dados locais, continuam dependendo de verificação física.
