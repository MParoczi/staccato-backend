namespace DomainModels.Constants;

public static class AuthErrorCodes
{
    public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
    public const string InvalidCredentials     = "INVALID_CREDENTIALS";
    public const string NoPasswordSet          = "NO_PASSWORD_SET";
    public const string InvalidToken           = "INVALID_TOKEN";
    public const string TokenExpired           = "TOKEN_EXPIRED";
    public const string GoogleAuthFailed       = "GOOGLE_AUTH_FAILED";
    public const string ServiceUnavailable     = "SERVICE_UNAVAILABLE";
}
