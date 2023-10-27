namespace Starward.Language;

public static class Localization
{



    public static readonly IReadOnlyCollection<(string Title, string LangCode)> LanguageList = new List<(string, string)>
    {
        ("English (en-US, @Scighost)", "en-US"),
        ("日本語 (ja-JP, @reindex-ot)", "ja-JP"),
        ("한국어 (ko-KR, @DE2SSE)", "ko-KR"),
        ("Русский (ru-RU, @DarkAssassin48)", "ru-RU"),
        ("ภาษาไทย (th-TH, @thunni-noi)", "th-TH"),
        ("Tiếng Việt (vi-VN, @phucho0237)", "vi-VN"),
        ("简体中文 (zh-CN, @Scighost)", "zh-CN"),
        ("繁體中文 (zh-TW, @XPRAMT)", "zh-TW"),
    }.AsReadOnly();



}
