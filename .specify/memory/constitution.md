<!--
  Sync Impact Report
  ==================================================
  Version change: 1.0.0 → 1.1.0
  Bump rationale: MINOR — new principle added (VII. Azure-First
  & Cost Optimization).

  Modified principles: None renamed or redefined.

  Added sections:
    - Core Principle VII. Azure-First & Cost Optimization

  Removed sections: N/A

  Templates requiring updates:
    - .specify/templates/plan-template.md        ✅ compatible (dynamic Constitution Check)
    - .specify/templates/spec-template.md         ✅ compatible (no changes needed)
    - .specify/templates/tasks-template.md        ✅ compatible (no changes needed)
    - .specify/templates/checklist-template.md    ✅ compatible (no changes needed)
    - .specify/templates/agent-file-template.md   ✅ compatible (no changes needed)

  Follow-up TODOs: None
  ==================================================
-->

# Prompt Babbler Constitution

## Core Principles

### I. Simplicity & YAGNI

- Every feature, abstraction, and dependency MUST justify its existence
  with a concrete, immediate requirement.
- Do NOT add code, configuration, or architecture "just in case."
- When two designs satisfy the same requirement, the simpler one MUST
  be chosen unless measurable evidence proves otherwise.
- Premature optimization is prohibited. Optimize only when profiling
  data demonstrates a bottleneck.
- Rationale: Unnecessary complexity is the primary source of defects,
  slow onboarding, and maintenance burden.

### II. Clean Code & Readability

- Code MUST be self-documenting: intention-revealing names, small
  focused functions, and clear control flow.
- Comments MUST explain *why*, never *what*. If a comment restates the
  code, the code MUST be rewritten for clarity instead.
- The DRY principle MUST be followed: duplicated logic MUST be
  extracted into shared, well-named abstractions. However, premature
  abstraction that sacrifices clarity violates Principle I.
- SOLID principles MUST guide class and module design:
  - **Single Responsibility**: each module/class owns exactly one reason
    to change.
  - **Open/Closed**: extend behavior through composition or new modules,
    not by modifying existing stable code.
  - **Liskov Substitution**: subtypes MUST be substitutable for their
    base types without altering correctness.
  - **Interface Segregation**: consumers MUST NOT depend on methods they
    do not use.
  - **Dependency Inversion**: high-level modules MUST depend on
    abstractions, not concrete implementations.
- Formatting MUST be enforced by automated tooling (linters,
  formatters) configured in the repository.
- Rationale: Readable code reduces review time, eases debugging, and
  enables confident refactoring.

### III. Modularity & Library-First

- Every feature MUST start as a standalone, self-contained module or
  library with a clear single purpose.
- Modules MUST be independently testable and documented.
- No "organizational-only" modules: a module that exists solely to
  group code without a distinct functional boundary MUST be
  eliminated or merged.
- Public API surfaces MUST be minimal and intentional. Internal
  implementation details MUST NOT leak through public interfaces.
- Circular dependencies between modules are prohibited.
- Rationale: Modular design enables parallel development, independent
  testing, and safe replacement of components.

### IV. Test-First Development (NON-NEGOTIABLE)

- TDD cycle MUST be followed: write a failing test → implement the
  minimum code to pass → refactor. Red-Green-Refactor is mandatory.
- Tests MUST be written *before* production code for every new
  behavior, bug fix, or contract change.
- Each test MUST verify exactly one behavior and MUST have a
  descriptive name that documents the expected outcome.
- Test code MUST meet the same quality standards as production code:
  readable, maintainable, and free of duplication.
- Untested code MUST NOT be merged into the main branch.
- Rationale: Test-first design produces cleaner interfaces, catches
  regressions immediately, and serves as living documentation.

### V. Integration Testing Over Mocks

- Integration tests that exercise real component interactions MUST be
  the primary verification strategy.
- Mocks and stubs SHOULD be used only when interacting with external
  third-party services, non-deterministic systems (clocks, random),
  or to isolate genuine unit boundaries.
- Contract tests MUST be written for every public API boundary and
  inter-module interface.
- Test coverage MUST include: new module contracts, contract changes,
  inter-service communication, and shared data schemas.
- End-to-end or acceptance tests SHOULD cover critical user journeys
  identified in feature specifications.
- Rationale: Over-mocking hides integration defects. Real interaction
  tests provide higher confidence in production behavior.

### VI. Industry-Standard Dependencies Only

- All packages, frameworks, and libraries MUST be well-established,
  actively maintained, and widely adopted in the ecosystem.
- Niche, experimental, or low-adoption dependencies are prohibited
  unless no industry-standard alternative exists AND the dependency
  is explicitly approved and documented with justification.
