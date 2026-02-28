# RSSFeedReader — App Features

## Feature 1: Add Feed Subscription
Users can subscribe to an RSS or Atom feed by providing its URL.

**User Story:**
As a user, I want to add a feed by entering its URL so that I can follow content from my favorite sites.

**Acceptance Criteria:**
- User can enter a URL in an input field and submit it
- System validates that the URL is well-formed (proper URI format)
- System fetches the feed to confirm it is a valid RSS 2.0 or Atom 1.0 feed
- If valid, the feed is saved and appears in the subscription list
- If invalid (bad URL format, unreachable, or not a feed), a clear error message is shown
- Duplicate feed URLs are rejected with an appropriate message
- Input field is cleared after a successful submission

**Edge Cases:**
- URL with missing scheme (e.g. "example.com" instead of "https://example.com") → error
- Feed URL that redirects (301/302) → follow redirect and save final URL
- Feed that returns HTTP 404 or 500 → display error, do not save
- Feed with self-signed certificate → reject, show security warning
- Very long URL (> 2048 characters) → reject with validation error

---

## Feature 2: View Subscription List
Users can see all feeds they have subscribed to.

**User Story:**
As a user, I want to see a list of all my subscriptions so that I can manage them and navigate to their articles.

**Acceptance Criteria:**
- Subscription list displays feed title (fetched from feed metadata) and URL
- List is ordered alphabetically by feed title
- If no subscriptions exist, an empty state message is shown (e.g. "No subscriptions yet. Add a feed to get started.")
- Clicking a subscription opens its article list

---

## Feature 3: Refresh Feeds
Users can manually trigger a refresh to fetch new articles from all subscribed feeds.

**User Story:**
As a user, I want to refresh my feeds so that I can read the latest articles.

**Acceptance Criteria:**
- A "Refresh All" button triggers fetching of all subscribed feeds
- New articles are added to the local database without duplicating existing ones
- Articles are deduplicated by their GUID or link URL
- A loading indicator is shown during refresh
- If a feed fails to refresh, the error is shown per-feed and other feeds continue refreshing
- Refresh respects a 10-second per-feed timeout
- Last refresh timestamp is shown in the UI

---

## Feature 4: Read Articles
Users can browse and read articles from their subscribed feeds.

**User Story:**
As a user, I want to view articles from my subscriptions sorted newest-first so that I can catch up on recent content.

**Acceptance Criteria:**
- Articles are displayed sorted by publication date descending (newest first)
- Each article shows: title, feed name, publication date, and a summary/description snippet
- Clicking an article opens its full content
- Article content is rendered as HTML with sanitization (no scripts, no external tracking pixels)
- A link to the original article on the web is provided
- If an article has no content, a "No content available" message is shown

---

## Feature 5: Track Read/Unread Status
Users can track which articles they have already read.

**User Story:**
As a user, I want to mark articles as read so that I can focus on content I haven't seen yet.

**Acceptance Criteria:**
- Articles are displayed as "unread" by default
- Opening an article automatically marks it as read
- Unread articles are visually distinct from read articles (e.g. bold title)
- Read/unread status persists across application restarts
- User can manually toggle read/unread status on any article
- A count of unread articles is shown per feed in the subscription list

---

## Feature 6: Remove Subscription
Users can unsubscribe from a feed they no longer want to follow.

**User Story:**
As a user, I want to remove a feed subscription so that I stop seeing its articles.

**Acceptance Criteria:**
- Each subscription has a "Remove" or "Unsubscribe" action
- A confirmation prompt is shown before removal
- Upon confirmation, the feed and all its articles are removed from the local database
- The subscription list updates immediately after removal
