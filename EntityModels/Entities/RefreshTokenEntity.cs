namespace EntityModels.Entities;

public class RefreshTokenEntity : IEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }

    public UserEntity User { get; set; } = null!;
    public Guid Id { get; set; }
}