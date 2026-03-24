namespace SchoolManagement.Common.Constants;

public static class AppMessages
{
    public static class Auth
    {
        public const string InvalidCredentials = "Invalid username or password.";
        public const string UsernameTaken = "Username or email already in use.";
        public const string InvalidToken = "Invalid token.";
        public const string InvalidTokenAlgorithm = "Invalid token algorithm.";
        public const string RefreshTokenInvalid = "Invalid or expired refresh token.";
        public const string RefreshTokenExpired = "Refresh token has expired.";
        public const string PasswordResetEmailSent = "If the email is registered, a password reset link has been sent.";
        public const string ResetTokenInvalid = "Invalid or expired password reset token.";
        public const string PasswordResetSuccess = "Password has been reset successfully.";
    }

    public static class General
    {
        public const string ConnectionStringMissing = "Connection string 'DefaultConnection' is not configured.";
    }

    public static class Encryption
    {
        public const string EncryptedKeyHeader  = "X-Encrypted-Key";
        public const string RequestNonceHeader  = "X-Encryption-Nonce";
        public const string ResponseNonceHeader = "X-Response-Nonce";
    }

    public static class Country
    {
        public static string NotFound(int id)
            => $"Country with id {id} was not found.";

        public static string AlreadyExists(string name, string code)
            => $"A country with the name '{name}' or code '{code}' already exists.";
    }

    public static class State
    {
        public static string NotFound(int id)
            => $"State with id {id} was not found.";

        public static string AlreadyExists(string name)
            => $"A state named '{name}' already exists in this country.";
    }

    public static class City
    {
        public static string NotFound(int id)
            => $"City with id {id} was not found.";

        public static string AlreadyExists(string name)
            => $"A city named '{name}' already exists in this state.";
    }

    public static class Organization
    {
        public static string NotFound(int id)
            => $"Organization with id {id} was not found.";

        public static string AlreadyExists(string name)
            => $"An organization named '{name}' already exists.";
    }

    public static class MenuMaster
    {
        public static string NotFound(int id)
            => $"Menu with id {id} was not found.";
    }

    public static class PageMaster
    {
        public static string NotFound(int id)
            => $"Page with id {id} was not found.";

        public static string ModuleNotFound(int id)
            => $"Page module with id {id} was not found.";

        public static string ActionNotFound(int id)
            => $"Page module action with id {id} was not found.";

        public static string SinglePageViolation(int menuId)
            => $"Menu {menuId} does not support child menus; only one page entry is permitted.";

        public const string ActionAlreadyExists
            = "This action mapping already exists for the selected page and module.";
    }

    public static class MenuAndPagePermission
    {
        public static string NotFound(int id)
            => $"Permission with id {id} was not found.";
    }

}
