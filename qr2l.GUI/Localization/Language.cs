namespace qr2l.GUI.Localization;

public enum Languages
{
    English,
    Italian,
    Spanish,
    French,
    German,
    Chinese,
    Japanese,
    Russian
}

public static class LanguageExtensions
{
    public static string ToCultureCode(this Languages lang)
    {
        return lang switch {
            Languages.English => "en",
            Languages.Italian => "it",
            Languages.Spanish => "es",
            Languages.French => "fr",
            Languages.German => "de",
            Languages.Chinese => "zh",
            Languages.Japanese => "ja",
            Languages.Russian => "ru",
            var _ => "en"
        };
    }
}