using Microsoft.AspNetCore.Http;

namespace ApiModels.Users;

public record UploadAvatarRequest(IFormFile File);
