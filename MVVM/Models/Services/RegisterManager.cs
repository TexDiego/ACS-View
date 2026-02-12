using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;
using ACS_View.MVVM.Views;

namespace ACS_View.MVVM.Models.Services
{
    public class RegisterManager
    {
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();
        private readonly IVaccineService _vaccineService = App.ServiceProvider.GetRequiredService<IVaccineService>();
        private readonly IRegisterValidator _validator = App.ServiceProvider.GetRequiredService<IRegisterValidator>();
        private readonly IRegisterFactory _factory = App.ServiceProvider.GetRequiredService<IRegisterFactory>();
        private readonly IUserDialogService _dialogService = App.ServiceProvider.GetRequiredService<IUserDialogService>();

        private async Task CreateAsync(AddRegisterViewModel vm)
        {
            try
            {
                var record = _factory.CreateHealthRecord(vm);
                var vaccines = _factory.CreateVaccines(vm);

                await _healthRecordService.AddRecordAsync(record);
                await _vaccineService.AdicionarVacinasAsync(vaccines);

                await _dialogService.ShowSuccess("Cadastro adicionado com sucesso.");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowError($"Erro ao adicionar cadastro: {ex.Message}");
            }
        }

        private async Task UpdateAsync(HealthRecord existing, AddRegisterViewModel vm)
        {
            try
            {
                var updated = _factory.CreateHealthRecord(vm);
                updated.SusNumber = existing.SusNumber;

                var vaccines = await _vaccineService.GetVaccinesBySusAsync(existing.SusNumber);
                if (vaccines == null)
                {
                    vaccines = _factory.CreateVaccines(vm);
                    await _vaccineService.AdicionarVacinasAsync(vaccines);
                }
                else
                {
                    vaccines.BirthDate = updated.BirthDate;
                    vaccines.IsPregnant = updated.IsPregnant;
                    await _vaccineService.AtualizarVacinasAsync(vaccines);
                }

                await _healthRecordService.UpdateRecordAsync(updated);
                await _dialogService.ShowSuccess("Cadastro atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowError($"Erro ao atualizar cadastro: {ex.Message}");
            }
        }
    }
}