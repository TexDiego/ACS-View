using ACS_View.Application.Interfaces;
using ACS_View.Domain.Enums;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class DataCleanupViewModel(IUserDataCleanupService cleanupService) : BaseViewModel
{
    public ICommand DeleteAllCommand => new Command(async () => await DeleteAsync(UserDataDeletionScope.All));
    public ICommand DeletePatientsCommand => new Command(async () => await DeleteAsync(UserDataDeletionScope.Patients));
    public ICommand DeleteHousesCommand => new Command(async () => await DeleteAsync(UserDataDeletionScope.Houses));
    public ICommand DeleteNotesCommand => new Command(async () => await DeleteAsync(UserDataDeletionScope.Notes));
    public ICommand GoBackCommand => new Command(async () => await NavigateBackAsync());

    private async Task DeleteAsync(UserDataDeletionScope scope)
    {
        var config = GetDeletionConfig(scope);
        var confirm = await DisplayConfirmationAsync(
            config.Title,
            config.Message,
            "Apagar");

        if (!confirm)
        {
            return;
        }

        try
        {
            await ExecuteWithRunningAsync(async () =>
            {
                await cleanupService.DeleteAsync(scope);
            });

            await DisplayAlertAsync("Dados apagados", config.SuccessMessage);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", $"Nao foi possivel apagar os dados. {ex.Message}");
        }
    }

    private static DeletionConfig GetDeletionConfig(UserDataDeletionScope scope)
    {
        return scope switch
        {
            UserDataDeletionScope.All => new DeletionConfig(
                "Apagar todos os registros",
                "Esta acao apaga pacientes, residencias, visitas, vacinas, condicoes, CIDs e notas deste login. Os dados de outros usuarios nao serao afetados. Deseja continuar?",
                "Todos os registros deste login foram apagados."),
            UserDataDeletionScope.Patients => new DeletionConfig(
                "Apagar pacientes",
                "Esta acao apaga todos os pacientes deste login, incluindo vacinas, condicoes, CIDs e visitas relacionadas. Residencias e notas serao mantidas. Deseja continuar?",
                "Os pacientes deste login foram apagados."),
            UserDataDeletionScope.Houses => new DeletionConfig(
                "Apagar residencias",
                "Esta acao apaga todas as residencias deste login, remove familias e visitas, e deixa os pacientes sem residencia e sem familia. Deseja continuar?",
                "As residencias deste login foram apagadas."),
            UserDataDeletionScope.Notes => new DeletionConfig(
                "Apagar notas",
                "Esta acao apaga todas as notas deste login. Pacientes e residencias serao mantidos. Deseja continuar?",
                "As notas deste login foram apagadas."),
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Opcao de limpeza invalida.")
        };
    }

    private sealed record DeletionConfig(string Title, string Message, string SuccessMessage);
}
