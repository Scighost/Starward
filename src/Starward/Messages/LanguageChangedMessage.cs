using System.Globalization;

namespace Starward.Messages;


public record LanguageChangedMessage(string Language, CultureInfo CultureInfo)
{
    public bool Completed { get; set; }
}

