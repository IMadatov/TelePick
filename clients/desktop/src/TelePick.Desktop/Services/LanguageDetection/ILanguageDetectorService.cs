namespace TelePick.Desktop.Services.LanguageDetection;

public interface ILanguageDetectorService
{
    /// <summary>
    /// Detects if the given text is likely to be source code and returns the detected language.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>A tuple containing whether it's code, and the language name if identified.</returns>
    (bool IsCode, string? Language) Detect(string text);
}
