# Android: lembretes e validação da primeira distribuição

## Diagnóstico e implementação

A análise usou o pacote instalado Plugin.LocalNotification 13.0.0 e o [commit registrado no NuGet](https://github.com/thudugala/Plugin.LocalNotification/tree/49520e134154909b313fda896abcc0ac7415ba02). Não houve troca nem atualização da biblioteca.

- Faltava `UseLocalNotification()`, que registra serviços, inicialização, canais e eventos de plataforma, inclusive o delegate iOS.
- POST_NOTIFICATIONS era solicitado pelo plugin e novamente por MAUI. Agora o plugin controla esse pedido, e seu resultado é reconsultado para detectar notificações totalmente desativadas.
- Não havia acesso especial a alarmes exatos. No Android 12/API 31+, o adaptador consulta `AlarmManager.CanScheduleExactAlarms()`. A versão 13.0.0 não expõe um status público separado de alarme exato e só inclui essa consulta em sua verificação de permissão a partir da API 33; por isso a consulta nativa é necessária na API 31/32. As APIs de versões posteriores do plugin não foram presumidas como disponíveis.
- O plugin usa `SetExactAndAllowWhileIdle` na API 23+ quando há acesso exato e cai para `SetAndAllowWhileIdle` sem esse acesso. Para estes lembretes com horário escolhido pelo usuário, o app exige acesso exato em vez de informar sucesso para um alarme inexato.
- O bool de `Show()` era ignorado e `NotifyOn` era gravado mesmo quando a operação retornava false. Agora também é conferida a lista pendente antes da gravação. Falhas de reagendamento ou de SQLite restauram o pedido anterior quando possível; falhas na restauração são informadas, com nova tentativa na reconciliação.
- `Int32.GetHashCode()` devolve o próprio inteiro. IDs positivos continuam iguais a `Note.Id`, com unicidade global do SQLite entre contas. IDs não positivos ficam reservados para futuras categorias, que devem também usar um marcador próprio; não se deve reutilizar os IDs positivos de notas.

O canal `acs_note_reminders` tem nome e descrição em português, importância Default e não ignora Não Perturbe. A permissão de exibição e o canal são verificados separadamente de alarmes exatos. Havendo necessidade de acesso especial, o usuário recebe uma explicação, pode recusar, ou abrir Alarmes e lembretes; ao retornar o acesso é novamente verificado. A interação externa usa a proteção já existente de ciclo de vida, sem alterar autenticação, SecureStorage ou biometria.

Só `SCHEDULE_EXACT_ALARM` foi declarado, sem `USE_EXACT_ALARM`, acesso à política de notificações ou full screen intent. Também foram incluídos VIBRATE, WAKE_LOCK e RECEIVE_BOOT_COMPLETED. O Android 13+ pede POST_NOTIFICATIONS; no Android 14+, alarmes exatos normalmente começam negados em instalações novas. Referências: [permissão de notificações](https://developer.android.com/develop/ui/views/notifications/notification-permission), [alarmes](https://developer.android.com/develop/background-work/services/alarms) e [mudança no Android 14](https://developer.android.com/about/versions/14/changes/schedule-exact-alarms).

## Persistência e recuperação

`Note.NotifyOn` continua sendo a intenção persistida do lembrete. O campo opcional e aditivo `Note.ReminderMessage` preserva a mensagem personalizada para recuperação; o sqlite-net acrescenta a coluna ao inicializar a tabela existente. Registros antigos sem mensagem usam até 160 caracteres do conteúdo da nota. Nenhum campo de pacientes foi alterado.

A reconciliação ocorre após a inicialização do banco, restauração de sessão, login, retorno ao app e carregamento de notas. Ela lê os lembretes de todas as contas apenas para manutenção do agendamento, sem abrir uma sessão nem exibir dados de outra conta. Agendamento e cancelamento interativos exigem a conta proprietária. A regra existente de dez lembretes futuros por conta é preservada e contada no banco atualizado, sob um lock de operações, após limpar vencidos e pedidos órfãos. Limpeza em massa de notas também reconcilia o sistema.

A reconciliação remove pedidos de notas excluídas/canceladas, limpa NotifyOn e mensagem vencidos, e restaura pedidos ausentes ou com horário/mensagem divergentes. Não repete lembretes vencidos. O cancelamento solicitado pelo usuário remove também a notificação entregue daquele ID.

O receiver do plugin `plugin.LocalNotification.ScheduledAlarmReceiver` recebe BOOT_COMPLETED e MY_PACKAGE_REPLACED e restaura os pedidos salvos nas SharedPreferences privadas, sem depender de login. A permissão RECEIVE_BOOT_COMPLETED permite essa estratégia após reinicialização e atualização. É necessário testar esse receiver no aparelho: a implementação da biblioteca usa uma operação assíncrona no broadcast e fabricantes podem impor restrições adicionais.

Revogar acesso exato apaga alarmes do AlarmManager, mas pode deixar a lista do plugin intacta. Um receiver adicional recebe SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED quando o acesso é concedido, marca o cache como inválido e reconcilia o banco usando GoAsync. A mesma marca é persistida quando o app detecta revogação e na inicialização de cada processo Android, cobrindo também a reabertura após Forçar parada. Nessa situação, os pedidos antigos de notas são descartados e os futuros são reconstruídos com os mesmos IDs; não são criadas duplicatas. Se o processo não conseguir concluir, a próxima abertura retoma a recuperação. Essa reconstrução ainda requer observação em aparelho real.

`GetPendingNotificationList()` no Android é o registro persistido da biblioteca, não uma consulta aos alarmes efetivamente existentes no AlarmManager. Sua conferência valida a aceitação/registro do pedido e a consistência de IDs/horários, **não comprova entrega**. Processo encerrado pelo sistema e remoção da tela de recentes normalmente permitem delivery pelo receiver; **Forçar parada** nas configurações é diferente e bloqueia alarmes/broadcasts até reabrir o app. Doze impõe quotas e fabricantes podem atrasar/bloquear execução mesmo com permissão. Não há pedido de exceção global à otimização de bateria e não há garantia absoluta de horário sem validação real.

## Build e assinatura

```powershell
dotnet restore "ACS View.csproj" -p:TargetFrameworks=net10.0-android
dotnet build "ACS View.csproj" -f net10.0-android -c Debug --no-restore
dotnet publish "ACS View.csproj" -f net10.0-android -c Release -p:TargetFrameworks=net10.0-android -p:AndroidPackageFormats=apk
dotnet run --project Tests/ACSView.AuthTests/ACSView.AuthTests.csproj
dotnet run --project Tests/ACSView.ReminderTests/ACSView.ReminderTests.csproj
```

O publish local sem configuração de assinatura pode usar a chave de desenvolvimento do SDK. A chave definitiva foi criada em 06/10/2026 fora do checkout, e os quatro secrets foram cadastrados via `gh` no repositório correto. O pipeline rejeita secrets ausentes e confere assinatura, fingerprint definitivo, pacote, versão, SDK e permissões do APK final. Nenhuma chave privada ou senha foi adicionada ao código. Os testes de lembretes cobrem estado, falhas, recuperação, limite e concorrência com adaptadores substituídos; não simulam AlarmManager, reboot, Doze ou entrega física.

## Checklist obrigatório em aparelho

Validação final local em 06/10/2026, depois de `dotnet clean` Android Debug/Release: restore concluído; Debug com 0 erros e 51 warnings preexistentes; publish Release com 0 erros e 233 warnings preexistentes (51 C# e 182 XAML). A comparação de cada warning com os logs anteriores não encontrou warnings novos. As seis suítes passaram: autenticação/biometria/migração (71 verificações), lembretes (32), Bolsa Família (22), importação/persistência/CNS múltiplos, ciclo de vida de sessão (6) e suíte geral. A expectativa antiga de 10 pontos para `NoVulnerability` foi corrigida para os 5 pontos definidos no catálogo pelo commit `79e6bf7`, sem alterar a regra do aplicativo.

O APK definitivo foi inspecionado com aapt: `com.texdiego.acsview`, `versionName=1.0.0`, `versionCode=1`, minSdk 21 e targetSdk 36, cinco permissões de lembretes esperadas e receivers de boot/atualização/concessão exata. Backup permaneceu desabilitado e o APK não declara `debuggable`. apksigner validou os esquemas v1/v2/v3, um único assinante RSA de 3072 bits e o certificado definitivo; não há assinatura Debug. A inspeção das entradas ZIP não encontrou banco real ou material de assinatura; a busca nos conteúdos descompactados não encontrou nenhuma das senhas/Base64 locais nem chave privada PEM. O workflow passou no actionlint 1.7.12, e seus dois gates Python foram executados localmente contra o APK final e as credenciais externas. O ambiente hospedado do GitHub Actions ainda não foi executado.

### Chave definitiva e APK local

- Keystore: `S:\Projetos Mobile\ACS View Secrets\ACSView-release.jks`.
- Credenciais: `S:\Projetos Mobile\ACS View Secrets\ACSView-release-credentials.txt` (valores somente neste arquivo externo).
- Base64: `S:\Projetos Mobile\ACS View Secrets\ACSView-release-base64.txt`.
- Alias: `acsview-release`; RSA 3072 bits, assinatura SHA256withRSA; validade de 06/10/2026 a 21/02/2054 (10.000 dias).
- SHA-256 do JKS: `7F725D01FEE49ABE09F953F9BF1BD81AF42F4B19215DBEFCECD624B676224056`.
- SHA-256 do certificado: `AA6753D2D7763ED69A7DC7453CC19584C2A69604D524615956C72737FC5F09BD`.
- Roundtrip Base64 aprovado por SHA-256, integridade/alias conferidos por keytool, acesso NTFS somente ao usuário atual; arquivos externos ao Git.
- Actions Secrets configurados em `TexDiego/ACS-View`, com confirmação somente dos nomes: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`.
- APK: `S:\Projetos Mobile\ACS View Release Validation\ACSView-v1.0.0.apk`, 73.905.146 bytes.
- SHA-256 do APK: `74B91C12BEEE9BC73FBB2FBB31CDF2BD165A087596FD454F96DEA7D62971089E`.

Preserve este keystore e ambas as senhas para todas as futuras atualizações; perder a chave impede APKs atualizáveis sobre instalações existentes. `ApplicationVersion`/versionCode deve crescer estritamente nas futuras versões; a v1.0.0 permanece em 1. `com.companyname.acsview` é outro aplicativo, sem migração automática de seus dados. Nenhum banco real ou dado de paciente foi alterado nesta validação.

### Gate físico e publicação

`adb devices -l` não encontrou aparelho conectado. Todos os itens físicos abaixo estão **PENDENTES**, incluindo primeira abertura, permissões nativas, entrega observada, foreground/background/tela bloqueada, reboot, recuperação após concessão de alarmes e atualização com a mesma chave preservando o SQLite. Os testes locais com SQLite real aprovam a migração aditiva e preservação de dados fictícios, mas não comprovam uma atualização instalada no Android.

Não houve teste físico executado, aprovado ou reprovado. Não foi criada a tag `v1.0.0`, não houve push nem GitHub Release: o gate exige sucesso dos testes físicos críticos antes da publicação. A tag não existia local ou remotamente na auditoria. Depois de concluir esse checklist em aparelho autorizado, envie o commit, crie/envie a tag, acompanhe o workflow e valide a assinatura/hash do APK exato baixado da GitHub Release, instalando esse mesmo arquivo para a conferência final. Não é necessário cadastrar manualmente os secrets.

O primeiro publish expôs erro preexistente de resolução XAML de `MauiIcon` em Release. `Properties/IconXmlns.cs` acrescenta o mapeamento explícito para o assembly MauiIcons.Core, sem trocar pacote, ícones ou páginas; o publish posterior passou. iOS teve o ApplicationId existente preservado e todo acesso Android nativo está isolado, mas não foi compilado/testado em Mac nesta execução.

Use dados fictícios. Execute em Android 13/API 33, Android 14/API 34 e versões mais recentes compatíveis (incluindo API 35 e 36), registrando aparelho, fabricante, versão, permissões e horário observado. Instale sempre o APK assinado com a chave de distribuição para o teste de atualização.

- [ ] Instalação limpa de ACSView-v1.0.0.apk: nome, versão e abertura corretos; cadastro/login e banco local funcionam.
- [ ] Aceitar POST_NOTIFICATIONS e autorizar Alarmes e lembretes; agendar para alguns minutos no futuro e conferir na lista ativa.
- [ ] Android 12/API 31 e 12L/API 32: repetir o acesso especial a alarmes sem pedido de POST_NOTIFICATIONS.
- [ ] Recusar POST_NOTIFICATIONS: mensagem clara, nenhum NotifyOn novo, nenhum falso sucesso.
- [ ] Permitir notificações e recusar acesso exato: mensagem diferente, sem salvar; autorizar nas configurações e voltar, confirmando novo agendamento.
- [ ] Desativar só o canal Lembretes de notas: o app deve informar notificações desativadas.
- [ ] Entrega com app em primeiro plano.
- [ ] Entrega com app em segundo plano.
- [ ] Entrega após remover o app dos recentes; repetir com processo encerrado pelo Android, sem Forçar parada.
- [ ] Forçar parada, constatar a restrição do sistema, reabrir e verificar recuperação de lembretes ainda futuros.
- [ ] Entrega com tela bloqueada; respeitar preferências de privacidade do canal e Não Perturbe.
- [ ] Economia de bateria e Doze: verificar horários reais e restrições do fabricante; não considerar apenas o registro pendente como aprovação.
- [ ] Reiniciar antes do horário e não abrir o app: confirmar entrega pelo receiver de boot, sem duplicação.
- [ ] Reiniciar com um lembrete já vencido: não reapresentar lembrete antigo; ao abrir, limpar o estado vencido.
- [ ] Revogar e conceder novamente acesso exato: confirmar recuperação dos futuros e nenhum lembrete duplicado.
- [ ] Cancelar lembrete: retirar da lista e não entregar no horário antigo; repetir cancelamento de lembrete entregue.
- [ ] Reagendar: apenas o novo horário dispara; mensagem personalizada é preservada após reinicialização.
- [ ] Criar dez lembretes ativos; tentar o 11º e confirmar recusa; substituir um dos dez e confirmar sucesso; cancelar um e criar outro.
- [ ] Disparar lembrete, reabrir notas e confirmar que ele deixa de ocupar uma vaga.
- [ ] Excluir uma nota e fazer limpeza em massa de notas: cancelar todos os pedidos correspondentes.
- [ ] Em duas contas locais, confirmar isolamento da lista e limite por conta, sem colisão de IDs.
- [ ] Instalar versão posterior com o mesmo ApplicationId e a mesma chave, aumentando ApplicationVersion: preservar pacientes, famílias, visitas, notas e banco SQLite.
- [ ] Atualizar APK antes do horário de um lembrete e não abrir o app: testar MY_PACKAGE_REPLACED e entrega única; depois conferir recuperação na abertura.
- [ ] Confirmar que desinstalação limpa os dados apenas em aparelho de teste; nunca usar essa operação para atualizar instalações reais.

## Permissões de arquivos mantidas

READ_EXTERNAL_STORAGE, WRITE_EXTERNAL_STORAGE, MANAGE_EXTERNAL_STORAGE e MANAGE_MEDIA foram preservadas para evitar ampliar esta tarefa. O único fluxo de seleção encontrado usa `FilePicker` para planilhas, além do armazenamento privado do app. WRITE_EXTERNAL_STORAGE não concede acesso amplo em Android moderno, READ_EXTERNAL_STORAGE não resolve o modelo de mídia no Android 13+, e MANAGE_EXTERNAL_STORAGE/MANAGE_MEDIA são acessos especiais amplos que não são necessários para um seletor de documentos baseado em SAF. Não foi encontrada solicitação de acesso especial a esses recursos. A remoção deve ser tratada como débito técnico com testes de importação em APIs antigas e atuais; esta mudança não habilita backup nem reescreve acesso a arquivos.

## Arquivos da implementação

- Publicação: `.gitignore`, `ACS View.csproj`, `global.json`, `.github/workflows/release-android.yml`, `Platforms/Android/AndroidManifest.xml`, `README.md` e este documento.
- Inicialização e integração: `MauiProgram.cs`, `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, `Views/App.xaml.cs`, `ViewModels/NotesPageViewModel.cs`, `UseCases/Services/UserDataCleanupService.cs` e `Properties/IconXmlns.cs`.
- Lembretes: `Domain/Entities/Note.cs`, `Application/Interfaces/INoteReminderService.cs`, `Application/Reminders/ReminderContracts.cs`, `UseCases/Services/NoteReminderService.cs`, `Infrastructure/Services/SQLiteNoteReminderStore.cs`, `Infrastructure/Services/LocalNoteNotificationScheduler.cs` e `Platforms/Android/ExactAlarmAccessReceiver.cs`.
- Testes: `Tests/ACSView.AuthTests/ACSView.AuthTests.csproj`, `Tests/ACSView.AuthTests/Program.cs`, `Tests/ACSView.ReminderTests/ACSView.ReminderTests.csproj` e `Tests/ACSView.ReminderTests/Program.cs`.
