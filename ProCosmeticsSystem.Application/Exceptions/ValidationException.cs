namespace ProCosmeticsSystem.Application.Exceptions;

public class ValidationException : AppException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : base("One or more validation errors occurred.", 400)
    {
        Errors = new Dictionary<string, string[]> { { field, [message] } };
    }
}
