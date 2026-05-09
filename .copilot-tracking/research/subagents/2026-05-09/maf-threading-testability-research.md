# MAF Threading & Testability Research

**Date:** 2026-05-09
**Status:** Complete
**Topics:** CancellationToken propagation, Singleton safety, AIAgent testability

---

## Question 1: CancellationToken Propagation to Function Tools

### Answer: YES — fully propagated end-to-end

### Evidence

**Call chain** (source-verified):

1. `AIAgent.RunAsync(string message, ..., CancellationToken cancellationToken)` — abstract base in `Microsoft.Agents.AI.Abstractions/AIAgent.cs`
   - All RunAsync overloads funnel to `RunCoreAsync(messages, session, options, cancellationToken)`

1. `ChatClientAgent.RunCoreAsync(messages, session, options, CancellationToken cancellationToken)` — sealed impl in `Microsoft.Agents.AI/ChatClient/ChatClientAgent.cs`
   - Calls `chatClient.GetResponseAsync(inputMessagesForChatClient, chatOptions, cancellationToken)`

1. The `chatClient` pipeline includes `FunctionInvokingChatClient` (from `Microsoft.Extensions.AI`) added by `chatClient.WithDefaultAgentMiddleware(options, services)`.

1. `FunctionInvokingChatClient.GetResponseAsync(messages, options, CancellationToken cancellationToken)` — passes CT to `ProcessFunctionCallsAsync(..., cancellationToken)`.

1. `FunctionInvokingChatClient.InvokeFunctionAsync(context, CancellationToken cancellationToken)`:

   ```csharp
   protected virtual ValueTask<object?> InvokeFunctionAsync(
       FunctionInvocationContext context, CancellationToken cancellationToken)
   {
       return FunctionInvoker is { } invoker
           ? invoker(context, cancellationToken)
           : context.Function.InvokeAsync(context.Arguments, cancellationToken);
   }
   ```

   Source: dotnet/extensions — `FunctionInvokingChatClient.cs`

1. `context.Function.InvokeAsync(context.Arguments, cancellationToken)` — the `AIFunction` created by `AIFunctionFactory.Create` receives the token.

### AIFunctionFactory.Create and CancellationToken

`AIFunctionFactory.Create` wraps delegates as `AIFunction` objects. The `AIFunction.InvokeAsync` signature accepts `CancellationToken`. If the delegate includes a `CancellationToken` parameter, it is automatically bound:

```csharp
// Sync — no CT needed
static string GetWeather(string location) => "cloudy";
AITool tool = AIFunctionFactory.Create(GetWeather);

// Async with CT — CT is propagated from RunAsync chain
static async Task<string> GetWeatherAsync(string location, CancellationToken ct)
{
    await Task.Delay(100, ct); // ct honoured
    return "cloudy";
}
AITool asyncTool = AIFunctionFactory.Create(GetWeatherAsync);
```

The CT reflection-binding in `AIFunctionFactory` inspects method parameters for `CancellationToken` and injects the propagated token. This is standard `Microsoft.Extensions.AI` behaviour.

### Streaming

`RunCoreStreamingAsync` uses `[EnumeratorCancellation] CancellationToken cancellationToken` and passes it to `GetStreamingResponseAsync`, which passes it through the same `FunctionInvokingChatClient` pipeline. CT propagation is identical for streaming.

---

## Question 2: AddSingleton Safety for AIAgent / Orchestrator

### Answer: SAFE — when function tools are stateless. Scoped required if tools hold per-request/per-user state

### Evidence

The official MAF ASP.NET end-to-end sample (`05-end-to-end/AspNetAgentAuthorization/Service/Program.cs`) registers the agent as **Singleton** with an explicit comment:

```csharp
// Here we are using Singleton lifetime, since none of the services, function tools
// and user context classes in the sample have state that are per request.
// You should evaluate the appropriate lifetime for your own services and tools
// based on their behavior and dependencies.
// E.g. if any of the service instances or tools maintain state that is specific
// to a user, and each request may be from a different user,
// you should use Scoped lifetime instead, so that a new instance is created for
// each request.
builder.Services.AddSingleton<AIAgent>(sp => { ... });
```

### Why Singleton is Safe

- `AIAgent` (abstract base) uses `AsyncLocal<AgentRunContext?>` for per-run context — flows across async boundaries without cross-request contamination.
- **Per-run isolation is provided by `AgentSession`**: each call to `agent.CreateSessionAsync()` returns a new `ChatClientAgentSession` instance. Session state (chat history, conversation ID, state bag) is stored in that session, not in the agent.
- `FunctionInvokingChatClient` is "thread-safe for concurrent use so long as the AIFunction instances employed as part of the supplied ChatOptions are also safe." (Source: `FunctionInvokingChatClient.cs` XML doc)

### When to Use Scoped

Use `Scoped` (or inject a factory) when:

