# Language Detection Subsystem Design

## Goal
Implement a fast, zero-dependency heuristics-based language detection subsystem for `TelePick.Desktop`. This replaces the `GuessLang.TextAnalyzer` dependency to improve reliability and remove native payload requirements.

## Architecture & Components

1. **`ILanguageDetectorService`**
   - Interface defining `(bool IsCode, string? Language) Detect(string text)`.
   - Located in `Services/LanguageDetection/ILanguageDetectorService.cs`.

2. **`HeuristicLanguageDetectorService`**
   - Implementation of `ILanguageDetectorService`.
   - Uses hardcoded string matching and keyword density checks (heuristics) to detect C#, JavaScript/TypeScript, Python, PHP, HTML, SQL, Kotlin, and Go.
   - Extremely fast, runs entirely in managed C# code.
   - Located in `Services/LanguageDetection/HeuristicLanguageDetectorService.cs`.

3. **Dependency Injection & Integration**
   - Register `ILanguageDetectorService` as a Singleton in the DI container.
   - Inject `ILanguageDetectorService` into `ClipboardMonitorService`.
   - Before instantiating a new `ClipboardItem` (when text is copied), `ClipboardMonitorService` calls `Detect()` and passes the resulting boolean and language string directly into the `ClipboardItem` constructor.
   
4. **Cleanup (`ClipboardItem.cs`)**
   - Remove `DetermineIfCode()` method completely from `ClipboardItem` since domain logic should reside in services, not data models.
   - Change `IsLikelyCode` and `Language` to be simple settable properties.

## Trade-offs
- **Pros:** Maximum performance, minimal app size overhead, no native library dependencies, clean separation of concerns (moves detection logic out of the data model).
- **Cons:** Adding a new language requires modifying the `HeuristicLanguageDetectorService` class and recompiling the application. However, for the current scope, this is acceptable.

## Testing
- Ensure standard texts do not trigger false positives.
- Ensure snippets of C#, JS, and Python are correctly identified.
