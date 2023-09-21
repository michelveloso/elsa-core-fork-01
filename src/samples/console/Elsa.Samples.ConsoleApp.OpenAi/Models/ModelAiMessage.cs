using OpenAI.Chat;

namespace Elsa.Samples.ConsoleApp.OpenAi.Models;

public class ModelAiMessage
{
    public Role Role { get; set; }
    public string? Text { get; set; }
}