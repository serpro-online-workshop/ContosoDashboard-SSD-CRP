# Research: Document Upload and Management

## Decision 1: Local Malware Scanning

**Decision**: Add `IFileSafetyScanner` with a local ClamAV adapter. Scan staged files before they become available. Treat unsafe results, scanner errors, timeouts, and missing scanner installation as rejection; never bypass scanning.

**Rationale**: The stakeholder requirements mandate malware scanning and offline operation. The current environment does not have `clamscan` or `clamdscan`, so the runtime prerequisite must be documented and verified. Tests use a deterministic fake scanner.

**Alternatives considered**:
- Cloud scanning service: rejected because it violates offline operation and introduces external credentials and data transfer.
- Accept uploads when scanning is unavailable: rejected because it violates the fail-closed security requirement.
- Extension-only validation: rejected because it does not satisfy malware scanning.

## Decision 2: Local File Storage and Upload Atomicity

**Decision**: Implement `IFileStorageService` with local storage under `AppData/uploads`, outside `wwwroot`. Generate a random storage key independent of the submitted filename. Stage, validate, and scan each file before moving it into final storage and inserting metadata. If metadata persistence fails, remove the committed file; if cleanup fails, log an actionable error without exposing the file.

**Rationale**: This preserves offline operation, prevents user-controlled paths, and follows the stakeholder upload sequence while avoiding public static-file access. The abstraction keeps future provider replacement explicit without adding cloud code now.

**Alternatives considered**:
- Store files under `wwwroot`: rejected because this bypasses authorization checks.
- Use the original name as the path: rejected because of traversal, collisions, and path normalization risks.
- Add Azure Blob Storage now: rejected because the training constitution requires local-only operation.

## Decision 3: SQLite Schema Evolution

**Decision**: Add an idempotent schema initializer for the new document tables and indexes, preserving existing tables and records. Keep the EF Core model as the query and change-tracking source; the initializer must be safe to run repeatedly. A migration baseline is a separate future repository-level change.

**Rationale**: The application currently calls `EnsureCreated` and has no migrations or migration history; it already applies an additive SQLite column change at startup. Applying a conventional migration to this pre-existing database without baselining it could fail or risk data. An additive initializer fits the current training setup and preserves data.

**Alternatives considered**:
- Drop and recreate the database: rejected because it destroys local user and sample data.
- Run `EnsureCreated` after adding entities: rejected because it does not alter an existing database schema.
- Adopt and baseline migrations as part of this feature: deferred because it broadens the database-management change beyond document support.

## Decision 4: Authorization and Share Semantics

**Decision**: Centralize document checks in `DocumentAuthorizationService` and call it from every document operation. Project access derives from current project manager and membership relationships. Direct shares grant preview and download only. Represent team recipients by the existing `User.Department` value because the application has no Team entity; evaluate membership dynamically against the current department.

**Rationale**: Service-level checks uphold the constitution's data-isolation requirement. The direct-share permission was clarified in the spec. Department is the closest existing group concept and avoids adding a new organizational subsystem.

**Alternatives considered**:
- Trust page visibility as authorization: rejected because direct endpoints and future callers could bypass the UI.
- Make a direct share equivalent to project membership: rejected because that grants broader access than requested.
- Add a Team/Group model: deferred because no such concept exists in the current app and the training requirement can map to existing departments.

## Decision 5: Document Search and Tagging

**Decision**: Store tags as child rows, normalize tag values for matching, and implement search, filter, and sort in `DocumentService` queries with authorization predicates applied before counts and results are returned.

**Rationale**: Relational tags support filtering without parsing ad hoc delimited text. Query-time authorization prevents unauthorized rows from leaking through counts or pagination.

**Alternatives considered**:
- Store tags as comma-separated text: rejected because escaping, exact matching, and filtering are error-prone.
- Search the full database and filter after pagination: rejected because unauthorized rows could affect results and page contents.

## Decision 6: UI, Culture, and Testing

**Decision**: Use Blazor Server components and the existing `IStringLocalizer<SharedResource>` resources for `en-US`, `pt-BR`, `es`, and `de`. Add focused xUnit tests using temporary SQLite databases and fake scanner and storage implementations, plus endpoint authorization and upload-flow integration coverage.

**Rationale**: This follows existing page, service, and localization conventions. No test project exists, so repeatable automated coverage is part of the feature rather than relying only on manual testing.

**Alternatives considered**:
- Hard-coded strings or a new localization framework: rejected because resource-based localization is already configured and required by the constitution.
- Manual tests only: rejected because upload validation, access boundaries, and file cleanup are security-sensitive.

## Open Operational Prerequisite

The developer or training host must install and configure the local ClamAV scanner before accepting uploads. The application remains usable without it, but upload attempts fail closed with a clear error. Scanner installation is not automated by the application.
