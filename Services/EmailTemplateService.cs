using System.IO;
using System.Threading.Tasks;

public class EmailTemplateService
{
    private readonly IWebHostEnvironment _env;

    public EmailTemplateService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> GetTemplateAsync(string templateName, Dictionary<string, string> placeholders)
    {
        var path = Path.Combine(_env.ContentRootPath, "Templates", templateName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Template {templateName} not found");

        var content = await File.ReadAllTextAsync(path);

        foreach (var ph in placeholders)
        {
            content = content.Replace($"{{{{{ph.Key}}}}}", ph.Value);
        }

        return content;
    }
}
