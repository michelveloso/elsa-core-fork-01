using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class DefaultWorkflowExecutionContextFactory : IWorkflowExecutionContextFactory
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IActivitySchedulerFactory _schedulerFactory;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    private readonly ISystemClock _systemClock;
    private readonly IHasher _hasher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowExecutionContextFactory(
        IActivityVisitor activityVisitor,
        IIdentityGraphService identityGraphService,
        IActivitySchedulerFactory schedulerFactory,
        IActivityRegistry activityRegistry,
        IWorkflowStateExtractor workflowStateExtractor,
        ISystemClock systemClock,
        IHasher hasher)
    {
        _activityVisitor = activityVisitor;
        _identityGraphService = identityGraphService;
        _schedulerFactory = schedulerFactory;
        _activityRegistry = activityRegistry;
        _workflowStateExtractor = workflowStateExtractor;
        _systemClock = systemClock;
        _hasher = hasher;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionContext> CreateAsync(
        IServiceProvider serviceProvider,
        Workflow workflow,
        string instanceId,
        WorkflowState? workflowState,
        IDictionary<string, object>? input = default,
        string? correlationId = default,
        ExecuteActivityDelegate? executeActivityDelegate = default,
        string? triggerActivityId = default,
        CancellationTokens cancellationTokens = default)
    {
        var root = workflow;

        // Build graph.
        var useActivityIdAsNodeId = workflow.CreatedWithModernTooling();
        var graph = await _activityVisitor.VisitAsync(root, useActivityIdAsNodeId, cancellationTokens.ApplicationCancellationToken);
        var flattenedList = graph.Flatten().ToList();
        
        // Register activity types.
        var activityTypes = flattenedList.Select(x => x.Activity.GetType()).Distinct().ToList();
        await _activityRegistry.RegisterAsync(activityTypes, cancellationTokens.ApplicationCancellationToken);
        
        var needsIdentityAssignment = flattenedList.Any(x => string.IsNullOrEmpty(x.Activity.Id));

        if (needsIdentityAssignment)
            _identityGraphService.AssignIdentities(flattenedList);

        // Create scheduler.
        var scheduler = _schedulerFactory.CreateScheduler();

        // Setup a workflow execution context.
        var workflowExecutionContext = new WorkflowExecutionContext(
            serviceProvider,
            _hasher,
            _systemClock,
            instanceId,
            correlationId,
            workflow,
            graph,
            flattenedList,
            scheduler,
            _activityRegistry,
            input,
            executeActivityDelegate,
            triggerActivityId,
            default,
            workflowState?.Incidents ?? new List<ActivityIncident>(),
            workflowState?.CreatedAt ?? _systemClock.UtcNow,
            cancellationTokens);

        // Restore workflow execution context from state, if provided.
        if (workflowState != null) _workflowStateExtractor.Apply(workflowExecutionContext, workflowState);

        return workflowExecutionContext;
    }
}