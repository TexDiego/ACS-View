namespace ACS_View.Domain.ValueObjects
{
    public static class DataChangeTracker
    {
        private static int patientsVersion;
        private static int housesVersion;
        private static int visitsVersion;
        private static int notesVersion;
        private static int sessionVersion;

        public static int PatientsVersion => patientsVersion;
        public static int HousesVersion => housesVersion;
        public static int VisitsVersion => visitsVersion;
        public static int NotesVersion => notesVersion;
        public static int SessionVersion => sessionVersion;
        public static int MetricsVersion => patientsVersion + housesVersion + visitsVersion;

        public static void MarkPatientsChanged()
        {
            Interlocked.Increment(ref patientsVersion);
        }

        public static void MarkHousesChanged()
        {
            Interlocked.Increment(ref housesVersion);
        }

        public static void MarkVisitsChanged()
        {
            Interlocked.Increment(ref visitsVersion);
        }

        public static void MarkNotesChanged()
        {
            Interlocked.Increment(ref notesVersion);
        }

        public static void MarkAllChanged()
        {
            MarkPatientsChanged();
            MarkHousesChanged();
            MarkVisitsChanged();
            MarkNotesChanged();
        }

        public static void MarkSessionChanged()
        {
            Interlocked.Increment(ref sessionVersion);
        }
    }
}
