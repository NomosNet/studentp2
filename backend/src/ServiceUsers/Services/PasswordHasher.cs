using Isopoh.Cryptography.Argon2;

namespace ServiceUsers.Services;

public sealed class PasswordHasher
{
    public string Hash(string password) => Argon2.Hash(password);

    public bool Verify(string password, string hash) => Argon2.Verify(hash, password);
}
