namespace Starward.Language;

public static class Localization
{



    public static readonly IReadOnlyCollection<(string Title, string LangCode)> LanguageList = new List<(string, string)>
    {
        ("简体中文 (zh-CN, @Scighost)", "zh-CN"),
        ("English (en-US, @Scighost)", "en-US"),
        ("Tiếng Việt (vi-VN, @phucho0237)", "vi-VN"),
    }.AsReadOnly();



}
