# MCP Elicitation Demo

A .NET 10 MCP server used to demonstrate form and URL elicitation.

## Run

```powershell
dotnet run --project .\McpElicitation\McpElicitation.csproj
```

The server exposes Streamable HTTP MCP at `https://localhost:<port>/`. The startup output shows the selected port.

## Project Structure

- `McpElicitation.slnx`: solution entry point.
- `McpElicitation/Program.cs`: application setup, MCP registration, and request logging.
- `McpElicitation/Tools/01 HelloWorldTools.cs`: the `Hello` tool.
- `McpElicitation/Tools/02 DeploymentTools.cs`: the form approval demo.
- `McpElicitation/Tools/03 UnsafeDemoTools.cs`: the unsafe secret-entry demonstration.
- `McpElicitation/Tools/04 TransferTools.cs`: the URL approval demo, browser endpoints, and in-memory approval store.

## Connect with MCP Inspector

```powershell
npx @modelcontextprotocol/inspector
```

There were issues with Firefox and the Inspector Web UI, so use Chrome or Edge for this demo.

In the Inspector Web UI, select Streamable HTTP and enter the server's root URL. For the default development profile, use `http://localhost:63891/`. Use protocol version `2026-07-28` to demonstrate MRTR elicitation, then call `Hello` to verify the connection.

## Form Approval Demo

Call `ApproveDeployment`. The server returns an MRTR form request containing an `environment` field and a `confirmed` Boolean. Select `accept`, `decline`, or `cancel` in the Inspector.

For an accepted response, submit one of `development`, `staging`, or `production` and set `confirmed` to `true`. The server validates both values. The result deliberately states that accepting this form is low-risk input collection, not proof that a particular person authorized the deployment.

## Unsafe Secret Demo

Call `EnterApiKeyUnsafely` and enter only `demo-not-a-secret`. Inspect the MCP traffic to show that the form value crosses the protocol boundary. The tool deliberately neither reads the submitted field nor logs, persists, or uses it.

This is an intentionally unsafe demonstration. Real passwords, API keys, access tokens, payment data, and other secrets must never be collected through form elicitation.

## URL Approval Demo

Call `SendMoney` with a `receiver` and positive `amount`. The client receives a URL elicitation-required response. Its opaque browser URL displays the stored transfer details and offers **Approve** and **Decline**. Let Inspector retry the unchanged tool call after choosing an outcome.

The server consumes only a completed approval record whose stored receiver and amount exactly match the retried call. It returns the recorded approved or declined outcome. The demo permits only one pending transfer for an identical receiver and amount pair, because those unchanged arguments are the available retry correlation values.

The browser approval data stays outside the MCP client and LLM context. This in-memory demo is deliberately not production-secure: it has no authentication, user or operation binding, expiry, persistent audit record, or replay protection.
