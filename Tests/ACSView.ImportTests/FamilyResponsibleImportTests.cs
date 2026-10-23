using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.UseCases.Services;
using System.Text.Json;

internal static class FamilyResponsibleImportTests
{
    public static async Task RunAsync()
    {
        static void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            Console.WriteLine("PASS: " + message);
        }

        static Patient Copy(Patient source)
        {
            var copy = JsonSerializer.Deserialize<Patient>(JsonSerializer.Serialize(source))!;
            copy.SusNumberRecords = source.SusNumberRecords;
            return copy;
        }

        async Task<(List<Patient> Patients, PatientImportResultDto Report)> Resolve(params Patient[] patients)
        {
            var stored = patients.Select(Copy).ToList();
            Task Save(Patient patient)
            {
                stored.RemoveAll(p => p.Id == patient.Id);
                stored.Add(Copy(patient));
                return Task.CompletedTask;
            }
            var people = Proxy.For<IPatientService>((method, args) => method switch
            {
                "GetAllPatients" => Task.FromResult<List<Patient>?>(stored.Select(Copy).ToList()),
                "UpdatePatient" => Save((Patient)args[0]!),
                _ => throw new Exception(method)
            });
            var houses = Proxy.For<IHouseService>((method, _) => method == "GetAllHousesAsync"
                ? Task.FromResult(new List<House>
                {
                    new() { CasaId = 10, TipoLogradouro = "Rua", Rua = "Brasil", NumeroCasa = "10", Complemento = "Casa A", Bairro = "Centro", Cidade = "São Paulo", Estado = "SP", CEP = "01001000" },
                    new() { CasaId = 20, TipoLogradouro = "Avenida", Rua = "Paulista", NumeroCasa = "20", Complemento = "Apto 2", Bairro = "Bela Vista", Cidade = "São Paulo", Estado = "SP", CEP = "01310100" }
                }) : throw new Exception(method));
            var families = Proxy.For<IFamilyService>((method, args) => method == "GetMaxIdAsync"
                ? Task.FromResult(stored.Where(p => p.HouseId == (int)args[0]!).Select(p => p.FamilyId).Append(0).Max()) : throw new Exception(method));
            var report = new PatientImportResultDto();
            var dependent = Copy(patients[0]);
            await new PatientFamilyLinkResolver(people, houses, families).ResolveAsync(
                [new() { RowNumber = 2, Patient = dependent, FamilyResponsibleSus = dependent.FamilyResponsibleSus ?? "" }],
                new(), report, null, 0, 100, CancellationToken.None);
            return (stored, report);
        }

        Patient Dependent(string reported = "222", int house = 10) => new()
        {
            Id = 1, Name = "Dependente", SusNumber = "111", HouseId = house, FamilyResponsibleSus = reported
        };
        Patient Responsible(int house = 10) => new()
        {
            Id = 2, Name = "Responsável", SusNumber = "222", HouseId = house
        };

        var existing = await Resolve(Dependent(), Responsible());
        Check(existing.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId == 2 && existing.Report.Errors.Count == 0,
            "CNS único do responsável já cadastrado associa sem exigir nova importação do responsável");

        var relational = Responsible();
        relational.SusNumberRecords = [new() { Number = "222", IsPrimary = true }, new() { Number = "333", OriginalOrder = 1 }];
        var secondary = await Resolve(Dependent("3.33"), relational);
        Check(secondary.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId == 2 && secondary.Report.Errors.Count == 0,
            "CNS secundário relacional e formatado localiza o responsável existente");
        var multiple = await Resolve(Dependent("222; 3.33"), relational);
        Check(multiple.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId == 2 && multiple.Report.Errors.Count == 0,
            "Vários CNS do mesmo responsável são pesquisados separadamente, sem concatenar os números");

        var withoutHouse = await Resolve(Dependent(), Responsible(-1));
        Check(withoutHouse.Patients.Single(p => p.Id == 2).HouseId == 10 &&
            withoutHouse.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId == 2 && withoutHouse.Report.Errors.Count == 0,
            "Responsável sem residência recebe a única residência válida dos pacientes que o indicam");

