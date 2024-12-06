using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModel
{
    public class NotesPageViewModel : BaseViewModel
    {
        private readonly NoteService _noteService;
        private readonly ObservableCollection<Note> _notes = new();
        public ObservableCollection<Note> Notes => _notes;
        public ObservableCollection<Note> _note { get; private set; }
        public event Action ScrollToTopRequested;

        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreationDate { get; set; }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand DeleteCommand { get; }
        public ICommand SalvarNota { get; }


        public NotesPageViewModel() { }

        public NotesPageViewModel(NoteService NoteService) : base()
        {
            _noteService = NoteService ?? throw new ArgumentNullException(nameof(NoteService));
            LoadNotesAsync().ConfigureAwait(false); // Evitar deadlocks

            _note = new ObservableCollection<Note>();

            Console.WriteLine("Antes de deleteCommand");

            SalvarNota = new Command(async () => await SalvarNotaAsync(), () => true);
            DeleteCommand = new Command<int>(async id => await DeleteNoteAsync(id));

        }

        private async Task DeleteNoteAsync(int id)
        {
            try
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Confirmação", "Deseja excluir esta nota?", "Sim", "Não");
                if (!confirm) return;

                int rowsAffected = await _noteService.DeleteNoteAsync(id);
                if (rowsAffected > 0)
                {
                    var noteToRemove = _notes.FirstOrDefault(n => n.Id == id);
                    if (noteToRemove != null)
                    {
                        _notes.Remove(noteToRemove);
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível excluir a nota.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Erro", "Ocorreu um erro ao tentar excluir a nota.", "OK");
            }
        }

        private async Task SalvarNotaAsync()
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "O conteúdo não pode estar vazio.", "OK");
                return;
            }

            var newNote = new Note { Content = Content, CreationDate = DateTime.Now };
            await _noteService.SaveNoteAsync(newNote);
            await LoadNotesAsync();

            ScrollToTopRequested?.Invoke(); // Dispara o evento
        }

        public async Task LoadNotesAsync()
        {
            try
            {
                IsLoading = true;
                var notes = await _noteService.GetAllNotesAsync();
                _notes.Clear();
                foreach (var note in notes)
                {
                    _notes.Add(note);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar registros: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível carregar as notas.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }



        private void UpdateDatas()
        {
            _notes.Clear();
            foreach (var note in Notes)
            {
                _notes.Add(new Note
                {
                    Content = note.Content,
                    CreationDate = note.CreationDate
                });
            }
            OnPropertyChanged(nameof(Notes));
        }


        private List<string> GetContent(List<Note> note)
        {
            return note.Select(note => note.Content).ToList();
        }

        private List<DateTime> GetDateTimes(List<Note> note)
        {
            return note.Select(note => note.CreationDate).ToList();
        }
    }
}
