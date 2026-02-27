using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Email;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class EmailEndpoints
{
    public static void MapEmailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/emails").WithTags("Email").RequireAuthorization();

        group.MapPost("/send", async (SendEmailRequest request, IEmailService emailService, EmailTemplateService templateService) =>
        {
            if (request.To.Count == 0)
                return Results.BadRequest(ApiResponse<object>.Fail("At least one recipient is required."));

            if (string.IsNullOrWhiteSpace(request.Subject))
                return Results.BadRequest(ApiResponse<object>.Fail("Subject is required."));

            if (string.IsNullOrWhiteSpace(request.Body))
                return Results.BadRequest(ApiResponse<object>.Fail("Body is required."));

            // Validate email addresses
            foreach (var email in request.To)
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                    return Results.BadRequest(ApiResponse<object>.Fail($"Invalid email address: {email}"));
            }

            var html = templateService.CustomEmail(request.Subject, request.Body);
            await emailService.SendAsync(request.To, request.Subject, html);

            return Results.Ok(ApiResponse<string>.Ok("sent", "Email sent successfully."));
        }).RequirePermission("Email:Send");
    }
}
