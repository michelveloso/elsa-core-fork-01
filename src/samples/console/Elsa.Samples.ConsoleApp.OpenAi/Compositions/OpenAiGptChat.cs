using Elsa.Extensions;
using Elsa.Samples.ConsoleApp.OpenAi.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.ConsoleApp.OpenAi.Activities.Compositions;

public class OpenAiGptChat : Composite<OpenAiResponse>
{
    public Input<OpenAiRequest> OpenAiRequest { get; set; }
    public Variable<int> MaximumAttempts { get; set; } = new(3);
    public Variable<int> AttemptsNumbers { get; set; } = new(0);
    public Variable<string> InputRead { get; set; } = new();
    private Variable<OpenAiResponse> OpenAiResponse { get; set; } = new(new OpenAiResponse());
    
    public OpenAiGptChat(Variable<OpenAiRequest> openAiRequest)
    {
        OpenAiRequest = new Input<OpenAiRequest>(openAiRequest);
        
        Variables = new List<Variable> { openAiRequest, OpenAiResponse, MaximumAttempts, AttemptsNumbers, InputRead };
        Root = new Sequence
        {
            Activities =
            {
                new While(context => AttemptsNumbers.Get<int>(context) <= MaximumAttempts.Get<int>(context) && !OpenAiResponse.Get<OpenAiResponse>(context)!.IsSucceeded)
                {
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new OpenGptProvider(openAiRequest, OpenAiResponse),
                            new If
                            {
                                Condition = new (context => !OpenAiResponse.Get<OpenAiResponse>(context)!.IsSucceeded),
                                // Then = new WriteLine(context => $"response: { OpenAiResponse.Get<OpenAiResponse>(context)?.Result}"),
                                Then = new Sequence
                                {
                                    Activities =
                                    {
                                        new SetVariable<int>(AttemptsNumbers, context => AttemptsNumbers.Get<int>(context) + 1),
                                        new If
                                        {
                                            Condition = new (context => AttemptsNumbers.Get<int>(context) >= MaximumAttempts.Get<int>(context)),
                                            Then = new WriteLine("I'm tired of trying =/ "),
                                            Else = new WriteLine("Let's try again ..."),
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    protected override void OnCompleted(ActivityCompletedContext context)
    {
        var response = OpenAiResponse.Get<OpenAiResponse>(context.TargetContext)!;
        context.TargetContext.Set(Result, response);
    }
}