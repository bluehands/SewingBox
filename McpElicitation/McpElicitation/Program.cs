using System.Diagnostics;
using McpElicitation.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PendingApprovalStore>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<HelloWorldTools>()
    .WithTools<DeploymentTools>()
    .WithTools<UnsafeDemoTools>()
    .WithTools<TransferTools>();

var app = builder.Build();

AddRequestLogging();

app.AddApprovalEndpoints();

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