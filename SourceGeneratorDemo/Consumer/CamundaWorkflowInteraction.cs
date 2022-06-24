namespace WorkflowInteraction;
public class CamundaWorkflowInteraction
{
	readonly ICamundaProcess _workflow;

	public CamundaWorkflowInteraction(ICamundaProcess workflow) => _workflow = workflow;

	public void TalkToCamunda()
	{
		var variableValue = _workflow.GetVariable(WorkflowVariables.Risikoeinstufung);
	}
}