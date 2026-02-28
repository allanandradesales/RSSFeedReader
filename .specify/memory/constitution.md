<!--
Sync Impact Report
==================
Version change: 1.0.0 → 1.1.0 (MINOR — all principles materially expanded with concrete
tech-specific details; all deferred TODOs resolved from stakeholder documents)

Modified principles:
  - I. Security by Design → replaced generic rules with HtmlSanitizer, specific SSRF IP
    ranges, TLS 1.2+, and self-signed certificate rejection from TechStack.md
  - II. Maintainability & Clean Architecture → replaced generic layer description with
    concrete Domain/Application/Infrastructure/Presentation layer names and DI tooling
  - III. Code Quality Standards → replaced generic lint/review rules with dotnet-format,
    Roslyn analyzers, SonarAnalyzer.CSharp, nullable reference types, XML doc comments
  - IV. Reliability & Resilience → added concrete 10s timeout, 2s startup targets,
    UI-thread non-blocking requirement from ProjectGoals.md and AppFeatures.md
  - V. Testable by Default → added xUnit, Moq/NSubstitute, in-memory SQLite, ≥80%
    coverage target, and test naming convention from TechStack.md

Added sections: N/A
Removed sections: N/A

Templates checked:
  - .specify/templates/plan-template.md ✅ aligned (Constitution Check section compatible)
  - .specify/templates/spec-template.md ✅ aligned (FR/SC structure compatible)
  - .specify/templates/tasks-template.md ✅ aligned (security hardening task present)

Deferred items resolved:
  - ✅ TODO(PROJECT_GOALS): resolved via StakeholderDocuments/ProjectGoals.md
  - ✅ TODO(APP_FEATURES): resolved via StakeholderDocuments/AppFeatures.md
  - ✅ TODO(TECH_STACK): resolved via StakeholderDocuments/TechStack.md

Remaining deferred items: none
-->

# RSSFeedReader Constitution

## Core Principles

### I. Security by Design

All code MUST treat external RSS/Atom feed content as untrusted input. Feed URLs MUST be
validated before any HTTP request: reject non-HTTP/HTTPS schemes, reject private IP ranges
(10.x, 172.16.x, 192.168.x, 127.x, ::1), and reject URLs exceeding 2048 characters.
HTTPS-only MUST be enforced for all outbound feed requests; HTTP-only feeds MUST surface
a visible security warning and require explicit user opt-in. TLS 1.2+ is the minimum
accepted; self-signed certificates MUST be rejected. All HTML content from feed entries
MUST be sanitized using `HtmlSanitizer` (NuGet) before rendering — stripping `<script>`
tags, event handler attributes (`on*`), `javascript:` hrefs, and external tracking pixels;
only the approved safe-tag allowlist may pass through. No credentials, secrets, or API
keys MUST ever appear in source code, logs, error messages, or user-visible stack traces.
Security controls are non-negotiable and MUST NOT be bypassed under time pressure.

**Rationale**: RSS readers consume arbitrary third-party content, making them a high-risk
surface for SSRF, XSS injection, and credential leakage. A single compromised feed can
affect every subscriber. The desktop/local-first nature of this app means security
failures directly impact the user's machine.

### II. Maintainability & Clean Architecture

The codebase MUST follow Clean Architecture with four strict layers:
- **Domain** — entities (`Feed`, `Article`), value objects, and repository/service interfaces
- **Application** — use cases, DTOs, and service interfaces (no infrastructure references)
- **Infrastructure** — EF Core repositories, `HttpClient` feed fetcher, `HtmlSanitizer` adapter
- **Presentation** — UI components and view models

The dependency rule is absolute: outer layers MUST depend on inner layers; inner layers
MUST NEVER reference outer layers or infrastructure concerns. Dependency injection MUST be
wired via `Microsoft.Extensions.DependencyInjection`; no service locator pattern. Each
class MUST have a single, documented responsibility. A developer MUST be able to modify
any one layer without reading the internals of any other.

**Rationale**: A maintainable architecture reduces the cost of change as the feature set
grows (OPML import, cloud sync, mobile support) and lowers onboarding friction for new
contributors.

### III. Code Quality Standards

All code MUST pass `dotnet-format` formatting and Roslyn analyzer checks
(including `SonarAnalyzer.CSharp`) before merging — enforced as CI gates on every pull
request; analyzer suppressions require an inline justification comment. Nullable reference
types MUST be enabled globally (`<Nullable>enable</Nullable>`); null warnings MUST be
resolved, not suppressed. XML documentation comments (`/// <summary>`) are REQUIRED on
all public APIs, methods, and properties. All code MUST follow Microsoft C# coding
conventions; magic numbers and hardcoded strings MUST be replaced with named constants or
configuration values. Every pull request MUST be reviewed by at least one contributor
before merging to main. Dead code MUST be removed, not commented out.

