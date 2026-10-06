# ACS View

**ACS View** é uma aplicação desenvolvida em .NET MAUI com o objetivo de auxiliar agentes comunitários de saúde no acompanhamento e organização de dados de pacientes. A ferramenta permite uma visualização prática e centralizada das informações, facilitando o trabalho diário desses profissionais.

---

## Funcionalidades Previstas

- Visualização rápida de pacientes cadastrados
- Acompanhamento de dados importantes como condições de saúde e informações demográficas
- Acompanhamento de situação vacinal
- Acompanhamento de pacientes cadastrados como beneficiários no Programa Bolsa Família
- Registro prático de anotações
- Criação de notificações personalizadas
- Consulta automática de endereço via CEP com integração à API pública [ViaCEP](https://viacep.com.br)
- Interface intuitiva e adaptada para uso em campo
- Armazenamento local de dados
- Registro de famílias visitadas com filtro de período
- Sugestões de visitas
- Consulta de CIDs com catálogo completo
- Criação de métricas personalizadas por unificação de condições (por exemplo: Hipertensos + Diabéticos)
- Filtrar registros de pacientes de forma personalizada
- Importação de dados
- Exclusão de dados em massa
- Persistência de dados por login

---

## Métricas Disponíveis

### Métricas Gerais

- Quantidade de pacientes
- Quantidade de residências
- Quantidade de famílias
- Idosos (60 anos ou mais)
- Crianças menores de 6 anos
- Mulheres de 25 a 64 anos
- Beneficiários do Bolsa Família
- Pacientes sem residência
- Residências vazias
- Pacientes inativos

### Métricas de Saúde

- Gestante
- Diabetes
- Hipertensão
- Dependentes de insulina
- Tuberculose
- Acamado
- Domiciliado
- Condição mental
- Fumante
- Usuário de álcool
- Portadores de deficiência
- Dependentes químicos
- CIDs específicos quando houver registro em algum paciente

> NOTA: é possível unir 3 condições para cruzar dados automaticamente. Está disponível para todas as condições de saúde e para algumas métricas gerais, evitando unificações que sempre resultam em 0 registros como idosos + crianças

---

## Tecnologias Utilizadas

- [.NET MAUI](https://learn.microsoft.com/pt-br/dotnet/maui/)
- C#
- SQLite (persistência local)
- Consumo de API REST com `HttpClient`
- Manipulação de JSON com `System.Text.Json`

---

## Planos Futuros

- Implementação de persistencia em nuvem. Banco de dados futuro ainda não definido






## Conta local e recuperação

O cadastro usa usuário e senha de 15 a 128 caracteres, com confirmação. Frases longas são aceitas, incluindo espaços. Não há autenticação por serviços externos, envio de e-mail ou sincronização de credenciais.

Ao cadastrar, trocar a senha ou recuperar a conta, guarde o código de recuperação exibido uma única vez. Ele tem 128 bits de aleatoriedade e só seu hash é persistido. Cada uso gera um código novo e invalida o anterior. Sem senha e sem código não existe recuperação por pergunta de segurança. O código não restaura dados removidos ao desinstalar ou apagar os dados do aplicativo.

Contas antigas mantêm seu identificador e seus registros. Senhas legadas em texto são convertidas em hash na inicialização; hashes antigos são reforçados após um login válido. Perguntas e respostas de segurança são removidas. Após entrar com a senha atual, configure o código em **Perfil > Senha, recuperação e biometria**. A atualização encerra as sessões do modelo anterior e exige um novo login.

Senhas usam PBKDF2-HMAC-SHA256 com salt aleatório de 16 bytes, hash de 32 bytes e 600.000 iterações, com versionamento para migração. A sessão fica no SecureStorage do sistema, vence após sete dias e é invalidada ao recuperar a senha. Cinco falhas de login ou de recuperação bloqueiam aquele fluxo por cinco minutos; os contadores ficam no banco local. Trocas de segurança exigem a senha atual. O Android exclui os dados do aplicativo de backup automático e transferência automática entre aparelhos.

Essas proteções cobrem as credenciais e a sessão. O banco de pacientes existente continua SQLite; esta alteração não implementa criptografia integral do banco nem proteção contra um sistema operacional comprometido.

Referências: [armazenamento de senhas (OWASP)](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html), [SecureStorage (.NET MAUI)](https://learn.microsoft.com/dotnet/maui/platform-integration/storage/secure-storage), [backup no Android](https://developer.android.com/identity/data/autobackup).

Validação de autenticação e migração com SQLite real e armazenamento seguro substituído somente nos testes:

```powershell
dotnet run --project Tests/ACSView.AuthTests/ACSView.AuthTests.csproj
```

### Biometria local no Android

Em **Perfil > Senha, recuperação e biometria**, confirme a senha atual e toque em **Ativar biometria**. O sistema pede uma confirmação biométrica antes de ativar. No login, use **Entrar com biometria** ou a senha. Depois de ativada, uma sessão persistida não libera automaticamente a conta ao abrir ou retornar normalmente ao aplicativo.

O suporte nativo requer Android 11/API 30 ou superior e uma biometria forte (classe 3) cadastrada no aparelho. A autorização usa `BiometricPrompt.CryptoObject` e uma chave AES-256/GCM no Android Keystore, com autenticação obrigatória para cada operação. A chave é invalidada quando o conjunto de biometrias cadastradas muda. O aplicativo não recebe nem armazena digitais ou imagens faciais: guarda um token aleatório criptografado, vinculado ao usuário e à revisão das credenciais, e seu hash no SQLite. Senha e código de recuperação não ficam no token.

Todas as biometrias registradas no aparelho poderão desbloquear a conta que ativou esse recurso. Trocar ou recuperar a senha, desativar a biometria, sair da conta ou entrar com outra conta revoga a ativação local. Cancelamento, sensor indisponível e autorização inválida mantêm a alternativa de senha. iOS, Windows e Android anteriores ao 11 continuam usando senha; não há implementação biométrica nessas plataformas nesta versão.

Referência: [BiometricPrompt e operações criptográficas no Android](https://developer.android.com/identity/sign-in/biometric-auth).

Os testes locais substituem somente os adaptadores do sistema operacional; a validação de sensor, diálogo nativo, alteração das biometrias e ciclo de vida precisa ser feita em um aparelho real.
