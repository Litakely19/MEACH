namespace SchoolManagement.Application.Validators;

/// <summary>
/// Minimum strength required for every stored password, applied when creating a
/// user, resetting a password and changing one's own password.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    public static bool IsValid(string? password, out string? error)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            error = "The password is required.";
            return false;
        }

        if (password.Length < MinimumLength)
        {
            error = $"The password must contain at least {MinimumLength} characters.";
            return false;
        }

        if (!password.Any(char.IsLetter))
        {
            error = "The password must contain at least one letter.";
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            error = "The password must contain at least one digit.";
            return false;
        }

        error = null;
        return true;
    }

    public static string Describe() =>
        $"At least {MinimumLength} characters, including one letter and one digit.";
}
