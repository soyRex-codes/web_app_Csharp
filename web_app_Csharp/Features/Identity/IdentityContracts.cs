namespace web_app_Csharp.Features.Identity;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RegistrationResponse(string Id, string Email, string Role);
