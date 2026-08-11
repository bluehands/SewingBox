using System.ComponentModel;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<BootstrapTools>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapMcp();

app.Run();

[McpServerToolType]
public sealed class BootstrapTools
{
    [McpServerTool, Description("Returns a greeting from the MCP elicitation demo server.")]
    public static string Hello() => "Hello from the MCP elicitation demo server.";
}
