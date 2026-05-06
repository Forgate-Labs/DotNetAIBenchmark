namespace BddPasswordPolicy001.Core;

public sealed class PasswordPolicy
{
    public bool IsValid(string password) => password.Length >= 8;
}
