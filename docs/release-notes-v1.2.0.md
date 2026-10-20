# ACS View 1.2.0

Esta versão amplia a pesquisa e a organização das residências. Mantém o identificador Android `com.texdiego.acsview`, a assinatura de distribuição e a atualização opcional (`minimumVersionCode: 1`). O código interno Android passa de 3 para 4.

- Adiciona filtros de residências por número inicial, número final e paridade (todos, pares ou ímpares), combinados com a barra de pesquisa e com a ação “Remover filtros”.
- Adiciona o controle vetorial com organização por setor censitário e quadra, pesquisa por endereço, paciente ou classificação territorial e seleção individual ou por trecho para aplicação em lote.
- Inclui setor censitário e quadra no cadastro da residência, com migração aditiva e preservação do histórico territorial.
- Ajusta a seleção de residências no controle vetorial para manter o resumo das seleções individuais e evitar reconstruções da lista ao marcar opções.
- Alinha a contagem e a listagem de residências vazias às famílias ativas cadastradas.
- Corrige a contagem de filhos cadastrados apresentada ao iniciar uma gestação.
- Atualiza dependências e adapta a integração de notificações às APIs da biblioteca instalada.

Os testes automatizados usam dados sintéticos. A instalação e a atualização em aparelho real, incluindo a preservação dos dados locais e a validação visual, continuam dependendo de verificação física.
