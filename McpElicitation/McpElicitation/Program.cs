using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<BootstrapTools>()
    .WithTools<DeploymentTools>();

var app = builder.Build();

//app.UseHttpsRedirection();
AddRequestLogging(app);

app.MapMcp();

app.Run();

void AddRequestLogging(WebApplication webApplication)
{
	webApplication.Use(async (context, next) =>
	{
		var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
			.CreateLogger("McpElicitation.Requests");
		var timer = Stopwatch.StartNew();

		logger.LogInformation("Received {Method} {Path}", context.Request.Method, context.Request.Path);

		try
		{
			await next(context);
			logger.LogInformation(
				"Completed {Method} {Path} with {StatusCode} in {ElapsedMilliseconds} ms",
				context.Request.Method,
				context.Request.Path,
				context.Response.StatusCode,
				timer.ElapsedMilliseconds);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed {Method} {Path} after {ElapsedMilliseconds} ms", context.Request.Method, context.Request.Path, timer.ElapsedMilliseconds);
			throw;
		}
	});
}

[McpServerToolType]
public sealed class BootstrapTools

{
    [McpServerTool, Description("Returns a greeting from the MCP elicitation demo server.")]
    public static string Hello() => "Hello from the MCP elicitation demo server.";
}

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
