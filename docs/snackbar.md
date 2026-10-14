# Snackbar reutilizável

Injete `ISnackbarService` no construtor do view model e chame:

```csharp
await snackbarService.ShowAsync("Alterações salvas.");
await snackbarService.ShowAsync("Não foi possível salvar.", SnackbarStyle.Error);
```

O serviço é singleton e admite apenas uma mensagem por vez em todo o aplicativo.
Qualquer solicitação recebida enquanto há uma exibição ativa retorna `false` imediatamente,
incluindo mensagens diferentes. Não há fila, substituição nem extensão do tempo por cliques.
Depois do fechamento, uma nova solicitação pode exibir a mesma mensagem novamente.

`AppSnackbar` é a superfície visual reutilizável, com cores de informação/erro e adaptação
ao tema. `MauiSnackbarPresenter` a sobrepõe à página ativa, incluindo popups modais,
por quatro segundos. Ao sair da página ou cancelar o token, remove a superfície e restaura
o conteúdo anterior, sem substituir um conteúdo que tenha sido alterado durante a exibição.
As operações visuais ocorrem na thread principal. O leitor de tela anuncia a mensagem.
Falhas de apresentação liberam o serviço e retornam `false`, sem abrir outro diálogo.

Validação da concorrência: `dotnet run --project Tests/ACSView.SnackbarTests`.
