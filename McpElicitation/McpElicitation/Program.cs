using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<BootstrapTools>();

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
