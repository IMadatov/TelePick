namespace TelePick.Desktop.Models;

public class SendResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static SendResult Ok() => new() { Success = true };
    public static SendResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
