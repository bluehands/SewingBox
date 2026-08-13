using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpElicitation.Tools;

[McpServerToolType]
public sealed class HelloWorldTools

{
	[McpServerTool]
	[Description("Returns a greeting from the MCP elicitation demo server.")]
	public static string Hello() => "Hello from the MCP elicitation demo server.";
}