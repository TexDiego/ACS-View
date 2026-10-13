# Inicialização Android sem Visual Studio

Validação local em 13/10/2026.

## Causa reproduzida

A abertura sem depurador de `com.texdiego.acsview` no Samsung SM-S928B
encerrou o processo durante `App.InitializeComponent` com:

```text
Microsoft.Maui.Controls.Xaml.XamlParseException
Position 263:63. StaticResource not found for key BaseImageButton
```

`DeleteImageButton` e `EditImageButton` referenciavam `BaseImageButton`
antes da sua declaração no dicionário de estilos. A declaração base agora
precede os estilos derivados, preservando os seus setters.

## Empacotamento

O Debug Android usa `EmbedAssembliesIntoApk=false` para habilitar Fast
Deployment no Visual Studio e transferir as bibliotecas incrementalmente.
Para gerar um APK Debug independente, use explicitamente
`-p:EmbedAssembliesIntoApk=true` na compilação. Esse APK inclui as bibliotecas;
o APK de Fast Deployment exige a implantação completa pelo IDE ou pelo
destino MSBuild `Install`, em vez de somente `adb install`.
O assembly usa o nome `ACSView` em todos os destinos, sem espaços
no nome da biblioteca nativa e com identidade consistente para a restauração
do Visual Studio. ApplicationId, versões, namespace e banco permanecem
com os mesmos valores. A instalação Debug continua no perfil Android 0.

Referência: https://learn.microsoft.com/en-us/dotnet/android/messages/xa0127

## Evidência

```powershell
dotnet build 'ACS View.csproj' -f net10.0-android -c Debug `
  -p:EmbedAssembliesIntoApk=true `
  -p:IntermediateOutputPath=tmp/standalone-validation/android-obj/ `
  -p:OutputPath=tmp/standalone-validation/android-output/ -v minimal
```

- Build concluído: 0 erros, 49 avisos. Log: `tmp/standalone-build.log`.
- Nenhuma referência antecipada em `BasedOn` no dicionário de estilos.
- APK inspecionado: `lib_ACSView.dll.so` e `lib_Microsoft.Maui.Controls.dll.so`
  presentes para arm64-v8a e x86_64.
- APK instalado via `adb install --no-incremental -r --user 0`, com sucesso,
  sem desinstalação nem limpeza dos dados do aplicativo.
- Cache antigo de Fast Deployment movido de `files/.__override__` para
  `files/.__override_standalone_backup_20261013`, preservando seu conteúdo.
- Duas inicializações a frio sem depurador: processos ativos, Activity em
  primeiro plano e nenhum novo crash. A primeira captura foi inspecionada
  e mostrou a tela de login: `tmp/standalone-validation/startup.png`.

## Limites e confirmação final

O ADB permaneceu conectado para instalação e coleta das evidências; nenhum
depurador foi anexado. A desconexão física do cabo ainda precisa ser
confirmada: desconectar, fechar o app e abrir novamente pelo ícone.
Login e fluxos autenticados não foram exercitados nesta validação.

## Restauração do Visual Studio

A declaração inicial de `AssemblyName` somente no Android produzia
`AssemblyName`/`PackageId` diferentes entre os destinos: `ACSView` no
Android e `ACS View` no iOS/Windows. Após o relato de restauração inválida
com `ProjectName` ausente no IDE, a declaração foi movida para o grupo
comum. Todos os destinos e a avaliação externa agora retornam `ACSView`.

A restauração forçada da solução com o MSBuild 18.10.1 do Visual Studio
concluiu para ambos os projetos. O arquivo NuGet dgspec do app contém
`projectName: ACSView`. `ResolveReferences` com `DesignTimeBuild=true` e
`BuildingInsideVisualStudio=true` também concluiu no Android. A exibição
dos pacotes no IDE precisa ser confirmada após recarregar o projeto.
