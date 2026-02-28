# Data Model: OPML Export

## No schema changes required

The SQLite schema from `InitialCreate` migration fully covers this feature. OPML export is read-only — it reads existing `Feed` records and writes a file to disk.

---

## Existing entity used (no changes)

### Feed

| Field   | Type   | Used in OPML output |
|---------|--------|---------------------|
| Id      | Guid   | Not included in OPML |
| Url     | string | `xmlUrl` attribute on each `<outline>` |
| Title   | string | `text` attribute on each `<outline>` |

All other Feed fields (`LastRefreshedAt`, `CreatedAt`, `Articles`) are ignored by the export.

---

## OPML 2.0 output format

```xml
<?xml version="1.0" encoding="UTF-8"?>
<opml version="2.0">
  <head>
    <title>RSS Feed Reader Subscriptions</title>
    <dateCreated>Thu, 27 Feb 2026 22:00:00 GMT</dateCreated>
  </head>
  <body>
    <outline type="rss" text="Tech News" xmlUrl="https://technews.example.com/rss" />
    <outline type="rss" text="Science Daily" xmlUrl="https://science.example.com/feed" />
  </body>
</opml>
```

Special characters in `text` or `xmlUrl` are automatically XML-escaped by `System.Xml.Linq.XAttribute`.

---

## File output

| Property | Value |
|----------|-------|
| Filename | `subscriptions.opml` |
| Location | Platform Downloads folder (`~/Downloads` on macOS, `%USERPROFILE%\Downloads` on Windows) |
| Encoding | UTF-8 without BOM |
| Conflict | Overwrite silently |
