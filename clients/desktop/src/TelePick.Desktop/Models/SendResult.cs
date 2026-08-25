using System.Collections.Generic;

namespace TelePick.Desktop.Models;

public class SendResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalCount { get; set; }
    public List<string> Errors { get; set; } = [];

    public static SendResult Ok() => new() { Success = true, SuccessCount = 1, TotalCount = 1 };
    public static SendResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
