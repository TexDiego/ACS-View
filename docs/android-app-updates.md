# Atualizações antes do login

O Android consulta a última release estável pública de `TexDiego/ACS-View` ao abrir o login, antes da senha, biometria ou restauração da sessão. Sem `NetworkAccess.Internet`, nenhuma requisição é iniciada. Com internet, a consulta tem limite total de três segundos; erros, ausência de release, limite da API e metadados inválidos liberam o acesso local.

O APK esperado é `ACSView-vX.Y.Z.apk`. Releases antigas sem metadados são opcionais e usam comparação numérica da versão. Novas releases comparam `ApplicationVersion` (versionCode), sem oferecer builds iguais ou anteriores.

## Publicação

Incremente `ApplicationDisplayVersion` e `ApplicationVersion` no projeto e use uma tag compatível. O workflow inclui automaticamente nas notas:

```text
<!-- acs-update {"versionCode":3,"minimumVersionCode":2} -->
```

`update-policy.json` controla a versão mínima aceita. Mantenha `minimumVersionCode` em 1 para atualizações opcionais. Para exigir uma atualização grande de todos os builds anteriores, defina o mínimo igual ao `ApplicationVersion` da nova release. Preserve esse mínimo nas releases seguintes para usuários que pularem a release obrigatória. Não basta marcar apenas uma release como obrigatória, pois o app consulta a última release.

Quando o build instalado está abaixo do mínimo, senha, biometria, restauração da sessão e cadastro ficam bloqueados nessa tela. Fechar o aviso ou cancelar a instalação não libera o login. Sem conexão ou sem resposta válida do GitHub, o app permite acesso local, inclusive quando há uma atualização obrigatória no servidor; a obrigatoriedade depende de conseguir consultar a release.

O download só começa após tocar em Instalar, pode ser cancelado e tem limite de cinco minutos. O tamanho e, quando fornecido pelo GitHub, SHA-256 são conferidos antes de abrir o instalador. O APK fica no cache privado compartilhável do MAUI. O Android exige autorização de instalação por esta fonte, confirmação do usuário, mesmo identificador e assinatura compatível. Nenhum dado SQLite é apagado pelo fluxo.

## Validação em aparelho

- Modo avião: login imediato sem busca.
- Internet sem acesso ao GitHub: login liberado após no máximo a consulta de três segundos.
- Release mais nova opcional: aviso antes da biometria; Agora não permite entrar.
- Release obrigatória: Fechar, cancelar download ou cancelar instalação mantém Entrar bloqueado.
- Autorizar esta fonte, voltar e tocar Instalar novamente; verificar abertura do instalador.
- Instalar APK assinado compatível, reabrir e conferir versão e dados existentes.
- Repetir com sessão persistida e biometria habilitada; nenhum caminho deve pular o aviso.

Referências: [Releases API](https://docs.github.com/en/rest/releases/releases#get-the-latest-release), [MAUI Launcher](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/launcher), [Android instalação por fonte](https://developer.android.com/reference/android/content/pm/PackageManager#canRequestPackageInstalls()).
