namespace ApiModels.Auth;

public record AuthResponse(string AccessToken, int ExpiresIn);
