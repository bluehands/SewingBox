# MCP Elicitation Demo Plan

## Goal

Build a .NET 10 C# MCP server that demonstrates the difference between form and URL elicitation for human-in-the-loop workflows.

Use the MCP `2026-07-28` specification and the official C# SDK. Use MCP Inspector in Chrome or Edge as the demo client.

## Demo Scope

The live coding session should fit in 40 minutes. Keep the first version deliberately small and runnable after each step.

Do not implement real authentication, OAuth, persistence, cryptography, or real secret handling during the live session.

## Core Messages

- Form elicitation is appropriate for ordinary, low-risk input.
- A form response with `action: "accept"` is a client assertion, not proof that a specific human approved the exact operation.
- Treat all form responses as untrusted input and validate them server-side.
- Form elicitation must not collect passwords, API keys, access tokens, payment data, or other secrets.
- URL elicitation keeps sensitive browser interaction outside the MCP client and LLM context.
- URL elicitation still needs production safeguards: browser authentication, binding to the MCP user and exact operation, short expiry, one-time use, and replay protection.

## Implementation Steps

### Step 1: Bootstrap Server

Create a .NET 10 ASP.NET Core Streamable HTTP MCP server.

- Use `ModelContextProtocol.AspNetCore`.
- Keep application setup and request logging in `McpElicitation/Program.cs`.
- Expose a `Hello` tool from `McpElicitation/Tools/01 HelloWorldTools.cs`.
- Connect with MCP Inspector and verify `Hello`.

Status: complete.

### Step 2: MRTR Form Approval

Implement an `ApproveDeployment` tool using MRTR.

- Keep the tool in `McpElicitation/Tools/02 DeploymentTools.cs`.
- On the first call, throw `InputRequiredException` with a form elicitation request.
- Ask for a deployment environment and a Boolean confirmation.
- On the retry, read `inputResponses` and handle `accept`, `decline`, and `cancel`.
- Validate values on the server.
- Explain that this is low-risk input collection, not authorization proof.

### Step 3: Unsafe Form Secret Demonstration

Implement an intentionally unsafe tool, such as `EnterApiKeyUnsafely`.

- Keep the tool in `McpElicitation/Tools/03 UnsafeDemoTools.cs`.
- Request a value named `apiKey` using form elicitation.
- Enter only `demo-not-a-secret` during the presentation.
- Do not log, persist, or use the submitted value.
- Show that the value crosses the MCP protocol boundary in the Inspector trace.
- Explain why this is prohibited for real credentials.

### Step 4: URL Approval Flow

Implement a harmless out-of-band approval flow.

- Keep the tool, browser endpoints, and in-memory approval store in `McpElicitation/Tools/04 TransferTools.cs`.
- Add an in-memory pending-approval store keyed by an opaque `Guid`.
- Add a browser endpoint such as `/approve/{id}` with Approve and Decline buttons.
- Add a `SendMoney` tool.
- When approval is absent, return the SDK-supported URL elicitation required flow with an opaque URL.
- The browser endpoint marks the pending approval complete.
- Retrying the tool succeeds only after server-side completion.

Explain that the client receives the URL and consent result, but not the browser-side approval data.

## Explicit Deferrals

The minimal URL demo is not production-secure. Defer these topics until after the live session:

- Browser/MCP user identity binding
- Authentication and authorization
- Third-party OAuth
- Signed state tokens
- Persistent state and audit records
- Expiration and replay prevention
- One-time-use transaction approval
- Automated tests

## Suggested Timing

1. 0-5 minutes: start the server, connect Inspector, call `Hello`.
2. 5-17 minutes: add and demonstrate MRTR form approval.
3. 17-24 minutes: add unsafe form-secret demonstration and inspect the trace.
4. 24-35 minutes: add the URL approval endpoint and URL elicitation.
5. 35-40 minutes: compare the security boundaries and summarize production requirements.

## Presentation Workflow

Work in small, runnable milestones. The user creates a Git commit after each accepted step.

For each step:

1. Implement the smallest working change.
2. Build and run it.
3. Demonstrate it in MCP Inspector.
4. Explain its security implication.
5. Review and commit before continuing.
