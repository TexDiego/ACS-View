# Contas lembradas e Windows Hello

Implementação local de 05/11/2026. Não houve commit, push, publicação ou alteração dos modelos de pacientes, famílias, residências e visitas. Alterações locais anteriores nesses módulos foram preservadas.

## Escopo e arquitetura

A tela Windows ganhou seleção de várias contas, consentimento explícito para lembrar, indicação individual de Hello, ação de esquecer e opção de usar outra conta. Selecionar preenche o usuário, apaga a senha anterior e solicita foco na senha; a seleção não atualiza o contexto autenticado. Não há login Hello automático.

`IRememberedAccountService` gerencia somente metadados locais. O serviço exige um contexto autenticado para registrar/atualizar um perfil. Uma conta existente não é lembrada automaticamente. O registro após login usa o consentimento do formulário; perfis já lembrados têm a data atualizada. A leitura valida banco, existência e nome do usuário, elimina duplicatas e ordena por último uso. Falha/corrupção na lista retorna um seletor vazio sem impedir a senha. A lista não contém senhas, hashes de senha, códigos de recuperação nem dados de pacientes.

`IWindowsHelloService` isola WinRT; a implementação nativa está em `Platforms/Windows`. O restante da lógica pode ser testado com um fake. `IAuthService` continua sendo a única entrada para criar sessões; senha, biometria Android e Hello compartilham `CompleteLoginAsync`, que utiliza a persistência de sessão e o contexto existentes. Não se chama LoginAsync com senha fictícia e não se restaura sessão para substituir uma prova Hello.

`LocalAuthenticationGate` serializa autenticação/configuração/revogação por caminho de banco, inclusive entre instâncias de serviço. Tentativas sobrepostas de senha ou Hello são rejeitadas, e esquecer uma conta durante uma autenticação também é rejeitado. A interface desabilita os controles enquanto autentica. A revisão, o bloqueio e a associação são conferidos novamente após o diálogo nativo.

## Persistência Windows

São usados o `ISecureStorage` e a implementação MAUI já registrados no projeto. No Windows, o MAUI protege os valores com DataProtectionProvider; na versão portátil persiste o conteúdo protegido no diretório privado da aplicação. Na versão empacotada usa LocalSettings. Não há uma tabela SQLite para a lista de contas.

Existe apenas uma nova tabela técnica de identidade: `LocalAuthIdentity`, com uma linha aleatória, criada de forma aditiva. Ela não altera registros ou vínculos de usuários existentes. O identificador efetivo combina GUID persistente, caminho absoluto, volume, ID físico do arquivo e instante de criação. A leitura do ID físico usa GetFileInformationByHandle no Windows. Uma cópia física do mesmo banco, inclusive no mesmo caminho e com o mesmo GUID, recebe outra identidade efetiva.

Snapshots explícitos e bancos preparados pelo serviço de restauração recebem novo GUID. Snapshots internos de migração continuam sendo evidência fiel do estado anterior. A restauração continua usando novo arquivo e seleção apenas na próxima inicialização. No Android, esse ajuste de identidade de backup não é executado.

A lista e cada autorização usam a identidade do banco. Autorizações de uma instalação/snapshot não são transportadas para outro banco somente porque os IDs numéricos coincidem. Uma restauração externa manual que reescreva o mesmo arquivo, preserve seu ID físico e reutilize um snapshot interno antigo com o mesmo GUID não pode ser distinguida de um rollback completo por essa camada: use o fluxo de restauração do aplicativo. Não se promete proteção contra um administrador ou malware capaz de reescrever banco, armazenamento protegido e estado do sistema operacional.

O armazenamento seguro tem limites de tamanho por valor. Se uma lista excepcionalmente grande atingir esse limite, a gravação falha de forma segura e a autenticação por senha continua; não é criado fallback em texto puro.

## Credencial criptográfica

1. A habilitação exige sessão válida, senha atual, ausência de bloqueio e confirmação explícita sobre os usuários autorizados do Windows.
2. KeyCredentialManager cria uma chave com nome aleatório exclusivo. A chave privada permanece no Windows Hello.
3. O aplicativo obtém a chave pública X.509 SubjectPublicKeyInfo e testa uma assinatura de desafio aleatório antes de registrar a autorização.
4. O registro protegido contém nome da credencial, chave pública, UserId, CredentialRevision, identidade de banco e impressão de vinculação ao nome/salt/versão da credencial. Essa impressão não é aceita como prova de login.
5. Cada login gera nonce de 32 bytes com RandomNumberGenerator. RequestSignAsync solicita a verificação do Hello a cada assinatura. O aplicativo valida RSA de pelo menos 2048 bits, SHA-256 e padding PKCS#1 v1.5.
6. O desafio vive apenas nessa invocação. A API pública não aceita um desafio/assinatura pré-existente fornecido pelo chamador. Uma assinatura anterior não passa na nova verificação.
7. A chave pública aberta precisa coincidir com a cadastrada. Banco, usuário, revisão, autorização persistida e bloqueio são revalidados antes da sessão.

