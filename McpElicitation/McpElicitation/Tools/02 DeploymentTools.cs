using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpElicitation.Tools;

[McpServerToolType]
public sealed class DeploymentTools
{
	[McpServerTool, Description("Collects deployment details and confirmation. This does not prove authorization.")]
	public static string ApproveDeployment(
		McpServer server,
		RequestContext<CallToolRequestParams> context)
	{
		const string requestState = "approve-deployment-v1";

		if (context.Params.InputResponses?.TryGetValue("deploymentApproval", out var response) is true)
		{
			var result = response.Deserialize(InputResponse.ElicitResultJsonTypeInfo);

			if (result is null)
				return "Deployment approval response was invalid.";

			if (result.Action == "decline")
				return "Deployment was declined.";

			if (result.Action == "cancel")
				return "Deployment approval was cancelled.";

			if (result.IsAccepted is not true)
				return "Deployment approval response was invalid.";

			var environment = result.Content?.TryGetValue("environment", out var environmentValue) is true
				? environmentValue.GetString()
				: null;
			var confirmed = result.Content?.TryGetValue("confirmed", out var confirmedValue) is true
			                && confirmedValue.ValueKind is System.Text.Json.JsonValueKind.True;

			if (environment is not ("development" or "staging" or "production"))
				return "Deployment environment must be development, staging, or production.";

			if (!confirmed)
				return "Deployment was not confirmed.";

			return $"Deployment to '{environment}' was confirmed.";
		}

		if (server.IsMrtrSupported)
		{
			throw new InputRequiredException(
				inputRequests: new Dictionary<string, InputRequest>
				{
					["deploymentApproval"] = InputRequest.ForElicitation(new ElicitRequestParams
					{
						Message = "Choose a deployment environment and confirm this low-risk demo request.",
						RequestedSchema = new()
						{
							Properties =
							{
								["environment"] = new ElicitRequestParams.StringSchema
								{
									Title = "Deployment environment",
									Description = "One of: development, staging, production.",
									Default = "development"
								},
								["confirmed"] = new ElicitRequestParams.BooleanSchema
								{
									Title = "Confirm deployment request",
									Description = "Check to confirm the demo deployment request.",
								},
							},
							Required = ["environment", "confirmed"],
						},
					})
				},
				requestState: requestState);
		}

		return "This client cannot perform an interactive approval. ApproveDeployment requires a client with MRTR support.";
	}
}