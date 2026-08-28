using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace web_app_Csharp.Features.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapPost("/register", Register).AllowAnonymous();
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapPost("/logout", Logout).RequireAuthorization();

        return endpoints;
    }

    private static async Task<Results<Ok<RegistrationResponse>, ValidationProblem>> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email))
        {
            return ValidationProblem(nameof(request.Email), "Email is required.");
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            return ValidationProblem(nameof(request.Password), "Password is required.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ValidationProblem(createResult);
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Customer);
        if (!addRoleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return ValidationProblem(addRoleResult);
        }

        return TypedResults.Ok(new RegistrationResponse(user.Id, user.Email!, ApplicationRoles.Customer));
    }

    private static async Task<Results<Ok, ProblemHttpResult, ValidationProblem>> Login(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email))
        {
            return ValidationProblem(nameof(request.Email), "Email is required.");
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            return ValidationProblem(nameof(request.Password), "Password is required.");
        }

        var result = await signInManager.PasswordSignInAsync(email, request.Password, false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return TypedResults.Problem(
                title: "Login rejected",
                detail: "The email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok();
    }

    private static async Task<NoContent> Logout(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static ValidationProblem ValidationProblem(string key, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [key] = [message]
        });

    private static ValidationProblem ValidationProblem(IdentityResult result) =>
        TypedResults.ValidationProblem(
            result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray()));
}
