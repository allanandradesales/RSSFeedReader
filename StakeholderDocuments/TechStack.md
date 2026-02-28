# RSSFeedReader — Technical Stack

## Runtime & Framework
- **Runtime:** .NET 8 (LTS)
- **UI Framework:** Blazor WebAssembly or .NET MAUI (cross-platform desktop)
- **Target platforms:** Windows 10+, macOS 12+

**Rationale:** .NET 8 provides long-term support, strong cross-platform capabilities, and a mature ecosystem for both desktop UI and local data access.

---

## Data Storage
- **Database:** SQLite (local file, no server required)
- **ORM:** Entity Framework Core 8 with SQLite provider
- **Migration strategy:** EF Core migrations applied on startup

**Rationale:** SQLite is ideal for local-first desktop applications — zero configuration, single file, ACID compliant, and widely supported. Entity Framework Core provides type-safe database access with automatic migration support.

---

## Feed Parsing
- **Library:** `System.ServiceModel.Syndication` (built into .NET)
- **Supported formats:** RSS 2.0, Atom 1.0
- **HTTP client:** `System.Net.Http.HttpClient` with named client configuration

**Rationale:** `System.ServiceModel.Syndication` is a battle-tested, zero-dependency RSS/Atom parser included in the .NET standard library.

---

## Security
- **HTML sanitization:** HtmlSanitizer (NuGet package)
  - Strip all `<script>` tags and event attributes
  - Strip external image tracking pixels
  - Allow safe tags: `<p>`, `<a>`, `<img>`, `<ul>`, `<ol>`, `<li>`, `<h1>`–`<h6>`, `<blockquote>`, `<code>`, `<pre>`
- **SSRF prevention:**
  - Reject private IP ranges (10.x, 172.16.x, 192.168.x, 127.x, ::1)
  - Reject non-HTTP/HTTPS schemes
  - Enforce HTTPS-only for feed fetching
- **TLS:** Only TLS 1.2+ accepted; self-signed certificates rejected

---

## Architecture Pattern
- **Pattern:** Clean Architecture with strict layer separation
  - `Domain` — entities (Feed, Article), value objects, interfaces
  - `Application` — use cases, service interfaces, DTOs
  - `Infrastructure` — EF Core repositories, HttpClient feed fetcher, HTML sanitizer
  - `Presentation` — UI components, view models
- **Dependency rule:** outer layers depend on inner layers; never the reverse
- **Dependency injection:** Microsoft.Extensions.DependencyInjection (built-in)

---

## Testing
- **Unit testing:** xUnit
- **Mocking:** Moq or NSubstitute
- **Integration testing:** TestContainers (if needed for DB tests) or in-memory SQLite
- **Coverage target:** ≥ 80% for Application and Domain layers
- **Test naming convention:** `MethodName_StateUnderTest_ExpectedBehavior`

---

## Code Quality
- **Linter/formatter:** dotnet-format (enforced in CI)
- **Static analysis:** Roslyn analyzers + SonarAnalyzer.CSharp
- **Nullable reference types:** enabled globally
- **Code style:** Microsoft C# coding conventions
- **Documentation:** XML doc comments required on all public APIs

---

## Build & CI
- **Build system:** dotnet CLI
- **CI pipeline:** GitHub Actions
  - On pull request: build, lint, test, coverage report
  - On merge to main: build, test, publish artifacts
- **Branch strategy:** feature branches → PR → main

---

## Key Constraints
- No cloud services or external APIs in MVP (fully local)
- No user accounts or authentication in MVP
- Application must function without internet after initial feed fetch
- All feed content must be sanitized before any rendering
- Feed fetch operations must not block the UI thread
