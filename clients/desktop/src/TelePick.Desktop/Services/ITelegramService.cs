using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TelePick.Desktop.Models;

namespace TelePick.Desktop.Services;

public interface ITelegramService
{
    Task<SendResult> SendMessageAsync(
        string text,
        string note,
        Settings settings,
        List<Destination>? selectedDestinations = null);

    Task<SendResult> SendPhotoAsync(
        Stream photoStream,
        string fileName,
        string? caption,
        Settings settings,
        List<Destination>? selectedDestinations = null);

    Task<SendResult> TestConnectionAsync(
        Settings settings,
        List<Destination>? selectedDestinations = null);
}