        var neitherHouse = await Resolve(Dependent(house: -1), Responsible(-1));
        Check(neitherHouse.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId is null &&
            neitherHouse.Report.Errors.Any(e => e.Contains("cadastro #2") && e.Contains("sem residência")),
            "CNS encontrado sem residência segura gera motivo específico, sem alegar cadastro inexistente");

        var differentHouse = await Resolve(Dependent(), Responsible(20));
        Check(differentHouse.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId is null &&
            differentHouse.Patients.Single(p => p.Id == 2).HouseId == 20 &&
            differentHouse.Report.Errors.Any(e => e.Contains("Residência do paciente: Rua Brasil, 10, Casa A, Centro, São Paulo/SP, CEP 01001-000") &&
                e.Contains("Residência do responsável: Avenida Paulista, 20, Apto 2, Bela Vista, São Paulo/SP, CEP 01310-100") && !e.Contains("Linha 2")),
            "Divergência mostra os endereços completos do paciente e do responsável, sem número da linha");

        var duplicate = Responsible(20);
        duplicate.Id = 3;
        var ambiguous = await Resolve(Dependent(), Responsible(), duplicate);
        Check(ambiguous.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId is null &&
            ambiguous.Report.Errors.Any(e => e.Contains("CNS ambíguo") && e.Contains("#2") && e.Contains("#3")),
            "CNS compartilhado por pessoas em casas diferentes não é desambiguado apenas pela residência");

        var conflictingDependent = Dependent(house: 20);
        conflictingDependent.Id = 4;
        var conflictingHouses = await Resolve(Dependent(), Responsible(-1), conflictingDependent);
        Check(conflictingHouses.Patients.Single(p => p.Id == 2).HouseId == -1 &&
            conflictingHouses.Patients.All(p => p.Id == 2 || p.FamilyResponsiblePatientId is null) &&
            conflictingHouses.Report.Errors.Any(e => e.Contains("Pacientes que o indicam possuem residências diferentes") &&
                e.Contains("Rua Brasil, 10") && e.Contains("Avenida Paulista, 20") && !e.Contains("Linha 2")),
            "Indicações conflitantes não escolhem arbitrariamente a residência do responsável");

        var protectedDependent = Dependent();
        protectedDependent.FamilyResponsiblePatientId = 3;
        var oldResponsible = new Patient { Id = 3, Name = "Responsável anterior", SusNumber = "444", HouseId = 10 };
        var protectedLink = await Resolve(protectedDependent, Responsible(), oldResponsible);
        Check(protectedLink.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId == 3 &&
            protectedLink.Report.Errors.Any(e => e.Contains("Vínculo existente preservado")),
            "Responsável anterior não é substituído silenciosamente pelo CNS importado");

        var inconsistentReference = Dependent();
        inconsistentReference.Id = 4;
        inconsistentReference.FamilyResponsiblePatientId = 3;
        var unsafeHouse = await Resolve(Dependent(house: -1), Responsible(-1), inconsistentReference, oldResponsible);
        Check(unsafeHouse.Patients.Single(p => p.Id == 2).HouseId == -1 &&
            unsafeHouse.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId is null,
            "Referência que contradiz um vínculo existente não é usada para inferir endereço do responsável");

        var unknown = await Resolve(Dependent("999"), Responsible());
        Check(unknown.Report.Errors.Any(e => e.Contains("nenhum cadastro da conta atual contém o CNS")),
            "CNS realmente ausente tem diagnóstico distinto de ambiguidade e residência divergente");

        var mismatchedFamily = Dependent();
        mismatchedFamily.FamilyId = 7;
        var otherFamilyHead = Responsible();
        otherFamilyHead.FamilyId = 8;
        var familyConflict = await Resolve(mismatchedFamily, otherFamilyHead);
        Check(familyConflict.Patients.Single(p => p.Id == 1).FamilyId == 7 &&
            familyConflict.Patients.Single(p => p.Id == 1).FamilyResponsiblePatientId is null &&
            familyConflict.Report.Errors.Any(e => e.Contains("Família existente preservada")),
            "Conflito de famílias na mesma residência não cria vínculo familiar inconsistente");
    }
}
