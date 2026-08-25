using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TelePick.Desktop.Models;

namespace TelePick.Desktop.Services;

public class TelegramService : ITelegramService
{
    private const string TelegramApiBase = "https://api.telegram.org/bot";
    private const int MaxMessageLength = 4096;
    private const int MaxCaptionLength = 1024;
    private const string TruncationSuffix = "… [truncated]";

    private readonly HttpClient _httpClient;

    public TelegramService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ── Public API ──────────────────────────────────────────────

    public async Task<SendResult> SendMessageAsync(
        ClipboardItem? item,
        string note,
        Settings settings,
        List<Destination>? selectedDestinations = null)
    {
        if (string.IsNullOrWhiteSpace(settings.BotToken))
            return SendResult.Fail("Bot token is not configured.");

        if (item == null || string.IsNullOrWhiteSpace(item.PreviewText))
            return SendResult.Fail("Item or text is empty.");

        MigrateSettings(settings);

        var destinations = ResolveDestinations(selectedDestinations, settings.Recipients);
        if (destinations.Count == 0)
            return SendResult.Fail("No destinations configured. Add at least one recipient in Settings.");

        var message = BuildMessage(item, note);
        message = Truncate(message, MaxMessageLength);

        var results = await Task.WhenAll(
            destinations.Select(dest => SendMessageSingleAsync(settings.BotToken, dest, message)));

        return BuildAggregateResult(results);
    }

    public async Task<SendResult> SendPhotoAsync(
        Stream photoStream,
        string fileName,
        string? caption,
        Settings settings,
        List<Destination>? selectedDestinations = null)
    {
        if (string.IsNullOrWhiteSpace(settings.BotToken))
            return SendResult.Fail("Bot token is not configured.");

        if (photoStream == null || photoStream.Length == 0)
            return SendResult.Fail("Photo is empty.");

        MigrateSettings(settings);

        var destinations = ResolveDestinations(selectedDestinations, settings.Recipients);
        if (destinations.Count == 0)
            return SendResult.Fail("No destinations configured. Add at least one recipient in Settings.");

        var formattedCaption = string.IsNullOrWhiteSpace(caption) ? "" : BuildPhotoCaption(caption);
        formattedCaption = Truncate(formattedCaption, MaxCaptionLength);

        // Read stream into byte array once — reused across all destinations
        using var memoryStream = new MemoryStream();
        await photoStream.CopyToAsync(memoryStream);
        var photoBytes = memoryStream.ToArray();

        var results = await Task.WhenAll(
            destinations.Select(dest => SendPhotoSingleAsync(settings.BotToken, dest, formattedCaption, photoBytes, fileName)));

        return BuildAggregateResult(results);
    }

    public async Task<SendResult> TestConnectionAsync(
        Settings settings,
        List<Destination>? selectedDestinations = null)
    {
        if (string.IsNullOrWhiteSpace(settings.BotToken))
            return SendResult.Fail("Bot token is not configured.");

        MigrateSettings(settings);

        var destinations = ResolveDestinations(selectedDestinations, settings.Recipients);
        if (destinations.Count == 0)
            return SendResult.Fail("No destinations configured. Add at least one recipient in Settings.");

        var results = await Task.WhenAll(
            destinations.Select(dest =>
            {
                var label = ResolveDestinationLabel(dest, settings.Recipients);
                using var testItem = new ClipboardItem { Type = ClipboardItemType.Text, PreviewText = $"TelePick test message — target: {label}" };
                var testMessage = BuildMessage(testItem, "");
                return SendMessageSingleAsync(settings.BotToken, dest, testMessage);
            }));

        return BuildAggregateResult(results);
    }

    // ── Single-destination senders ──────────────────────────────

    private async Task<SingleResult> SendMessageSingleAsync(string botToken, Destination dest, string message)
    {
        var url = $"{TelegramApiBase}{botToken}/sendMessage";
        var body = new Dictionary<string, object>
        {
            ["chat_id"] = dest.ChatId,
            ["text"] = message,
            ["parse_mode"] = "HTML",
            ["disable_web_page_preview"] = false
        };

        if (!string.IsNullOrEmpty(dest.TopicId) && long.TryParse(dest.TopicId, out var threadId))
            body["message_thread_id"] = threadId;

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, body);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (response.IsSuccessStatusCode && json.TryGetProperty("ok", out var ok) && ok.GetBoolean())
                return SingleResult.Success();

            var description = json.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? $"HTTP {(int)response.StatusCode}"
                : $"HTTP {(int)response.StatusCode}";

