namespace ACS_View.MVVM.Models.Services
{
    public class AppServiceLocator
    {
        private static AppServiceLocator _instance;
        public static AppServiceLocator Instance => _instance ??= new AppServiceLocator();

        public DatabaseService DatabaseService { get; }
        public HealthRecordService HealthRecordService { get; }
        public NoteService NoteService { get; }

        private AppServiceLocator()
        {
            // Inicializa os serviços
            DatabaseService = new DatabaseService();

            // Inicializa os serviços de CRUD com a instância do DatabaseService
            HealthRecordService = new HealthRecordService(DatabaseService);
            NoteService = new NoteService(DatabaseService);
        }
    }
}
