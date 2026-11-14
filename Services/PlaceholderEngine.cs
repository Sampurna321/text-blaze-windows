using System.Text;
using System.Text.RegularExpressions;
using TextBlaze.Models;

namespace TextBlaze.Services;

// Parses snippet body and resolves placeholders: {date}, {time}, {clipboard},
// {cursor}, {form:label}, {choice:opt1|opt2}, {image:N}.
public static class PlaceholderEngine
{
    private static readonly Regex P = new(@"\{([a-zA-Z_]+)(?::([^}]*))?\}");

    public static RenderResult Render(string body, SnippetRenderContext ctx)
    {
        var result = new StringBuilder();
        var prompts = new List<PromptField>();
        var cursorPos = -1;

        foreach (Match m in P.Matches(body))
        {
            var before = body[..m.Index];
            result.Append(before);
            body = body[(m.Index + m.Length)..];

            var name = m.Groups[1].Value.ToLowerInvariant();
            var arg = m.Groups[2].Success ? m.Groups[2].Value : "";

            switch (name)
            {
                case "date":
                    result.Append(DateTime.Now.ToString("yyyy-MM-dd"));
                    break;
                case "time":
                    result.Append(DateTime.Now.ToString("HH:mm"));
                    break;
                case "datetime":
                    result.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    break;
                case "clipboard":
                    result.Append(ctx.ClipboardText ?? "");
                    break;
                case "cursor":
                    cursorPos = result.Length;
                    break;
                case "form" when arg.Length > 0:
                    prompts.Add(new PromptField(arg, arg, cursorPos = result.Length));
                    break;
                case "choice" when arg.Contains('|'):
                    var choices = arg.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    result.Append(choices[0]);
                    break;
                default:
                    result.Append(m.Value);
                    break;
            }
        }

        result.Append(body);
        return new(result.ToString(), prompts, cursorPos);
    }
}

public record SnippetRenderContext
{
    public string? ClipboardText { get; set; }
}

public record RenderResult(string Text, List<PromptField> Prompts, int CursorPosition);

public record PromptField(string Label, string DefaultValue, int CursorPosition);