- Function tools read from `IHttpContextAccessor` or hold per-user data
- Tools maintain mutable state between tool calls within a single request
- Dependencies of tools are themselves Scoped

### Foundry-Backed AIAgent (Persistent Agents)

For `aiProjectClient.AsAIAgent(...)` (Foundry/Azure AI Foundry), the agent resource is created once in the backing service (persistent Foundry agent) and shared across sessions. Each `CreateSessionAsync()` creates a new Foundry thread/session. The agent itself is safely reused as Singleton. The Foundry service manages session isolation.

---

## Question 3: Testability — Can AIAgent Be Mocked?

### Answer: No direct mock via NSubstitute/Moq. Best pattern is fake IChatClient or subclass AIAgent

### AIAgent Type Hierarchy

| Type | Modifier | Can Subclass? | Notes |
|---|---|---|---|
| `AIAgent` | `abstract partial class` | YES | Base abstraction — can subclass for tests |
| `ChatClientAgent` | `sealed class : AIAgent` | NO | Sealed — cannot be mocked or subclassed |
| `DelegatingAIAgent` | `abstract class : AIAgent` | YES | Decorator base — forwards all operations |
| `AnonymousDelegatingAIAgent` | concrete `: DelegatingAIAgent` | n/a | Allows creating agents with custom callbacks |

### No IAIAgent Interface

A search of the entire `microsoft/agent-framework` repository for `IAIAgent` returns **no code results** (only 1 discussion issue). There is no interface to inject.

### Recommended Test Patterns

#### Pattern A — Fake IChatClient (most idiomatic, best isolation)

`ChatClientAgent` takes an `IChatClient` constructor parameter. `IChatClient` IS an interface. In unit tests, inject a fake/stub `IChatClient`:

```csharp
// NSubstitute
var chatClient = Substitute.For<IChatClient>();
chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
    .Returns(new ChatResponse([new ChatMessage(ChatRole.Assistant, "stubbed response")]));

var agent = chatClient.AsAIAgent("gpt-5.4-mini",
    instructions: "You are a test agent.",
    name: "TestAgent");
```

This exercises the full `ChatClientAgent` + `FunctionInvokingChatClient` pipeline with a controlled response. It is the most realistic test short of calling a real LLM.

#### Pattern B — Subclass AIAgent (for testing orchestration logic)

Because `AIAgent` is abstract (not sealed), you can create a `FakeAIAgent` in tests:

```csharp
internal sealed class FakeAIAgent(string responseText) : AIAgent
{
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken ct)
        => new(new FakeAgentSession());

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages, AgentSession? session,
        AgentRunOptions? options, CancellationToken ct)
        => Task.FromResult(new AgentResponse(new ChatResponse([new(ChatRole.Assistant, responseText)])));

    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(...)
        => throw new NotImplementedException();

    // Serialization stubs omitted
}
```

#### Pattern C — Extract an orchestrator interface (most testable for service layer)

The most testable design for `PromptBabblerAgentOrchestrator` is to extract an interface:

```csharp
public interface IPromptBabblerAgentOrchestrator
{
    Task<string> GeneratePromptAsync(string babbleText, CancellationToken ct = default);
}

public sealed class PromptBabblerAgentOrchestrator(AIAgent agent) : IPromptBabblerAgentOrchestrator
{
    public async Task<string> GeneratePromptAsync(string babbleText, CancellationToken ct = default)
    {
        var session = await agent.CreateSessionAsync(ct);
        var response = await agent.RunAsync(babbleText, session, cancellationToken: ct);
        return response.Text;
    }
}
```

Unit tests mock `IPromptBabblerAgentOrchestrator` (pure interface mock). Integration tests use Pattern A (fake `IChatClient`).

### MAF Samples — No Test Projects Found

The `dotnet/samples` directory in `microsoft/agent-framework` contains no dedicated test projects. The framework does not ship unit test samples for agents. Pattern A (fake `IChatClient`) is the closest to idiomatic .NET AI testing per `Microsoft.Extensions.AI` conventions.

---

## Key References

- `dotnet/src/Microsoft.Agents.AI.Abstractions/AIAgent.cs` — abstract base, RunAsync/RunCoreAsync signatures
- `dotnet/src/Microsoft.Agents.AI.Abstractions/DelegatingAIAgent.cs` — decorator pattern
- `dotnet/src/Microsoft.Agents.AI/ChatClient/ChatClientAgent.cs` — sealed concrete impl
- `dotnet/src/Microsoft.Agents.AI/AnonymousDelegatingAIAgent.cs` — anonymous delegate wrapper
- `dotnet/samples/05-end-to-end/AspNetAgentAuthorization/Service/Program.cs` — Singleton registration with official comment
- `dotnet/extensions/src/Libraries/Microsoft.Extensions.AI/ChatCompletion/FunctionInvokingChatClient.cs` — CT propagation to `InvokeFunctionAsync`

---

## Clarifying Questions

None — all three research questions answered with source evidence.
