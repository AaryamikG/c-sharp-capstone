using UserService.Services;

namespace UserService.Tests;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_Succeeds()
    {
        var hash = _hasher.Hash("SecurePass123!");
        Assert.True(_hasher.Verify("SecurePass123!", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        var hash = _hasher.Hash("SecurePass123!");
        Assert.False(_hasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Hash_DoesNotReturnPlainText()
    {
        var hash = _hasher.Hash("SecurePass123!");
        Assert.NotEqual("SecurePass123!", hash);
    }
}
