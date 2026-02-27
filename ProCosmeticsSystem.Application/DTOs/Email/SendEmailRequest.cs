namespace ProCosmeticsSystem.Application.DTOs.Email;

public class SendEmailRequest
{
    public List<string> To { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
