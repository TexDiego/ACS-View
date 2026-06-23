using ACS_View.Application.Security;

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

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
