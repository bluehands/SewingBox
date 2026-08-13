using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpElicitation.Tools;

[McpServerToolType]
public sealed class UnsafeDemoTools
{
	[McpServerTool]
	[Description("Unsafe demonstration only: requests an API key through form elicitation. Never use this pattern for real credentials.")]
	public static string EnterApiKeyUnsafely(
		McpServer server,
		RequestContext<CallToolRequestParams> context)
	{
		if (context.Params.InputResponses?.TryGetValue("apiKey", out var response) is true)
		{
			var result = response.Deserialize(InputResponse.ElicitResultJsonTypeInfo);

			return result?.Action switch
			{
				"accept" => "Unsafe demo complete. The submitted value was deliberately not logged, persisted, or used.",
				"decline" => "API key entry was declined.",
				"cancel" => "API key entry was cancelled.",
				_ => "API key response was invalid.",
			};
		}

		if (server.IsMrtrSupported)
		{
			throw new InputRequiredException(
				inputRequests: new Dictionary<string, InputRequest>
				{
					["apiKey"] = InputRequest.ForElicitation(new ElicitRequestParams
					{
						Message = "UNSAFE DEMO ONLY: enter demo-not-a-secret. Never submit credentials through form elicitation.",
						RequestedSchema = new()
						{
							Properties =
							{
								["apiKey"] = new ElicitRequestParams.StringSchema
								{
									Title = "API key (unsafe demo)",
									Description = "Enter only demo-not-a-secret. This illustrates why form elicitation is prohibited for real secrets.",
								},
							},
							Required = ["apiKey"],
						},
					})
				},
				requestState: "unsafe-api-key-demo-v1");
		}

		return "This client cannot perform the unsafe elicitation demo because it does not support MRTR.";
	}
}