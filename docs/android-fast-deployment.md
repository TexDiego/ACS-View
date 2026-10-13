# Fast Deployment Android

Validado em 13/10/2026 no Samsung SM-S928B, Android 16 / API 36.

## Configuração

`ACS View.csproj` define `EmbedAssembliesIntoApk=false` somente no Debug
Android. O SDK instalado (36.1.69) usa essa propriedade para habilitar
Fast Deployment. Release continua com `EmbedAssembliesIntoApk=true`.
Debug continua direcionado ao perfil Android 0.

O APK Debug sozinho não contém as bibliotecas gerenciadas. Implantar pelo
Visual Studio ou pelo destino `Install` transfere essas bibliotecas para
`files/.__override__/arm64-v8a/`. Depois da implantação completa, o app
também pode ser aberto pelo ícone. Para um APK Debug independente, compilar
com `-p:EmbedAssembliesIntoApk=true`, conforme `android-standalone-startup.md`.

Referência: https://learn.microsoft.com/en-us/dotnet/android/building-apps/build-properties#embedassembliesintoapk

## Validação real

Comando usado, com saídas isoladas para preservar os arquivos do IDE:

```powershell
dotnet build 'ACS View.csproj' -f net10.0-android -c Debug -t:Install `
  -p:IntermediateOutputPath=tmp/fast-deploy-validation/android-obj/ `
  -p:OutputPath=tmp/fast-deploy-validation/android-output/ `
  -p:AdbTarget=-sRQCY5007Q0H
```

- `run-as` funciona no aparelho e as bibliotecas gerenciadas não estão no APK.
- Primeira compilação e implantação: 2min09s, 0 erros e 49 avisos;
  transferência inicial: 52,8s. Log: `tmp/fast-deploy-validation-first.log`.
- Adicionada uma marca de metadados temporária ao assembly para exercitar
  uma alteração real. Compilação e implantação: 11,16s; transferência: 807ms.
  Diagnóstico: `tmp/fast-deploy-validation/fastdeploy-incremental.json`.
- SHA256 do assembly atualizado no aparelho idêntico ao arquivo compilado.
- Marca removida e código final reimplantado: 9,94s, transferência: 697ms,
  0 erros e 49 avisos. Log: `tmp/fast-deploy-validation-final.log`.
- SHA256 do assembly final também corresponde ao arquivo compilado.
- Duas aberturas a frio pelo ADB sem depurador; tela de login inspecionada
  em `tmp/fast-deploy-validation/startup.png`, processo final ativo.
- Atualizações feitas sem desinstalar nem limpar os dados do app.

O teste exerceu o destino MSBuild `Install`, que usa a tarefa `FastDeploy`
do SDK. F5, conexão do depurador e breakpoint no Visual Studio não foram
exercitados nesta validação. Os tempos são desta execução; mudanças em
recursos, Java ou bibliotecas nativas podem exigir recompilar o APK.
