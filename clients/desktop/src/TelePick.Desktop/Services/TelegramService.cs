using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TelePick.Desktop.Models;

namespace TelePick.Desktop.Services;

public class TelegramService : ITelegramService
{
    private readonly HttpClient _httpClient;

    public TelegramService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SendResult> SendMessageAsync(string text, string note, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.BotToken) || string.IsNullOrWhiteSpace(settings.ChatId))
        {
            return SendResult.Fail("Bot token or Chat ID is not configured.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return SendResult.Fail("Clipboard text is empty.");
        }

        var message = text;
        if (!string.IsNullOrWhiteSpace(note))
        {
            message += $"\n\n---\nNote: {note}";
        }

        var url = $"https://api.telegram.org/bot{settings.BotToken}/sendMessage";
        var payload = new
        {
            chat_id = settings.ChatId,
            text = message
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload);
            if (response.IsSuccessStatusCode)
            {
                return SendResult.Ok();
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            return SendResult.Fail($"Telegram API Error: {response.StatusCode} - {errorResponse}");
        }
        catch (Exception ex)
        {
            return SendResult.Fail($"Network Error: {ex.Message}");
        }
    }
}
