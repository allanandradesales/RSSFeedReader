# 📡 RSSFeedReader

> A local-first RSS/Atom feed reader for desktop, built entirely using **Spec-Driven Development (SDD)** with GitHub Spec Kit and Claude Code.

---

## ✨ Features

| Feature | Status |
|---|---|
| Add feed subscriptions by URL (RSS 2.0 + Atom 1.0) | ✅ |
| View articles sorted newest-first | ✅ |
| Unread article badge per feed | ✅ |
| Mark articles as read/unread | ✅ |
| Export subscriptions to OPML 2.0 | ✅ |
| Full offline support (SQLite local storage) | ✅ |
| SSRF protection + HTML sanitization | ✅ |

---

## 🏗️ Architecture

Built with **Clean Architecture** — strict layer separation with one-way dependencies:

```
Presentation (.NET MAUI)
    ↓
Application (Use Cases, Commands, Queries)
    ↓
Domain (Entities, Interfaces)
    ↓
Infrastructure (EF Core SQLite, FeedFetcher, HtmlSanitizer)
```

### Tech Stack

- **Runtime:** .NET 8 (LTS)
- **UI:** .NET MAUI (cross-platform desktop)
- **Database:** SQLite + Entity Framework Core 8
- **Feed parsing:** System.ServiceModel.Syndication
- **HTML sanitization:** HtmlSanitizer 9 (allowlist-based)
- **Testing:** xUnit + Moq + in-memory SQLite
- **CI:** GitHub Actions (build → lint → test on every PR)

---

## 🔒 Security

- **SSRF Guard** — blocks loopback, private IP ranges (10.x, 172.16.x, 192.168.x), link-local and CGNAT ranges before any HTTP request
- **HTML Sanitization** — all feed content stripped of scripts, event handlers and tracking pixels before rendering
- **HTTPS only** — self-signed certificates rejected, TLS 1.2+ enforced
- **URL validation** — length limits, scheme whitelist, duplicate detection

---

## 🧪 Test Coverage

```
59 tests passing across 3 test projects

Domain:         8 tests  — entities, value objects
Application:   13 tests  — use case handlers, command validation  
Infrastructure: 38 tests  — repositories, feed fetcher, SSRF guard, sanitizer
```

All tests run in CI on every pull request via GitHub Actions.

---

## 🚀 How This Was Built — Spec-Driven Development

This project was built using **[GitHub Spec Kit](https://github.com/github/spec-kit)** + **Claude Code** following the SDD methodology:

### The process

```
1. specify init          →  Project scaffolded
2. /speckit.constitution →  5 governing principles established
3. /speckit.specify      →  Spec generated from stakeholder docs (33 FRs, 5 user stories)
4. /speckit.plan         →  Technical plan + 9 artifacts (data model, contracts, research)
5. /speckit.tasks        →  52 tasks across 8 phases
6. /speckit.implement    →  MVP implemented in 22 minutes, 23/23 tests passing
```

### Why SDD?

Traditional development treats specs as scaffolding — written once and abandoned. SDD makes the **specification the source of truth**. The code is just its expression.

Benefits experienced in this project:
- Zero ambiguity between stakeholder requirements and implementation
- Constitution enforced `TreatWarningsAsErrors` and SonarAnalyzer from day one — no tech debt
- Auto-fix loop resolved CA1000, S3267 and CA1861 during implementation
- Adding new features (badge, OPML) followed the same repeatable SDD cycle

### Branches

| Branch | Feature | Tests |
|---|---|---|
| `001-mvp-rss-reader` | Core MVP (subscribe, view, refresh) | 23 ✅ |
| `002-unread-badge` | Unread article count badge | 36 ✅ |
| `003-opml-export` | OPML 2.0 export | ✅ |

---

## 📦 Getting Started

### Prerequisites

- .NET 8 SDK
- macOS 12+ or Windows 10+
- MAUI workload: `dotnet workload install maui`

### Run

```bash
git clone https://github.com/allanandradesales/RSSFeedReader.git
cd RSSFeedReader
dotnet restore
dotnet build
dotnet run --project src/RSSFeedReader.Presentation
```

### Test

```bash
dotnet test
```

---

## 📁 Project Structure

```
RSSFeedReader/
├── src/
│   ├── RSSFeedReader.Domain/          # Entities, interfaces
│   ├── RSSFeedReader.Application/     # Use cases, commands, queries
│   ├── RSSFeedReader.Infrastructure/  # EF Core, HTTP, sanitizer
│   └── RSSFeedReader.Presentation/    # .NET MAUI UI
├── tests/
│   ├── RSSFeedReader.Domain.Tests/
│   ├── RSSFeedReader.Application.Tests/
│   └── RSSFeedReader.Infrastructure.Tests/
├── specs/                             # SDD artifacts (spec, plan, tasks)
│   ├── 001-mvp-rss-reader/
│   ├── 002-unread-badge/
│   └── 003-opml-export/
└── StakeholderDocuments/              # Original requirements
```

---

## 📄 License

MIT

---

<p align="center">Built with ❤️ using Spec-Driven Development · GitHub Spec Kit · Claude Code</p>
