using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class NotesPageViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private readonly IPopupService _popupService;
        private int _loadedVersion = -1;
        private bool _hasLoaded;

        [ObservableProperty] private ObservableCollection<Note> notes = [];
        [ObservableProperty] private Note note = new();

        public ICommand DeleteCommand => new Command<int>(async id => await DeleteNoteAsync(id));
        public ICommand SalvarNota => new Command(async () => await SalvarNotaAsync());
        public ICommand GoBack => new Command(async () => await NavigateBackAsync());
        public ICommand AddNotification => new Command<int>(async id => await AddNotificationAsync(id));
        public ICommand Refresh => new Command(async () => await LoadNotesAsync());

        public NotesPageViewModel(INoteService noteService, IPopupService popupService)
        {
            _noteService = noteService;
            _popupService = popupService;
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
            var note = Notes.FirstOrDefault(n => n.Id == id);
            if (note is null)
            {
                return;
            }

            var result = await _popupService.ShowAsync<NoteNotificationRequestDto>(
                new AddNotificationPopup(note.Content));

            if (result.WasDismissed || result.Result is null)
            {
                return;
            }

            var reminderDate = result.Result.NotifyOn;

            bool permit = await LocalNotificationCenter.Current.RequestNotificationPermission();
            if (!permit)
            {
                await DisplayAlertAsync("Permissão negada", "Não foi possível definir o lembrete. Permissão de notificações negada.", "Ok");
                return;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(13))
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Permissão negada", "Não foi possível definir o lembrete. Permissão de notificações negada.", "Ok");
                    return;
                }
            }

            var request = new NotificationRequest
            {
                NotificationId = note.Id.GetHashCode(),
                Title = "Lembrete de anotação",
                Description = result.Result.Message,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = reminderDate
                }
            };

            LocalNotificationCenter.Current.Cancel(note.Id.GetHashCode());
            await LocalNotificationCenter.Current.Show(request);

            note.NotifyOn = reminderDate;
            await _noteService.UpdateNoteAsync(note);
            await LoadNotesAsync();

            await DisplayAlertAsync("Sucesso", $"Lembrete definido para {reminderDate:dd/MM/yyyy HH:mm}", "Ok");
        }
    }
}
