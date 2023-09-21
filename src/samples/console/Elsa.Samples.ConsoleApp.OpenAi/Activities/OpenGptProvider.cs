using Elsa.Extensions;
using Elsa.Samples.ConsoleApp.OpenAi.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using OpenAI;
using OpenAI.Chat;

namespace Elsa.Samples.ConsoleApp.OpenAi.Activities;

public class OpenGptProvider : CodeActivity<OpenAiResponse>
{
    public OpenGptProvider(Variable<OpenAiRequest> openAiRequest, Variable<OpenAiResponse> openAiResponse)
    {
        OpenAiRequest = new Input<OpenAiRequest>(openAiRequest);
        Result = new Output<OpenAiResponse>(openAiResponse);
    }
        
    public Input<OpenAiRequest> OpenAiRequest { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var openAiRequest = OpenAiRequest.Get(context);
        
        var messages = new List<Message>
        {
            new Message(Role.System, openAiRequest.Template),
            new Message(Role.User, openAiRequest.UserPrompt)
        };

        var openAiResponse = new OpenAiResponse();
        var api = new OpenAIClient(openAiRequest.AppKey);

        try
        {
            var chatRequest = new ChatRequest(messages, model: OpenAI.Models.Model.GPT4);
            var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

            openAiResponse.Result = response?.FirstChoice?.Message?.Content;
            openAiResponse.IsSucceeded = true;

        }
        catch (Exception e)
        {
            openAiResponse.Error = e.Message;
        }
        
        context.SetResult(openAiResponse);
    }
}