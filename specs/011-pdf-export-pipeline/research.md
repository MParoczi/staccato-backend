# Research: PDF Export Pipeline

**Branch**: `011-pdf-export-pipeline` | **Date**: 2026-03-29

## R1: Channel Abstraction for Domain Purity

**Decision**: Define `IPdfExportQueue` interface in `Domain/Interfaces/` with a single `EnqueueAsync(Guid exportId, CancellationToken ct)` method. Implement as `PdfExportChannel` in `Application/Channels/` wrapping `Channel<Guid>`.

**Rationale**: Domain must not reference Application or any infrastructure type. The channel itself is an infrastructure concern (bounded capacity, full-mode policy). By abstracting behind an interface, `PdfExportService` (Domain) can enqueue without knowing the transport. The background service in Application reads directly from `PdfExportChannel.Reader`.

**Alternatives considered**:
- Inject `Channel<Guid>` directly into Domain — violates Domain purity (infrastructure leak).
- Use event-based pub/sub — over-engineered for a single producer/consumer pair.
- Use database polling instead of channel — higher latency, unnecessary DB load.

**DI registration pattern**:
```
services.AddSingleton<PdfExportChannel>();
services.AddSingleton<IPdfExportQueue>(sp => sp.GetRequiredService<PdfExportChannel>());
```

## R2: PDF Rendering Architecture

**Decision**: Place all QuestPDF rendering classes in `Application/Pdf/`. Create a `StaccatoPdfDocument` class implementing `QuestPDF.Infrastructure.IDocument` as the top-level document definition. Delegate to specialized renderers for each page type and building block type.

**Rationale**: QuestPDF is an infrastructure dependency. Application can reference DomainModels (for data) and QuestPDF (for rendering). This keeps Domain free of rendering concerns. The rendering classes are purely presentational — no business logic.

**Alternatives considered**:
- Place renderers in a new dedicated project (e.g., `PdfRendering`) — violates the 9-project constitution. Not justified for a single feature.
- Render in Domain using an abstraction layer — over-engineered. PDF rendering is inherently infrastructure.

**File layout**:
```
Application/Pdf/
├── PdfDataLoader.cs           # Aggregates data from repositories
├── PdfRenderModels.cs         # POCOs for rendering (no DomainModel dependency in renderers)
├── StaccatoPdfDocument.cs     # IDocument implementation
├── DottedPaperBackground.cs   # Reusable 5mm dot grid component
├── CoverPageRenderer.cs       # Cover page composition
├── IndexPageRenderer.cs       # Table of contents composition
├── LessonPageRenderer.cs      # Lesson page with module grid
├── ModuleRenderer.cs          # Single module box with style
└── BuildingBlockRenderers.cs  # All 10 building block type renderers
```

## R3: PDF Data Loading Strategy

**Decision**: Create `PdfDataLoader` in `Application/Pdf/` that injects repository interfaces and loads all data in a structured `PdfExportData` record. Use multiple focused queries rather than one monolithic query.

**Rationale**: The PDF renderer needs deeply nested data: Notebook (with styles) → Lessons (filtered) → LessonPages (ordered) → Modules (per page) → plus Chord data for chord-referencing building blocks. A single query would be complex and brittle. Multiple queries are simpler and each repository method already exists.

**Alternatives considered**:
- Add a `LoadFullNotebookAsync` method to an existing service — bloats the service with rendering-specific concerns.
- Raw SQL query joining all tables — bypasses repository pattern, violates architecture.

**Data loading sequence**:
1. Load PdfExport record (get notebookId, lessonIds)
2. Load Notebook (title, coverColor, pageSize, instrumentName, user info)
3. Load NotebookModuleStyles for the notebook (12 records)
4. Load Lessons (all or filtered by lessonIds), ordered by CreatedAt
5. For each lesson: load LessonPages ordered by PageNumber
6. For each page: load Modules
7. Load referenced Chord data (extract chord IDs from ChordTablatureGroup and ChordProgression building blocks)

## R4: Building Block Deserialization

**Decision**: Deserialize `Module.ContentJson` (a JSON array) into typed building block objects using `System.Text.Json` with a custom `JsonConverter` that reads the `"type"` discriminator field.

**Rationale**: The ContentJson is a polymorphic JSON array where each element has a `"type"` field matching `BuildingBlockType`. A discriminator-based converter is the standard System.Text.Json approach and avoids manual parsing.

**Alternatives considered**:
- Manual `JsonDocument` parsing per element — error-prone, no type safety.
- `Newtonsoft.Json` — not in the approved technology stack.

**Note**: The abstract `BuildingBlock` base class and concrete subclasses already exist in `DomainModels/BuildingBlocks/`. The converter reads `"type"`, matches to `BuildingBlockType` enum, and deserializes to the concrete type.

## R5: Chord Diagram Rendering with QuestPDF

**Decision**: Use QuestPDF's `Canvas` API (which provides an `SKCanvas` from SkiaSharp) to draw fretboard diagrams as vector graphics. Each chord diagram renders: string lines (vertical), fret lines (horizontal), finger position dots, barre indicators, open/muted string markers, fret number label, and chord name.

**Rationale**: QuestPDF's Canvas API exposes SkiaSharp drawing primitives, perfect for vector diagram rendering. The diagrams scale cleanly at any resolution and match the visual language of chord diagram conventions.

**Alternatives considered**:
- SVG rendering via external library — adds a dependency outside the approved stack.
- Raster image generation — lower quality, doesn't scale.

**Rendering approach**:
- 6 vertical lines for strings (or N strings based on instrument)
- 4 horizontal fret lines
- Filled circles at fret positions (finger placements)
- Rounded rectangle for barre
- `X` above string for muted, `O` for open
- BaseFret number label on left when > 1

## R6: Stale Export Recovery

**Decision**: `PdfExportBackgroundService.StartAsync` queries for all exports with `Status == Processing` and resets them to `Status = Pending` before starting the channel reader loop. This allows the channel-based processing to pick them up naturally.

**Rationale**: If the server crashes while processing an export, the export remains stuck in Processing. On restart, resetting to Pending re-enqueues the intent. Since the channel is empty on fresh start, these recovered exports must also be written back to the channel.

**Recovery sequence**:
1. Query all exports with Status == Processing
2. For each: update Status = Pending, commit
3. For each: write exportId to channel
4. Start channel reader loop

## R7: Expired Export Cleanup Scope

**Decision**: Modify `IPdfExportRepository.GetExpiredExportsAsync` to return both:
- Ready exports where `CompletedAt + 24h <= utcNow`
- Failed exports where `CreatedAt + 24h <= utcNow`

**Rationale**: The current implementation excludes Failed exports. Per spec clarification, Failed exports should also be cleaned up after 24 hours (from creation time) to prevent database bloat.

**Alternatives considered**:
- Add a separate `GetFailedExportsOlderThanAsync` method — unnecessarily splits cleanup logic.
- Add `ExpiresAt` column to PdfExport entity — unnecessary given the simple 24h calculation from existing timestamps.

## R8: Failure Notification

**Decision**: Add `Task PdfFailed(string exportId, string errorCode)` method to `INotificationClient` interface. Call it from the background service when an export fails, providing an error code (`RENDER_FAILED` or `UPLOAD_FAILED`) indicating the failure type.

**Rationale**: Per spec clarification (FR-013a), users should be notified of failure in real-time with an error code identifying the failure type. The existing `PdfReady` method handles success; a separate `PdfFailed` method with an errorCode parameter is cleaner than overloading the success method with a status parameter.
