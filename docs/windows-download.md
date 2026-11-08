# ACS View para Windows

Baixe o ZIP Windows x64 em [Releases](https://github.com/TexDiego/ACS-View/releases/tag/windows-v1.4.0), extraia todos os arquivos para uma pasta e execute `ACSView.exe`. Não execute dentro do ZIP nem mova apenas o executável. Requer Windows 10 versão 1809 ou posterior, em computador x64. O .NET e o Windows App SDK estão incluídos.

Esta distribuição é portátil e não possui assinatura de editor. O Windows pode exibir um aviso de aplicativo não reconhecido. O arquivo SHA256SUMS.txt permite conferir a integridade do download.

Os lembretes da distribuição portátil exigem que o aplicativo permaneça aberto. O agendamento pelo sistema com o aplicativo fechado depende de uma instalação com identidade de pacote e não faz parte deste ZIP.

Os dados permanecem no diretório local do aplicativo. Para atualizar, feche o ACS View e extraia a nova versão em outra pasta. Não exclua os dados locais. Faça um backup no aplicativo antes de atualizar.

## Gerar o pacote a partir do código

No Windows, com o SDK e workload MAUI configurados conforme global.json:

```powershell
./scripts/publish-windows.ps1
```

A versão Windows é definida no grupo Windows do projeto. A versão Android permanece independente. A tag `windows-v1.4.0` não aciona o workflow Android e esta Release não substitui a versão Android marcada como Latest.
