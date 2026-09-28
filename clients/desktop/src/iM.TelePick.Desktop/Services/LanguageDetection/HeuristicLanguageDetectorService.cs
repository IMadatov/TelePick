using System;
using System.Linq;

namespace iM.TelePick.Desktop.Services.LanguageDetection;

public class HeuristicLanguageDetectorService : ILanguageDetectorService
{
    public (bool IsCode, string? Language) Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (false, null);
        }

        // C#
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"using\s+[A-Za-z0-9_\.]+;|\bnamespace\s+[A-Za-z0-9_\.]+|\bpublic\s+class\b|\bpublic\s+record\b|\[HttpGet\]|\[HttpPost\]|Console\.WriteLine"))
            return (true, "C#");

        // Java
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bimport\s+java\.|System\.out\.println|\bpublic\s+static\s+void\s+main\b"))
            return (true, "Java");

        // JavaScript / TypeScript
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bconsole\.log\(|\bconst\s+[a-zA-Z0-9_]+\s*=|\blet\s+[a-zA-Z0-9_]+\s*=|\bexport\s+const\b|\bimport\s+.*from\s+['""]|\bfunction\s*[a-zA-Z0-9_]*\s*\("))
            return (true, "JavaScript / TypeScript");

        // Python
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bdef\s+[a-zA-Z0-9_]+\s*\(|\bimport\s+[a-zA-Z0-9_]+$|\bfrom\s+[a-zA-Z0-9_]+\s+import\b|\bprint\s*\("))
            return (true, "Python");

        // PHP
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"<\?php|\$[a-zA-Z_x7f-xff][a-zA-Z0-9_x7f-xff]*\s*=|\becho\s+['""]"))
            return (true, "PHP");

        // HTML / XML
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"<html\b|</[a-zA-Z0-9]+>|<div\b|<span\b|<\?xml\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return (true, "HTML / XML");

        // SQL
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bSELECT\b.*\bFROM\b|\bINSERT\s+INTO\b|\bUPDATE\b.*\bSET\b|\bDELETE\s+FROM\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline))
            return (true, "SQL");

        // JSON
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\s*\{\s*""[a-zA-Z0-9_]+""\s*:\s*.*\}\s*$", System.Text.RegularExpressions.RegexOptions.Singleline) || System.Text.RegularExpressions.Regex.IsMatch(text, @"""[a-zA-Z0-9_]+""\s*:\s*(\[|\{|""|[0-9]|true|false|null)"))
            return (true, "JSON");

        // Rust
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bfn\s+[a-zA-Z0-9_]+\s*\(|\bpub\s+struct\b|\blet\s+mut\b"))
            return (true, "Rust");

        // Go
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bfunc\s+[a-zA-Z0-9_]+\s*\(|\bpackage\s+main\b|\bimport\s+\(""fmt""\)|\bfmt\.[A-Z][a-zA-Z0-9_]*\("))
            return (true, "Go");

        // Kotlin
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bfun\s+[a-zA-Z0-9_]+\s*\(|\bval\s+[a-zA-Z0-9_]+\s*=|println\("))
            return (true, "Kotlin");

        // C++
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"#include\s+<[a-zA-Z0-9_]+>|std::cout|\bint\s+main\s*\(\s*\)"))
            return (true, "C++");

        // Basic heuristic fallback for generic code detection
        var codeChars = new[] { '{', '}', ';', '<', '>', '=', '(', ')', '[', ']' };
        int symbolCount = text.Count(c => codeChars.Contains(c));
        
        string[] keywords = { "class ", "public ", "private ", "void ", "function ", "const ", "let ", "var ", "using ", "import ", "def ", "return ", "if ", "else ", "for " };
        int keywordCount = keywords.Count(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase));

        // If high symbol density or contains multiple keywords, treat as code
        bool isLikelyCode = (symbolCount > 5) || (keywordCount >= 2) || text.Contains("=>") || text.Contains("==") || text.Contains("</");
        
        return (isLikelyCode, null);
    }
}
