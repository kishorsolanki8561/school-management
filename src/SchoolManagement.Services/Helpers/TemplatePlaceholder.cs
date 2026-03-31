namespace SchoolManagement.Services.Helpers;

public static class TemplatePlaceholder
{
    /// <summary>
    /// Replaces {{Key}} placeholders in <paramref name="template"/> with values
    /// from <paramref name="placeholders"/>. Case-insensitive key match.
    /// </summary>
    public static string Apply(string template, Dictionary<string, string> placeholders)
    {
        foreach (var (key, value) in placeholders)
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        return template;
    }
}
