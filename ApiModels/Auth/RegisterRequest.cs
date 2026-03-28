namespace ApiModels.Auth;

public record RegisterRequest(string Email, string DisplayName, string Password);
