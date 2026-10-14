# ACS View 1.1.1

Atualização de correções da versão 1.1.0. Mantém o identificador Android `com.texdiego.acsview`, a assinatura definitiva e a política de atualização opcional (`minimumVersionCode: 1`). O código interno Android passa de 2 para 3.

- Corrige a comparação de versões das releases para reconhecer atualizações entre formatos equivalentes de versão.
- Corrige a verificação de espaço disponível no armazenamento privado Android antes de proteger, migrar ou restaurar o banco; inclui os dados ainda presentes no WAL no cálculo do tamanho do snapshot.
- Ajusta cores e ícones para acompanhar os temas claro e escuro.
- Exibe mensagens temporárias no acionamento do WhatsApp quando o número é inválido ou a conversa não pode ser aberta.
- Ajusta a senha mínima de novas contas para oito caracteres e mantém a orientação exibida no formulário consistente.
- Alinha a versão de pacote nas avaliações do projeto para evitar inconsistências na restauração NuGet.

Os testes automatizados utilizam dados sintéticos. Instalação, aparência e atualização com preservação dos dados em um aparelho real continuam dependendo de validação física.
