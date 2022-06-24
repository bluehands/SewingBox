namespace WorkflowInteraction;

public interface ICamundaProcess
{
	public void SetVariable(string variableName, object? value);
	public object? GetVariable(string variableName);
}