# ACS View 1.1.0

Esta versão amplia os recursos de cadastro e acompanhamento e mantém a identidade Android `com.texdiego.acsview`. A versão interna Android passa de 1 para 2. As migrações preservam os dados legados e registram vínculos ambíguos para conciliação.

- Cadastro com múltiplos números CNS e telefones, edição e escolha do contato principal.
- Acesso ao WhatsApp pelo telefone principal válido do paciente.
- Melhorias de carregamento do cadastro, busca de CID, filtros, indicadores e interface Android.
- Armazenamento relacional para famílias, residências e histórico clínico, com migrações versionadas, validação e recuperação por backup.
- Correções no acompanhamento de visitas, vacinação, gestação e isolamento dos dados entre contas.
- Consulta de novas versões no GitHub pela tela de login, com download do APK após escolha do usuário e acesso local quando a consulta está indisponível.

A atualização é opcional (`minimumVersionCode: 1`). A instalação usa a mesma assinatura definitiva da versão 1.0.0. A confirmação de instalação e a preservação dos dados em um aparelho real precisam de validação física; os testes automatizados utilizam dados sintéticos.
