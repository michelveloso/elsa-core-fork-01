namespace Elsa.Samples.ConsoleApp.OpenAi.Models.OpenAiRequest;

public class OpenAiRequest
{
    public string? AppKey { get; set; }
    public string? Model { get; set; }
    public string? Template { get; set; }
    public string? Prompt { get; set; }
}