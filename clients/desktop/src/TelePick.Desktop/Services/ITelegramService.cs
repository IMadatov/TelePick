using System.Threading.Tasks;
using TelePick.Desktop.Models;

namespace TelePick.Desktop.Services;

public interface ITelegramService
{
    Task<SendResult> SendMessageAsync(string text, string note, Settings settings);
}
