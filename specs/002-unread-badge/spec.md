# Feature Specification: Unread Article Count Badge

**Feature Branch**: `002-unread-badge`
**Created**: 2026-02-27
**Status**: Draft
**Input**: User description: "Add unread article count badge to each feed in the subscription list. Badge shows number of unread articles, hidden when count is zero. Updates in real-time after refresh or marking articles as read."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — See Which Feeds Have Unread Content (Priority: P1)

A user opens the subscription list and immediately knows which feeds have unread articles, and exactly how many, without opening any feed or performing any action. Feeds with no unread content look clean with no badge. Feeds with unread articles show a clear numeric count.

**Why this priority**: This is the core value of the feature — at-a-glance awareness of unread content. Without this, the badge has no purpose.

**Independent Test**: Can be fully tested by opening the app with a mix of feeds (some with unread articles, some without) and verifying that badges appear only on feeds with unread content and show the correct count.

**Acceptance Scenarios**:

1. **Given** the user has two subscribed feeds, one with 3 unread articles and one with 0 unread articles, **When** the subscription list is displayed, **Then** a badge with the number "3" appears on the first feed and no badge appears on the second feed.
2. **Given** all subscribed feeds have 0 unread articles, **When** the subscription list is displayed, **Then** no badges are visible on any feed entry.
3. **Given** a feed has 47 unread articles, **When** the subscription list is displayed, **Then** the badge shows the number "47".

---

### User Story 2 — Badge Updates After Feed Refresh (Priority: P2)

A user manually triggers a feed refresh. Once the refresh completes, the badge on that feed immediately shows the updated unread count — including any newly fetched articles — without the user navigating away and back or doing anything extra.

**Why this priority**: If the badge only reflects the count from when the app was opened, it loses trust. Users need confidence the count is current after they explicitly refresh.

**Independent Test**: Can be tested by refreshing a feed that has new articles available and observing the badge count change in the same session without any additional navigation.

**Acceptance Scenarios**:

1. **Given** a feed displays a badge of "2", **When** the user refreshes the feed and 5 new articles are added, **Then** the badge updates to "7" immediately after the refresh completes.
2. **Given** a feed displays a badge of "2", **When** the user refreshes and no new articles are found, **Then** the badge remains "2".
3. **Given** a feed has no badge (0 unread), **When** the user refreshes and 3 new articles are added, **Then** a badge with "3" appears on that feed.

---

### User Story 3 — Badge Updates After Marking Articles as Read (Priority: P3)

As a user reads or marks articles as read, the badge on the corresponding feed decrements in real-time. When the last unread article is marked as read, the badge disappears entirely — all within the same interaction, with no additional steps required.

**Why this priority**: Accurate post-reading state matters for a clean experience, but P1 and P2 deliver value independently. Included because the feature description explicitly calls for it.

**Independent Test**: Can be tested by marking individual articles as read and confirming the badge count on the parent feed decrements correctly, reaching zero and disappearing when all are read.

**Acceptance Scenarios**:

1. **Given** a feed shows a badge of "5", **When** the user marks one article as read, **Then** the badge updates to "4" immediately.
2. **Given** a feed shows a badge of "1", **When** the user marks the last unread article as read, **Then** the badge disappears from that feed entry.
3. **Given** a feed has no badge, **When** the user marks an already-read article as unread (toggle), **Then** a badge with "1" appears on that feed.

---

### Edge Cases

- What if a feed is removed from the subscription list? The feed entry is removed entirely; no orphan badge remains.
- What if a refresh fails partway through (e.g., network error)? The badge count stays unchanged from before the refresh attempt.
- What if the app is restarted after marking articles as read? Badges reflect the persisted read state — counts do not reset on restart.
- What if multiple feeds are refreshed in sequence? Each feed's badge updates independently as its own refresh completes; feeds not yet refreshed are unaffected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each feed entry in the subscription list MUST display a numeric badge showing the count of unread articles when that count is one or more.
- **FR-002**: The badge MUST NOT be displayed when a feed's unread article count is zero.
- **FR-003**: The badge MUST show the exact integer count of unread articles (e.g., "1", "42", "150").
- **FR-004**: After a manual feed refresh completes successfully, the badge for that feed MUST reflect the updated unread count — including any newly added articles — without requiring additional user interaction.
- **FR-005**: After an article is marked as read, the badge count on the corresponding feed MUST decrement by one immediately.
- **FR-006**: After an article is marked as unread (toggled back), the badge count on the corresponding feed MUST increment by one immediately, or appear if it was previously hidden.
- **FR-007**: When the unread count for a feed reaches zero, the badge MUST disappear from that feed entry without requiring navigation or reload.
- **FR-008**: Badge counts for different feeds MUST be independent — a change to one feed's badge MUST NOT affect any other feed's badge.

### Key Entities

- **Feed Subscription**: A source of articles the user follows. Each subscription independently tracks how many of its articles the user has not yet read.
- **Article Read Status**: A per-article flag indicating whether the user has marked the article as read. The unread count for a feed is the number of its articles where this flag is unset.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can identify all feeds with unread content at a glance, in under 2 seconds of the list appearing, without opening any feed or performing any action.
- **SC-002**: Badge counts are always accurate — they match the actual number of unread articles for each feed with no discrepancy at any point during normal use.
- **SC-003**: Badge count changes (increment, decrement, appear, disappear) are visible within the same user interaction that triggered them, with no additional user steps required.
- **SC-004**: The subscription list contains no badge elements for feeds with zero unread articles — the list remains visually uncluttered.
- **SC-005**: Badge state persists correctly across app restarts — counts reflect the actual persisted read status rather than resetting to zero.

## Assumptions

- "Real-time" means within the same UI interaction as the triggering event (feed refresh completion or mark-as-read confirmation). Live server-push updates are out of scope.
- Unread count is based on articles explicitly marked as read by the user. Auto-marking articles as read upon opening them is out of scope for this feature.
- The badge always shows the exact integer count with no upper truncation (e.g., 1024 is shown as "1024", not "999+").
- The feature applies to the subscription list view only. Badges in other views (e.g., article detail) are out of scope.