UserConsentVerifier é usado apenas para disponibilidade, nunca como autorização. Não existe token estático, hash de senha reutilizável ou senha armazenada para fazer login.

## Instalação MSIX e distribuição portátil

O caminho nativo implementado segue as APIs KeyCredential documentadas para aplicativos WinUI 3 empacotados. A implementação verifica identidade de pacote e não habilita esse caminho em um executável sem pacote. Assim, a distribuição portátil atual oferece contas lembradas e senha, mas **não oferece autenticação Hello funcional**. Exibe uma explicação para usar instalação MSIX quando aplicável. Essa limitação é intencional e não é substituída por uma confirmação genérica insegura.

KeyCredential não expõe parâmetro HWND nesses métodos. As chamadas que abrem diálogo executam na thread principal, verificam uma janela WinUI viva e ativam essa janela. Não foi usado um overload UWP fictício nem uma chamada UserConsentVerifier.RequestVerificationAsync sem interoperabilidade desktop. A associação/foco real dos diálogos deve ser testada no MSIX; há limitações de foco conhecidas nessa API. Nenhum fluxo de instalação/publicação foi executado.

## Configuração, disponibilidade e revogação

No Windows, Segurança da conta mostra o controle individual de Hello, senha atual, aviso sobre computador compartilhado, ativação/desativação e link `ms-settings:signinoptions`. A página atualiza o estado quando a janela volta a ser ativada. A habilitação também pede consentimento para lembrar o perfil. Falha ou cancelamento da criação apaga a nova chave quando possível; uma chave órfã sem autorização persistida não permite login.

Sem PIN, indisponível ou bloqueado por política, o botão de login rápido não é apresentado como funcional. A lista continua disponível e a seleção mantém o caminho por senha. Os estados de indisponibilidade são diferenciados. Cancelamento não conta como erro de senha nem provoca logout. Dispositivo ocupado e bloqueio do dispositivo/tentativas esgotadas levam a mensagens de uso da senha. A chave removida/alterada externamente nunca é aceita.

Trocar ou recuperar senha incrementa a revisão já existente e invalida imediatamente a autorização anterior, além de tentar remover o registro e a chave Hello. Se a limpeza falhar, a revisão continua rejeitando a autorização antiga. O perfil lembrado permanece. Desativar requer senha atual; esquecer remove primeiro a autorização local e depois o perfil, sem excluir o usuário ou seus dados. Falhas ao apagar a chave nativa não reativam uma autorização local removida.

Logout limpa sessão/contexto, preserva a lista e não remove Hello. A biometria Android conserva sua política anterior de revogação no logout. O contexto em memória é limpo mesmo se a remoção da sessão no armazenamento seguro falhar; a instância não volta a restaurar aquela sessão após revogá-la.

## Interface, teclado e atualizações

Os recursos CardBorder, SectionTitle, PrimaryButton, GhostButton e ThemedIcon existentes são reutilizados, incluindo seus vínculos de tema. CollectionView limita a altura e permite rolagem; cada perfil possui avatar genérico e ação individual. Não há lista vazia decorativa. Controles nativos mantêm navegação por Tab e foco.

Os eventos Completed existentes foram mantidos: usuário transfere foco à senha, senha executa login com a mesma guarda contra repetição. Não foram adicionados handlers nativos de Enter. Após cancelamento Hello, o formulário é reabilitado e o foco volta à senha.

O seletor está dentro de LoginForm, submetido à mesma autorização de atualização obrigatória. Hello verifica a mesma guarda `_loginAllowed` usada pelo login convencional. Cadastro/recuperação não podem iniciar durante autenticação. A conclusão por senha e Hello usa a mesma navegação que recria o Shell.

## Arquivos criados

- Application/Interfaces/IRememberedAccountService.cs
- Application/Interfaces/IWindowsHelloService.cs
- Application/Security/LocalAuthenticationGate.cs
- Application/Security/LocalDatabaseIdentity.cs
- Infrastructure/Services/UnavailableWindowsHelloService.cs
- Platforms/Windows/WindowsHelloService.cs
- UseCases/Services/RememberedAccountService.cs
- UseCases/Services/WindowsHelloEnrollment.cs
- Views/WindowsLoginAccountsView.cs
- Views/WindowsHelloSettingsView.cs
- Tests/ACSView.AuthTests/WindowsAccountTests.cs
- docs/windows-remembered-accounts-hello.md

## Arquivos modificados

