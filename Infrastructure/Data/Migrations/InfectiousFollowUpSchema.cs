using SQLite;
namespace ACS_View.Infrastructure.Data.Migrations;
internal static class InfectiousFollowUpSchema
{
    internal static void Create(SQLiteConnection c)
    {
        c.Execute("INSERT OR IGNORE INTO HealthConditionDefinition(Code,Description) VALUES('hiv','HIV')");
        CreateTable(c, "'tuberculosis','hiv'", "ConditionCode<>'hiv' OR Status<>'Tratamento concluído'");
        CreateIdentityTrigger(c);
    }

    internal static void AddImmunodeficiency(SQLiteConnection c)
    {
        c.Execute("ALTER TABLE PatientInfectiousFollowUp RENAME TO PatientInfectiousFollowUp_v8");
        CreateTable(c, "'tuberculosis','hiv','immunodeficiency'", "ConditionCode='tuberculosis' OR Status<>'Tratamento concluído'");
        c.Execute("""
            INSERT INTO PatientInfectiousFollowUp
                (Id, UserId, PatientId, ConditionCode, Status, TreatmentStarted, LastAppointment, NextAppointment, ReferenceService, Notes)
            SELECT Id, UserId, PatientId, ConditionCode, Status, TreatmentStarted, LastAppointment, NextAppointment, ReferenceService, Notes
            FROM PatientInfectiousFollowUp_v8
            """);
        c.Execute("""
            UPDATE sqlite_sequence SET seq=MAX(seq, COALESCE((SELECT seq FROM sqlite_sequence WHERE name='PatientInfectiousFollowUp_v8'), 0))
            WHERE name='PatientInfectiousFollowUp'
            """);
        c.Execute("""
            INSERT INTO sqlite_sequence (name, seq)
            SELECT 'PatientInfectiousFollowUp', seq FROM sqlite_sequence WHERE name='PatientInfectiousFollowUp_v8'
            AND NOT EXISTS (SELECT 1 FROM sqlite_sequence WHERE name='PatientInfectiousFollowUp')
            """);
        c.Execute("DROP TABLE PatientInfectiousFollowUp_v8");
        CreateIdentityTrigger(c);
    }

    private static void CreateTable(SQLiteConnection c, string codes, string completedStatusCheck)
    {
        c.Execute($"""
            CREATE TABLE IF NOT EXISTS PatientInfectiousFollowUp (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL CHECK(UserId>0), PatientId INTEGER NOT NULL,
                ConditionCode TEXT NOT NULL CHECK(ConditionCode IN ({codes})),
                Status TEXT NOT NULL DEFAULT 'Não informado'
                  CHECK(Status IN ('Não informado','Em acompanhamento','Em tratamento','Tratamento concluído','Acompanhamento interrompido')),
                TreatmentStarted BIGINT, LastAppointment BIGINT, NextAppointment BIGINT,
                ReferenceService TEXT NOT NULL DEFAULT '', Notes TEXT NOT NULL DEFAULT '',
                CHECK({completedStatusCheck}),
                CHECK(TreatmentStarted IS NULL OR NextAppointment IS NULL OR NextAppointment>=TreatmentStarted),
                UNIQUE(UserId,PatientId,ConditionCode),
                FOREIGN KEY(PatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
    }

    private static void CreateIdentityTrigger(SQLiteConnection c) =>
        c.Execute("CREATE TRIGGER IF NOT EXISTS PatientInfectiousFollowUp_IdentityImmutable BEFORE UPDATE OF UserId,PatientId,ConditionCode ON PatientInfectiousFollowUp WHEN NEW.UserId<>OLD.UserId OR NEW.PatientId<>OLD.PatientId OR NEW.ConditionCode<>OLD.ConditionCode BEGIN SELECT RAISE(ABORT,'Follow-up identity cannot change'); END");
}
