namespace PerfStringNormalizer001.Core;

public sealed class SlugGenerator
{
    public string Generate(string input)
    {
        var result = string.Empty;
        var previousDash = false;
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch))
            {
                result += char.ToLowerInvariant(ch);
                previousDash = false;
            }
            else if (!previousDash)
            {
                result += "-";
                previousDash = true;
            }
        }
        return result.Trim('-');
    }
}
