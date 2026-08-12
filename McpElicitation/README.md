# MCP Elicitation Demo

A .NET 10 MCP server used to demonstrate form and URL elicitation.

## Run

```powershell
dotnet run
```

The server exposes Streamable HTTP MCP at `https://localhost:<port>/`. The startup output shows the selected port.

## Connect with MCP Inspector

```powershell
npx @modelcontextprotocol/inspector
```

There were issues with Firefox and the Inspector Web UI, so use Chrome or Edge for this demo.

In the Inspector Web UI, select Streamable HTTP and enter the server's root URL. For the default development profile, use `http://localhost:63891/`. Set Protocol Era to `Legacy` for this bootstrap server, then call the `Hello` tool to verify the connection.
