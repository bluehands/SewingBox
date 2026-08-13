using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Encodings.Web;

namespace McpElicitation.Tools;

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

public static class ApprovalRegistrationExtensions
{
	public static void AddApprovalEndpoints(this WebApplication app)
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