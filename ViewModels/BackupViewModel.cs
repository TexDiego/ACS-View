using ACS_View.Application.Backups;
using ACS_View.Application.Interfaces;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS_View.ViewModels;

public partial class BackupViewModel(IEncryptedBackupService backups,IAuthService auth) : BaseViewModel
{
    [ObservableProperty] private string profileName="";
    [ObservableProperty] private bool canExport;
    [ObservableProperty] private string accountPassword="";
    [ObservableProperty] private string backupPassword="";
    [ObservableProperty] private string confirmPassword="";
    [ObservableProperty] private string restorePassword="";
    [ObservableProperty] private string fileName="";
    [ObservableProperty] private string selectedFile="Nenhum arquivo selecionado";
    [ObservableProperty] private string status="";
    [ObservableProperty] private string summary="";
    [ObservableProperty] private bool hasSummary;
    [ObservableProperty] private bool replaceCurrentProfile;
    [ObservableProperty] private bool hidePasswords=true;
    [ObservableProperty] private bool controlsEnabled=true;
    [ObservableProperty] private double progress;
    [ObservableProperty] private bool restartRequired;
    [ObservableProperty] private bool canCancel;
    private FileResult? selected;
    private CancellationTokenSource? operation;
    private bool confirmed;

    public async Task LoadAsync()
    {
        var user=await auth.GetCurrentUserAsync();
        CanExport=user is not null;
        ProfileName=user?.Username ?? "Restauração sem autenticação";
        var safeName=new string(ProfileName.Where(c=>char.IsLetterOrDigit(c) || c is '_' or '-').Take(40).ToArray());
        FileName=$"ACSView_{safeName}_{DateTime.Now:yyyy-MM-dd_HHmm}.acs";
    }

    [RelayCommand] private void TogglePassword()=>HidePasswords=!HidePasswords;
    [RelayCommand] private void Cancel() { if(!confirmed)operation?.Cancel(); }

