using ACS_View.Application.Security;
using ACS_View.Application.Querying;
using ACS_View.Domain.ValueObjects;

var secret = "SenhaForte123";
var otherSecret = "SenhaErrada123";

var firstHash = PasswordHasher.Hash(secret);
var secondHash = PasswordHasher.Hash(secret);

Assert(PasswordHasher.Verify(secret, firstHash.Hash, firstHash.Salt), "Senha correta deve validar.");
Assert(!PasswordHasher.Verify(otherSecret, firstHash.Hash, firstHash.Salt), "Senha incorreta não deve validar.");
Assert(firstHash.Hash != secondHash.Hash, "Hashes da mesma senha devem variar por salt.");
Assert(firstHash.Salt != secondHash.Salt, "Salts devem variar por hash.");
Assert(!PasswordHasher.Verify(secret, string.Empty, firstHash.Salt), "Hash vazio deve falhar.");
Assert(!PasswordHasher.Verify(secret, firstHash.Hash, string.Empty), "Salt vazio deve falhar.");

Console.WriteLine("PasswordHasher tests passed.");

var activeWhereParts = new List<string> { "p.UserId = ?" };
var activeParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause(DashboardFilterKeys.All, activeWhereParts, activeParameters);
Assert(activeWhereParts.Contains("COALESCE(p.IsActive, 1) = 1"), "Filtro padrao deve listar pacientes ativos e legados sem status.");
Assert(!activeWhereParts.Contains("p.IsActive = 0"), "Filtro padrao nao deve incluir inativos.");

var inactiveWhereParts = new List<string> { "p.UserId = ?" };
var inactiveParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause(DashboardFilterKeys.Inactive, inactiveWhereParts, inactiveParameters);
Assert(inactiveWhereParts.Contains("p.IsActive = 0"), "Filtro de inativos deve listar apenas pacientes inativos.");
Assert(!inactiveWhereParts.Contains("p.IsActive = 1"), "Filtro de inativos nao deve incluir ativos.");

Console.WriteLine("PatientFilterSqlBuilder tests passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
