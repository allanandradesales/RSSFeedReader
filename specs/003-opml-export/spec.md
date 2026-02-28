# Feature Specification: OPML Export

**Feature Branch**: `003-opml-export`
**Created**: 2026-02-27
**Status**: Draft
**Input**: User description: "Add OPML export feature. User can tap Export button to generate a standard OPML 2.0 file containing all current feed subscriptions (title + URL). File is saved to device Downloads folder."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Export All Subscriptions to a File (Priority: P1)

A user wants to back up their feed subscriptions or move them to another reader. They tap an Export button, and the app generates a file in the Downloads folder containing all their subscribed feeds. The user can then share, copy, or import that file into any compatible feed reader.

**Why this priority**: This is the entire purpose of the feature. Without the ability to generate and save the file, no other story has value.

**Independent Test**: Can be fully tested by tapping the Export button with at least one subscribed feed and confirming a correctly named file appears in the Downloads folder containing the feed title and URL.

**Acceptance Scenarios**:

1. **Given** the user has 3 subscribed feeds, **When** the user taps the Export button, **Then** a file named `subscriptions.opml` is saved to the device's Downloads folder containing all 3 feeds.
2. **Given** the user has 1 subscribed feed with title "Tech News" and URL "https://technews.example.com/rss", **When** the user taps the Export button, **Then** the saved file contains both the title "Tech News" and the URL "https://technews.example.com/rss".
3. **Given** the export completes successfully, **When** the user opens the Downloads folder, **Then** the file is present and can be opened by a standard file viewer.

---

### User Story 2 — Receive Confirmation After Export (Priority: P2)

After tapping Export, the user receives clear feedback that the file was saved — including where it was saved — so they know the action succeeded and where to find the file.

**Why this priority**: Without confirmation, users have no signal that the export worked and may tap Export multiple times or search for the file in the wrong location. P1 delivers the file; P2 makes the outcome visible to the user.

**Independent Test**: Can be tested by tapping Export and verifying a success message appears that mentions the file location, without needing to manually inspect the Downloads folder.

**Acceptance Scenarios**:

1. **Given** the export completes successfully, **When** the file is saved, **Then** a confirmation message is displayed telling the user the file was saved to the Downloads folder.
2. **Given** the export fails (e.g., the device has no available storage), **When** the failure occurs, **Then** an error message is displayed explaining that the export could not be completed, and no partial file is left behind.

---

### User Story 3 — Export an Empty Subscription List (Priority: P3)

A user with no subscribed feeds taps the Export button. The app handles this gracefully — either generating a valid empty export file or informing the user there is nothing to export — rather than silently failing or crashing.

**Why this priority**: Edge case handling. P1 and P2 cover the happy path completely; this story ensures the feature degrades gracefully for a minority of users.

**Independent Test**: Can be tested by removing all subscriptions then tapping Export, and verifying the app responds with a clear, non-error message.

**Acceptance Scenarios**:

1. **Given** the user has no subscribed feeds, **When** the user taps the Export button, **Then** the app informs the user there are no subscriptions to export and no file is created.

---

### Edge Cases

- What if the Downloads folder is not accessible or the device has insufficient storage? The export fails with a clear error message; no partial or corrupt file is left behind.
- What if a feed's title contains special characters (e.g., `<`, `>`, `&`, quotes)? The exported file safely encodes those characters so the file remains valid and parseable.
- What if two feeds have identical titles? Both are included in the export — title uniqueness is not required.
- What if the user taps Export while a previous export is still in progress? The second tap is ignored or queued until the first completes; the app does not create duplicate files simultaneously.
- What if a feed's URL was never successfully fetched (title is unknown)? The feed is still included in the export using whatever title was stored at subscription time.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The subscription list view MUST provide an Export button that users can tap to initiate the export.
- **FR-002**: Tapping Export MUST generate a file conforming to the OPML 2.0 standard.
- **FR-003**: The generated file MUST include one entry for every currently subscribed feed, with no feeds omitted.
- **FR-004**: Each entry in the file MUST contain the feed's title and its subscription URL.
- **FR-005**: The generated file MUST be saved to the device's standard Downloads folder using the filename `subscriptions.opml`.
- **FR-006**: If a file named `subscriptions.opml` already exists in Downloads, it MUST be overwritten by the new export.
- **FR-007**: After a successful export, the app MUST display a confirmation message to the user indicating the file was saved and its location.
- **FR-008**: If the export fails for any reason (insufficient storage, folder not accessible, etc.), the app MUST display an explanatory error message and MUST NOT leave a partial or corrupt file on the device.
- **FR-009**: If the user has no subscribed feeds, the app MUST inform the user there is nothing to export, and MUST NOT create a file.
- **FR-010**: Special characters in feed titles or URLs (such as `<`, `>`, `&`) MUST be safely encoded in the output file so the file remains well-formed and importable.

### Key Entities

- **Feed Subscription**: A source of articles the user follows. For export purposes, the relevant attributes are its display title and its subscription URL.
- **OPML Export File**: A structured document containing the full list of subscribed feeds in a format recognised by feed-reader applications. Contains a header (document title, creation date) and one outline entry per feed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user with any number of subscribed feeds can export all of them to a file in three taps or fewer from the subscription list screen.
- **SC-002**: The exported file can be successfully imported into at least one other standard feed reader application without modification.
- **SC-003**: The export operation completes and the confirmation message appears within 3 seconds for a subscription list of up to 500 feeds.
- **SC-004**: 100% of subscribed feeds present at the time of export appear in the exported file — no feed is silently omitted.
- **SC-005**: Users receive clear feedback (success or failure) for every export attempt — the action never completes silently.

## Assumptions

- The Downloads folder refers to the platform's standard user-accessible downloads location (e.g., the system Downloads directory on desktop, the shared Downloads folder on mobile). No app-private sandbox location is used.
- Overwriting an existing `subscriptions.opml` is acceptable; versioned filenames (e.g., `subscriptions-2.opml`) are out of scope.
- OPML import (the reverse operation) is out of scope for this feature.
- The export includes all feeds regardless of their refresh or read state. Filtering by folder, tag, or read status is out of scope.
- The OPML file header uses the app name as the document title and the current date-time as the date created attribute.
- No sharing sheet or share-to-app flow is required; saving to Downloads is the only delivery mechanism for this feature.
