using Kartly.Application.Interfaces;

namespace Kartly.Infrastructure.Security;

// -----------------------------------------------------------------------
// BCrypt automatically generates and embeds a random salt in the hash
// output, and its "work factor" makes brute-forcing deliberately slow.
// This is why User.PasswordHash below always looks different even for
// two users with the identical password.
// -----------------------------------------------------------------------
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);

    public bool Verify(string plainTextPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainTextPassword, hash);
}