- Dependency selection criteria:
  - Active maintenance (releases within the last 6 months).
  - Meaningful community adoption (downloads, stars, contributors).
  - Clear documentation and stable API surface.
  - Compatible open-source license.
- Direct dependencies MUST be pinned to specific versions.
  Transitive dependency updates MUST pass the full test suite.
- Rationale: Niche dependencies introduce supply-chain risk, limited
  community support, and potential abandonment.

### VII. Azure-First & Cost Optimization

- Azure MUST be the cloud platform for all hosted services,
  infrastructure, and managed resources. Alternative cloud providers
  MUST NOT be used unless Azure lacks a functionally equivalent
  service AND the exception is documented with justification.
- Every architecture and service selection decision MUST optimize for
  cost as a primary constraint, alongside functionality.
- Cost optimization requirements:
  - Choose consumption-based / serverless SKUs (e.g., Azure Functions
    Consumption plan, Azure SQL Serverless, Azure Cosmos DB
    serverless) over provisioned-capacity SKUs unless load profiles
    prove sustained utilization above the break-even threshold.
  - Use the lowest-cost tier that satisfies functional and
    performance requirements. Do NOT select premium tiers
    speculatively.
  - Prefer Azure-native PaaS and serverless services over IaaS.
    Virtual machines MUST NOT be used when a managed service can
    fulfill the same requirement.
  - Implement auto-scaling and scale-to-zero where supported.
  - Development and test environments MUST use free-tier or
    dev/test-priced SKUs. Production-grade resources in non-
    production environments are prohibited.
- Infrastructure MUST be defined as code (Bicep or Terraform) and
  stored in version control alongside the application.
- Azure service selection MUST prefer broadly adopted, GA (Generally
  Available) services. Preview services MUST NOT be used in
  production unless explicitly approved and documented.
- Managed identities MUST be used for Azure service-to-service
  authentication. Connection strings with embedded credentials are
  prohibited; use Azure Key Vault for any required secrets.
- Cost monitoring MUST be configured: Azure Cost Management budgets
  and alerts MUST be set for every resource group.
- Rationale: Azure-first standardizes operational expertise and
  tooling. Cost optimization prevents runaway spend and enforces
  disciplined resource selection aligned with Principle I
  (Simplicity & YAGNI).

## Development Standards

- **Language & framework choices** MUST favor ecosystem conventions
  and idiomatic patterns over custom abstractions.
- **Error handling** MUST be explicit. Silent failures and swallowed
  exceptions are prohibited. Errors MUST propagate with sufficient
  context for diagnosis.
- **Structured logging** MUST be used for all operational output.
  Logs MUST include correlation identifiers where applicable.
- **Configuration** MUST be externalized (environment variables,
  config files) and MUST NOT be hard-coded.
- **Secrets** MUST NEVER appear in source code, logs, or version
  control. Use secret management tooling appropriate to the platform.
- **Documentation**: Public APIs and modules MUST have usage
  documentation. Architecture decisions MUST be recorded as ADRs
  (Architecture Decision Records) when they affect multiple modules.

## Quality Gates & Workflow

- **All PRs** MUST pass automated linting, formatting checks, and the
  full test suite before merge.
- **Code review** is mandatory. Reviewers MUST verify alignment with
  this constitution's principles, especially Simplicity (I), Clean
  Code (II), and Test-First (IV).
- **Branch strategy**: Feature branches MUST be short-lived. Merge to
  main MUST occur through pull requests only.
- **Continuous integration** MUST run on every push and PR. Build
  failures MUST block merge.
- **Complexity justification**: Any addition that increases
  architectural complexity (new module, new dependency, new pattern)
  MUST be justified in the PR description against Principle I.
- **Definition of Done**: A feature is complete when all acceptance
  tests pass, documentation is updated, and the code satisfies every
  applicable principle in this constitution.

## Governance

- This constitution supersedes all other development practices and
  conventions. In case of conflict, the constitution prevails.
- **Amendments** require:
  1. A written proposal documenting the change and its rationale.
  1. Review and approval by the project maintainer(s).
  1. A migration plan for any existing code affected by the change.
  1. Version increment of this document per semantic versioning.
- **Versioning policy**:
  - MAJOR: Backward-incompatible governance or principle changes
    (removals, redefinitions).
  - MINOR: New principles, sections, or materially expanded guidance.
  - PATCH: Clarifications, wording, typo fixes, non-semantic edits.
- **Compliance review**: All PRs and code reviews MUST verify
  adherence to this constitution. Non-compliance MUST be flagged and
  resolved before merge.
- Refer to the project's agent-file-template for runtime-specific
  development guidance.

**Version**: 1.1.0 | **Ratified**: 2026-02-09 | **Last Amended**: 2026-02-09
