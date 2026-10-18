---
description: "Task list for document upload and management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`
**Prerequisites**: `plan.md`, `spec.md`; supporting documents: `research.md`, `data-model.md`, `contracts/documents.md`, `quickstart.md`

**Tests**: Included because the approved plan calls for a focused xUnit project, and the constitution requires automated verification for security-sensitive behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as an independent increment after the shared foundation.

**Task format**: `- [ ] T### [P?] [US#?] Action with exact file path`

## Phase 1: Setup

**Purpose**: Prepare automated verification and protect runtime upload data.

- [X] T001 Create the `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` xUnit test project targeting `net10.0`, reference `ContosoDashboard/ContosoDashboard.csproj`, and add the test SDK and SQLite test dependencies.
- [X] T002 [P] Add `ContosoDashboard/AppData/uploads/` to the repository `.gitignore` so uploaded documents and staging files are never committed.

---

## Phase 2: Foundational

**Purpose**: Add the shared persistence model and authorization primitives required by every story. No story work begins before this phase is complete.

- [X] T003 [P] Add document entity validation tests in `ContosoDashboard.Tests/Models/DocumentTests.cs` covering the exact data-model constraints: `Title: Non-empty; maximum 255 characters.`, `Description: Maximum 2,000 characters.`, `Category: One of Project Documents, Team Resources, Personal Files, Reports, Presentations, Other.`, `OriginalFileName: Display metadata only; never used as a storage path.`, `StorageKey: Unique generated relative key; never an absolute or web-root path.`, `FileSize: Greater than zero and at most 25 MB per file.`, `ContentType: Validated MIME value; maximum 255 characters.`, and `SafetyStatus: Pending, Clean, Rejected; only Clean documents are accessible.`
- [X] T004 [P] Add document tag validation tests in `ContosoDashboard.Tests/Models/DocumentTagTests.cs` for the exact constraint `Trimmed, non-empty, bounded length; normalized value is unique per document.`
- [X] T005 [P] Add share and activity model tests in `ContosoDashboard.Tests/Models/DocumentAccessModelTests.cs`, including `exactly one recipient kind is set`, the direct-share read-only rule, UTC activity timestamps, and the rule `Non-sensitive event context; never store file contents or credentials.`
- [X] T006 [P] Add idempotent existing-database schema tests in `ContosoDashboard.Tests/Data/DocumentSchemaInitializerTests.cs`, starting with a SQLite database that already contains the existing user, project, and task tables.
- [X] T007 [P] Add the authorization matrix tests in `ContosoDashboard.Tests/Services/DocumentAuthorizationServiceTests.cs` for owners, project members, project managers, department shares, direct shares, administrators, and unauthorized users.
- [X] T008 Implement the `Document` entity in `ContosoDashboard/Models/Document.cs`, preserving the field constraints quoted in T003 and the user/project/task relationships from `specs/001-document-upload-management/data-model.md`.
- [X] T009 Implement the `DocumentTag` entity in `ContosoDashboard/Models/DocumentTag.cs`, preserving the normalized per-document uniqueness and validation constraints quoted in T004.
- [X] T010 Implement the `DocumentShare` and `DocumentActivity` entities in `ContosoDashboard/Models/DocumentShare.cs` and `ContosoDashboard/Models/DocumentActivity.cs`; enforce the quoted rule `exactly one recipient kind is set` and the activity privacy constraint from T005.
- [X] T011 Configure document, tag, share, and activity DbSets, foreign keys, delete behavior, and search/report indexes in `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [X] T012 Implement an idempotent additive document schema initializer in `ContosoDashboard/Data/DocumentSchemaInitializer.cs` and invoke it from `ContosoDashboard/Program.cs`; preserve the existing SQLite database and its records.
- [X] T013 Implement `IDocumentAuthorizationService` and `DocumentAuthorizationService` in `ContosoDashboard/Services/IDocumentAuthorizationService.cs` and `ContosoDashboard/Services/DocumentAuthorizationService.cs`, then make all T007 role/share cases pass.

**Checkpoint**: Existing data remains intact, the document schema can be initialized repeatedly, and authorization decisions have a tested service-level home.

---

## Phase 3: User Story 1 - Upload Work Documents (Priority: P1) - MVP

**Goal**: An employee uploads supported files with required metadata and can safely find, preview, or download their documents.

**Independent Test**: Sign in as a training employee, upload a clean supported file no larger than 25 MB with a title and category, verify the per-file result and personal document entry, and retrieve its content through an authorized request.

### Tests for User Story 1

- [X] T014 [P] [US1] Add upload validation and workflow tests in `ContosoDashboard.Tests/Services/DocumentUploadTests.cs` for required title/category, allowed formats, the 25 MB limit, multiple-file partial failures, rejected/unavailable scanner results, successful metadata persistence, and cleanup after a persistence failure.
- [X] T015 [P] [US1] Add local storage security tests in `ContosoDashboard.Tests/Services/LocalFileStorageServiceTests.cs` proving generated keys remain under a temporary upload root, original filenames cannot control paths, and staged files are removed on failure.
- [X] T016 [P] [US1] Add scanner adapter tests in `ContosoDashboard.Tests/Services/ClamAvFileSafetyScannerTests.cs` for Clean, Unsafe, timeout, missing executable, and process-error outcomes.
- [X] T017 [P] [US1] Add content endpoint tests in `ContosoDashboard.Tests/Endpoints/DocumentContentEndpointTests.cs` for authorized preview/download, inaccessible IDs returning the same not-found response, non-clean scan states, and unsupported inline preview types.

### Implementation for User Story 1

- [X] T018 [US1] Implement `IFileStorageService` and `LocalFileStorageService` in `ContosoDashboard/Services/IFileStorageService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs`; stage under the configured local root, generate a random relative storage key, and guarantee containment outside `wwwroot`.
- [X] T019 [US1] Implement `IFileSafetyScanner` and `ClamAvFileSafetyScanner` in `ContosoDashboard/Services/IFileSafetyScanner.cs` and `ContosoDashboard/Services/ClamAvFileSafetyScanner.cs`; expose Clean/Unsafe/Unavailable, enforce a bounded scan timeout, and fail closed when ClamAV is absent or cannot finish.
- [X] T020 [US1] Implement `IDocumentService.CreateAsync` and the upload orchestration in `ContosoDashboard/Services/IDocumentService.cs` and `ContosoDashboard/Services/DocumentService.cs`: validate actual size/type and metadata, authorize project/task context, stage, scan, commit the file, persist metadata, and compensate on persistence failure.
- [X] T021 [US1] Register document, storage, scanner, and authorization services and configure the upload root and ClamAV executable in `ContosoDashboard/Program.cs` and `ContosoDashboard/appsettings.json`; keep ClamAV absence non-fatal at startup but reject uploads until available.
- [X] T022 [US1] Add authorized inline-preview and attachment-download handling in `ContosoDashboard/Endpoints/DocumentContentEndpoints.cs` and map it in `ContosoDashboard/Program.cs`, applying T013 authorization before storage access and safe content headers.
- [X] T023 [US1] Create the personal documents/upload page in `ContosoDashboard/Pages/Documents.razor` with multi-file progress, per-file results, required title/category, optional description/project/tags, and a list of the user's documents.
- [X] T024 [US1] Add every new upload, list, preview, and failure message from `ContosoDashboard/Pages/Documents.razor` to `ContosoDashboard/Resources/SharedResource.resx`, `ContosoDashboard/Resources/SharedResource.pt-BR.resx`, `ContosoDashboard/Resources/SharedResource.es.resx`, and `ContosoDashboard/Resources/SharedResource.de.resx`.

**Checkpoint**: The P1 journey works independently; invalid or unscanned files never appear as available documents, and authorized employees can retrieve clean uploads.

---

## Phase 4: User Story 2 - Organize and Find Documents (Priority: P2)

**Goal**: Employees browse personal/project documents and find only documents they are allowed to access.

**Independent Test**: Seed up to 500 documents with different owners, projects, categories, dates, and tags; verify sorting/filtering/search, permission filtering, and PDF/image preview timing.

### Tests for User Story 2

- [X] T025 [P] [US2] Add search, sort, filter, authorization, and paging tests in `ContosoDashboard.Tests/Services/DocumentSearchTests.cs`, including verification that unauthorized documents do not affect returned counts or pages.
- [X] T026 [P] [US2] Add project document access tests in `ContosoDashboard.Tests/Services/ProjectDocumentAccessTests.cs` for project members, project managers, and users removed from a project.

### Implementation for User Story 2

- [X] T027 [US2] Implement `SearchAsync`, personal/project collection queries, sorting, filtering, and paging in `ContosoDashboard/Services/IDocumentService.cs` and `ContosoDashboard/Services/DocumentService.cs`, applying authorization before counts and pagination.
- [X] T028 [US2] Add search controls, category/project/date filters, and title/date/category/size sorting to `ContosoDashboard/Pages/Documents.razor` and preserve selected culture formatting.
- [X] T029 [US2] Add the project documents section and authorized document links to `ContosoDashboard/Pages/ProjectDetails.razor`, using the current project membership/manager authorization service.
- [X] T030 [US2] Add localized search, sort, filter, project-list, and empty-state strings to all four shared resource files: `ContosoDashboard/Resources/SharedResource.resx`, `ContosoDashboard/Resources/SharedResource.pt-BR.resx`, `ContosoDashboard/Resources/SharedResource.es.resx`, and `ContosoDashboard/Resources/SharedResource.de.resx`.

**Checkpoint**: Personal and project collections, search, sorting, and filters return only documents authorized for the current user.

---

## Phase 5: User Story 3 - Manage and Share Documents (Priority: P3)

**Goal**: Owners and project managers maintain documents; owners can grant read-only access to users or departments.

**Independent Test**: As an owner and project manager, edit metadata, replace and delete documents; share a document with a user and department, then verify recipient preview/download, notification, revocation, and denied mutation/resharing.

### Tests for User Story 3

- [X] T031 [P] [US3] Add metadata edit, replacement, and deletion authorization tests in `ContosoDashboard.Tests/Services/DocumentManagementTests.cs`, including failed replacement preserving the prior clean file.
- [X] T032 [P] [US3] Add direct and department share tests in `ContosoDashboard.Tests/Services/DocumentShareTests.cs` for read-only recipient permissions, revocation, department membership changes, and project-role permissions remaining independent.

### Implementation for User Story 3

- [X] T033 [US3] Implement owner/project-manager metadata updates, validated file replacement, and confirmed permanent deletion in `ContosoDashboard/Services/IDocumentService.cs` and `ContosoDashboard/Services/DocumentService.cs`, preserving the previous file if replacement fails.
- [X] T034 [US3] Implement direct-user and department share/revoke operations in `ContosoDashboard/Services/DocumentService.cs` and persist them through `ContosoDashboard/Models/DocumentShare.cs`; direct shares grant only preview/download.
- [X] T035 [US3] Send in-app share notifications and create the “Shared with Me” collection in `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Services/NotificationService.cs`, and `ContosoDashboard/Pages/SharedDocuments.razor`.
- [X] T036 [US3] Add edit, replace, delete-confirmation, share, and revoke controls to `ContosoDashboard/Pages/Documents.razor`; hide unavailable actions without treating UI visibility as authorization.
- [X] T037 [US3] Add localized document-management and sharing text to `ContosoDashboard/Resources/SharedResource.resx`, `ContosoDashboard/Resources/SharedResource.pt-BR.resx`, `ContosoDashboard/Resources/SharedResource.es.resx`, and `ContosoDashboard/Resources/SharedResource.de.resx`.

**Checkpoint**: Owner/project-manager actions are enforced in services; direct-share recipients can preview/download only and lose access when the share is revoked.

---

## Phase 6: User Story 4 - Use Documents Across Dashboard Workflows (Priority: P4)

**Goal**: Documents connect to tasks and dashboard activity, and administrators can review usage reports.

**Independent Test**: Attach a document to a task and confirm project inheritance, verify the five most recent uploads/counts and notifications, then confirm administrator-only activity reporting.

### Tests for User Story 4

- [X] T038 [P] [US4] Add task attachment and project inheritance tests in `ContosoDashboard.Tests/Services/TaskDocumentIntegrationTests.cs`, including task access denial.
- [X] T039 [P] [US4] Add recent-document, document-count, notification, and administrator-report tests in `ContosoDashboard.Tests/Services/DocumentReportingTests.cs`, including denial for non-administrators.

### Implementation for User Story 4

- [X] T040 [US4] Add task document listing/attachment and upload entry points to `ContosoDashboard/Pages/TaskDetails.razor` and route from `ContosoDashboard/Pages/Tasks.razor`; inherit the task's project association in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T041 [US4] Add the five most recent uploads and document count to `ContosoDashboard/Services/DashboardService.cs` and `ContosoDashboard/Pages/Index.razor` for the current user only.
- [X] T042 [US4] Record upload, download, deletion, and share activity in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Endpoints/DocumentContentEndpoints.cs` without logging file contents, secrets, or local paths.
- [X] T043 [US4] Notify authorized project members when a project document is added in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`.
- [X] T044 [US4] Implement administrator-only file-type, uploader, and access reports in `ContosoDashboard/Services/DocumentReportService.cs`, register it in `ContosoDashboard/Program.cs`, and display results in `ContosoDashboard/Pages/DocumentReports.razor`.
- [X] T045 [US4] Add task attachment, dashboard widget, project-upload notification, and activity-report labels to all four shared resources: `ContosoDashboard/Resources/SharedResource.resx`, `ContosoDashboard/Resources/SharedResource.pt-BR.resx`, `ContosoDashboard/Resources/SharedResource.es.resx`, and `ContosoDashboard/Resources/SharedResource.de.resx`.

**Checkpoint**: Task/project integration, dashboard summaries, notifications, and administrator reporting work without exposing unauthorized documents.

---

## Phase 7: Polish and Cross-Cutting Concerns

**Purpose**: Complete operational documentation and cross-story verification.

- [X] T046 [P] Update `README.md` with the ClamAV installation/configuration prerequisite, fail-closed upload behavior, local storage location, and updated quickstart commands.
- [X] T047 [P] Add cross-culture resource coverage tests in `ContosoDashboard.Tests/Localization/DocumentLocalizationTests.cs` for all new document UI keys in `en-US`, `pt-BR`, `es`, and `de`.
- [X] T048 Run `dotnet test ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` and validate each scenario in `specs/001-document-upload-management/quickstart.md`; record any environment-dependent ClamAV prerequisite without weakening fail-closed behavior.
- [X] T049 Verify the 25 MB upload, 500-document list/search, two-second query, three-second preview, and three-action upload targets with checks documented in `specs/001-document-upload-management/quickstart.md`.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001 enables test work and T002 protects runtime files.
- **Foundational (Phase 2)**: Depends on Setup; blocks every user story because the schema and authorization service are shared.
- **US1 (Phase 3, P1)**: Depends on Foundation; delivers the upload MVP.
- **US2 (Phase 4, P2)**: Depends on US1 document persistence and authorization.
- **US3 (Phase 5, P3)**: Depends on the US1 document service; may build after US2 so management UI reuses the completed collection.
- **US4 (Phase 6, P4)**: Depends on the document service and access rules from US1-US3.
- **Polish (Phase 7)**: Depends on all selected stories.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundation; no dependency on other user stories.
- **US2 (P2)**: Reuses US1 Document metadata and service authorization; can be independently verified with seeded records.
- **US3 (P3)**: Reuses US1 documents and adds metadata management and share permissions.
- **US4 (P4)**: Integrates document IDs with Task/Project and adds dashboard/reporting behavior.

### Within Each User Story

- Tests are written before the implementation they verify and should fail until that implementation is complete.
- Models and persistence precede services; services and authorization precede UI and content endpoints.
- Story acceptance scenarios must pass before moving to the next priority.

### Parallel Opportunities

- **Setup**: T001 and T002 touch separate files and can proceed in parallel.
- **Foundation**: T003-T007 write separate test files and can be drafted in parallel after T001; model implementation follows those tests.
- **US1**: T014-T017 cover separate upload, storage, scanner, and endpoint test files and can proceed in parallel after Foundation.
- **US2**: T025 and T026 are independent search and project-access test files.
- **US3**: T031 and T032 are independent management and sharing test files.
- **US4**: T038 and T039 are independent task-integration and reporting test files.
- **Polish**: T046 and T047 edit documentation and test resources separately.

## Parallel Examples

### User Story 1

```text
T014 Document upload validation tests in ContosoDashboard.Tests/Services/DocumentUploadTests.cs
T015 Local storage security tests in ContosoDashboard.Tests/Services/LocalFileStorageServiceTests.cs
T016 Scanner behavior tests in ContosoDashboard.Tests/Services/ClamAvFileSafetyScannerTests.cs
T017 Content endpoint authorization tests in ContosoDashboard.Tests/Endpoints/DocumentContentEndpointTests.cs
```

### User Story 2

```text
T025 Search and paging tests in ContosoDashboard.Tests/Services/DocumentSearchTests.cs
T026 Project document access tests in ContosoDashboard.Tests/Services/ProjectDocumentAccessTests.cs
```

### User Story 3

```text
T031 Metadata/replacement/deletion tests in ContosoDashboard.Tests/Services/DocumentManagementTests.cs
T032 Direct/department share tests in ContosoDashboard.Tests/Services/DocumentShareTests.cs
```

### User Story 4

```text
T038 Task document integration tests in ContosoDashboard.Tests/Services/TaskDocumentIntegrationTests.cs
T039 Dashboard and reporting tests in ContosoDashboard.Tests/Services/DocumentReportingTests.cs
```

## Implementation Strategy

### MVP First

1. Complete Setup (T001-T002) and Foundation (T003-T013).
2. Complete User Story 1 (T014-T024).
3. **Stop and validate** the P1 upload/list/authorized content flow using `specs/001-document-upload-management/quickstart.md`.
4. MVP deliverable: an employee can upload a clean supported file, see its metadata in their personal list, and preview/download it without exposing it publicly.

### Incremental Delivery

1. Foundation establishes the entities, safe persistence, storage/scanning abstractions, and authorization.
2. US1 adds the upload MVP and authorized content retrieval.
3. US2 adds findability and project browsing.
4. US3 adds maintenance and read-only sharing.
5. US4 adds task/dashboard/report integration.
6. Complete Polish only after acceptance scenarios for selected stories pass.