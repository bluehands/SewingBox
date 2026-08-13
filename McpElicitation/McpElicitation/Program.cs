using System.ComponentModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Encodings.Web;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PendingApprovalStore>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<BootstrapTools>()
    .WithTools<DeploymentTools>()
    .WithTools<UnsafeDemoTools>()
    .WithTools<TransferTools>();

var app = builder.Build();

//app.UseHttpsRedirection();
AddRequestLogging();

AddApprovalEndpoints();

app.MapMcp();

app.Run();

void AddRequestLogging()
{
	app.Use(async (context, next) =>
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

void AddApprovalEndpoints()
{
	app.MapGet("/approve/{id:guid}", (Guid id, PendingApprovalStore approvals) =>
	{
		if (!approvals.TryGetPending(id, out var transfer))
			return Results.NotFound("Approval request was not found.");

		var receiver = HtmlEncoder.Default.Encode(transfer.Receiver);
		var amount = transfer.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

		return Results.Content($"""
		                        <!doctype html>
		                        <html lang="en"><head><meta charset="utf-8"><title>Demo approval</title></head>
		                        <body>
		                          <h1>Approve demo transfer</h1>
		                          <p>Send <strong>{amount} €</strong> to <strong>{receiver}</strong>.</p>
		                          <form method="post">
		                            <button type="submit" formaction="/approve/{id}/approve">Approve</button>
		                            <button type="submit" formaction="/approve/{id}/decline">Decline</button>
		                          </form>
		                        </body></html>
		                        """, "text/html");
	});

	app.MapPost("/approve/{id:guid}/approve", (Guid id, PendingApprovalStore approvals) =>
		approvals.Complete(id, ApprovalStatus.Approved)
			? Results.Content("Transfer approved. Return to the MCP client and retry the tool call.", "text/html")
			: Results.NotFound("Approval request was not found."));

	app.MapPost("/approve/{id:guid}/decline", (Guid id, PendingApprovalStore approvals) =>
		approvals.Complete(id, ApprovalStatus.Declined)
			? Results.Content("Transfer declined. Return to the MCP client and retry the tool call.", "text/html")
			: Results.NotFound("Approval request was not found."));
}

[McpServerToolType]
public sealed class BootstrapTools

{
	[McpServerTool]
	[Description("Returns a greeting from the MCP elicitation demo server.")]
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

[McpServerToolType]
public sealed class TransferTools
{
	[McpServerTool]
	[Description("Requires browser-based approval before completing a money transfer.")]
	public static string SendMoney(
        PendingApprovalStore approvals,
        IHttpContextAccessor httpContextAccessor,
        [Description("The transfer receiver.")] string receiver,
        [Description("The positive transfer amount.")] decimal amount)
    {
        if (string.IsNullOrWhiteSpace(receiver))
            return "Receiver is required.";

        if (amount <= 0)
            return "Amount must be greater than zero.";

        receiver = receiver.Trim();

        var decision = approvals.TryConsumeCompleted(receiver, amount);
        if (decision == ApprovalStatus.Approved)
            return $"Demo transfer of {amount:0.00}€ to '{receiver}' was approved.";

        if (decision == ApprovalStatus.Declined)
            return $"Demo transfer of {amount:0.00}€ to '{receiver}' was declined.";

        var request = httpContextAccessor.HttpContext?.Request;

        if (request is null)
            return "Unable to create the browser approval URL.";

        var newApprovalId = approvals.CreateOrGetPending(receiver, amount);
        var approvalUrl = $"{request.Scheme}://{request.Host}/approve/{newApprovalId}";

        throw new ModelContextProtocol.UrlElicitationRequiredException(
            "Browser approval is required before sending the money.",
            [
                new ElicitRequestParams
                {
                    Mode = "url",
                    ElicitationId = newApprovalId.ToString(),
                    Url = approvalUrl,
                    Message = "Open this URL to approve or decline the transfer, then retry the same tool call.",
                },
            ]);
    }
}

public sealed class PendingApprovalStore
{
	readonly ConcurrentDictionary<Guid, PendingApproval> _approvals = new();

    public Guid CreateOrGetPending(string receiver, decimal amount)
    {
        foreach (var approval in _approvals)
        {
            if (approval.Value.Receiver == receiver && approval.Value.Amount == amount && approval.Value.Status == ApprovalStatus.Pending)
                return approval.Key;
        }

        var id = Guid.NewGuid();
        _approvals[id] = new(receiver, amount, ApprovalStatus.Pending);
        return id;
    }

    public bool TryGetPending(Guid id, out PendingApproval approval) =>
        _approvals.TryGetValue(id, out approval!) && approval.Status == ApprovalStatus.Pending;

    public bool Complete(Guid id, ApprovalStatus decision)
    {
        if (!_approvals.TryGetValue(id, out var approval) || approval.Status != ApprovalStatus.Pending)
            return false;

        return _approvals.TryUpdate(id, approval with { Status = decision }, approval);
    }

    public ApprovalStatus? TryConsumeCompleted(string receiver, decimal amount)
    {
        foreach (var approval in _approvals)
        {
            if (approval.Value.Receiver != receiver || approval.Value.Amount != amount || approval.Value.Status == ApprovalStatus.Pending)
                continue;

            if (_approvals.TryRemove(approval.Key, out var completed))
                return completed.Status;
        }

        return null;
    }

    public sealed record PendingApproval(string Receiver, decimal Amount, ApprovalStatus Status);

}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Declined,
}
