namespace Elsa.Samples.ConsoleApp.OpenAi.Models;

public class OpenAiResponse
{
    public string? Result { get; set; }
    public bool IsSucceeded { get; set; }
    public string? Error { get; set; }
}