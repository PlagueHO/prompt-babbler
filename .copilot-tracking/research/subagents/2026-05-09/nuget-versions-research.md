# NuGet Package Versions Research

**Date:** 2026-05-09
**Status:** Complete

## Research Questions

1. What is the latest stable version of `Microsoft.Agents.AI`?
1. What is the latest stable version of `Microsoft.Agents.AI.Foundry`?
1. What is the latest stable version of `Azure.AI.Projects`?
1. What version do the agent-framework dotnet samples reference?

## Sources Checked

- <https://www.nuget.org/packages/Microsoft.Agents.AI>
- <https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry>
- <https://www.nuget.org/packages/Azure.AI.Projects>
- <https://github.com/microsoft/agent-framework/releases>
- <https://raw.githubusercontent.com/microsoft/agent-framework/main/dotnet/samples/02-agents/AgentsWithFoundry/Agent_Step03_UsingFunctionTools/Agent_Step03_UsingFunctionTools.csproj>

---

## Findings

### Microsoft.Agents.AI

| Field         | Value                                           |
|---------------|-------------------------------------------------|
| Package ID    | `Microsoft.Agents.AI`                           |
| Latest stable | **1.5.0**                                       |
| Released      | ~2026-05-08 ("a day ago" as of 2026-05-09)      |
| Source        | <https://www.nuget.org/packages/Microsoft.Agents.AI/1.5.0> |

NuGet page header confirms: `Microsoft.Agents.AI 1.5.0` is the current stable version.
Download link: `https://www.nuget.org/api/v2/package/Microsoft.Agents.AI/1.5.0`

### Microsoft.Agents.AI.Foundry

| Field         | Value                                                    |
|---------------|----------------------------------------------------------|
| Package ID    | `Microsoft.Agents.AI.Foundry`                            |
| Latest stable | **1.5.0**                                                |
| Released      | ~2026-05-08 ("a day ago" as of 2026-05-09)               |
| Source        | <https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry/1.5.0> |

NuGet page header confirms: `Microsoft.Agents.AI.Foundry 1.5.0` is the current stable version.
Download link: `https://www.nuget.org/api/v2/package/Microsoft.Agents.AI.Foundry/1.5.0`

### Azure.AI.Projects

| Field         | Value                                                 |
|---------------|-------------------------------------------------------|
| Package ID    | `Azure.AI.Projects`                                   |
| Latest stable | **2.0.1**                                             |
| Released      | ~2026-04-24 ("15 days ago" as of 2026-05-09)          |
| Source        | <https://www.nuget.org/packages/Azure.AI.Projects/2.0.1> |

NuGet page header confirms: `Azure.AI.Projects 2.0.1` is the current stable version.
Note: NuGet page indicates "There is a newer prerelease version of this package available." — meaning 2.0.1 is the latest **stable/GA** release; there exists a newer prerelease beyond it.
Download link: `https://www.nuget.org/api/v2/package/Azure.AI.Projects/2.0.1`

---

## GitHub Releases Confirmation

From <https://github.com/microsoft/agent-framework/releases>:

- **Latest dotnet release tag:** `dotnet-1.5.0` (published yesterday ~2026-05-08, marked **Latest**)
- This confirms `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Foundry` at version **1.5.0**

Previous dotnet tags visible on the releases page:

- `dotnet-1.4.0` — 4 days ago (~2026-05-05)
- `dotnet-1.3.0` — 2 weeks ago (~2026-04-25)
- `dotnet-1.2.0` — 3 weeks ago (~2026-04-18)

---

## Sample Project File

File: `dotnet/samples/02-agents/AgentsWithFoundry/Agent_Step03_UsingFunctionTools/Agent_Step03_UsingFunctionTools.csproj`

The sample uses a **ProjectReference** (local source reference), NOT a NuGet package reference:

```xml
<ItemGroup>
  <ProjectReference
    Include="..\..\..\..\src\Microsoft.Agents.AI.Foundry\Microsoft.Agents.AI.Foundry.csproj" />
</ItemGroup>
```

No explicit version number is encoded in this sample's project file — it pulls directly from source.

---

## Summary Table

| Package ID                    | Latest Stable Version | Released (~)  |
|-------------------------------|-----------------------|---------------|
| `Microsoft.Agents.AI`         | **1.5.0**             | 2026-05-08    |
| `Microsoft.Agents.AI.Foundry` | **1.5.0**             | 2026-05-08    |
| `Azure.AI.Projects`           | **2.0.1**             | 2026-04-24    |

---

## Caveats

1. `Azure.AI.Projects` has a newer **prerelease** beyond 2.0.1 available on NuGet. If prerelease is acceptable, a higher version may exist. 2.0.1 is the current **stable GA** version.
1. The release dates for `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Foundry` are approximated from the NuGet "last updated" relative timestamp ("a day ago") as of 2026-05-09.
1. The agent-framework sample `.csproj` does not pin a NuGet version — it uses a local project reference, so it tracks source directly.