            return SingleResult.Error(description, dest);
        }
        catch (Exception ex)
        {
            return SingleResult.Error(ex.Message, dest);
        }
    }

    private async Task<SingleResult> SendPhotoSingleAsync(
        string botToken, Destination dest, string caption, byte[] photoBytes, string fileName)
    {
        var url = $"{TelegramApiBase}{botToken}/sendPhoto";

        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(dest.ChatId), "chat_id");
            content.Add(new StringContent(caption), "caption");
            content.Add(new StringContent("HTML"), "parse_mode");

            if (!string.IsNullOrEmpty(dest.TopicId) && long.TryParse(dest.TopicId, out var threadId))
                content.Add(new StringContent(threadId.ToString()), "message_thread_id");

            var photoContent = new ByteArrayContent(photoBytes);
            photoContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(photoContent, "photo", fileName);

            var response = await _httpClient.PostAsync(url, content);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (response.IsSuccessStatusCode && json.TryGetProperty("ok", out var ok) && ok.GetBoolean())
                return SingleResult.Success();

            var description = json.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? $"HTTP {(int)response.StatusCode}"
                : $"HTTP {(int)response.StatusCode}";

            return SingleResult.Error(description, dest);
        }
        catch (Exception ex)
        {
            return SingleResult.Error(ex.Message, dest);
        }
    }

    // ── Message formatting ──────────────────────────────────────

    private static string BuildMessage(ClipboardItem item, string note)
    {
        var parts = new List<string>();
        string escapedText = EscapeHtml(item.PreviewText);
        
        if (item.IsLikelyCode)
        {
            parts.Add($"<pre><code>{escapedText}</code></pre>");
        }
        else
        {
            parts.Add(escapedText);
        }

        if (!string.IsNullOrWhiteSpace(note))
            parts.Add($"\n📝 Note: {EscapeHtml(note.Trim())}");

        return string.Join("\n", parts);
    }

    private static string BuildPhotoCaption(string note)
    {
        return $"📝 Note: {EscapeHtml(note.Trim())}";
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..(maxLength - TruncationSuffix.Length)] + TruncationSuffix;
    }

    // ── Destination resolution ──────────────────────────────────

    private static List<Destination> ResolveDestinations(
        List<Destination>? selectedDestinations, List<Recipient> recipients)
    {
        if (selectedDestinations is { Count: > 0 })
            return UniqueDestinations(selectedDestinations);

        // No selection — flatten all recipients + their topics
        var all = new List<Destination>();
        foreach (var recipient in recipients)
        {
            if (string.IsNullOrWhiteSpace(recipient.ChatId))
                continue;

            all.Add(new Destination { ChatId = recipient.ChatId.Trim() });

            foreach (var topic in recipient.Topics ?? [])
            {
                if (!string.IsNullOrWhiteSpace(topic.TopicId))
                    all.Add(new Destination { ChatId = recipient.ChatId.Trim(), TopicId = topic.TopicId.Trim() });
            }
        }

        return UniqueDestinations(all);
    }

    private static List<Destination> UniqueDestinations(List<Destination> destinations)
    {
        var seen = new HashSet<string>();
        var unique = new List<Destination>();

        foreach (var dest in destinations)
        {
            if (string.IsNullOrWhiteSpace(dest.ChatId))
                continue;

            var key = $"{dest.ChatId.Trim()}|{dest.TopicId?.Trim() ?? ""}";
            if (seen.Add(key))
                unique.Add(dest);
        }

        return unique;
    }

    private static string ResolveDestinationLabel(Destination dest, List<Recipient> recipients)
    {
        var recipient = recipients.FirstOrDefault(r => r.ChatId == dest.ChatId);
        if (recipient == null)
            return dest.TopicId != null ? $"{dest.ChatId} / topic {dest.TopicId}" : dest.ChatId;

        var label = !string.IsNullOrWhiteSpace(recipient.Label) ? recipient.Label : recipient.ChatId;
        if (string.IsNullOrEmpty(dest.TopicId))
            return label;

        var topic = recipient.Topics?.FirstOrDefault(t => t.TopicId == dest.TopicId);
        var topicLabel = !string.IsNullOrWhiteSpace(topic?.Label) ? topic.Label : dest.TopicId;
        return $"{label} -> {topicLabel}";
    }

    // ── Legacy migration ────────────────────────────────────────

    private static void MigrateSettings(Settings settings)
    {
        if (settings.Recipients.Count > 0)
            return;

        if (string.IsNullOrWhiteSpace(settings.ChatId))
            return;

        settings.Recipients.Add(new Recipient
        {
            Id = "legacy-1",
            Label = "Default",
            ChatId = settings.ChatId.Trim(),
            Topics = []
        });
    }

    // ── Result aggregation ──────────────────────────────────────

    private static SendResult BuildAggregateResult(SingleResult[] results)
    {
        if (results.Length == 0)
            return SendResult.Fail("No destinations selected.");

        var successCount = results.Count(r => r.Ok);
        var failResults = results.Where(r => !r.Ok).ToList();

        if (failResults.Count == 0)
        {
            return new SendResult
            {
                Success = true,
                SuccessCount = successCount,
                TotalCount = results.Length
            };
        }

        var errors = failResults
            .Where(r => r.ErrorText != null)
            .Select(r => r.ErrorText!)
            .ToList();

        return new SendResult
        {
            Success = false,
            ErrorMessage = errors.FirstOrDefault() ?? "One or more destinations failed.",
            SuccessCount = successCount,
            FailureCount = failResults.Count,
            TotalCount = results.Length,
            Errors = errors
        };
    }

    // ── Internal result type ────────────────────────────────────

    private record SingleResult(bool Ok, string? ErrorText)
    {
        public static SingleResult Success() => new(true, null);

        public static SingleResult Error(string error, Destination dest)
        {
            var label = dest.TopicId != null
                ? $"[{dest.ChatId} / topic {dest.TopicId}]"
                : $"[{dest.ChatId}]";
            return new SingleResult(false, $"{label} {error}");
        }
    }
}
