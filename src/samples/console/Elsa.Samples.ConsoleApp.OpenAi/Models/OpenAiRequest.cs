namespace Elsa.Samples.ConsoleApp.OpenAi.Models;

public class OpenAiRequest
{
    public string? AppKey { get; set; }
    public string? Model { get; set; }

    public List<ModelAiMessage> Messages = new();
}