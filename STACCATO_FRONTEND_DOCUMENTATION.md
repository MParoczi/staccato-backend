# Staccato — Frontend Design Documentation

**Version:** 1.0
**Date:** 2026-03-01
**Backend:** ASP.NET Core 10 WebAPI
**Frontend:** React TypeScript (to be designed)

---

## Table of Contents

1. [Application Overview](#1-application-overview)
2. [Terminology & Core Concepts](#2-terminology--core-concepts)
3. [Authentication & Authorization](#3-authentication--authorization)
4. [Grid System & Page Layout](#4-grid-system--page-layout)
5. [Module System](#5-module-system)
   - 5.1 [Module Types](#51-module-types)
   - 5.2 [Building Block Types](#52-building-block-types)
   - 5.3 [Building Block JSON Schemas](#53-building-block-json-schemas)
   - 5.4 [Allowed Building Blocks Per Module](#54-allowed-building-blocks-per-module)
6. [Styling System](#6-styling-system)
   - 6.1 [Module Style Properties](#61-module-style-properties)
   - 6.2 [Style Presets](#62-style-presets)
   - 6.3 [User-Saved Presets](#63-user-saved-presets)
7. [Data Models & API Contracts](#7-data-models--api-contracts)
8. [Complete API Reference](#8-complete-api-reference)
   - 8.1 [Authentication Endpoints](#81-authentication-endpoints)
   - 8.2 [User Endpoints](#82-user-endpoints)
   - 8.3 [Notebook Endpoints](#83-notebook-endpoints)
   - 8.4 [Style Endpoints](#84-style-endpoints)
   - 8.5 [Lesson Endpoints](#85-lesson-endpoints)
   - 8.6 [Lesson Page Endpoints](#86-lesson-page-endpoints)
   - 8.7 [Module Endpoints](#87-module-endpoints)
   - 8.8 [Chord Endpoints](#88-chord-endpoints)
   - 8.9 [Instrument Endpoints](#89-instrument-endpoints)
   - 8.10 [PDF Export Endpoints](#810-pdf-export-endpoints)
   - 8.11 [System Preset Endpoints](#811-system-preset-endpoints)
9. [Chord Library](#9-chord-library)
10. [Notebook Index](#10-notebook-index)
11. [PDF Export Pipeline](#11-pdf-export-pipeline)
12. [Real-time Features (SignalR)](#12-real-time-features-signalr)
13. [User Account Management](#13-user-account-management)
14. [Error Handling](#14-error-handling)
15. [Localization](#15-localization)
16. [Business Rules & Validation](#16-business-rules--validation)
17. [Frontend Implementation Notes](#17-frontend-implementation-notes)

---

## 1. Application Overview

**Staccato** is a web application that allows musicians to create and manage digital learning notebooks to track their progress in instrument learning. The application is designed around the metaphor of a physical dotted-paper notebook.

### Core Feature Summary

| Feature | Description |
|---|---|
| Notebook management | Create, organize, and style personal learning notebooks |
| Lesson records | Each lesson is a record in a notebook, spanning one or more pages |
| Free-form 2D layout | Modules are placed on a dotted grid canvas, freely positioned and resized |
| Module types | 12 predefined content block types (Theory, Practice, Homework, etc.) |
| Building blocks | Structured content elements inside modules (text, tables, chord diagrams, etc.) |
| Chord library | Searchable library of guitar chords rendered as fretboard diagrams |
| PDF export | Export notebooks or lessons to pixel-faithful PDF files |
| Notebook index | Auto-generated table of contents from lesson titles |
| Authentication | Local registration or Google OAuth login |
| Localization | English and Hungarian UI |

### User Flow (high level)

```
Register / Login
    ↓
My Notebooks (dashboard)
    ↓
Open Notebook → see Index page + lesson list
    ↓
Open Lesson → see pages with modules
    ↓
Edit modules on the dotted grid canvas
    ↓
Configure module styles (per notebook)
    ↓
Export to PDF
```

---

## 2. Terminology & Core Concepts

Understanding the exact hierarchy of objects is essential for frontend design.

### Hierarchy

```
User
└── Notebook (has a cover page, an index page, and lessons)
    ├── NotebookModuleStyle × 12 (one per module type — global styling)
    └── Lesson (ordered by creation date)
        └── LessonPage (one or more; user adds pages manually)
            └── Module (placed on the 2D grid canvas)
                └── Building Blocks (content elements inside the module, stored as JSON array)
```

### Term Definitions

| Term | Definition |
|---|---|
| **Notebook** | The top-level container. Represents one physical notebook. Has a cover, an index page, and an ordered list of lessons. Tied to a specific instrument and page size. |
| **Lesson** | A single learning session record within a notebook. A lesson has a title, a date, and spans one or more pages. Lessons are ordered by creation date. A lesson always starts at the top of a new page. |
| **LessonPage** | A single physical page within a lesson. A lesson always starts with page 1 (auto-created). The user can add more pages manually. Each page is an independent 2D grid canvas. |
| **Module** | A positioned, resizable content block on a LessonPage's grid canvas. A module has a type (Theory, Practice, etc.) and contains building blocks. Its position and size are stored in grid units. |
| **Building Block** | An individual content element inside a module. Examples: a paragraph of text, a bullet list, a chord diagram group, a chord progression. Stored as a JSON array in the module. |
| **NotebookModuleStyle** | The visual style configuration for one module type in one specific notebook. Covers background color, border, header color, font, etc. Every notebook has exactly 12 style records (one per module type). Changing a style affects all modules of that type throughout the notebook. |
| **System Style Preset** | A predefined set of 12 module type styles (one per type). Five presets exist: Classic, Colorful, Dark, Minimal, Pastel. Applying a preset overwrites all 12 styles of a notebook at once. |
| **User Saved Preset** | A user-created preset saved from a notebook's current style configuration. Stored at the user account level (not per notebook). |
| **Notebook Index** | An auto-generated table of contents derived from all lesson titles in the notebook. Displayed as: (a) a physical index page in the notebook between the cover and lesson 1, and (b) a sidebar/drawer for quick navigation. |
| **Breadcrumb Module** | A special module type that auto-generates its content from the subtitle modules within the same lesson. Has no user-editable content. |
| **Chord Library** | A read-only database of guitar chords with fretboard diagram data. Currently populated with comprehensive 6-string guitar chords. Browsed by instrument, root note, and chord quality. |
| **PDF Export** | An asynchronous export of a notebook (or subset of lessons) to a pixel-faithful PDF file. Includes the cover page, index page (for multi-lesson exports), and all lesson pages with dotted paper background. |

---

## 3. Authentication & Authorization

### Supported Authentication Methods

| Method | Description |
|---|---|
| Local registration | Email + password. No email verification required. Password hashed with BCrypt. |
| Google OAuth | User signs in with Google. ID token validated server-side by the backend. |

### Token Strategy

| Token | Storage | Lifetime | Notes |
|---|---|---|---|
| Access Token | JavaScript memory (not localStorage) | 15 minutes | JWT, signed HS256. Contains `userId`, `email`, `displayName`. |
| Refresh Token | HttpOnly cookie (`staccato_refresh`) | 7 days sliding (30 days with Remember Me) | SameSite=Strict, Secure=true in production. Rotated on every refresh. |

**Important for frontend:** The access token must be stored in memory (e.g. a React state/context variable or Zustand store), NOT in `localStorage` or `sessionStorage`. This prevents XSS attacks from reading the token. On page reload, the frontend must call `POST /auth/refresh` using the HttpOnly cookie to silently obtain a new access token.

### Authentication Flow

#### Local Registration
```
1. User fills registration form (email, displayName, password)
2. POST /auth/register
3. Backend creates user, returns access token in response body
4. Backend sets HttpOnly refresh cookie
5. Frontend stores access token in memory
6. Redirect to notebooks dashboard
```

#### Local Login
```
1. User fills login form (email, password, rememberMe checkbox)
2. POST /auth/login
3. Backend validates, returns access token in response body
4. Backend sets HttpOnly refresh cookie (7d or 30d depending on rememberMe)
5. Frontend stores access token in memory
6. Redirect to notebooks dashboard
```

#### Google Login
```
1. User clicks "Sign in with Google"
2. Frontend opens Google OAuth popup / redirect
3. Google returns an ID token to the frontend
4. POST /auth/google { idToken }
5. Backend validates token with Google servers
6. Backend finds or creates user, returns access token
7. Frontend stores access token in memory
8. Redirect to notebooks dashboard
```

#### Silent Token Refresh (on page load / token expiry)
```
1. Frontend loads — access token is not in memory
2. POST /auth/refresh (browser automatically sends HttpOnly cookie)
3. If valid: backend returns new access token, rotates refresh token cookie
4. If invalid/expired: redirect user to login page
```

#### Logout
```
1. User clicks logout
2. DELETE /auth/logout (sends current refresh cookie)
3. Backend revokes refresh token, clears cookie
4. Frontend clears access token from memory
5. Redirect to login page
```

### Authorization Rules

- All endpoints except `GET /chords`, `GET /instruments`, `GET /presets`, and auth endpoints require a valid JWT Bearer token.
- Include the access token in requests: `Authorization: Bearer <accessToken>`
- Users can only access their own data. The backend enforces ownership on all notebook/lesson/module operations.

---

## 4. Grid System & Page Layout

### The Dotted Paper Canvas

Each `LessonPage` is a 2D grid canvas rendered as dotted paper. The grid uses a fixed **5mm dot spacing** (not configurable). Modules are placed on this grid using integer grid unit coordinates.

**1 grid unit = 5mm (physical)**

### Page Sizes

The notebook page size is chosen at creation and cannot be changed afterward.

| Page Size | Physical Dimensions | Grid Width (units) | Grid Height (units) |
|---|---|---|---|
| A4 | 210mm × 297mm | 42 | 59 |
| A5 | 148mm × 210mm | 29 | 42 |
| A6 | 105mm × 148mm | 21 | 29 |
| B5 | 176mm × 250mm | 35 | 50 |
| B6 | 125mm × 176mm | 25 | 35 |

The frontend must use these dimensions to:
1. Render the canvas at the correct aspect ratio
2. Calculate pixel-to-grid-unit conversions for the current zoom level
3. Enforce boundary constraints visually (modules cannot be dragged outside the page)

### Grid Coordinate System

```
(0,0) ──────────────────────→ X (GridWidth - 1)
  │
  │   ┌───────────┐
  │   │  Module   │
  │   │ (x,y,w,h) │
  │   └───────────┘
  ↓
Y (GridHeight - 1)
```

- `GridX`: column position of module's left edge (0-indexed)
- `GridY`: row position of module's top edge (0-indexed)
- `GridWidth`: number of grid columns the module spans
- `GridHeight`: number of grid rows the module spans

A module occupies all cells from `(GridX, GridY)` to `(GridX + GridWidth - 1, GridY + GridHeight - 1)`.

### Pixel Conversion

The frontend must determine a display scale factor based on the screen size and desired zoom. Example:

```typescript
const GRID_UNIT_MM = 5;
const MM_PER_INCH = 25.4;
const SCREEN_DPI = 96; // or use window.devicePixelRatio

function gridUnitsToPixels(units: number, zoom: number = 1): number {
  const pixelsPerMm = SCREEN_DPI / MM_PER_INCH;
  return units * GRID_UNIT_MM * pixelsPerMm * zoom;
}
```

### Module Placement Rules

1. **No overlap**: Two modules on the same page cannot occupy any of the same grid cells.
2. **Page boundary**: A module must fit entirely within the page. `GridX >= 0`, `GridY >= 0`, `GridX + GridWidth <= pageGridWidth`, `GridY + GridHeight <= pageGridHeight`.
3. **Minimum size**: Each module type has a minimum width and height in grid units (see Section 5.1).
4. **Z-Index**: When modules are moved, a `ZIndex` property controls rendering order. Default is `0`. The user can "bring to front" or "send to back". This should only be used for visual layering when the user explicitly requests it — actual overlap is not allowed.

**Note:** The backend validates overlap and boundary server-side. The frontend should also perform these checks locally before sending PATCH requests to provide immediate feedback, but must handle 422 errors from the server as well.

### Multi-Page Lessons

A lesson starts with one page (auto-created). The user can manually add more pages. A soft limit warning is shown when a lesson reaches 10 pages (no hard stop).

**Page navigation within a lesson:** The frontend should provide left/right arrows or a page indicator (e.g. "Page 2 / 4") to navigate between pages in a lesson.

**Page breaks:** Page breaks are conceptual — a new `LessonPage` record represents the start of a new page. There is no mid-lesson page break; pages are discrete.

### Global Page Numbering

Page numbers are globally sequential across the entire notebook:

```
Cover page:   unnumbered (or "Cover")
Index page:   page 1
Lesson 1, Page 1: page 2
Lesson 1, Page 2: page 3
Lesson 1, Page 3: page 4
Lesson 2, Page 1: page 5
...
```

The `GET /notebooks/{id}/index` endpoint returns the starting page number for each lesson.

### The Notebook as a Book

The application is designed to feel like opening a physical notebook:

- The **Cover page** is the first thing shown when opening a notebook.
- The **Index page** follows immediately after the cover.
- **Lessons** follow in order, each starting on a new page.
- The user can **page forward/backward** through the notebook.
- The sidebar **Notebook Index** (navigation drawer) allows jumping directly to any lesson.

---

## 5. Module System

### 5.1 Module Types

There are 12 module types. Each module type has:
- A defined set of allowed building blocks
- Minimum grid dimensions
- Its own style configuration (per notebook)

| Module Type | Role | Min W | Min H | Notes |
|---|---|---|---|---|
| `Title` | Lesson title and date | 20 | 4 | Only one per lesson; must be the first module on the first page of the lesson |
| `Breadcrumb` | Auto-generated lesson navigation path | 20 | 3 | Content is derived from subtitle modules; no user-editable content |
| `Subtitle` | Section heading within a lesson | 10 | 3 | Multiple subtitles allowed per lesson |
| `Theory` | Theoretical explanations and concepts | 8 | 5 | Supports tables, bullet lists, musical notes |
| `Practice` | Practice exercises and instructions | 8 | 5 | Supports chord progressions and chord diagrams |
| `Example` | Concrete musical examples | 8 | 5 | Supports chord progressions and musical notes |
| `Important` | Key information to remember | 8 | 4 | Typically short, highlighted content |
| `Tip` | Quick tips and shortcuts | 8 | 4 | Typically short advisory content |
| `Homework` | Practice tasks to complete | 8 | 5 | Supports checklists |
| `Question` | Self-assessment questions | 8 | 4 | Simple text content |
| `ChordTablature` | Dedicated chord diagram display | 8 | 10 | Primarily fretboard diagrams; taller minimum for diagram rendering |
| `FreeText` | Unrestricted sandbox | 4 | 4 | Accepts all building block types |

### 5.2 Building Block Types

Building blocks are the individual content elements inside a module. They are stored as an ordered JSON array in `Module.ContentJson`.

| Building Block | Enum Value | Description |
|---|---|---|
| Section Heading | `SectionHeading` | A sub-heading inside a module (smaller than the module's title bar). Bold display. |
| Date | `Date` | A date value. Rendered as a formatted date string. Always plain (no bold). |
| Text | `Text` | A paragraph of plain text with optional bold spans. |
| Bullet List | `BulletList` | An unordered list with bullet points. Each item supports bold spans. |
| Numbered List | `NumberedList` | An ordered numbered list. Each item supports bold spans. |
| Checkbox List | `CheckboxList` | A list with checkbox indicators. Each item supports bold spans. Checkboxes are functional - the checked state is stored. |
| Table | `Table` | A user-defined table with custom column headers and rows. Cells support plain text with optional bold. |
| Musical Notes | `MusicalNotes` | A visual sequence of musical note names selected from the 12-note chromatic scale. Rendered as circular badges. |
| Chord Progression | `ChordProgression` | A horizontal sequence of chords with time signature, beat counts, section labels, and repeat markers. Chords are referenced from the chord library by ID. |
| Chord Tablature Group | `ChordTablatureGroup` | An ordered group of fretboard diagrams. Each diagram references a chord from the chord library. The user can reorder diagrams within the group. |

### 5.3 Building Block JSON Schemas

All building block JSON objects share a `type` discriminator field matching the `BuildingBlockType` enum value.

#### TextSpan (used in all text-bearing blocks)

```json
{
  "text": "string content",
  "bold": false
}
```

#### SectionHeading

```json
{
  "type": "SectionHeading",
  "spans": [
    { "text": "Mi az akkord?", "bold": false }
  ]
}
```

Rendered as a smaller heading inside the module body (e.g. small-caps or bold uppercase style, visually distinct from the module's own title bar).

#### Date

```json
{
  "type": "Date",
  "value": "2025-02-23"
}
```

`value` is an ISO 8601 date string (`YYYY-MM-DD`). The frontend formats this for display according to the user's locale (e.g. "2025. február 23." in Hungarian, "February 23, 2025" in English). Always rendered as plain text — no bold formatting.

#### Text

```json
{
  "type": "Text",
  "spans": [
    { "text": "Az akkord ", "bold": false },
    { "text": "három vagy több hang", "bold": true },
    { "text": " együttes megszólaltatása.", "bold": false }
  ]
}
```

#### BulletList

```json
{
  "type": "BulletList",
  "items": [
    { "spans": [{ "text": "Nagy terc (4 félhang)", "bold": false }] },
    { "spans": [{ "text": "Kis terc (3 félhang)", "bold": false }] }
  ]
}
```

Rendered with bullet point markers (•).

#### NumberedList

```json
{
  "type": "NumberedList",
  "items": [
    { "spans": [{ "text": "Practice slowly at 60 BPM", "bold": false }] },
    { "spans": [{ "text": "Increase tempo by 5 BPM daily", "bold": false }] }
  ]
}
```

Rendered with sequential numbers (1. 2. 3. ...).

#### CheckboxList

```json
{
  "type": "CheckboxList",
  "items": [
    { "spans": [{ "text": "Practice the C major scale", "bold": false }], "isChecked": false },
    { "spans": [{ "text": "Learn the G chord", "bold": false }], "isChecked": true }
  ]
}
```

Rendered with checkbox visual indicators (☐). The checked/unchecked state is persisted — these are functional elements, interactive to-do items.

#### Table

```json
{
  "type": "Table",
  "columns": [
    { "header": "Típus" },
    { "header": "Képlet (½ hang)" },
    { "header": "Jelleg" }
  ],
  "rows": [
    [
      { "spans": [{ "text": "Dúr (major)", "bold": false }] },
      { "spans": [{ "text": "4+3", "bold": false }] },
      { "spans": [{ "text": "vidám", "bold": false }] }
    ],
    [
      { "spans": [{ "text": "Moll (minor)", "bold": false }] },
      { "spans": [{ "text": "3+4", "bold": false }] },
      { "spans": [{ "text": "szomorú", "bold": false }] }
    ]
  ]
}
```

Column count and row count are fully user-defined. Column headers support plain text only (no spans). Cell content supports plain text + bold spans.

#### MusicalNotes

```json
{
  "type": "MusicalNotes",
  "sequence": ["C", "D", "E", "F", "G", "A", "B"]
}
```

The `sequence` array contains note names from the 12-note chromatic scale. Valid values: `C`, `C#`, `D`, `D#`, `E`, `F`, `F#`, `G`, `G#`, `A`, `A#`, `B`.

**Rendering:** Each note is displayed as a circular badge. The frontend derives the interval type between consecutive notes automatically: if two consecutive notes are 1 semitone apart → display "S" (semitone); if 2 semitones apart → display "W" (whole tone); wider intervals are simply displayed without a label or with the distance in semitones.

The user builds the sequence by selecting notes from a full chromatic scale picker. Notes can appear multiple times (e.g. `["C", "D", "E", "F", "G", "A", "B", "C"]` for a full octave).

#### ChordProgression

```json
{
  "type": "ChordProgression",
  "timeSignature": "4/4",
  "sections": [
    {
      "label": "Verse",
      "repeat": true,
      "measures": [
        {
          "chords": [
            { "chordId": "uuid-of-C-major", "displayName": "C", "beats": 4 }
          ]
        },
        {
          "chords": [
            { "chordId": "uuid-of-Am", "displayName": "Am", "beats": 2 },
            { "chordId": "uuid-of-F", "displayName": "F", "beats": 2 }
          ]
        }
      ]
    }
  ]
}
```

**Fields:**
- `timeSignature`: string like `"4/4"`, `"3/4"`, `"6/8"`, etc.
- `sections[].label`: optional name for the section (e.g. "Verse", "Chorus"). `null` if not set.
- `sections[].repeat`: if `true`, render with `||:` and `:||` repeat markers.
- `measures[].chords[].chordId`: UUID referencing the chord in the chord library.
- `measures[].chords[].displayName`: the chord label shown in the UI (stored alongside ID so rendering works without a chord lookup).
- `measures[].chords[].beats`: how many beats this chord lasts within the measure.

**Beat validation:** The sum of beats in a measure must equal the time signature numerator. E.g. in 4/4, a measure's chord beats must sum to 4.

**Rendering:** Display as a horizontal sequence of colored pill/badge components showing the chord name. Beat counts shown below each badge. Repeat markers `||:` and `:||` shown at section boundaries. Section labels shown above the section.

#### ChordTablatureGroup

```json
{
  "type": "ChordTablatureGroup",
  "chords": [
    { "chordId": "uuid-of-F-major", "label": "F Major" },
    { "chordId": "uuid-of-Am", "label": "Am" },
    { "chordId": "uuid-of-C", "label": "C Major" }
  ]
}
```

**Fields:**
- `chords[].chordId`: UUID referencing the chord in the chord library.
- `chords[].label`: display label shown below the fretboard diagram (can differ from the chord's canonical name).
- Order is significant — the `chords` array order determines the display order. The user can reorder diagrams.

**Rendering:** Each chord is rendered as a fretboard diagram (see Section 9 for the chord data structure). Diagrams are displayed horizontally in a row. The frontend fetches chord position data via `GET /chords/{id}` when rendering.

### 5.4 Allowed Building Blocks Per Module

This table defines which building block types are allowed inside each module type. The backend enforces this server-side; the frontend should also use it to build the content editor UI (only show relevant block-add buttons).

| Module Type | Allowed Building Blocks |
|---|---|
| `Title` | `Date`, `Text` |
| `Breadcrumb` | *none — auto-generated, no user content* |
| `Subtitle` | `Text` |
| `Theory` | `SectionHeading`, `Text`, `BulletList`, `NumberedList`, `Table`, `MusicalNotes` |
| `Practice` | `SectionHeading`, `Text`, `ChordProgression`, `ChordTablatureGroup`, `MusicalNotes` |
| `Example` | `SectionHeading`, `Text`, `ChordProgression`, `MusicalNotes` |
| `Important` | `SectionHeading`, `Text`, `MusicalNotes` |
| `Tip` | `SectionHeading`, `Text`, `MusicalNotes` |
| `Homework` | `SectionHeading`, `Text`, `BulletList`, `NumberedList`, `CheckboxList` |
| `Question` | `SectionHeading`, `Text` |
| `ChordTablature` | `ChordTablatureGroup`, `MusicalNotes` |
| `FreeText` | *all building block types* |

**Special rules:**
- `Breadcrumb` module content is always an empty array `[]`. The frontend renders its content dynamically by reading the subtitle modules in the same lesson.
- No building block is mandatory. A module can be created with zero building blocks.
- Building blocks are ordered. The order in the `ContentJson` array determines display order. The user can reorder blocks within a module.

---

## 6. Styling System

### 6.1 Module Style Properties

Each of the 12 module types has a dedicated style configuration per notebook. This is stored as a `NotebookModuleStyle` entity. Changing a style immediately applies to **all instances** of that module type across the entire notebook, past and future.

| Property | Type | Applies to | Description |
|---|---|---|---|
| `BackgroundColor` | `string` (hex `#RRGGBB`) | All modules except Title/Subtitle | Fill color of the module body |
| `BorderColor` | `string` (hex `#RRGGBB`) | All modules except Title/Subtitle | Color of the module border |
| `BorderStyle` | `BorderStyle` enum | All modules except Title/Subtitle | `None`, `Solid`, `Dashed`, `Dotted` |
| `BorderWidth` | `integer` (px) | All modules except Title/Subtitle | Border thickness in pixels |
| `BorderRadius` | `integer` (px) | All modules except Title/Subtitle | Corner rounding radius |
| `HeaderBgColor` | `string` (hex `#RRGGBB`) | All modules except Title/Subtitle | Background color of the module's header/title bar |
| `HeaderTextColor` | `string` (hex `#RRGGBB`) | All modules except Title/Subtitle | Text color of the module's header/title bar |
| `BodyTextColor` | `string` (hex `#RRGGBB`) | All module types | Main text color in the module body |
| `FontFamily` | `FontFamily` enum | All module types | `Default`, `Monospace`, `Serif` |

**Title and Subtitle modules** only expose `BodyTextColor` and `FontFamily`. They have no background box or border.

**BorderStyle enum values:**
- `None` — no border (module appears as a floating content block)
- `Solid` — standard solid border line
- `Dashed` — dashed border line
- `Dotted` — dotted border line

**FontFamily enum values:**
- `Default` — the application's default sans-serif font
- `Monospace` — monospace font (useful for code-like content or tab notation)
- `Serif` — serif font (classic book-like appearance)

### 6.2 Style Presets

Five system-defined style presets are available. Each preset defines all 12 module type styles at once. Applying a preset **overwrites all 12 styles** of the notebook.

| Preset | Color Mood | Best For |
|---|---|---|
| `Classic` | Warm beige/brown tones | Traditional, book-like aesthetic |
| `Colorful` | Vibrant distinct colors per type | Clear visual differentiation, matches the reference design |
| `Dark` | Dark backgrounds, light text | Low-light reading, dark mode feel |
| `Minimal` | White backgrounds, thin borders | Clean, distraction-free |
| `Pastel` | Soft pastel backgrounds | Gentle, easy on the eyes |

**Colorful preset color scheme (reference):**

| Module Type | Background | Header |
|---|---|---|
| Theory | Teal `#E0F7FA` | Teal dark `#00838F` |
| Practice | Orange `#FFF3E0` | Orange dark `#E65100` |
| Example | Green `#E8F5E9` | Green dark `#2E7D32` |
| Important | Yellow `#FFFDE7` | Amber dark `#F57F17` |
| Tip | Blue `#E3F2FD` | Blue dark `#1565C0` |
| Homework | Purple `#F3E5F5` | Purple dark `#6A1B9A` |
| Question | Pink `#FCE4EC` | Pink dark `#880E4F` |
| ChordTablature | Grey `#F5F5F5` | Grey dark `#424242` |
| FreeText | White `#FFFFFF` | Grey `#9E9E9E` |

### 6.3 User-Saved Presets

Users can save their own style presets from any notebook's current configuration. A saved preset stores all 12 module type style definitions. Presets are stored at the user account level and can be applied to any of the user's notebooks.

**Applying a preset:** Selecting any preset (system or user-saved) replaces all 12 module type styles in the target notebook simultaneously.

**Style configuration timing:** Styles can be configured:
1. **At notebook creation** — the creation flow includes a style configuration step (or select a preset)
2. **At any time** — from the notebook settings/style editor

---

## 7. Data Models & API Contracts

### User

```typescript
interface User {
  id: string;                    // UUID
  email: string;
  firstName: string;
  lastName: string;
  language: 'en' | 'hu';
  defaultPageSize: PageSize | null;
  defaultInstrumentId: string | null;  // UUID of default instrument
  avatarUrl: string | null;      // URL to proxied avatar endpoint, or null
  scheduledDeletionAt: string | null;  // ISO 8601 datetime — set when deletion is scheduled
}
```

### Notebook (Summary — list view)

```typescript
interface NotebookSummary {
  id: string;
  title: string;
  instrumentName: string;
  pageSize: PageSize;
  coverColor: string;            // hex color
  lessonCount: number;
  createdAt: string;
  updatedAt: string;
}
```

### Notebook (Detail — single view)

```typescript
interface NotebookDetail {
  id: string;
  title: string;
  instrumentId: string;
  instrumentName: string;
  pageSize: PageSize;
  coverColor: string;
  lessonCount: number;
  createdAt: string;
  updatedAt: string;
  styles: NotebookModuleStyle[];  // always 12 items
}
```

### NotebookModuleStyle

```typescript
interface NotebookModuleStyle {
  id: string;
  notebookId: string;
  moduleType: ModuleType;
  backgroundColor: string;        // hex
  borderColor: string;            // hex
  borderStyle: BorderStyle;
  borderWidth: number;            // px
  borderRadius: number;           // px
  headerBgColor: string;          // hex
  headerTextColor: string;        // hex
  bodyTextColor: string;          // hex
  fontFamily: FontFamily;
}
```

### Lesson (Summary)

```typescript
interface LessonSummary {
  id: string;
  title: string;
  createdAt: string;
  pageCount: number;
}
```

### Lesson (Detail)

```typescript
interface LessonDetail {
  id: string;
  notebookId: string;
  title: string;
  createdAt: string;
  pages: LessonPage[];
}
```

### LessonPage

```typescript
interface LessonPage {
  id: string;
  lessonId: string;
  pageNumber: number;             // 1-based within the lesson
  moduleCount: number;
}
```

### Module

```typescript
interface Module {
  id: string;
  lessonPageId: string;
  moduleType: ModuleType;
  gridX: number;
  gridY: number;
  gridWidth: number;
  gridHeight: number;
  zIndex: number;
  content: BuildingBlock[];       // deserialized from ContentJson
}
```

### NotebookIndex (auto-generated TOC)

```typescript
interface NotebookIndex {
  entries: NotebookIndexEntry[];
}

interface NotebookIndexEntry {
  lessonId: string;
  title: string;
  createdAt: string;
  startPageNumber: number;        // global page number where lesson begins
}
```

### Instrument

```typescript
interface Instrument {
  id: string;
  key: InstrumentKey;
  name: string;                   // e.g. "6-String Guitar"
  stringCount: number;
}
```

### Chord (Summary — search result)

```typescript
interface ChordSummary {
  id: string;
  instrumentKey: InstrumentKey;
  name: string;                   // e.g. "F Major"
  root: string;                   // e.g. "F"
  quality: string;                // e.g. "major"
  extension: string | null;       // e.g. "maj7", null for basic chords
  alternation: string | null;     // e.g. "b5", null if not applicable
  previewPosition: ChordPosition; // first position, for thumbnail rendering
}
```

### Chord (Detail — full data)

```typescript
interface ChordDetail {
  id: string;
  instrumentKey: InstrumentKey;
  name: string;
  root: string;
  quality: string;
  extension: string | null;
  alternation: string | null;
  positions: ChordPosition[];
}

interface ChordPosition {
  label: string;                  // e.g. "Barre 1st position"
  baseFret: number;               // 1 = nut, higher = higher up the neck
  barre: ChordBarre | null;
  strings: ChordString[];         // one entry per string
}

interface ChordBarre {
  fret: number;
  fromString: number;             // 1 = highest pitched string
  toString: number;
}

interface ChordString {
  string: number;                 // 1 = highest pitched, N = lowest pitched
  state: 'open' | 'fretted' | 'muted';
  fret: number | null;            // null if state is open or muted
  finger: number | null;          // 1=index, 2=middle, 3=ring, 4=pinky; null if open/muted
}
```

### PdfExport

```typescript
interface PdfExport {
  id: string;
  notebookId: string;
  notebookTitle: string;
  status: 'Pending' | 'Processing' | 'Ready' | 'Failed';
  createdAt: string;
  completedAt: string | null;
  lessonIds: string[] | null;     // null = whole notebook
}
```

### SystemStylePreset

```typescript
interface SystemStylePreset {
  id: string;
  name: string;                   // "Classic" | "Colorful" | "Dark" | "Minimal" | "Pastel"
  displayOrder: number;
  isDefault: boolean;
  styles: NotebookModuleStyle[];  // 12 items
}
```

### StyleEntry (used in user-saved presets)

```typescript
interface StyleEntry {
  moduleType: string;             // ModuleType enum value
  stylesJson: string;             // JSON string containing style properties
}
```

### UserSavedPreset

```typescript
interface UserSavedPreset {
  id: string;
  name: string;
  styles: StyleEntry[];           // 12 items, one per module type
}
```

### Enums

```typescript
type ModuleType =
  'Title' | 'Breadcrumb' | 'Subtitle' | 'Theory' | 'Practice' |
  'Example' | 'Important' | 'Tip' | 'Homework' | 'Question' |
  'ChordTablature' | 'FreeText';

type BuildingBlockType =
  'SectionHeading' | 'Date' | 'Text' | 'BulletList' | 'NumberedList' |
  'CheckboxList' | 'Table' | 'MusicalNotes' | 'ChordProgression' |
  'ChordTablatureGroup';

type BorderStyle = 'None' | 'Solid' | 'Dashed' | 'Dotted';

type FontFamily = 'Default' | 'Monospace' | 'Serif';

type PageSize = 'A4' | 'A5' | 'A6' | 'B5' | 'B6';

type InstrumentKey =
  'Guitar6String' | 'Guitar7String' | 'Bass4String' | 'Bass5String' |
  'Ukulele4String' | 'Banjo4String' | 'Banjo5String';

type Language = 'en' | 'hu';
```

---

## 8. Complete API Reference

**Base URL:** `https://{host}` (no `/api` prefix — only chord and instrument endpoints use `/api/` in their paths)
**Authentication:** `Authorization: Bearer {accessToken}` (except where noted)
**Content-Type:** `application/json` (except file upload endpoints)
**Accept-Language:** `en` or `hu` — controls error message language

---

### 8.1 Authentication Endpoints

Rate limited: 10 requests per minute per IP on all `/auth/*` endpoints.

#### POST /auth/register

Register a new local account.

**Request:**
```json
{
  "email": "user@example.com",
  "displayName": "John Doe",
  "password": "SecurePassword123!"
}
```

**Validation:**
- `email`: required, valid email format, max 256 chars
- `displayName`: required, 2–100 chars
- `password`: required, min 8 chars, must contain uppercase, lowercase, digit

**Response 201:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900
}
```
Sets `staccato_refresh` HttpOnly cookie.

**Error 409:** Email already registered.

---

#### POST /auth/login

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "rememberMe": false
}
```

**Response 200:**
```json
{
  "accessToken": "...",
  "expiresIn": 900
}
```
Sets `staccato_refresh` HttpOnly cookie (7d or 30d based on `rememberMe`).

**Error 401:** Invalid credentials.

---

#### POST /auth/google

```json
{
  "idToken": "google-id-token-from-frontend"
}
```

**Response 200:** Same as login response. Sets cookie.

**Error 400:** Invalid Google ID token.

---

#### POST /auth/refresh

No request body. Reads refresh token from `staccato_refresh` cookie automatically.

**Response 200:** New access token. Rotates the refresh cookie.

**Error 401:** Missing, invalid, or expired refresh token.

---

#### DELETE /auth/logout

No request body. Reads refresh token from cookie.

**Response 204:** Cookie cleared.

---

### 8.2 User Endpoints

All require authentication.

#### GET /users/me

**Response 200:** Full `User` object.

---

#### PUT /users/me

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "language": "hu",
  "defaultPageSize": "A5",
  "defaultInstrumentId": "uuid-or-null"
}
```

**Fields:**
- `firstName`: required
- `lastName`: required
- `language`: required, `"en"` or `"hu"`
- `defaultPageSize`: optional, one of `A4`, `A5`, `A6`, `B5`, `B6`, or `null`
- `defaultInstrumentId`: optional, UUID or `null`

**Response 200:** Updated `User` object.

---

#### DELETE /users/me

Schedules account for deletion in 30 days. Does not delete immediately.

**Response 204.**
**Error 409:** Account is already scheduled for deletion.

---

#### POST /users/me/cancel-deletion

Cancels a scheduled deletion.

**Response 204.**
**Error 400:** Account is not scheduled for deletion.

---

#### PUT /users/me/avatar

`Content-Type: multipart/form-data`

Form field: `File` — image file (JPG, PNG, WebP). Max 2MB.

**Response 200:** Updated `User` object (full user profile with new `avatarUrl`).

**Error 400:** Invalid file type or size.

---

#### DELETE /users/me/avatar

Removes the user's custom avatar.

**Response 204.**

---

#### GET /users/me/presets

**Response 200:** Array of `UserSavedPreset`.

---

#### POST /users/me/presets

```json
{
  "name": "My Custom Preset",
  "styles": [
    { "moduleType": "Theory", "stylesJson": "{\"backgroundColor\":\"#E0F7FA\",\"borderColor\":\"#00838F\",...}" },
    { "moduleType": "Practice", "stylesJson": "{...}" }
  ]
}
```

Each `styles` entry is a `StyleEntry` with `moduleType` (string) and `stylesJson` (a JSON string containing the individual style properties). Must contain 12 entries, one per `ModuleType`.

**Response 201:** Created `UserSavedPreset`.

---

#### PUT /users/me/presets/{id}

```json
{
  "name": "Updated Name",
  "styles": [ /* 12 StyleEntry objects, or null to keep current styles */ ]
}
```

Both `name` and `styles` are optional — only provided fields are updated.

**Response 200:** Updated `UserSavedPreset`.
**Error 404:** Preset not found or belongs to another user.

---

#### DELETE /users/me/presets/{id}

**Response 204.**

---

### 8.3 Notebook Endpoints

All require authentication. Users only see and modify their own notebooks.

#### GET /notebooks

**Response 200:** Array of `NotebookSummary`.

---

#### POST /notebooks

```json
{
  "title": "Guitar Journey 2025",
  "instrumentId": "uuid-of-guitar-instrument",
  "pageSize": "A5",
  "coverColor": "#8B4513",
  "styles": null
}
```

**Fields:**
- `title`: required, max 200 chars
- `instrumentId`: required, must reference a seeded instrument
- `pageSize`: required, one of `A4`, `A5`, `A6`, `B5`, `B6`
- `coverColor`: required, valid hex color
- `styles`: optional — if `null` or omitted, the Colorful system preset is applied

**Response 201:** `NotebookDetail` including all 12 styles.

---

#### GET /notebooks/{id}

**Response 200:** `NotebookDetail`.
**Error 403:** Notebook belongs to another user.
**Error 404:** Not found.

---

#### PUT /notebooks/{id}

```json
{
  "title": "New Title",
  "coverColor": "#3E2723"
}
```

**Note:** `instrumentId` and `pageSize` **cannot** be changed after creation. Sending a non-null value for either field returns a **400** validation error with codes `NOTEBOOK_INSTRUMENT_IMMUTABLE` / `NOTEBOOK_PAGE_SIZE_IMMUTABLE`. Omit these fields or send `null`.

**Response 200:** Updated `NotebookDetail`.

---

#### DELETE /notebooks/{id}

Hard deletes the notebook and all associated data (lessons, pages, modules).

**Response 204.**

---

#### GET /notebooks/{id}/index

Returns the auto-generated table of contents.

**Response 200:**
```json
{
  "entries": [
    {
      "lessonId": "uuid",
      "title": "1. Gitarlecke",
      "createdAt": "2025-02-23T10:00:00Z",
      "startPageNumber": 2
    }
  ]
}
```

`startPageNumber` is globally sequential. Cover = unnumbered, Index = 1, Lesson 1 starts at 2.

---

### 8.4 Style Endpoints

#### GET /notebooks/{id}/styles

**Response 200:** Array of 12 `NotebookModuleStyle` objects.

---

#### PUT /notebooks/{id}/styles

Bulk replace all 12 module type styles.

```json
[
  {
    "moduleType": "Theory",
    "backgroundColor": "#E0F7FA",
    "borderColor": "#00838F",
    "borderStyle": "Solid",
    "borderWidth": 1,
    "borderRadius": 4,
    "headerBgColor": "#00838F",
    "headerTextColor": "#FFFFFF",
    "bodyTextColor": "#212121",
    "fontFamily": "Default"
  }
  // ... 11 more
]
```

**Validation:** Array must contain exactly 12 items, one per `ModuleType`.

**Response 200:** Array of 12 updated `NotebookModuleStyle` objects.

---

#### POST /notebooks/{id}/styles/apply-preset/{presetId}

Applies a system preset or user-saved preset to the notebook. Replaces all 12 styles.

`presetId` can be either a `SystemStylePreset` ID or a `UserSavedPreset` ID. The backend resolves which table to check.

**Response 200:** Array of 12 updated `NotebookModuleStyle` objects.
**Error 404:** Preset not found.
**Error 403:** User preset belongs to another user.

---

### 8.5 Lesson Endpoints

#### GET /notebooks/{id}/lessons

**Response 200:** Array of `LessonSummary`, ordered by `createdAt` ascending.

---

#### POST /notebooks/{id}/lessons

```json
{
  "title": "1. Gitarlecke"
}
```

**Validation:** `title` required, max 200 chars.

**Response 201:** `LessonDetail`. The first `LessonPage` (pageNumber = 1) is automatically created.

---

#### GET /lessons/{id}

**Response 200:** `LessonDetail` with pages array.

---

#### PUT /lessons/{id}

```json
{
  "title": "Updated Lesson Title"
}
```

**Response 200:** Updated `LessonDetail`.

---

#### DELETE /lessons/{id}

Hard deletes the lesson and all its pages and modules.

**Response 204.**

---

### 8.6 Lesson Page Endpoints

#### GET /lessons/{id}/pages

**Response 200:** Array of `LessonPage`, ordered by `pageNumber` ascending.

---

#### POST /lessons/{id}/pages

Adds a new page. Auto-assigns `pageNumber = max(existing) + 1`.

**Response 201** (normal) or **200 with warning** (at 10+ pages):

```json
{
  "data": {
    "id": "uuid",
    "lessonId": "uuid",
    "pageNumber": 10,
    "moduleCount": 0
  },
  "warning": "This lesson has reached the recommended maximum of 10 pages."
}
```

When no warning: `warning` field is `null`.

---

#### DELETE /lessons/{lessonId}/pages/{pageId}

Hard deletes the page and all its modules.

**Error 400:** Cannot delete the last remaining page of a lesson.

**Response 204.**

---

### 8.7 Module Endpoints

#### GET /pages/{id}/modules

**Response 200:** Array of `Module` with `content` as deserialized building block array.

---

#### POST /pages/{id}/modules

```json
{
  "moduleType": "Theory",
  "gridX": 2,
  "gridY": 5,
  "gridWidth": 18,
  "gridHeight": 10,
  "zIndex": 0,
  "content": []
}
```

**Validation (server-side):**
1. `gridWidth >= MinWidth` for the module type
2. `gridHeight >= MinHeight` for the module type
3. Module fits within page boundaries
4. No overlap with existing modules on the page
5. If `moduleType = Title`: no other Title module exists anywhere in the lesson
6. If `moduleType = Breadcrumb`: `content` must be `[]`
7. Building block types in `content` must be allowed for the module type

**Response 201:** Created `Module`.

**Error 422:** Business rule violation. See Section 14 for error codes.
**Error 409:** Duplicate Title module.

---

#### PUT /modules/{id}

Full update including content.

```json
{
  "moduleType": "Theory",
  "gridX": 2,
  "gridY": 5,
  "gridWidth": 18,
  "gridHeight": 12,
  "zIndex": 0,
  "content": [
    {
      "type": "SectionHeading",
      "spans": [{ "text": "Mi az akkord?", "bold": false }]
    },
    {
      "type": "Text",
      "spans": [{ "text": "Az akkord három hang.", "bold": false }]
    }
  ]
}
```

Same validation rules as POST. Excludes the current module from overlap checks.

**Response 200:** Updated `Module`.

---

#### DELETE /modules/{id}

**Response 204.**

---

#### PATCH /modules/{id}/layout

Lightweight update for drag/resize operations. Only updates grid position and z-index.

```json
{
  "gridX": 4,
  "gridY": 7,
  "gridWidth": 20,
  "gridHeight": 12,
  "zIndex": 0
}
```

Same boundary, overlap, and minimum size validation as POST/PUT.

**Response 200:** Updated `Module`.

**Usage note:** This endpoint is called by the frontend during drag-and-drop/resize operations. Calls should be **debounced** (minimum 500ms) to avoid flooding the server. The frontend should apply the layout changes optimistically (immediately in the UI) and roll back if the server returns an error.

---

### 8.8 Chord Endpoints

No authentication required (public read-only). Responses cached for 5 minutes.

#### GET /chords

Query parameters:
- `instrument` (required): `InstrumentKey` enum value, e.g. `Guitar6String`
- `root` (optional): note name, e.g. `F`, `C#`, `Bb`
- `quality` (optional): quality string, e.g. `major`, `minor`, `maj7`

Example: `GET /chords?instrument=Guitar6String&root=F&quality=major`

**Response 200:** Array of `ChordSummary`.

---

#### GET /chords/{id}

**Response 200:** `ChordDetail` with full position and string data.

---

### 8.9 Instrument Endpoints

No authentication required. Responses cached for 5 minutes.

#### GET /instruments

**Response 200:** Array of `Instrument`.

---

### 8.10 PDF Export Endpoints

All require authentication.

#### POST /exports

Queue a PDF export job.

```json
{
  "notebookId": "uuid",
  "lessonIds": null
}
```

`lessonIds`: if `null` or omitted, export the whole notebook. If an array of lesson UUIDs, export only those lessons. If multiple lessons, an index page is prepended.

**Response 202:**
```json
{
  "exportId": "uuid",
  "status": "Pending"
}
```

**Error 409:** An active export (Pending or Processing) already exists for this notebook.

---

#### GET /exports

**Response 200:** Array of `PdfExport` for the current user, ordered by `createdAt` descending.

---

#### GET /exports/{id}

**Response 200:** `PdfExport` with current status.

---

#### GET /exports/{id}/download

Streams the PDF file through the API. The blob URL is never exposed to the client.

**Response 200:**
- `Content-Type: application/pdf`
- `Content-Disposition: attachment; filename="{notebook-title}.pdf"`
- Body: PDF binary stream

**Error 404:** Export not found, not ready (status ≠ Ready), or expired.
**Error 403:** Export belongs to another user.

---

#### DELETE /exports/{id}

Cancels a pending export or deletes a completed export (removes DB record and Azure Blob).

**Response 204.**

---

### 8.11 System Preset Endpoints

No authentication required.

#### GET /presets

**Response 200:** Array of `SystemStylePreset` (5 items).

---

## 9. Chord Library

### Data Structure

The chord library currently contains comprehensive chords for **6-string guitar** only. Other instruments will be added in future releases. The library is read-only and never modified.

Each chord has one or more **positions** (voicings). A position describes exactly where to place each finger on the fretboard.

### Fretboard Rendering

The frontend must render fretboard diagrams from `ChordPosition` data. Key rendering rules:

```
String numbering:  1 = highest pitched (e.g. high E on guitar)
                   6 = lowest pitched (e.g. low E on guitar)

baseFret:          The fret number displayed at the top of the diagram.
                   baseFret = 1 means the diagram starts at the nut.
                   baseFret = 5 means the diagram shows frets 5-9.

Diagram height:    Always shows 5 frets (baseFret to baseFret + 4).

String states:
  "open"    → circle above the nut (○)
  "muted"   → X above the nut (✕)
  "fretted" → filled dot at the specified fret position

Barre:             Draw a horizontal bar across fromString to toString
                   at the specified fret. Render as a rounded rectangle
                   or thick horizontal line spanning the barre strings.

Finger numbers:    Optionally display 1-4 inside or below the finger dots.
```

### Diagram Layout (example)

```
     E  A  D  G  B  e    ← string names (optional)
     6  5  4  3  2  1    ← string numbers
✕    ○           ○       ← muted / open strings above nut

1fr  ┌──┬──┬──┬──┬──┐
     │  │  │  │  │  │
2fr  ├──┼──┼──┼──┼──┤
     │  ●  ●  │  │  │    ← fretted strings at fret 2
3fr  ├──┼──┼──┼──┼──┤
     │  │  │  ●  │  │    ← fretted string at fret 3
4fr  ├──┼──┼──┼──┼──┤
     │  │  │  │  ●  │    ← fretted string at fret 4
5fr  └──┴──┴──┴──┴──┘
```

### Filtering in the UI

The chord selector should allow filtering by:
1. **Root note**: one of the 12 chromatic notes. Display as a grid of note buttons.
2. **Quality**: major, minor, 7, maj7, min7, dim, aug, sus2, sus4, etc. Display as a list or tab.

After filtering, show a grid of chord thumbnails (fretboard diagram previews). When the user selects a chord, all its positions are available for selection (some chords have multiple voicings).

---

## 10. Notebook Index

### Auto-generation

The notebook index is generated automatically. It does not require any user input to maintain — it updates whenever lessons are added, removed, or renamed.

**Source:** The title of each `Title` module across all lessons. Since a lesson can have exactly one `Title` module (enforced), the lesson title in the index is always the text from that module.

**Ordering:** Lessons appear in the index in creation date order (ascending).

### Two Representations

#### 1. Physical Index Page (in-notebook)

The index page is the **first content page** of the notebook (after the cover). It is rendered as part of the notebook canvas — on dotted paper, looking like a hand-written table of contents.

```
                    INDEX
─────────────────────────────────────────────────────
 1.  1. Gitarlecke .................................. 2
 2.  Akkordszerkezetek .............................. 8
 3.  Hangközök és skálák .......................... 15
 4.  CAGED rendszer ................................ 22
─────────────────────────────────────────────────────
```

- Lesson number (sequential) on the left
- Lesson title in the middle
- Starting page number on the right (global page number)
- Dotted paper background

This page is **read-only** in the web UI. It updates automatically.

#### 2. Sidebar Navigation Drawer

A slide-in panel accessible from anywhere in the notebook. Lists all lessons with their titles and creation dates. Clicking a lesson jumps to its first page.

The sidebar is separate from the physical index page and is purely a navigation UX element. It does not appear in PDF exports.

### Page Number Calculation

Global page numbers work as follows:
- Cover page: no number (or displayed as "Cover")
- Index page: **page 1**
- Lesson 1, Page 1: **page 2** (if lesson 1 has 3 pages: 2, 3, 4)
- Lesson 2, Page 1: **page 5**
- Lesson 2, Page 2: **page 6**
- etc.

Formula: `startPageNumber = 2 + sum(pageCount of all previous lessons)`

The `GET /notebooks/{id}/index` endpoint pre-calculates `startPageNumber` for every lesson.

---

## 11. PDF Export Pipeline

### Export Modes

| Mode | Description |
|---|---|
| Whole notebook | All lessons included. Index page prepended. |
| Single lesson | One lesson's pages only. No index page. |
| Multiple lessons | Selected lessons. Index page prepended (containing only the selected lessons). |

### Async Flow

PDF generation is asynchronous because it can take several seconds for large notebooks.

```
1.  User clicks "Export PDF" → selects scope (whole / single / selection)
2.  POST /exports → returns exportId, status = "Pending"
3.  Frontend connects to SignalR hub (if not already connected)
4.  Backend processes: loads data → renders PDF with QuestPDF → uploads to Azure Blob
5.  SignalR pushes "PdfReady" event to the user's connection
6.  Frontend receives event → shows download button / notification
7.  User clicks download → GET /exports/{id}/download → browser downloads PDF
```

**Constraint:** Only one active export per notebook at a time. Attempting a second export while one is Pending or Processing returns **409 Conflict**.

### Export Lifecycle

```
Pending → Processing → Ready ──→ (24h) → [deleted]
                    ↘ Failed
```

- `Pending`: queued, not yet started
- `Processing`: being rendered
- `Ready`: available for download (download link active)
- `Failed`: error occurred during rendering (show error to user)
- After 24 hours from completion: the export record and Azure Blob are automatically deleted by a background job

### PDF Content

The PDF is pixel-faithful to the on-screen appearance:

| Page | Content |
|---|---|
| Cover page | Solid color background (notebook's `coverColor`). Centered notebook title, instrument name, owner's display name, creation date. |
| Index page | Dotted paper background. Table of contents with lesson titles and page numbers. Page number: 1. |
| Lesson pages | Dotted paper background. Modules at exact grid positions. Module styles applied. Building blocks rendered. Global page numbers in bottom corner. |

**Font rendering:** The PDF uses the same font families as the web UI.

**Dotted paper:** Each page (except cover) has a background of small dots at exactly 5mm intervals, rendered as vector graphics.

### Polling Fallback

If SignalR is unavailable, the frontend can poll `GET /exports/{id}` every 3 seconds until status changes from `Pending`/`Processing` to `Ready` or `Failed`.

---

## 12. Real-time Features (SignalR)

### Hub Connection

Connect to the SignalR hub after successful authentication:

```typescript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Events

#### PdfReady

Fired when a PDF export completes successfully.

```typescript
connection.on('PdfReady', (exportId: string, fileName: string) => {
  // Show notification to user
  // Enable download button
});
```

The `fileName` is the suggested download file name (e.g. `"Guitar Journey 2025.pdf"`).

#### PdfFailed

Fired when a PDF export fails.

```typescript
connection.on('PdfFailed', (exportId: string, errorCode: string) => {
  // Show error notification to user
});
```

### Connection Lifecycle

- Connect after login, disconnect on logout.
- Use `withAutomaticReconnect()` to handle transient disconnections.
- If connection drops during an export, use polling as a fallback (see Section 11).

---

## 13. User Account Management

### Profile

Users can update their first name, last name, language, default page size, and default instrument from the profile/settings page.

The `defaultPageSize` and `defaultInstrument` are used to pre-fill the notebook creation form.

### Avatar

- Google OAuth users get their Google profile picture automatically.
- Local account users get an auto-generated initials avatar by default.
- Any user can upload a custom avatar (JPG, PNG, WebP, max 2MB).

### Account Deletion (Soft Delete with 30-day Grace Period)

```
1. User requests deletion → DELETE /users/me
2. Account is marked for deletion (ScheduledDeletionAt = now + 30 days)
3. User can still log in and use the app during the grace period
4. A warning banner appears on the profile/settings page:
   "Your account is scheduled for deletion on [date]. Cancel deletion."
5. User can cancel: POST /users/me/cancel-deletion
6. After 30 days: background job permanently deletes the account and all data
```

**UI requirement:** The deletion countdown/cancellation UI must appear on the profile/settings page whenever `user.scheduledDeletionAt` is not null.

---

## 14. Error Handling

### Infrastructure Errors (Problem Details — RFC 7807)

For unexpected server errors, the API returns standard Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred."
}
```

### Business Rule Errors (Custom Format)

For validation and business logic errors, the API returns:

```json
{
  "code": "MODULE_OVERLAP",
  "message": "The module overlaps with an existing module on this page.",
  "details": {
    "conflictingModuleId": "uuid"
  }
}
```

### Error Codes Reference

| Code | HTTP Status | Description |
|---|---|---|
| `MODULE_OVERLAP` | 422 | Module placement overlaps with an existing module |
| `MODULE_OUT_OF_BOUNDS` | 422 | Module extends beyond the page boundaries |
| `MODULE_TOO_SMALL` | 422 | Module dimensions are below the minimum for its type |
| `DUPLICATE_TITLE_MODULE` | 409 | A Title module already exists in this lesson |
| `INVALID_BUILDING_BLOCK` | 422 | A building block type is not allowed in the module type |
| `BREADCRUMB_CONTENT_NOT_EMPTY` | 422 | Breadcrumb module content must be empty |
| `NOTEBOOK_PAGE_SIZE_IMMUTABLE` | 400 | Page size cannot be changed after notebook creation |
| `NOTEBOOK_INSTRUMENT_IMMUTABLE` | 400 | Instrument cannot be changed after notebook creation |
| `LAST_PAGE_DELETION` | 400 | Cannot delete the last remaining page of a lesson |
| `ACTIVE_EXPORT_EXISTS` | 409 | An export is already in progress for this notebook |
| `EXPORT_NOT_READY` | 404 | Export download requested but export is not Ready |
| `EXPORT_EXPIRED` | 404 | Export download link has expired (24h elapsed) |
| `PRESET_NOT_FOUND` | 404 | Style preset not found |
| `ACCOUNT_DELETION_ALREADY_SCHEDULED` | 409 | Account is already scheduled for deletion |
| `ACCOUNT_DELETION_NOT_SCHEDULED` | 400 | Cancel deletion called on a non-scheduled account |
| `DUPLICATE_PRESET_NAME` | 409 | A user-saved preset with this name already exists |
| `INSTRUMENT_NOT_FOUND` | 404 | The specified instrument does not exist in the database |
| `INVALID_GOOGLE_TOKEN` | 400 | Google ID token validation failed |

### Validation Errors (FluentValidation)

Field-level validation errors use the standard ASP.NET validation error format:

```json
{
  "errors": {
    "email": ["The email field is required.", "Invalid email format."],
    "password": ["Password must be at least 8 characters."]
  }
}
```

Validation error messages are localized based on the `Accept-Language` request header.

---

## 15. Localization

The application supports two languages:

| Language | `Accept-Language` value | Notes |
|---|---|---|
| English | `en` | Default |
| Hungarian | `hu` | Full support |

### Frontend Responsibilities

- Send `Accept-Language: en` or `Accept-Language: hu` with every API request based on the user's `language` setting.
- All static UI strings are managed by the frontend (not the API).
- Date formatting should respect locale:
  - English: `February 23, 2025`
  - Hungarian: `2025. február 23.`
- Use the user's `language` field from `GET /users/me` after login (`"en"` or `"hu"`).
- Store the selected language in the user's profile (synced with the backend via `PUT /users/me`).

### Backend Responsibilities

- Validation error messages are returned in the language specified by `Accept-Language`.
- Business rule error `message` fields are localized.
- `code` fields are always in English (for frontend programmatic handling).

---

## 16. Business Rules & Validation

This section consolidates all business rules that the frontend should be aware of for a smooth user experience (show errors before sending to the server).

### Notebook Rules

| Rule | Details |
|---|---|
| Instrument immutable | Cannot change after creation |
| Page size immutable | Cannot change after creation |
| Cover color | Must be a valid hex color string (`#RRGGBB`) |
| Style update | Must include all 12 module types |

### Lesson Rules

| Rule | Details |
|---|---|
| Title required | Lesson title is mandatory, max 200 chars |
| First page auto-created | Creating a lesson always creates LessonPage #1 |
| Ordering | Always by creation date ascending; no manual reordering |

### LessonPage Rules

| Rule | Details |
|---|---|
| Minimum 1 page | A lesson must always have at least one page |
| Soft limit | Warning shown at 10 pages (no hard block) |
| Page numbers | Sequential within the lesson; auto-assigned; not user-editable |

### Module Rules

| Rule | Details |
|---|---|
| No overlap | Modules on the same page cannot overlap |
| Page boundary | Module must fit entirely within the page |
| Minimum size | Per module type — see table in Section 5.1 |
| Title uniqueness | Only one Title module per lesson (across all pages) |
| Title position | Title module should be first on the first page (frontend convention; not strictly enforced by backend position) |
| Breadcrumb content | Always empty `[]`; content derived dynamically |
| Allowed blocks | Building block types must match the module type's allowed set |
| ZIndex | Must be ≥ 0 |

### Building Block Rules

| Rule | Details |
|---|---|
| Order preserved | The order in the `content` array is the display order |
| Date format | ISO 8601 `YYYY-MM-DD` |
| Musical note values | Must be from: `C, C#, D, D#, E, F, F#, G, G#, A, A#, B` |
| Chord progression beats | Beat sum per measure must equal time signature numerator |
| Chord references | `chordId` must reference an existing chord in the library |

### Authentication Rules

| Rule | Details |
|---|---|
| Password complexity | Min 8 chars, uppercase, lowercase, digit required |
| Email uniqueness | One account per email address |
| Refresh token rotation | Token is replaced on every use |
| Concurrent exports | One per notebook (Pending or Processing status) |

---

## 17. Frontend Implementation Notes

This section provides guidance specifically for designing and building the React TypeScript frontend.

### Recommended Technology Stack

| Concern | Recommendation | Notes |
|---|---|---|
| Build tool | Vite | Fast HMR, native ESM |
| State management | Zustand | Lightweight, minimal boilerplate |
| Server state | TanStack Query (React Query) | Caching, background refresh, optimistic updates |
| Routing | React Router v7 | File-based or config routing |
| HTTP client | Axios or Fetch API | Interceptors for token injection and refresh |
| SignalR client | `@microsoft/signalr` | Official Microsoft client |
| Drag and drop | dnd-kit | Modern, accessible, composable |
| Canvas / grid | CSS Grid + absolute positioning | For the module grid canvas |
| PDF viewer | None needed | Direct download via API |
| Localization | react-i18next | Language switching |
| Forms | React Hook Form + Zod | Validation schema mirrors backend rules |
| Styling | Tailwind CSS | Utility-first, easy theming |

### Token Management

```typescript
// Token store (Zustand)
interface AuthStore {
  accessToken: string | null;
  setAccessToken: (token: string | null) => void;
}

// Axios interceptor for silent refresh
axiosInstance.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status === 401 && !error.config._retry) {
      error.config._retry = true;
      const { accessToken } = await post('/auth/refresh');
      setAccessToken(accessToken);
      error.config.headers.Authorization = `Bearer ${accessToken}`;
      return axiosInstance(error.config);
    }
    return Promise.reject(error);
  }
);
```

### Grid Canvas Implementation

The module grid is the most complex UI component. Key considerations:

```typescript
interface GridCanvasProps {
  page: LessonPage;
  modules: Module[];
  pageSize: PageSize;
  styles: NotebookModuleStyle[];
  zoom: number;
  onModuleMove: (id: string, x: number, y: number) => void;
  onModuleResize: (id: string, w: number, h: number) => void;
  onModuleSelect: (id: string) => void;
}
```

**Rendering the grid:**
- Use a CSS background with `radial-gradient` for dots: `background: radial-gradient(circle, #999 1px, transparent 1px); background-size: {dotSpacingPx}px {dotSpacingPx}px`
- Position modules absolutely using pixel values derived from grid coordinates + zoom level
- Snap to grid: round pixel positions to nearest grid unit on drag end

**Drag and drop with dnd-kit:**
- Each module is a draggable item
- The canvas is a droppable area
- On drag end: calculate new grid position (snap to grid), validate locally (overlap + boundary), call `PATCH /modules/{id}/layout` (debounced)
- If server returns 422: revert to previous position with an error message

**Resize handles:**
- Add 8-point resize handles (4 corners + 4 edge midpoints) to selected modules
- On resize end: snap to grid, validate minimum size, call `PATCH /modules/{id}/layout`

**Z-Index management:**
- Modules with higher `ZIndex` render on top
- Context menu on module: "Bring to Front" (set ZIndex to max+1), "Send to Back" (set ZIndex to 0)

### Module Style Application

When rendering a module, look up its `NotebookModuleStyle` by matching `moduleType`:

```typescript
function getModuleStyle(
  moduleType: ModuleType,
  styles: NotebookModuleStyle[]
): NotebookModuleStyle {
  return styles.find(s => s.moduleType === moduleType)!;
}
```

Apply styles as inline CSS or CSS variables:

```typescript
const style: React.CSSProperties = {
  backgroundColor: moduleStyle.backgroundColor,
  border: moduleStyle.borderStyle !== 'None'
    ? `${moduleStyle.borderWidth}px ${moduleStyle.borderStyle.toLowerCase()} ${moduleStyle.borderColor}`
    : 'none',
  borderRadius: `${moduleStyle.borderRadius}px`,
  color: moduleStyle.bodyTextColor,
  fontFamily: FONT_FAMILY_MAP[moduleStyle.fontFamily],
};
```

### Breadcrumb Module

The Breadcrumb module has no `content`. Instead, the frontend derives its display from the lesson's subtitle modules:

```typescript
function getBreadcrumbContent(modules: Module[]): string[] {
  return modules
    .filter(m => m.moduleType === 'Subtitle')
    .sort((a, b) => a.gridY - b.gridY || a.gridX - b.gridX)
    .flatMap(m => m.content
      .filter(b => b.type === 'Text')
      .map(b => (b as TextBlock).spans.map(s => s.text).join(''))
    );
}
```

Display as: `→ Subtitle 1 → Subtitle 2 → Subtitle 3`

### Chord Progression Rendering

Render as a horizontal scrollable row of chord badges:

```
||: [C] [Am] [F] [G] :||
    (4)  (4)  (4) (4)
```

- Each chord is a colored pill/badge
- Beat count shown below in smaller text
- `||:` and `:||` symbols at repeat boundaries
- Section label above the section (if set)
- Time signature shown at the start

### Musical Notes Rendering

```typescript
const CHROMATIC_SCALE = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];

function getInterval(from: string, to: string): 'S' | 'W' | number {
  const fromIdx = CHROMATIC_SCALE.indexOf(from) % 12;
  const toIdx = CHROMATIC_SCALE.indexOf(to) % 12;
  const distance = ((toIdx - fromIdx) + 12) % 12;
  if (distance === 1) return 'S';
  if (distance === 2) return 'W';
  return distance;
}
```

Render each note as a circular badge, with the interval label between consecutive badges.

### Auto-Save Strategy

All content changes should be saved automatically with debouncing:

```typescript
const debouncedSave = useMemo(
  () => debounce((moduleId: string, content: BuildingBlock[]) => {
    patchModule(moduleId, { content });
  }, 1000),
  []
);

// In the editor:
onChange={(newContent) => {
  setLocalContent(newContent);   // immediate local update
  debouncedSave(module.id, newContent);  // debounced server sync
}}
```

For layout changes (drag/resize), use a shorter debounce of 500ms.

### Navigation Structure

```
/ (root)
├── /login
├── /register
├── /app (requires auth)
│   ├── /app/notebooks (dashboard — list of notebooks)
│   ├── /app/notebooks/new (create notebook)
│   ├── /app/notebooks/:notebookId (notebook view)
│   │   ├── Default view: shows cover page
│   │   ├── /index (index page view)
│   │   └── /lessons/:lessonId/pages/:pageId (lesson page editor)
│   ├── /app/profile (user profile & settings)
│   └── /app/exports (export history)
```

### Notebook View UX

The notebook should feel like a physical book:

1. **Cover view**: Shows the notebook cover (colored background with title/instrument).
2. **Index view**: Shows the auto-generated table of contents on dotted paper.
3. **Lesson page view**: Shows the 2D grid canvas editor for the current page.
4. **Navigation**: Previous/next page arrows at the bottom of the canvas. Page indicator (e.g. "Page 3 / 12").
5. **Sidebar**: Notebook index drawer accessible via a button (bookmark icon). Clicking a lesson entry navigates to its first page.
6. **Jump to lesson**: Clicking a lesson in the sidebar navigates to `LessonPage` with `pageNumber = 1`.

### Export UX Flow

```typescript
// 1. User initiates export
const { exportId } = await postExport({ notebookId, lessonIds: null });

// 2. Listen for SignalR notification
connection.on('PdfReady', (id, fileName) => {
  if (id === exportId) showDownloadButton(id, fileName);
});

// 3. Fallback polling (if SignalR unavailable)
const poll = setInterval(async () => {
  const { status } = await getExport(exportId);
  if (status === 'Ready') { clearInterval(poll); showDownloadButton(); }
  if (status === 'Failed') { clearInterval(poll); showError(); }
}, 3000);

// 4. Download
window.location.href = `/api/exports/${exportId}/download`;
// OR use fetch + blob URL for progress indication
```

### Chord Selector Component

The chord selector is used when adding `ChordTablatureGroup` or `ChordProgression` building blocks:

1. Display an instrument selector (only instruments in the chord library: currently Guitar6String only)
2. Display a root note grid (12 notes, A–G#)
3. Display a quality list (major, minor, 7, maj7, etc.)
4. On selection: call `GET /chords?instrument=...&root=...&quality=...`
5. Display matching chords as thumbnail fretboard diagrams
6. If multiple positions exist: show position tabs/arrows
7. On confirm: add the selected `{ chordId, label }` to the building block

### Module Minimum Size Enforcement

```typescript
const MODULE_MIN_SIZES: Record<ModuleType, { w: number; h: number }> = {
  Title:         { w: 20, h: 4 },
  Breadcrumb:    { w: 20, h: 3 },
  Subtitle:      { w: 10, h: 3 },
  Theory:        { w: 8,  h: 5 },
  Practice:      { w: 8,  h: 5 },
  Example:       { w: 8,  h: 5 },
  Important:     { w: 8,  h: 4 },
  Tip:           { w: 8,  h: 4 },
  Homework:      { w: 8,  h: 5 },
  Question:      { w: 8,  h: 4 },
  ChordTablature:{ w: 8,  h: 10 },
  FreeText:      { w: 4,  h: 4 },
};
```

### Module Content Editor

The content editor inside a module should:

1. Display existing building blocks in order
2. Provide an "Add block" button that shows only the allowed block types for the current module type
3. Allow drag-to-reorder blocks (dnd-kit within the module)
4. Support inline bold toggling: select text → toggle bold (the only formatting supported)
5. For tables: provide row/column add/remove controls
6. For musical notes: a note picker with all 12 chromatic notes
7. For chord progression: a progression builder UI (add measures, add chords, set time signature, set beats)
8. For chord tablature group: a chord picker that opens the chord selector component

### Key Design Principle

The application simulates a physical dotted notebook. All UI decisions should reinforce this metaphor:
- Paper texture or dotted background on canvas pages
- Book-like page transitions when navigating between lessons
- Natural, handwritten-style fonts for notebook content
- Cover page with a tangible, book-cover aesthetic
- Index page styled like a hand-written table of contents

---

*End of Staccato Frontend Design Documentation*
