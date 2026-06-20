using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class NotesPageViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private int _loadedVersion = -1;
        private bool _hasLoaded;

        [ObservableProperty] private ObservableCollection<Note> notes = [];
        [ObservableProperty] private Note note = new();


        public ICommand DeleteCommand => new Command<int>(async id => await DeleteNoteAsync(id));
        public ICommand SalvarNota => new Command(async () => await SalvarNotaAsync());
        public ICommand GoBack => new Command(async () => await NavigateBackAsync());
        public ICommand AddNotification => new Command<int>(async (id) => await AddNotificationAsync(id));
        public ICommand Refresh => new Command(async () => await LoadNotesAsync());


        public NotesPageViewModel(INoteService noteService)
        {
            _noteService = noteService;
        }

        private async Task DeleteNoteAsync(int id)
        {
            try
            {
                bool confirm = await DisplayConfirmationAsync("Confirmação", "Deseja excluir esta nota?", "Excluir");
                if (!confirm) return;

                Note note = Notes.Where(n => n.Id == id).First();
                if (note.NotifyOn is not null)
                    LocalNotificationCenter.Current.Cancel(id.GetHashCode());

                await _noteService.DeleteNoteAsync(id);

                var noteToRemove = Notes.FirstOrDefault(n => n.Id == id);
                if (noteToRemove != null) Notes.Remove(noteToRemove);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar: {ex.Message}");
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private async Task SalvarNotaAsync()
        {
            if (string.IsNullOrWhiteSpace(Note.Content))
            {
                await DisplayAlertAsync("Ops", "O conteúdo não pode estar vazio", "Ok");
                return;
            }

            await _noteService.SaveNoteAsync(Note);
            Note = new Note();
            await LoadNotesAsync();
        }

        public async Task LoadNotesAsync()
        {
            if (_hasLoaded && _loadedVersion == DataChangeTracker.NotesVersion)
            {
                return;
            }

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var notes = await _noteService.GetAllNotesAsync();

                    Notes.Clear();

                    foreach (var note in notes)
                    {
                        if (note.NotifyOn < DateTime.Now) note.NotifyOn = null;
                        Notes.Add(note);
                    }

                    _loadedVersion = DataChangeTracker.NotesVersion;
                    _hasLoaded = true;
                });
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Erro", "Não foi possível carregar as notas.", "Voltar");
            }
        }

        private async Task AddNotificationAsync(int id)
        {
            Note note = Notes.Where(n => n.Id == id).First();

            var reminder = await DisplayActionSheetAsync(
                "Definir lembrete para:",
                "Cancelar",
                null,
                "1 dia",
                "3 dias",
                "1 semana",
                "1 mês",
                "teste");

            if (reminder is null) return;

            DateTime reminderDate = reminder switch
            {
                "1 dia" => DateTime.Now.AddDays(1),
                "3 dias" => DateTime.Now.AddDays(3),
                "1 semana" => DateTime.Now.AddDays(7),
                "1 mês" => DateTime.Now.AddMonths(1),
                _ => DateTime.Now.AddSeconds(10) // Para teste
            };

            var request = new NotificationRequest
            {
                NotificationId = note.Id.GetHashCode(),
                Title = "Lembrete",
                Description = note.Content,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = reminderDate
                }
            };

            bool permit = await LocalNotificationCenter.Current.RequestNotificationPermission();

            if (permit)
            {
                LocalNotificationCenter.Current.Cancel(note.Id.GetHashCode());
                await LocalNotificationCenter.Current.Show(request);
                await DisplayAlertAsync("Sucesso", $"Lembrete definido para {reminderDate.ToShortDateString()}", "Ok");
            }
            else await DisplayAlertAsync("Permissão Negada", "Não foi possível definir o lembrete. Permissão de notificações negada.", "Ok");

            if (OperatingSystem.IsAndroidVersionAtLeast(13))
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();

                if (status != PermissionStatus.Granted)
                {
                    // Tratar recusa (log, fallback ou informar usuário)
                    return;
                }
            }

            note.NotifyOn = reminderDate;

            await _noteService.UpdateNoteAsync(note);

            await LoadNotesAsync();
        }
    }
}