- Application/Interfaces/IAuthService.cs
- Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
- UseCases/Services/AuthService.cs
- UseCases/Services/DatabaseBackupService.cs
- Views/LoginPage.xaml
- Views/LoginPage.xaml.cs
- Views/AccountSecurityPage.cs
- ViewModels/ProfileViewModel.cs
- Tests/ACSView.AuthTests/ACSView.AuthTests.csproj
- Tests/ACSView.AuthTests/Program.cs

## Validação automatizada

Os testes são executáveis de console: usar `dotnet run`, não inferir sucesso de um `dotnet test` sem descobertas.

| Suíte | Resultado |
| --- | --- |
| AuthTests | 166 verificações; 76 anteriores e 90 novas |
| SessionTests | 6 verificações |
| UpdateTests | 31 verificações |
| MigrationTests | 326 verificações |
| BugRegressionTests | 79 verificações |
| BolsaFamiliaTests | 22 verificações |
| PatientLoadTests | 21 verificações |
| SnackbarTests | 14 verificações |
| MetricTests | 32 verificações |
| ReminderTests | 32 verificações; entrega Android não simulada |
| Tests | Todas as rotinas passaram, exit 0 |
| ImportTests | Todas as rotinas passaram, exit 0 |

Os novos testes exercitam opt-in, múltiplos usuários, ordenação, reinicialização dos serviços, remoção, usuário excluído, lista corrompida, falhas de armazenamento, ausência de segredos, disponibilidade, criação com senha, cancelamento, assinatura inválida/reutilizada, chave substituída/removida, credencial de outra conta, revogação, senha/recuperação, logout, concorrência e cooldown. Há teste específico de substituição física no mesmo caminho preservando o GUID copiado. A criptografia RSA e SQLite são reais nos testes; somente os limites nativos e ISecureStorage são fakes. Seleção/foco da interface não são simulados por esses testes.

Compilações finais Windows e Android: sucesso, zero erros e 50 warnings em cada target. Não foram identificados warnings nas novas classes; permanecem os avisos existentes de nulabilidade e infraestrutura. Compilações executadas com TargetFrameworks específico, `-m:1 -nr:false` e `-p:NuGetAudit=false` para a compilação. As primeiras tentativas também identificaram assets de restore sem o target Windows; uma restauração do target resolveu. Warnings de nulabilidade/MVVMTK já presentes continuam; nenhum warning foi identificado nas novas classes. Os executáveis de teste emitiram NU1900 por indisponibilidade da consulta de vulnerabilidades do NuGet. A auditoria de vulnerabilidades não foi validada nesta execução.

Logs locais: `tmp-windows-hello-build.log`, `tmp-windows-hello-android.log`, `tmp-windows-hello-auth.log`, `tmp-windows-hello-session.log`, `tmp-windows-hello-update.log`, `tmp-windows-hello-migration.log` e `tmp-windows-hello-ACSView.*.log`.

## Validação manual e critérios ainda pendentes

O executável Windows abriu, e a árvore de acessibilidade mostrou o checkbox de lembrança, usuário, senha e ações tradicionais. A ferramenta de desktop retornou `failed to activate captured window` em duas tentativas; a captura visual não foi válida. Não foram inseridas contas reais nem alteradas configurações de segurança do Windows.

Ainda é necessário verificar em ambiente interativo:

- MSIX com identidade de pacote: habilitar, assinar e entrar com PIN, digital/rosto disponíveis; confirmar a política de verificação de cada assinatura e foco/associação do diálogo WinUI.
- Windows sem PIN, política de sistema, dispositivo ocupado/bloqueado, cancelamento e fechamento da janela com diálogo nativo aberto.
- Cards preenchidos, seleção, indicador de seleção, esquecer, outra conta, Tab/Shift+Tab, Enter e foco após cancelamento.
- Tema claro e escuro, janela mínima, nomes longos, rolagem e leitores de tela.
- Persistência real MAUI/SecureStorage entre processos e instalações, logout e troca de usuários com dados reais isolados.
- Fluxo visual de atualização obrigatória com catálogo real e Android em dispositivo físico.

A implementação em código e os testes não representam prova de funcionamento com hardware Hello. Os critérios que exigem esses testes manuais e o uso de Hello na distribuição portátil não devem ser considerados concluídos.

## Referências oficiais

- https://learn.microsoft.com/pt-br/windows/apps/develop/security/windows-hello
- https://learn.microsoft.com/pt-br/windows/apps/develop/security/windows-hello-login
- https://learn.microsoft.com/pt-br/windows/apps/develop/security/windows-hello-auth-service
- https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.keycredential.requestsignasync
- https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage?view=net-maui-10.0
- https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings
- https://github.com/microsoft/WindowsAppSDK/issues/4290
