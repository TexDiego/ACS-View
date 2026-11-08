using System.Text.Json;
using System.Text.Json.Nodes;
using ACS_View.Application.Backups;
using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using SQLite;

namespace ACS_View.Infrastructure.Data.Migrations;

internal static class UserBackupData
{
    // Order is deliberate; patient self references and batch references are deferred
    // by temporarily removing ONLY the trusted destination's reference triggers.
    internal static readonly string[] OwnedTables = ["User", "House", "Patient", "Family", "Vaccines", "FamilyRecord", "LegacyFamilyMap", "FamilyMember", "FamilyResidenceHistory", "PatientPhone", "PatientSusNumber", "PatientConditions", "PatientCID", "PatientBolsaFamilia", "PatientInsulinDependency", "PatientPregnancy", "PatientVaccineDose", "VisitBatch", "Visits", "VisitCareLine", "Note", "CareNotification", "TerritorialAssignment", "ImportHistory", "MigrationIssue", "FamilyRelationship", "PatientGlucoseMonitoring", "PatientStripPickup", "PatientInfectiousFollowUp"];
    internal static readonly string[] Catalogs = ["CidSubcategory", "HealthConditionDefinition"];
    private static readonly Dictionary<string, string> Keys = new() { ["House"] = "CasaId", ["Family"] = "IdFamilia" };
    private static string Key(string table) => Keys.GetValueOrDefault(table, "Id");
    private static string Q(string value) => SchemaMigrator.Quote(value);
    internal static long Number(Dictionary<string, JsonElement> row, string column) => row.TryGetValue(column, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetInt64() : 0;
    private static string Text(Dictionary<string, JsonElement> row, string column) => row.TryGetValue(column, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() ?? "" : "";
    private static void Set(Dictionary<string, JsonElement> row, string column, object? value) => row[column] = JsonSerializer.SerializeToElement(value);
    private sealed class JsonRow { public string Value { get; set; } = ""; }
    private sealed class NameRow { public string Name { get; set; } = ""; }
    private sealed class TriggerRow { public string Name { get; set; } = ""; public string Sql { get; set; } = ""; }

    internal static BackupDocument Extract(SQLiteConnection c, int userId, string version, string preferences)
    {
        if (userId <= 0) throw new InvalidOperationException("Entre na conta antes de exportar.");
        var known = OwnedTables.Concat(Catalogs).Concat(["FamilyRelationshipType", "SchemaMigration", "SchemaPreservation", "LocalAuthIdentity", "CidCategory", "CidChapter", "CidGroup", "CommonConditions", "CepAddressCache"]).ToHashSet();
        if (c.Query<NameRow>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").Any(t => !known.Contains(t.Name)))
            throw new InvalidDataException("Há tabelas sem classificação de backup. Atualize o inventário antes de exportar.");
        var document = new BackupDocument { FormatVersion = 2, SchemaVersion = c.ExecuteScalar<int>("PRAGMA user_version"), AppVersion = version, CreatedUtc = DateTimeOffset.UtcNow, ProfileIdentity = Guid.NewGuid().ToString("N"), DashboardPreferences = preferences };
        long totalRows = 0, totalBytes = 0;
        foreach (var table in OwnedTables.Concat(Catalogs))
        {
            var columns = c.GetTableInfo(table).Select(x => x.Name).ToArray();
            var expression = "json_object(" + string.Join(",", columns.Select(n => "'" + n.Replace("'", "''") + "'," + Q(n))) + ")";
            var filter = Catalogs.Contains(table) ? "" : table == "User" ? " WHERE Id=?" : " WHERE UserId=?";
            object[] args = Catalogs.Contains(table) ? [] : [userId];
            totalRows += c.ExecuteScalar<long>($"SELECT COUNT(*) FROM {Q(table)}{filter}", args);
            totalBytes += c.ExecuteScalar<long>($"SELECT COALESCE(SUM({string.Join('+', columns.Select(n => $"COALESCE(length(CAST({Q(n)} AS BLOB)),0)"))}),0) FROM {Q(table)}{filter}", args);
            if (totalRows > 200_000 || totalBytes > BackupEncryptionService.MaxBytes)
                throw new InvalidDataException("O perfil excede os limites de quantidade ou tamanho de backup desta versão.");
            document.Tables[table] = c.Query<JsonRow>($"SELECT {expression} AS Value FROM {Q(table)}{filter}", Catalogs.Contains(table) ? [] : [userId])
                .Select(r => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(r.Value)!).ToList();
        }
        foreach (var user in document.Tables["User"])
        {
            Set(user, "BiometricTokenHash", "");
            // DatabaseService upgrades plaintext credentials before the snapshot.
            if (Text(user, "Password") != "") throw new InvalidDataException("Atualize a segurança da conta antes de exportar.");
        }
        Validate(document);
        return document;
    }

    internal static BackupSummary Summary(BackupDocument d) => new(Text(d.Tables["User"].Single(), "Username"), d.CreatedUtc, d.Tables.Where(t => OwnedTables.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value.Count));

    internal static void Validate(BackupDocument d)
    {
        if (d.FormatVersion is not (1 or 2) || d.SchemaVersion is < 4 or > SchemaMigrator.CurrentVersion || d.ProfileIdentity.Length != 32 || !Guid.TryParseExact(d.ProfileIdentity, "N", out _))
            throw new InvalidDataException("Versão ou manifesto de backup incompatível.");
        if (d.FormatVersion == 1 && d.SchemaVersion < 6 && !d.Tables.ContainsKey("FamilyRelationship")) d.Tables["FamilyRelationship"] = [];
        if (d.SchemaVersion < 7)
            foreach (var table in new[]{"PatientGlucoseMonitoring", "PatientStripPickup"})
                if (!d.Tables.ContainsKey(table)) d.Tables[table] = [];
        if (d.SchemaVersion < 8 && !d.Tables.ContainsKey("PatientInfectiousFollowUp")) d.Tables["PatientInfectiousFollowUp"] = [];
        if (d.Tables.Count != OwnedTables.Length + Catalogs.Length || OwnedTables.Concat(Catalogs).Any(t => !d.Tables.ContainsKey(t)) || d.Tables.Sum(t => t.Value.Count) > 200_000)
            throw new InvalidDataException("Inventário ou quantidade de registros inválida.");
        if (d.Tables["User"].Count != 1) throw new InvalidDataException("O backup deve conter exatamente um perfil.");
        var user = d.Tables["User"][0]; var owner = Number(user, "Id");
        if (owner <= 0 || string.IsNullOrWhiteSpace(Text(user, "Username")) || Text(user, "Username").Length > 64 || Text(user, "Password") != "" || Text(user, "BiometricTokenHash") != ""
            || Text(user, "PasswordHash") == "" || Text(user, "PasswordSalt") == "") throw new InvalidDataException("Conta inválida no backup.");
        if (Number(user, "PasswordHashVersion") is not (1 or 2) || !HasEncodedLength(Text(user, "PasswordHash"), 32) || !HasEncodedLength(Text(user, "PasswordSalt"), 16)
            || (Text(user, "RecoveryCodeHash") != "" && (!HasEncodedLength(Text(user, "RecoveryCodeHash"), 32) || !HasEncodedLength(Text(user, "RecoveryCodeSalt"), 16))))
            throw new InvalidDataException("Parâmetros de autenticação não suportados no backup.");
        var ids = new Dictionary<string, HashSet<long>>();
        foreach (var (table, rows) in d.Tables)
        {
            var columns = table switch
            {
                "LegacyFamilyMap" => new HashSet<string>(["UserId", "LegacyHouseId", "LocalNumber", "SourcePatientId", "FamilyId"]),
                "HealthConditionDefinition" => new HashSet<string>(["Id", "Code", "Description"]),
                _ => typeof(User).Assembly.GetTypes().Single(t => t.Name == table && t.Namespace?.StartsWith("ACS_View.Domain.Entities", StringComparison.Ordinal) == true)
                    .GetProperties().Where(p => p.CanWrite && !Attribute.IsDefined(p, typeof(IgnoreAttribute))).Select(p => p.Name).ToHashSet(StringComparer.Ordinal)
            };
            if (table == "Patient") columns.Add("BolsaFamilia");
            if (rows.Any(r => r.Keys.Any(k => !columns.Contains(k)))) throw new InvalidDataException($"Coluna sem classificação de backup em {table}.");
            if (table != "User" && OwnedTables.Contains(table) && rows.Any(r => Number(r, "UserId") != owner)) throw new InvalidDataException("Backup contém dados de outro perfil.");
            if (table == "LegacyFamilyMap") continue;
            ids[table] = rows.Select(r => Number(r, Key(table))).ToHashSet();
            if (ids[table].Count != rows.Count || ids[table].Any(id => id <= 0 || id > int.MaxValue)) throw new InvalidDataException("Identificadores inválidos no backup.");
        }
        foreach (var (table, rows) in d.Tables)
            foreach (var row in rows)
            {
                foreach (var (column, target) in References(table))
                {
                    var id = Number(row, column);
                    if (id > 0 && !ids[target].Contains(id)) throw new InvalidDataException($"Referência externa ou ausente em {table}.{column}. Concilie os registros antes de exportar.");
                }
                if (table == "MigrationIssue")
                {
                    var source = Text(row, "SourceTable");
                    if (!ids.TryGetValue(source, out var sourceIds) || !sourceIds.Contains(Number(row, "SourceId"))) throw new InvalidDataException("Histórico de migração sem propriedade comprovada.");
                    ValidateEvidence(row, ids);
                }
                if (table == "CareNotification") TransformNotification(row, (target, id) => ids[target].Contains(id) ? id : throw new InvalidDataException("Notificação contém referência externa."));
                if (table == "ImportHistory") TransformReport(row, (target, id) => ids[target].Contains(id) ? id : throw new InvalidDataException("Relatório contém referência externa."));
            }
        var people = d.Tables["Patient"].ToDictionary(r => checked((int)Number(r,"Id")), r => new Patient { Id=checked((int)Number(r,"Id")), UserId=checked((int)owner), Name=Text(r,"Name") });
        var links = d.Tables["FamilyRelationship"].Select(r => new FamilyRelationship {
            Id=checked((int)Number(r,"Id")), UserId=checked((int)Number(r,"UserId")), FromPatientId=checked((int)Number(r,"FromPatientId")), ToPatientId=checked((int)Number(r,"ToPatientId")),
            TypeCode=Text(r,"TypeCode"), ForwardLabel=Text(r,"ForwardLabel"), ReverseLabel=Text(r,"ReverseLabel"), Nature=Text(r,"Nature"), IsActive=Number(r,"IsActive")==1,
            StartDate=Number(r,"StartDate")>0?new DateTime(Number(r,"StartDate")):null, EndDate=Number(r,"EndDate")>0?new DateTime(Number(r,"EndDate")):null, EndReason=Text(r,"EndReason") }).ToArray();
        ACS_View.Domain.ValueObjects.KinshipEngine.Validate(new(checked((int)owner),people,links));
        if (d.DashboardPreferences.Length > 256 * 1024) throw new InvalidDataException("Configurações excessivas.");
        if (d.DashboardPreferences != "") using (JsonDocument.Parse(d.DashboardPreferences, new JsonDocumentOptions { MaxDepth = 16 })) { }
    }

    // Patient.FamilyId and visits' FamilyId are LOCAL FAMILY NUMBERS, never FamilyRecord IDs.
    private static IEnumerable<(string Column, string Target)> References(string table)
    {
        if (table == "FamilyRelationship") return [("FromPatientId", "Patient"), ("ToPatientId", "Patient")];
        if (table == "Patient") return [("HouseId", "House"), ("MotherPatientId", "Patient"), ("FatherPatientId", "Patient"), ("FamilyResponsiblePatientId", "Patient")];
        if (table == "Family") return [("IdPessoa", "Patient")];
        if (table == "Vaccines") return [("Id", "Patient")];
        if (table == "FamilyRecord") return [("HouseId", "House"), ("ResponsiblePatientId", "Patient")];
        if (table == "LegacyFamilyMap") return [("LegacyHouseId", "House"), ("SourcePatientId", "Patient"), ("FamilyId", "FamilyRecord")];
        if (table == "FamilyMember") return [("FamilyId", "FamilyRecord"), ("PatientId", "Patient")];
        if (table == "FamilyResidenceHistory") return [("FamilyId", "FamilyRecord"), ("HouseId", "House")];
        if (table == "VisitBatch") return [("HouseId", "House")];
        if (table == "Visits") return [("PatientId", "Patient"), ("HouseId", "House"), ("BatchId", "VisitBatch")];
        if (table == "VisitCareLine") return [("VisitId", "Visits")];
        if (table == "TerritorialAssignment") return [("HouseId", "House")];
        if (table == "PatientCID") return [("PatientId", "Patient"), ("CidId", "CidSubcategory")];
        if (table == "PatientConditions") return [("PatientId", "Patient"), ("ConditionId", "HealthConditionDefinition")];
        if (table == "PatientBolsaFamilia") return [("PatientId", "Patient"), ("ResponsiblePatientId", "Patient")];
        if (table.StartsWith("Patient", StringComparison.Ordinal) || table == "CareNotification") return [("PatientId", "Patient")];
        return [];
    }

    private static void TransformNotification(Dictionary<string, JsonElement> row, Func<string, long, long> map)
    {
        if (Text(row, "MetadataJson") != "") throw new InvalidDataException("Metadados de notificação ainda não classificados para backup.");
        var key = Text(row, "UniqueKey"); if (key == "") return;
        var parts = key.Split(':');
        if (parts.Length != 4 || parts[0] is not ("pregnancy" or "puerperium") || !long.TryParse(parts[2], out var patient) || !long.TryParse(parts[3], out var pregnancy))
            throw new InvalidDataException("Chave de notificação não reconhecida.");
        parts[2] = map("Patient", patient).ToString(System.Globalization.CultureInfo.InvariantCulture);
        parts[3] = map("PatientPregnancy", pregnancy).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Set(row, "UniqueKey", string.Join(':', parts));
    }

    private static void TransformReport(Dictionary<string, JsonElement> row, Func<string, long, long> map)
    {
        var json = Text(row, "ReportJson"); if (json == "") return;
        var root = JsonNode.Parse(json, new JsonNodeOptions(), new JsonDocumentOptions { MaxDepth = 24 }) as JsonObject ?? throw new InvalidDataException("Relatório inválido.");
        string[] allowedRoot = ["Entries", "MergedCount", "Details", "ImportedCount", "UpdatedCount", "IgnoredCount", "Errors", "TotalProcessed"];
        if (root.Any(p => !allowedRoot.Contains(p.Key))) throw new InvalidDataException("Conteúdo de auditoria não classificado.");
        if (root["Entries"] is JsonArray entries)
            foreach (var entry in entries.OfType<JsonObject>())
            {
                string[] allowedEntry = ["EntityType", "RowNumber", "RecordId", "Status", "Reason", "Description", "Changes", "DisplayText"];
                if (entry.Any(p => !allowedEntry.Contains(p.Key))) throw new InvalidDataException("Campo de auditoria não classificado.");
                if (entry["RecordId"] is JsonValue value && value.TryGetValue<long>(out var id) && id > 0)
                {
                    var type = entry["EntityType"]?.GetValue<string>();
                    var table = type switch { "Paciente" => "Patient", "Residência" or "Residencia" => "House", _ => throw new InvalidDataException("Tipo de auditoria não reconhecido.") };
                    entry["RecordId"] = map(table, id);
                    if (entry.ContainsKey("DisplayText")) entry["DisplayText"] = JsonSerializer.Deserialize<ImportAuditEntryDto>(entry.ToJsonString())!.DisplayText;
                }
            }
        Set(row, "ReportJson", root.ToJsonString());
    }

    private static void ValidateEvidence(Dictionary<string, JsonElement> row, Dictionary<string, HashSet<long>> ids)
    {
        var code = Text(row, "Code"); var evidence = Text(row, "Evidence");
        if (code == "LegacyFamilyUnresolved")
        {
            var parts = evidence.Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var family) || !ids["Family"].Contains(family)
                || !long.TryParse(parts[1], out var patient) || !ids["Patient"].Contains(patient)) throw new InvalidDataException("Evidência familiar externa.");
        }
        else if (code == "PreexistingMembershipConflict")
        {
            foreach (var part in evidence.Split(':').Where(p => p != ""))
                if (!long.TryParse(part, out var id) || !ids["FamilyRecord"].Contains(id)) throw new InvalidDataException("Evidência de vínculo inválida.");
        }
        else if (code == "FamilyWithoutValidHouse" && evidence.Split(':')[0] is var house && long.TryParse(house, out var houseId) && houseId > 0)
            throw new InvalidDataException("Residência legada sem propriedade comprovada; concilie o registro.");
        else if (evidence != "" && code is not ("LegacyCnsEvidence" or "CnsNeedsReview" or "CnsShared" or "UnmappedCondition" or "FamilyWithoutValidHouse"))
            throw new InvalidDataException("Evidência de migração não classificada.");
    }

    internal static int Merge(SQLiteConnection destination, BackupDocument original, int? replaceUser, CancellationToken ct)
    {
        // Clone: inspection and retry always retain the authenticated original document.
        var d = JsonSerializer.Deserialize<BackupDocument>(JsonSerializer.Serialize(original))!;
        Validate(d);
        if (d.SchemaVersion == 4)
        {
            // Equivalent of migration 5, confined to imported houses. The destination's
            // migration ledger records its own history and is never replaced by source data.
            foreach (var house in d.Tables["House"])
            {
                var territory = d.Tables["TerritorialAssignment"].SingleOrDefault(t => Number(t, "HouseId") == Number(house, "CasaId") && (!t.TryGetValue("ValidTo", out var end) || end.ValueKind == JsonValueKind.Null));
                if (!house.ContainsKey("SetorCensitario")) Set(house, "SetorCensitario", territory is null ? null : Text(territory, "CensusSector"));
                if (!house.ContainsKey("NumeroQuadra")) Set(house, "NumeroQuadra", territory is null ? null : Text(territory, "BlockNumber"));
            }
        }
        var name = Summary(d).Username;
        var conflicts = destination.ExecuteScalar<int>("SELECT COUNT(*) FROM User WHERE trim(Username)=? COLLATE NOCASE AND Id<>?", name, replaceUser ?? -1);
        if (conflicts != 0) throw new InvalidOperationException("Já existe uma conta com esse nome. Entre no perfil correspondente e confirme a substituição em Backup e restauração.");
        var maps = new Dictionary<string, Dictionary<long, long>>();
        foreach (var table in OwnedTables.Where(t => t != "LegacyFamilyMap"))
        {
            long next = destination.ExecuteScalar<long>($"SELECT COALESCE(MAX({Q(Key(table))}),0) FROM {Q(table)}");
            next = Math.Max(next, destination.ExecuteScalar<long>("SELECT COALESCE((SELECT seq FROM sqlite_sequence WHERE name=?),0)", table));
            if (table == "Patient") next = Math.Max(next, destination.ExecuteScalar<long>("SELECT COALESCE(MAX(Id),0) FROM Vaccines"));
            maps[table] = d.Tables[table].ToDictionary(r => Number(r, Key(table)), _ => checked((long)(int)++next));
        }
        // Legacy vaccines use Patient.Id as their primary key.
        maps["Vaccines"] = d.Tables["Vaccines"].ToDictionary(r => Number(r, "Id"), r => maps["Patient"][Number(r, "Id")]);
        foreach (var table in Catalogs)
        {
            maps[table] = [];
            var identity = "Code";
            foreach (var row in d.Tables[table])
            {
                var old = Number(row, "Id");
                var found = destination.ExecuteScalar<long>($"SELECT Id FROM {Q(table)} WHERE {identity}=?", Text(row, identity));
                if (found == 0)
                {
                    found = destination.ExecuteScalar<long>($"SELECT COALESCE(MAX(Id),0)+1 FROM {Q(table)}");
                    Set(row, "Id", found); Insert(destination, table, row);
                }
                maps[table][old] = found;
            }
        }
        long Map(string table, long id) => id <= 0 ? id : maps[table].TryGetValue(id, out var value) ? value : throw new InvalidDataException("Referência não remapeável.");
        var triggers = destination.Query<TriggerRow>("SELECT name,sql FROM sqlite_master WHERE type='trigger'");
        foreach (var trigger in triggers) destination.Execute($"DROP TRIGGER {Q(trigger.Name)}");
        // Only inside a transaction on a private prepared database. Foreign keys remain ON
        // and deferred until commit; all trusted triggers are recreated before validation.
        destination.Execute("PRAGMA defer_foreign_keys=ON");
        if (replaceUser.HasValue)
        {
            foreach (var table in OwnedTables.Reverse())
                destination.Execute($"DELETE FROM {Q(table)} WHERE {(table == "User" ? "Id" : "UserId")}=?", replaceUser.Value);
        }
        var newUser = checked((int)maps["User"].Values.Single());
        foreach (var table in OwnedTables)
            foreach (var row in d.Tables[table])
            {
                ct.ThrowIfCancellationRequested();
                if (table == "CareNotification") TransformNotification(row, Map);
                if (table == "ImportHistory") TransformReport(row, Map);
                if (table == "MigrationIssue")
                {
                    var source = Text(row, "SourceTable"); Set(row, "SourceId", Map(source, Number(row, "SourceId")));
                    var code = Text(row, "Code"); var parts = Text(row, "Evidence").Split(':');
                    if (code == "LegacyFamilyUnresolved") Set(row, "Evidence", $"{Map("Family", long.Parse(parts[0]))}:{Map("Patient", long.Parse(parts[1]))}");
                    if (code == "PreexistingMembershipConflict") Set(row, "Evidence", string.Join(':', parts.Select(p => p == "" ? "" : Map("FamilyRecord", long.Parse(p)).ToString())));
                }
                foreach (var (column, target) in References(table))
                    if (row.TryGetValue(column, out var value) && value.ValueKind != JsonValueKind.Null) Set(row, column, Map(target, Number(row, column)));
                if (table != "LegacyFamilyMap" && table != "Vaccines") Set(row, Key(table), maps[table][Number(row, Key(table))]);
                if (table != "User") Set(row, "UserId", newUser);
                else { Set(row, "BiometricTokenHash", ""); Set(row, "CredentialRevision", checked(Number(row, "CredentialRevision") + 1)); }
                if(table=="FamilyRelationship" && Number(row,"FromPatientId")>Number(row,"ToPatientId") && (Text(row,"TypeCode")=="custom" || ACS_View.Domain.ValueObjects.KinshipCatalog.Get(Text(row,"TypeCode")).Symmetric))
                {
                    var from=Number(row,"FromPatientId");var forward=Text(row,"ForwardLabel");
                    Set(row,"FromPatientId",Number(row,"ToPatientId"));Set(row,"ToPatientId",from);
                    Set(row,"ForwardLabel",Text(row,"ReverseLabel"));Set(row,"ReverseLabel",forward);
                }
                Insert(destination, table, row);
            }
        if(d.SchemaVersion<6) KinshipSchema.BackfillLegacyParents(destination,newUser);
        foreach (var trigger in triggers) destination.Execute(trigger.Sql);
        ACS_View.Domain.ValueObjects.KinshipEngine.Validate(new(newUser,
            destination.Table<Patient>().Where(p=>p.UserId==newUser).ToList().ToDictionary(p=>p.Id),
            destination.Table<FamilyRelationship>().Where(r=>r.UserId==newUser).ToList()));
        SchemaMigrator.ValidateIntegrity(destination);
        RelationalStore.ValidateProjection(destination);
        return newUser;
    }

    private static void Insert(SQLiteConnection c, string table, Dictionary<string, JsonElement> row)
    {
        var allowed = c.GetTableInfo(table).Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        // The only extra legacy scalar found in the persistence inventory. It has no
        // identity semantics and must survive clean installs as well as in-place restores.
        if (table == "Patient" && row.ContainsKey("BolsaFamilia") && !allowed.Contains("BolsaFamilia"))
        {
            c.Execute("ALTER TABLE Patient ADD COLUMN BolsaFamilia INTEGER NOT NULL DEFAULT 0");
            allowed.Add("BolsaFamilia");
        }
        if (row.Keys.Any(k => !allowed.Contains(k))) throw new InvalidDataException($"Coluna não suportada em {table}. Use uma versão compatível do aplicativo.");
        var columns = row.Keys.ToArray();
        object? Value(JsonElement v) => v.ValueKind switch { JsonValueKind.Null => null, JsonValueKind.String => v.GetString(), JsonValueKind.Number => v.GetInt64(), _ => throw new InvalidDataException("Valor SQLite inválido.") };
        c.Execute($"INSERT INTO {Q(table)} ({string.Join(',', columns.Select(Q))}) VALUES ({string.Join(',', columns.Select(_ => "?"))})", columns.Select(k => Value(row[k])).ToArray());
    }

    private static bool HasEncodedLength(string value, int expected)
    {
        Span<byte> buffer = stackalloc byte[64];
        return Convert.TryFromBase64String(value, buffer, out var length) && length == expected;
    }
}
