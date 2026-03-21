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
}
