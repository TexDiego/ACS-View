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

## Distribuição Android por APK

O identificador Android definitivo é `com.texdiego.acsview`. O `.csproj` é a fonte de versão: inicialmente `ApplicationDisplayVersion=1.0.0` e `ApplicationVersion=1`. Em cada publicação, altere a versão visível e aumente o inteiro `ApplicationVersion`; a tag deve corresponder exatamente à versão visível. O workflow rejeita divergências e não calcula versionCode a partir de SemVer. SDK e versões diretas de MAUI estão fixados; o CI instala o workload Android do conjunto `10.0.401`.

Em 06/10/2026 foi criada a chave definitiva, **fora do repositório**, em `S:\Projetos Mobile\ACS View Secrets\ACSView-release.jks`, alias `acsview-release`. A pasta tem acesso NTFS restrito ao usuário proprietário. `ACSView-release-credentials.txt` guarda as duas senhas e o hash do keystore; `ACSView-release-base64.txt` guarda a representação para o GitHub. Preserve a chave e ambas as senhas para todas as atualizações e mantenha uma cópia segura independente deste computador. Não gere outra chave para uma atualização.

Os quatro Actions Secrets já foram configurados em `TexDiego/ACS-View`: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` e `ANDROID_KEY_PASSWORD`. Para cadastrá-los novamente, leia os arquivos locais, sem imprimir os valores:

```powershell
$signingDir = 'S:\Projetos Mobile\ACS View Secrets'
$values = @{}
Get-Content (Join-Path $signingDir 'ACSView-release-credentials.txt') | ForEach-Object {
    if ($_ -match '^([^:]+): (.*)$') { $values[$matches[1]] = $matches[2] }
}
Get-Content (Join-Path $signingDir 'ACSView-release-base64.txt') -Raw | gh secret set ANDROID_KEYSTORE_BASE64 --repo TexDiego/ACS-View
$values.KeystorePassword | gh secret set ANDROID_KEYSTORE_PASSWORD --repo TexDiego/ACS-View
$values.Alias | gh secret set ANDROID_KEY_ALIAS --repo TexDiego/ACS-View
$values.KeyPassword | gh secret set ANDROID_KEY_PASSWORD --repo TexDiego/ACS-View
gh secret list --repo TexDiego/ACS-View --json name --jq '.[].name'
```

Somente após passar os gates locais e o checklist físico crítico (incluindo atualização com preservação de SQLite), envie o commit e crie a primeira versão, confirmando antes que a tag não existe local ou remotamente:

```powershell
git tag -a v1.0.0 -m "ACS View 1.0.0"
git push origin v1.0.0
```

O push de uma tag `v*` aciona `.github/workflows/release-android.yml`. Também é possível executar o workflow manualmente, informando uma tag existente, para aplicar uma correção no pipeline sem mover a tag; o checkout continua sendo o código dessa tag. O CI exige uma tag estável `vN.N.N`, valida secrets, restaura, compila Debug e executa todas as seis suítes. Publica apenas Android Release/APK, reconstrói o keystore temporariamente, assina, verifica identidade, SDK e fingerprint SHA-256 do certificado definitivo e cria a GitHub Release com `ACSView-v1.0.0.apk`. O keystore temporário é removido mesmo em caso de falha; não há publicação a cada push da branch nem atualização interna automática. Depois, baixe e valide o APK exato anexado à Release.

Baixe o APK em **Releases**, autorize a instalação pela origem usada (navegador/gerenciador de arquivos) e abra o arquivo. Todas as versões distribuídas precisam manter **o mesmo ApplicationId e a mesma chave de assinatura**, com versionCode crescente. Guarde cópias seguras da chave e das senhas: perder a chave impede atualizar instalações existentes. Atualizações compatíveis preservam o banco SQLite; **desinstalar ou limpar os dados remove os dados locais**. Backup Android continua desabilitado. A instalação antiga de desenvolvimento `com.companyname.acsview` é outro aplicativo: a mudança de identificador não transfere seus dados automaticamente.

Veja [diagnóstico das notificações e checklist de Android real](docs/android-release-validation.md) antes da primeira distribuição. Um publish local sem os secrets usa a chave de desenvolvimento do SDK e serve para validação de build, não como APK de distribuição.

O iOS está fora desta distribuição: o identificador existente foi preservado e não houve validação em Mac. As permissões antigas de armazenamento Android permanecem como dívida técnica documentada no checklist.
