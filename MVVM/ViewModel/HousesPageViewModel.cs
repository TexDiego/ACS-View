using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModel
{
    public class HousesPageViewModel
    {
        private readonly HouseService _houseService;
        private List<House> _houses;
        public ObservableCollection<House> houses { get; private set; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }

        public HousesPageViewModel(HouseService houseService)
        {
            _houseService = houseService;
            houses = new ObservableCollection<House>();

            DeleteCommand = new Command<int>(async id=>
            {
                try
                {
                    bool result = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Confirmar", "Tem certeza de que deseja excluir o cadastro?\n\nSUS: " + id, true, "Excluir", true, "Cancelar")));
                    if (result) return;

                    var record = _houses.FirstOrDefault(r => r.CasaId == id);
                    if (record != null)
                    {
                        // Exclusão do banco de dados
                        await _houseService.DeleteHouseAsync(record.CasaId);

                        // Remover da lista interna
                        _houses.Remove(record);

                        // Atualiza a lista visível na UI
                        UpdateDatas();
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", $"Não foi possível deletar o registro.\n\n{ex.Message}", true, "Voltar", false, ""));
                }
            });
            EditCommand = new Command<int>(async (id) =>
            {
                var record = _houses.FirstOrDefault(r => r.CasaId == id);
                if (record != null)
                {
                    // Navegar para a página de edição e passar os dados do registro
                    await Application.Current.MainPage.Navigation.PushAsync(new AddHouse());
                }
            });

            Task.Run(async () => await LoadAndUpdateHouses()).Wait();
        }

        private async Task LoadAndUpdateHouses()
        {
            try
            {
                _houses = await _houseService.GetAllHousesAsync();
                UpdateDatas();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar registros: {ex.Message}");
            }
        }

        private void UpdateDatas()
        {
            throw new NotImplementedException();
        }
    }
}