**Rationale**: Consistent, automatically enforced quality standards reduce cognitive load,
prevent subtle null-reference bugs, and ensure the codebase remains readable as the team
and feature set evolve.

### IV. Reliability & Resilience

A hard timeout of **10 seconds per feed** MUST be enforced on every outbound
`HttpClient` request — no unbounded waits. Feed-fetching operations MUST run
asynchronously and MUST NOT block the UI thread. Individual feed failures MUST NOT halt
or crash processing of other feeds; errors MUST be surfaced per-feed in the UI while
remaining feeds continue refreshing. The application MUST start and display cached
content within **2 seconds** even when offline. New articles MUST be deduplicated by
GUID or link URL before insertion. HTTP redirects (301/302) MUST be followed, with the
final URL saved as the canonical feed URL. Failure conditions MUST be logged with enough
diagnostic context to reproduce, without exposing sensitive user data.

**Rationale**: RSS feeds are external, unreliable resources outside the project's control.
Users expect the application to remain functional and responsive even when upstream sources
fail intermittently or the device is offline.

### V. Testable by Default

Every class in the **Domain** and **Application** layers MUST be designed for independent
unit testing without live network calls or a real database — achieved through interfaces
and constructor-injected dependencies (Moq or NSubstitute for test doubles). Unit tests
MUST cover all feed parsing logic, all `HtmlSanitizer` integration points, all SSRF
validation rules, and all use-case business logic. Integration tests MUST use in-memory
SQLite (or TestContainers if needed) to cover the full fetch → parse → store → display
pipeline. Test coverage for **Application** and **Domain** layers MUST remain at or above
**80%**. All tests MUST be named using the convention
`MethodName_StateUnderTest_ExpectedBehavior`. Tests MUST run and pass in GitHub Actions
on every pull request; no merge to main is permitted with failing tests.

**Rationale**: Testability enforces the Clean Architecture principle (untestable code is a
layering violation) and provides a regression safety net for future features like OPML
import, cloud sync, and new feed formats.

## Security Constraints

Concrete library and configuration requirements (all from `TechStack.md`):

- **HTML sanitization**: `HtmlSanitizer` NuGet package. Allowed tags: `<p>`, `<a>`,
  `<img>`, `<ul>`, `<ol>`, `<li>`, `<h1>`–`<h6>`, `<blockquote>`, `<code>`, `<pre>`.
  All other tags, all event attributes, and all `javascript:` hrefs are stripped.
- **SSRF prevention**: reject private IP ranges (10.x, 172.16.x–172.31.x, 192.168.x,
  127.x, ::1) and non-HTTP/HTTPS schemes before issuing any `HttpClient` request.
- **TLS**: minimum TLS 1.2; `ServerCertificateCustomValidationCallback` MUST NOT be
  overridden to accept self-signed certificates.
- **HTTPS enforcement**: HTTP-only feed URLs trigger a visible warning and require
  explicit user confirmation; they are not silently upgraded.
- **Secrets**: no credentials, API keys, or tokens in source code or committed config
  files. Use environment variables or OS secret stores.
- **Dependencies**: all NuGet packages MUST be pinned to exact versions and audited for
  known CVEs before merging new packages.

## Development Workflow

- All features MUST be developed on named feature branches and merged via pull request.
- Pull requests MUST pass all CI gates (build, `dotnet-format`, Roslyn analyzers, xUnit
  tests, coverage threshold) before human review begins.
- Every pull request MUST reference a spec (`/specs/`) or task ticket.
- The **Constitution Check** section in each `plan-template.md` MUST be completed and
  signed off before Phase 0 research begins on any feature.
- Breaking changes to public APIs, EF Core entity shapes, or SQLite schema MUST include
  a migration note and, if applicable, an EF Core migration file.
- The `main` branch MUST always be in a buildable, passing-tests state.
- GitHub Actions runs on every PR: build → lint → test → coverage report.
  On merge to main: build → test → publish artifacts.

## Governance

This constitution supersedes all informal coding conventions and undocumented team habits.
Amendments require:

1. A written rationale explaining why the current principle is insufficient or incorrect.
2. Review and explicit approval from at least one additional project contributor.
3. A migration plan if any existing code violates the amended principle.

**Versioning policy**: Semantic versioning applies —
- MAJOR: principle removals, redefinitions, or backward-incompatible governance changes.
- MINOR: new principles or sections added, or material expansion of existing guidance.
- PATCH: clarifications, wording improvements, typo fixes, non-semantic refinements.

**Compliance review**: The Constitution Check section in each implementation plan
(`plan-template.md`) is the per-feature enforcement gate. Any plan unable to satisfy a
principle MUST document the violation in the Complexity Tracking table with explicit
justification. Unjustified violations block plan approval.

**Runtime guidance**: See `.specify/` directory for command templates and agent-specific
guidance files. Consult `.specify/memory/` for living project memory artifacts.

**Version**: 1.1.0 | **Ratified**: 2026-02-27 | **Last Amended**: 2026-02-27
