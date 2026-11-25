using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

internal static class VaccineManual2026Tests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var today = DateTime.Today;
        VaccineDoseDefinition Definition(string key) => VaccineDoseCatalog.GetDefinition(key)!;
        check(Definition("VIP5").MinAgeMonths == 48 && Definition("Covid3").MinAgeMonths == 9,
            "2026 adds second VIP booster and third infant covid dose at the documented ages");
        check(Definition("VPC20-1").VaccineName == "Pneumo 20" &&
              Definition(VaccineDoseKeys.Pneumo10_1).VaccineName == "Pneumo 10" &&
              !Definition(VaccineDoseKeys.Pneumo10_1).IsRoutine,
            "2026 product transition never renames historical Pneumo 10 applications");
        check(!VaccineDoseCatalog.GetRequiredChildDefinitions().Any(d => d.RequiresAssessment || !d.IsRoutine || d.IsSeasonal),
            "Special, seasonal and retired indications do not become automatic overdue metrics");
        check(!VaccineDoseCatalog.GetExpectedDefinitions(60, false).Any(d => d.DoseKey == VaccineDoseKeys.FebreAmarela3),
            "2026 does not prescribe a universal third yellow fever dose at age five");
        check(!VaccineDoseCatalog.GetApplicableDefinitions(131, false).Any(d => d.DoseKey == VaccineDoseKeys.Acwy) &&
              VaccineDoseCatalog.GetApplicableDefinitions(132, false).Any(d => d.DoseKey == VaccineDoseKeys.Acwy) &&
              !VaccineDoseCatalog.GetApplicableDefinitions(180, false).Any(d => d.DoseKey == VaccineDoseKeys.Acwy),
            "Adolescent ACWY window is exactly eleven through fourteen years");
        var birth = new DateTime(2024, 1, 15);
        check(VaccineStatusCalculator.Calculate(birth, birth.AddMonths(12).AddDays(-1), Definition(VaccineDoseKeys.Vrh1), null) == VaccineStatus.Late &&
              VaccineStatusCalculator.Calculate(birth, birth.AddMonths(12), Definition(VaccineDoseKeys.Vrh1), null) == VaccineStatus.OutsideRoutine,
            "Rotavirus first dose expires at twelve months, not at the old recommended window");
        check(VaccineStatusCalculator.Calculate(birth, birth.AddMonths(24), Definition(VaccineDoseKeys.Vrh2), null) == VaccineStatus.OutsideRoutine &&
              VaccineStatusCalculator.Calculate(birth, birth.AddMonths(60), Definition("VIP5"), null) == VaccineStatus.OutsideRoutine,
            "Expired second rotavirus and VIP doses cannot enter the overdue batch");
        check(VaccineStatusCalculator.Calculate(birth, birth.AddMonths(30), Definition("VPC20-R"), null) == VaccineStatus.RequiresAssessment,
            "Pneumococcal catch-up requires age and history assessment");
        check(VaccineStatusCalculator.Calculate(birth, birth, Definition("VPC20-1"), null) == VaccineStatus.NotYetDue,
            "Future assessment indications still respect their starting age");
        check(VaccineDoseCatalog.GetDefinition("Influenza-Child@season-2026") is not null &&
              VaccineDoseCatalog.GetDefinition("Penta1@season-2026") is null &&
              VaccineDoseCatalog.GetDefinition("VSR-Gestante@pregnancy-bad") is null,
            "Cycle keys are recognized only for supported vaccines and valid cycle identifiers");

        var c = new SQLiteAsyncConnection(Path.Combine(directory, "vaccine-manual-2026.db"));
        var db = new DatabaseService(c);
        await db.InitializeAsync();
        var context = new CurrentUserContext();
        context.SetCurrentUser(1);
        var service = new VaccineService(db, context);
        var child = new Patient { UserId = 1, Name = "Synthetic child 2026", BirthDate = today.AddMonths(-30) };
        await c.InsertAsync(child);
        async Task Apply(int patientId, string key, DateTime? date, string lot = "original") =>
            await service.ApplyDoseAsync(new(patientId, key, date, lot, "Synthetic source"));
        await Apply(child.Id, VaccineDoseKeys.TetraViral, null);
        await Apply(child.Id, VaccineDoseKeys.Pneumo10_1, today.AddYears(-2));
        await c.InsertAsync(new PatientVaccineDose { UserId = 1, PatientId = child.Id, DoseKey = "unknown-historical-product", IsApplied = true, LotNumber = "imported" });
        var schedule = (await service.GetScheduleForPatientAsync(child.Id))!;
        check(!schedule.Doses.Any(d => d.Definition.DoseKey is "TripliceViral-Infantil2" or "Varicela-Infantil1"),
            "Historical tetraviral covers both components without fabricating new applications");
        check(schedule.ApplicationsByDose[VaccineDoseKeys.TetraViral] is { ApplicationDate: null, LotNumber: "original" } &&
              schedule.ApplicationsByDose[VaccineDoseKeys.Pneumo10_1].Definition.VaccineName == "Pneumo 10" &&
              schedule.ApplicationsByDose["unknown-historical-product"].LotNumber == "imported",
            "Retired and unknown imported products remain visible with original dates and batches");
        try
        {
            await service.ApplyOverdueDosesAsync(child.Id, 1, new Dictionary<string, DateTime> { ["VPC20-R"] = today });
            throw new Exception("Assessment dose entered batch");
        }
        catch (InvalidOperationException) { check(true, "Batch rejects pneumococcal assessment indications"); }
        var expired = new Patient { UserId = 1, Name = "Expired routine", BirthDate = today.AddYears(-5) };
        await c.InsertAsync(expired);
        try
        {
            await service.ApplyOverdueDosesAsync(expired.Id, 1, new Dictionary<string, DateTime> { ["VIP5"] = today });
            throw new Exception("Expired dose entered batch");
        }
        catch (InvalidOperationException) { check(true, "Batch revalidates routine age limits transactionally"); }
        var booster = new Patient { UserId = 1, Name = "Synthetic delayed booster", BirthDate = today.AddMonths(-58) };
        await c.InsertAsync(booster);
        await Apply(booster.Id, VaccineDoseKeys.Vip4, today.AddMonths(-2));
        schedule = (await service.GetScheduleForPatientAsync(booster.Id))!;
        check(schedule.Doses.Single(d => d.Definition.DoseKey == "VIP5").Status == VaccineStatus.RequiresAssessment,
            "Recently applied first VIP booster does not create an immediately actionable second booster");
        try
        {
            await service.ApplyOverdueDosesAsync(booster.Id, 1, new Dictionary<string, DateTime> { ["VIP5"] = today });
            throw new Exception("Six-month booster interval ignored");
        }
        catch (InvalidOperationException) { check(true, "Batch rejects VIP booster before six-month interval even if age suggests overdue"); }
        await Apply(booster.Id, VaccineDoseKeys.Vip4, null);
        try
        {
            await service.ApplyOverdueDosesAsync(booster.Id, 1, new Dictionary<string, DateTime> { ["VIP5"] = today });
            throw new Exception("Unknown previous dose date invented");
        }
        catch (InvalidOperationException) { check(true, "Unknown previous application dates never authorize automatic booster registration"); }

        foreach (var d in VaccineDoseCatalog.GetRequiredChildDefinitions().Where(d => d.DoseKey != "VIP5"))
            await Apply(expired.Id, d.DoseKey, null);
        var where = new List<string> { "p.UserId=?" };
        var parameters = new List<object> { 1 };
        ACS_View.Application.Querying.PatientFilterSqlBuilder.AddFilterClause(
            DashboardFilterKeys.ChildrenOverdueVaccines, where, parameters);
        var filtered = await c.QueryAsync<Patient>($"SELECT p.* FROM Patient p WHERE {string.Join(" AND ", where)}", parameters.ToArray());
        check(!filtered.Any(p => p.Id == expired.Id), "Dashboard SQL excludes expired VIP booster using the same routine age limit as the schedule");

        // Keep only the combination doses missing in the child fixture; old tetraviral must cover both in SQL too.
        foreach (var d in VaccineDoseCatalog.GetRequiredChildDefinitions().Where(d => d.DoseKey is not "TripliceViral-Infantil2" and not "Varicela-Infantil1"))
            if (await c.Table<PatientVaccineDose>().Where(row => row.UserId == 1 && row.PatientId == child.Id && row.DoseKey == d.DoseKey).CountAsync() == 0)
                await Apply(child.Id, d.DoseKey, null);
        filtered = await c.QueryAsync<Patient>($"SELECT p.* FROM Patient p WHERE {string.Join(" AND ", where)}", parameters.ToArray());
        check(!filtered.Any(p => p.Id == child.Id), "Dashboard SQL respects documented tetraviral without requiring duplicated component records");

        var seasonKey = $"Influenza-Child@season-{today.Year}";
        await Apply(child.Id, seasonKey, today);
        schedule = (await service.GetScheduleForPatientAsync(child.Id))!;
        check(schedule.Doses.Any(d => d.Definition.DoseKey == $"Influenza-Primaria2@season-{today.Year}"),
            "First influenza season exposes a separate second-dose record without overwriting dose one");
        await Apply(child.Id, $"Influenza-Child@season-{today.Year - 1}", today.AddYears(-1), "previous-season");
        schedule = (await service.GetScheduleForPatientAsync(child.Id))!;
        check(schedule.ApplicationsByDose[seasonKey].LotNumber == "original" &&
              schedule.ApplicationsByDose[$"Influenza-Child@season-{today.Year - 1}"].LotNumber == "previous-season" &&
              !schedule.Doses.Any(d => d.Definition.DoseKey == $"Influenza-Primaria2@season-{today.Year}"),
            "Seasonal records coexist and a documented prior season suppresses primary second-dose indication");

        // Reproduce a historical July 2025 application entered through the current season's card.
        var periodicPatient = new Patient { UserId = 1, Name = "Synthetic historical influenza", BirthDate = today.AddYears(-35) };
        await c.InsertAsync(periodicPatient);
        const string influenzaAdult = "Influenza-Adult";
        var currentAdultKey = $"{influenzaAdult}@season-{today.Year}";
        var oldDate = new DateTime(2025, 7, 15);
        await Apply(periodicPatient.Id, currentAdultKey, oldDate, "july-2025");
        schedule = (await service.GetScheduleForPatientAsync(periodicPatient.Id))!;
        check(schedule.ApplicationsByDose[$"{influenzaAdult}@season-2025"].ApplicationDate == oldDate &&
              schedule.Doses.Any(d => d.Definition.DoseKey == currentAdultKey && !d.IsApplied),
            "July 2025 influenza belongs to 2025 and leaves the current season available");
        await Apply(periodicPatient.Id, currentAdultKey, today, "current-season");
        var currentApplication = (await service.GetScheduleForPatientAsync(periodicPatient.Id))!.ApplicationsByDose[currentAdultKey];
        var newDoseEditor = new ACS_View.ViewModels.VaccineApplicationPopupViewModel(periodicPatient.Id, currentApplication, true);
        check(newDoseEditor.Title == "Registrar nova dose" && newDoseEditor.ApplicationDate == today &&
              newDoseEditor.LotNumber == string.Empty && newDoseEditor.Notes == string.Empty &&
              newDoseEditor.TryCreateRequest(out var newRequest) && newRequest!.IsNewPeriodicApplication,
            "New periodic dose starts a blank application without inheriting prior date, lot or notes");
        var oldId = currentApplication.ApplicationId;
        await service.ApplyDoseAsync(new(periodicPatient.Id, currentAdultKey, today, "additional", "new record", true));
        schedule = (await service.GetScheduleForPatientAsync(periodicPatient.Id))!;
        check(schedule.ApplicationsByDose.Count(d => d.Key.StartsWith(influenzaAdult)) == 3 &&
              schedule.ApplicationsByDose[currentAdultKey].ApplicationId == oldId &&
              schedule.ApplicationsByDose[currentAdultKey].LotNumber == "current-season",
            "Explicit new influenza application inserts independently and preserves both historical records");
        await service.ApplyDoseAsync(new(periodicPatient.Id, currentAdultKey, today.AddDays(-1), "edited", "edit prior"));
        schedule = (await service.GetScheduleForPatientAsync(periodicPatient.Id))!;
        check(schedule.ApplicationsByDose.Count(d => d.Key.StartsWith(influenzaAdult)) == 3 &&
              schedule.ApplicationsByDose[currentAdultKey].ApplicationId == oldId &&
              schedule.ApplicationsByDose[currentAdultKey].LotNumber == "edited",
            "Editing an existing periodic application still edits that record without creating a new one");

        var malformedPatient = new Patient { UserId = 1, Name = "Synthetic old seasonal bug", BirthDate = today.AddYears(-40) };
        await c.InsertAsync(malformedPatient);
        var malformed = new PatientVaccineDose { UserId = 1, PatientId = malformedPatient.Id, DoseKey = currentAdultKey,
            IsApplied = true, AppliedAt = oldDate, LotNumber = "preserved-old-bug" };
        await c.InsertAsync(malformed);
        schedule = (await service.GetScheduleForPatientAsync(malformedPatient.Id))!;
        var available = schedule.Doses.Single(d => !d.IsApplied && d.Definition.VaccineName == "Influenza");
        check(available.Definition.DoseKey != currentAdultKey,
            "Previously misclassified seasonal records do not block a new seasonal slot");
        await Apply(malformedPatient.Id, available.Definition.DoseKey, today, "recovered-current");
        schedule = (await service.GetScheduleForPatientAsync(malformedPatient.Id))!;
        check(schedule.ApplicationsByDose[currentAdultKey].ApplicationDate == oldDate &&
              schedule.ApplicationsByDose[currentAdultKey].LotNumber == "preserved-old-bug" &&
              schedule.ApplicationsByDose.Count(d => d.Key.StartsWith(influenzaAdult)) == 2,
            "Recovery from the old seasonal bug never rewrites the original date, key or lot");

        check(Definition(VaccineDoseKeys.DtAdulto).AllowsPeriodicApplications &&
              Definition(VaccineDoseKeys.DtpaAdulto).AllowsPeriodicApplications,
            "Decennial dT and occupational dTpa boosters support independent periodic applications");
        await Apply(periodicPatient.Id, VaccineDoseKeys.DtAdulto, oldDate, "original-dt");
        await service.ApplyDoseAsync(new(periodicPatient.Id, VaccineDoseKeys.DtAdulto, today, "new-dt", "", true));
        schedule = (await service.GetScheduleForPatientAsync(periodicPatient.Id))!;
        check(schedule.ApplicationsByDose.Count(d => d.Key.Split('@')[0] == VaccineDoseKeys.DtAdulto) == 2,
            "Registering another dT application preserves its prior record and does not complete the scheme automatically");

        const string adultCovid = "Covid-Periodica-Adult";
        await Apply(periodicPatient.Id, adultCovid, oldDate, "old-covid");
        schedule = (await service.GetScheduleForPatientAsync(periodicPatient.Id))!;
        check(schedule.Doses.Any(d => d.Definition.DoseKey.StartsWith(adultCovid + "@after-") && !d.IsApplied),
            "Adult periodic covid remains available after the first recorded dose even when interval depends on group");
        await service.ApplyDoseAsync(new(periodicPatient.Id, adultCovid, today, "new-covid", "", true));
        schedule = (await service.GetScheduleForPatientAsync(periodicPatient.Id))!;
        check(schedule.ApplicationsByDose.Count(d => d.Key.StartsWith(adultCovid)) == 2,
            "Other periodic vaccines also accept new independent applications");
        try
        {
            await service.ApplyDoseAsync(new(periodicPatient.Id, VaccineDoseKeys.Penta1, today, "", "", true));
            throw new Exception("Nonperiodic dose accepted periodic insertion");
        }
        catch (ArgumentException) { check(true, "Explicit periodic insertion is restricted to periodic definitions"); }

        var elderly = new Patient { UserId = 1, Name = "Synthetic elderly", BirthDate = today.AddYears(-70) };
        await c.InsertAsync(elderly);
        const string covid = "Covid-Periodica-Elderly";
        await Apply(elderly.Id, covid, today.AddMonths(-7));
        schedule = (await service.GetScheduleForPatientAsync(elderly.Id))!;
        var next = schedule.Doses.Single(d => d.Definition.DoseKey.StartsWith(covid + "@"));
        check(!next.IsApplied && next.Definition.Guidance!.Contains(today.AddMonths(-7).AddMonths(6).ToString("dd/MM/yyyy")),
            "Semestral covid retains prior application and exposes next interval for assessment");
        await Apply(elderly.Id, next.Definition.DoseKey, today);
        schedule = (await service.GetScheduleForPatientAsync(elderly.Id))!;
        check(schedule.ApplicationsByDose.Count(d => d.Key.StartsWith(covid)) == 2 &&
              schedule.Doses.Any(d => d.Definition.DoseKey.StartsWith(covid) && !d.IsApplied && d.Status == VaccineStatus.NotYetDue),
            "Consecutive periodic doses receive independent records and retain the next assessment");

        var mother = new Patient { UserId = 1, Name = "Synthetic pregnancy", BirthDate = today.AddYears(-30) };
        await c.InsertAsync(mother);
        var pregnancy = new PatientPregnancy { UserId = 1, PatientId = mother.Id, LastMenstrualPeriod = today.AddDays(-210) };
        await c.InsertAsync(pregnancy);
        schedule = (await service.GetScheduleForPatientAsync(mother.Id))!;
        var vsrKey = $"VSR-Gestante@pregnancy-{pregnancy.Id}";
        check(schedule.IsPregnant && schedule.Doses.Any(d => d.Definition.DoseKey == vsrKey &&
              d.Status == VaccineStatus.RequiresAssessment && d.Definition.Guidance!.Contains(pregnancy.LastMenstrualPeriod.Value.AddDays(196).ToString("dd/MM/yyyy"))),
            "Active pregnancy without a legacy condition exposes VSR and derives week 28 from documented LMP");
        check(!schedule.Doses.Any(d => d.Definition.AvoidDuringPregnancy && !d.IsApplied),
            "Pregnancy does not generate routine live-vaccine or HPV indications");
        await Apply(mother.Id, vsrKey, today);
        pregnancy.Status = PregnancyStatus.Ended;
        await c.UpdateAsync(pregnancy);
        var nextPregnancy = new PatientPregnancy { UserId = 1, PatientId = mother.Id };
        await c.InsertAsync(nextPregnancy);
        schedule = (await service.GetScheduleForPatientAsync(mother.Id))!;
        check(schedule.ApplicationsByDose.ContainsKey(vsrKey) && schedule.Doses.Any(d =>
              d.Definition.DoseKey == $"VSR-Gestante@pregnancy-{nextPregnancy.Id}" && !d.IsApplied &&
              d.Definition.Guidance!.Contains("não documentada")),
            "New pregnancy never inherits prior VSR completion or invents an undocumented gestational date");
        context.SetCurrentUser(2);
        check(await service.GetScheduleForPatientAsync(mother.Id) is null, "Updated vaccine schedules remain isolated by profile");
        await c.CloseAsync();
    }
}