    [RelayCommand]
    private async Task ExportAsync()=>await RunAsync(async ct=>
    {
        if(!CanExport)throw new InvalidOperationException("Entre na conta para exportar.");
        BackupEncryptionService.ValidatePassword(BackupPassword);
        if(BackupPassword!=ConfirmPassword)throw new InvalidOperationException("As senhas do backup não conferem.");
        if(BackupPassword==AccountPassword)throw new InvalidOperationException("Escolha uma senha de backup diferente da senha da conta.");
        var name=Path.GetFileName(FileName.Trim());
        if(name!=FileName.Trim() || name.Length is < 5 or > 120 || name.IndexOfAny(Path.GetInvalidFileNameChars())>=0 || !name.EndsWith(".acs",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Informe um nome de arquivo válido terminado em .acs.");
        var path=Path.Combine(FileSystem.CacheDirectory,$"backup-{Guid.NewGuid():N}.acs");
        using var interaction=(Microsoft.Maui.Controls.Application.Current as Views.App)?.BeginExternalInteraction();
        try
        {
            Status="Preparando e criptografando somente este perfil…";Progress=.1;
            await using(var output=File.Create(path))await backups.ExportAsync(output,AccountPassword,BackupPassword,ct);
            Status="Validando o arquivo criptografado…";Progress=.75;
            await using(var check=File.OpenRead(path))await BackupEncryptionService.DecryptAsync(check,BackupPassword,ct);
            ct.ThrowIfCancellationRequested();
            Status="Escolha onde salvar o backup…";
            await using var file=File.OpenRead(path);
            var result=await FileSaver.Default.SaveAsync(name,file,ct);
            if(!result.IsSuccessful)throw new IOException("O arquivo não foi salvo. Escolha um destino com espaço disponível e tente novamente.");
            Progress=1;Status="Backup criptografado salvo. Guarde o arquivo e a senha em locais seguros.";
        }
        finally { File.Delete(path);AccountPassword=BackupPassword=ConfirmPassword=""; }
    });

    [RelayCommand]
    private async Task SelectAsync()=>await RunAsync(async ct=>
    {
        backups.Discard();HasSummary=false;Summary="";selected=null;
        using var interaction=(Microsoft.Maui.Controls.Application.Current as Views.App)?.BeginExternalInteraction();
        var file=await FilePicker.Default.PickAsync(new PickOptions { PickerTitle="Selecionar backup ACS View (.acs)" });
        ct.ThrowIfCancellationRequested();
        if(file is null)return;
        if(!file.FileName.EndsWith(".acs",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Selecione um arquivo .acs.");
        selected=file;SelectedFile=file.FileName;Status="Informe a senha do backup para ver o resumo.";
    });

    [RelayCommand]
    private async Task InspectAsync()=>await RunAsync(async ct=>
    {
        HasSummary=false;backups.Discard();
        if(selected is null)throw new InvalidOperationException("Selecione um arquivo .acs.");
        Status="Descriptografando e verificando o backup…";Progress=.2;
        await using var stream=await selected.OpenReadAsync();
        var info=await backups.InspectAsync(stream,RestorePassword,ct);
        Summary=$"Perfil: {info.Username}\nBackup: {info.CreatedUtc.ToLocalTime():dd/MM/yyyy HH:mm}\nPacientes: {info.Counts.GetValueOrDefault("Patient")} · Residências: {info.Counts.GetValueOrDefault("House")}\nFamílias: {info.Counts.GetValueOrDefault("FamilyRecord")} · Visitas: {info.Counts.GetValueOrDefault("Visits")}\nNotas: {info.Counts.GetValueOrDefault("Note")} · Doses: {info.Counts.GetValueOrDefault("PatientVaccineDose")}";
        HasSummary=true;Progress=1;Status="Backup validado. Confira o perfil antes de confirmar.";
        RestorePassword="";
    });

    [RelayCommand]
    private async Task RestoreAsync()=>await RunAsync(async ct=>
    {
        if(!HasSummary)throw new InvalidOperationException("Valide o arquivo antes de restaurar.");
        var message=Summary+"\n\n"+(ReplaceCurrentProfile?"Todos os dados do perfil autenticado de mesmo nome serão substituídos. As demais contas serão preservadas.":"O perfil será acrescentado sem substituir contas existentes.")+"\n\nApós preparar a restauração, feche e abra o aplicativo para ativá-la e entrar com a senha original. Até reiniciar, o banco ficará somente para leitura.";
        if(!await DisplayConfirmationAsync("Confirmar restauração",message,"Restaurar","Cancelar"))return;
        ct.ThrowIfCancellationRequested();
        confirmed=true;CanCancel=false;Status="Consolidando e validando os perfis. Aguarde…";Progress=.3;
        await backups.RestoreAsync(ReplaceCurrentProfile,AccountPassword,ct);
        AccountPassword="";HasSummary=false;RestartRequired=true;Progress=1;
        Status="Restauração preparada e validada. Feche completamente e abra o aplicativo para ativar o perfil. Entre com a senha original. O banco anterior foi preservado.";
        await DisplayAlertAsync("Reinicie o ACS View",Status);
        if(Microsoft.Maui.Controls.Application.Current is Views.App app)await app.ResetToLoginShellAsync();
    });

    private async Task RunAsync(Func<CancellationToken,Task> action)
    {
        if(operation is not null || RestartRequired)return;
        operation=new CancellationTokenSource();IsBusy=true;CanCancel=true;ControlsEnabled=false;Progress=0;confirmed=false;
        try { await action(operation.Token); }
        catch(OperationCanceledException) { Status="Operação cancelada. O banco anterior foi preservado."; }
        catch(Exception ex)
        {
            Status=ex is InvalidOperationException or InvalidDataException?ex.Message:"Não foi possível concluir. Confira a senha, o arquivo e o espaço disponível. O banco anterior foi preservado.";
            await DisplayAlertAsync("Backup e restauração",Status);
        }
        finally { operation.Dispose();operation=null;IsBusy=false;CanCancel=false;ControlsEnabled=!RestartRequired;confirmed=false; }
    }

    public void Leave()
    {
        if(!confirmed)operation?.Cancel();
        backups.Discard();AccountPassword=BackupPassword=ConfirmPassword=RestorePassword="";
        HasSummary=false;
    }
}
