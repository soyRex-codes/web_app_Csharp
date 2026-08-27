using Microsoft.AspNetCore.Identity;

namespace web_app_Csharp.Features.Identity;

/// <summary>Application-specific user type. Identity supplies the credential and security-stamp fields.</summary>
public sealed class ApplicationUser : IdentityUser
{
}
