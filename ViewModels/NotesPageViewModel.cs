using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class NotesPageViewModel : BaseViewModel
    {
        private readonly INoteReminderService _reminders;
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
        public ICommand ShowActiveNotificationsCommand => new Command(async () => await ShowActiveNotificationsAsync());
        public ICommand Refresh => new Command(async () => await LoadNotesAsync(force: true));

        public NotesPageViewModel(INoteService noteService, IPopupService popupService, INoteReminderService reminders)
        {
            _noteService = noteService;
            _popupService = popupService;
            _reminders = reminders;
        }

        private async Task DeleteNoteAsync(int id)
        {
            try
            {
                bool confirm = await DisplayConfirmationAsync("Confirmação", "Deseja excluir esta nota?", "Excluir");
                if (!confirm) return;

                Note note = Notes.Where(n => n.Id == id).First();
                await _reminders.CancelAsync(note.Id);

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

        public async Task LoadNotesAsync(bool force = false)
        {
            if (!force && _hasLoaded && _loadedVersion == DataChangeTracker.NotesVersion)
            {
                return;
            }

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    await _reminders.ReconcileAsync();
                    var notes = await _noteService.GetAllNotesAsync();

                    Notes.Clear();

                    foreach (var note in notes)
                    {
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
            await LoadNotesAsync(force: true);

            var note = Notes.FirstOrDefault(n => n.Id == id);
            if (note is null)
            {
                return;
            }

            var result = await _popupService.ShowAsync<NoteNotificationRequestDto>(
                new AddNotificationPopup(note.Content, note.NotifyOn));

            if (result.WasDismissed || result.Result is null)
            {
                return;
            }

            if (result.Result.CancelExisting)
            {
                await CancelNotificationAsync(note, showAlert: true);
                return;
            }

            var reminderDate = result.Result.NotifyOn;
            var replacedExistingNotification = note.NotifyOn is not null && note.NotifyOn.Value > DateTime.Now;

            try
            {
                if (!await _reminders.EnsurePermissionAsync()) return;
                await _reminders.ScheduleAsync(note.Id, reminderDate, result.Result.Message);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lembrete não definido", ex.Message, "Ok");
                await LoadNotesAsync(force: true);
                return;
            }
            await LoadNotesAsync(force: true);

            var message = replacedExistingNotification
                ? $"A notificação anterior foi cancelada e uma nova foi definida para {reminderDate:dd/MM/yyyy HH:mm}."
                : $"Lembrete definido para {reminderDate:dd/MM/yyyy HH:mm}.";
            await DisplayAlertAsync("Sucesso", message, "Ok");
        }

        private async Task ShowActiveNotificationsAsync()
        {
            await LoadNotesAsync(force: true);

            var result = await _popupService.ShowAsync<NoteNotificationManagementRequestDto>(
                new ActiveNoteNotificationsPopup(GetActiveNoteNotifications()));

            if (result.WasDismissed || result.Result is null)
            {
                return;
            }

            var note = Notes.FirstOrDefault(item => item.Id == result.Result.NoteId);
            if (note is null)
            {
                await DisplayAlertAsync("Aviso", "Nota não encontrada.", "Ok");
                return;
            }

            if (result.Result.Action == NoteNotificationManagementAction.Cancel)
            {
                await CancelNotificationAsync(note, showAlert: true);
                return;
            }

            await AddNotificationAsync(note.Id);
        }

        private async Task CancelNotificationAsync(Note note, bool showAlert)
        {
            try
            {
                await _reminders.CancelAsync(note.Id);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Cancelamento não confirmado", ex.Message, "Ok");
                return;
            }
            await LoadNotesAsync(force: true);

            if (showAlert)
            {
                await DisplayAlertAsync("Notificação cancelada", "A notificação desta nota foi cancelada.", "Ok");
            }
        }

        private IReadOnlyList<ActiveNoteNotificationDto> GetActiveNoteNotifications()
        {
            return Notes
                .Where(note => note.NotifyOn is DateTime notifyOn && notifyOn > DateTime.Now)
                .OrderBy(note => note.NotifyOn)
                .Select(note => new ActiveNoteNotificationDto
                {
                    NoteId = note.Id,
                    Content = note.Content ?? string.Empty,
                    NotifyOn = note.NotifyOn!.Value
                })
                .ToList();
        }
    }
}
