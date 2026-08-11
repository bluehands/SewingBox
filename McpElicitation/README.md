# MCP Elicitation Demo

A .NET 10 MCP server used to demonstrate form and URL elicitation.

## Run

```powershell
dotnet run
```

The server exposes Streamable HTTP MCP at `https://localhost:<port>/mcp`. The startup output shows the selected port.

## Connect with MCP Inspector

```powershell
npx @modelcontextprotocol/inspector
```

In the Inspector Web UI, select Streamable HTTP and enter the server's `/mcp` URL. Call the `Hello` tool to verify the connection.
