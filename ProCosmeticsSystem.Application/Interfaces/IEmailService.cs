namespace ProCosmeticsSystem.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
    Task SendAsync(List<string> to, string subject, string body);
    Task SendWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string fileName);
}
