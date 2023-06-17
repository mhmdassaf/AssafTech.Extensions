namespace AssafTech.Extensions.Models;

public class SwaggerModel
{
    public required string Title { get; set; }
    public required string Version { get; set; } = "v1";
    public required Dictionary<string, string> IdentityServerScopes { get; set; }
}
