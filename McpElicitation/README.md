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

In the Inspector Web UI, select Streamable HTTP and enter the server's root URL. For the default development profile, use `http://localhost:63891/`. Use protocol version `2026-07-28` to demonstrate MRTR elicitation, then call `Hello` to verify the connection.

## Form Approval Demo

Call `ApproveDeployment`. The server returns an MRTR form request containing an `environment` field and a `confirmed` Boolean. Select `accept`, `decline`, or `cancel` in the Inspector.

For an accepted response, submit one of `development`, `staging`, or `production` and set `confirmed` to `true`. The server validates both values. The result deliberately states that accepting this form is low-risk input collection, not proof that a particular person authorized the deployment.
