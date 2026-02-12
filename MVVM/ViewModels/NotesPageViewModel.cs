using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class NotesPageViewModel : BaseViewModel
    {
        private readonly INoteService _noteService = App.ServiceProvider.GetRequiredService<INoteService>();

        [ObservableProperty] private ObservableCollection<Note> notes = [];
        [ObservableProperty] private Note note = new();
        [ObservableProperty] private bool isLoading = false;


        public ICommand DeleteCommand => new Command<int>(async id => await DeleteNoteAsync(id));
        public ICommand SalvarNota => new Command(async () => await SalvarNotaAsync());
        public ICommand GoBack => new Command(async () => await Application.Current.MainPage.Navigation.PopAsync());

        public NotesPageViewModel()
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadNotesAsync());
        }

        private async Task DeleteNoteAsync(int id)
        {
            try
            {
                bool confirm = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Confirmação", "Deseja excluir esta nota?", true, "Excluir", true, "Cancelar")));
                if (confirm) return;

                int rowsAffected = await _noteService.DeleteNoteAsync(id);
                if (rowsAffected > 0)
                {
                    var noteToRemove = Notes.FirstOrDefault(n => n.Id == id);
                    if (noteToRemove != null)
                    {
                        Notes.Remove(noteToRemove);
                    }
                }
                else
                {
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", "Não foi possível excluir a nota.", true, "Voltar", false, ""));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar: {ex.Message}");
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task SalvarNotaAsync()
        {
            if (string.IsNullOrWhiteSpace(Note.Content))
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Ops", "O conteúdo não pode estar vazio", false, "", true, "Ok"));
                return;
            }

            await _noteService.SaveNoteAsync(Note);
            ClearNote();
            await LoadNotesAsync();
        }

        public async Task LoadNotesAsync()
        {
            try
            {
                IsLoading = true;

                var notes = await _noteService.GetAllNotesAsync();

                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }
            }
            catch (Exception)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", "Não foi possível carregar as notas.", true, "Voltar", false, ""));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearNote()
        {
            Note.CreationDate = DateTime.Now;
            Note.Content = "";
        }
    }
}
