using Elsa.Extensions;
using Elsa.Samples.ConsoleApp.OpenAi.Compositions;
using Elsa.Samples.ConsoleApp.OpenAi.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

var inputRead = new Variable<string>();
var openAiResponse = new Variable<OpenAiResponse>(new OpenAiResponse());
var openAiRequest = new Variable<OpenAiRequest>(new OpenAiRequest
{
    AppKey = "[your-openai-key]",
    Model = "GPT-4",
    Messages = new List<ModelAiMessage>
    {
        new()
        {
            Role = Role.System,
            Text = "You are a virtual assistant, always respond to the user with 10 words",
        },
    }
});

var workflow = new Workflow
{
    Root = new Sequence
    {
        Variables = { openAiRequest, openAiResponse, inputRead },
        Activities =
        {
            new WriteLine("What is your doubt?"),
            new ReadLine(inputRead),
            new SetVariable
            {
                Variable = openAiRequest,
                Value = new Input<object?>(context =>
                {
                    var openAiRequestVariable = openAiRequest.Get<OpenAiRequest>(context);
                    openAiRequestVariable!.Messages.Add(new ModelAiMessage
                    {
                        Role = Role.User,
                        Text = inputRead.Get(context)
                    });
                    
                    return openAiRequestVariable;
                })
            },
            new OpenAiGptChat(openAiRequest)
            {
                Result = new(openAiResponse)
            },
            new WriteLine(context =>
            {
                var response = openAiResponse.Get(context)!;
                if (response.IsSucceeded)
                    return $"response: {response.Result}";
                
                return $"error: {response.Error}";
            })
        }
    }
};

var services = new ServiceCollection();
services.AddElsa();

var serviceProvider = services.BuildServiceProvider();

var serializer = serviceProvider.GetRequiredService<IActivitySerializer>();
var populator = serviceProvider.GetRequiredService<IRegistriesPopulator>();
await populator.PopulateAsync();

var json = serializer.Serialize(workflow.Root);

var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();
await workflowRunner.RunAsync(workflow);


