# Quickstart: Document Upload and Management

## Prerequisites

- .NET 10 SDK.
- Existing SQLite development configuration (`ContosoDashboard/ContosoDashboard.db` is created by the app).
- ClamAV installed locally and its scanner executable available to the application process. The application intentionally rejects uploads if the scanner is unavailable.
- No cloud credentials or internet access are required for document operations.

## Build and Run

From the repository root:

```sh
dotnet restore ContosoDashboard/ContosoDashboard.csproj
dotnet build ContosoDashboard/ContosoDashboard.csproj
dotnet test ContosoDashboard.Tests/ContosoDashboard.Tests.csproj
dotnet run --project ContosoDashboard/ContosoDashboard.csproj --urls http://127.0.0.1:5000
```

Open `http://127.0.0.1:5000`, sign in with a training user, and select a supported UI language. The application must preserve existing SQLite users, projects, and tasks after additive document schema initialization.

## Validation Scenarios

1. **Successful upload**: As an employee, upload a clean PDF under 25 MB with a title and category. Confirm progress and success feedback; find its metadata in My Documents; confirm the stored file is outside `wwwroot` under a generated key.
2. **Validation rejection**: Attempt an oversized file, unsupported extension, mismatched file type, and empty title. Confirm clear rejection and no accessible document record or content.
3. **Fail-closed scanning**: Stop or misconfigure the local scanner and attempt an upload. Confirm the UI reports the scanner unavailable and the file remains unavailable; restore the scanner and confirm a clean file can then be uploaded.
4. **Authorization**: Sign in as a non-member and try to list, search, preview, download, replace, delete, and share a project document by direct URL. Confirm no metadata or content disclosure. Repeat as a project member, manager, owner, direct-share recipient, and administrator against the [authorization matrix](contracts/documents.md).
5. **Direct share**: Share a document with a specific user. Confirm an in-app notification and visibility in Shared with Me; confirm the recipient can preview/download but cannot edit, replace, delete, or reshare based on the direct share alone.
6. **Persistence and recovery**: Interrupt storage or database writes. Confirm no partial document becomes accessible, cleanup is attempted, and previously saved metadata/file state remains unchanged.
7. **Search and performance**: Seed up to 500 accessible and inaccessible documents. Verify sort/filter/search results, access filtering, and the two-second list/search targets.
8. **Localization and dashboard integration**: Repeat the principal upload and browse journey in English, pt-BR, Spanish, and German. Verify localized messages/date formats, task attachment project association, Recent Documents, document count, and notifications.
9. **Audit report**: As an administrator, perform upload, download, delete, and share operations; confirm each appears in activity reporting and non-administrators cannot access the report.

## Test Strategy

- Unit-test extension, MIME, size, category, tag, project/task authorization, and scanner-state validation.
- Test upload cleanup and state transitions with temporary storage and a fake scanner.
- Test metadata persistence and additive SQLite initialization using temporary SQLite databases, including a database containing pre-existing users/projects/tasks.
- Test every content endpoint permission and generic not-found behavior for inaccessible documents.
- Run browser-level manual scenarios for progress, previews, localization, and dashboard integration where automated component tests are insufficient.

## Performance and Environment Checks

Run the focused automated checks from the repository root:

```sh
dotnet test ContosoDashboard.Tests/ContosoDashboard.Tests.csproj --no-restore --filter 'FullyQualifiedName~FileAt25MbLimitIsAccepted|FullyQualifiedName~SearchForFiveHundredAccessibleDocumentsCompletesWithinTwoSeconds|FullyQualifiedName~BatchUploadReportsSuccessAndFailureIndependently|FullyQualifiedName~AuthorizedLocalPdfPreviewStreamsWithinThreeSeconds'
```

The 25 MB upload boundary, 500-document search under two seconds, three-file independent results, and authorized local-file preview stream under three seconds have automated coverage. The preview timing measures file open and endpoint streaming, not browser rendering. The upload service still fails closed in tests unless the scanner fake reports Clean; these tests do not represent a ClamAV installation check.

On a host with ClamAV installed, also run the application and verify that three files can be selected and processed independently, including one rejected file, and that a clean PDF/image preview loads within three seconds. Repeat with ClamAV stopped or misconfigured and confirm the upload is rejected without an available document record. Record the host, scanner version, and measured preview duration when validating these runtime targets.
