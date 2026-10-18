# Document Service and Content Contracts

These are internal application contracts for the Blazor Server feature. Metadata operations remain in application services; the HTTP surface is limited to authorized content streaming.

## In-Process Service Operations

| Operation | Input | Result | Authorization |
|---|---|---|---|
| `CreateAsync` | upload metadata and one file stream | document metadata or per-file validation result | current user; project/task access checked before save |
| `SearchAsync` | query, category/project/date filters, sort, paging | only documents accessible to current user | authorization predicate applied before count and page |
| `GetAccessibleAsync` | document id and current user | metadata or not found | owner, project manager/member, department share, or direct share |
| `UpdateMetadataAsync` | document id and editable metadata | updated metadata | owner or project manager; direct share is read-only |
| `ReplaceFileAsync` | document id and replacement stream | updated metadata after validation and scan | owner or project manager |
| `DeleteAsync` | document id and confirmation | success/not found/forbidden | owner or project manager; permanently removes stored file and access records |
| `ShareAsync` | document id and user/department recipient | share record and in-app notification | owner or project manager |
| `GetTaskDocumentsAsync` | task id and current user | accessible associated documents | task access plus document access |
| `GetRecentAsync` | current user and limit of 5 | recent uploaded documents | current user's own uploads |
| `GetAdminReportAsync` | report range and filters | file-type, uploader, and access summaries | Administrator role only |

`IFileStorageService` exposes stage, commit, open-read, and delete operations using generated relative storage keys. `IFileSafetyScanner` scans a staged file and returns Clean, Unsafe, or Unavailable. Unavailable is not a successful scan.

## Authorized Content Endpoint

### `GET /documents/{documentId:int}/content?disposition=inline|attachment`

- Requires an authenticated user.
- Resolves document metadata through `IDocumentService.GetAccessibleAsync` before opening storage.
- Only a Clean document can be returned.
- `inline` is permitted for PDF and JPEG/PNG previews; unsupported preview types return `415 Unsupported Media Type`.
- `attachment` downloads any supported file the caller may access.
- Uses the persisted validated content type and a sanitized original filename for `Content-Disposition`; sets `X-Content-Type-Options: nosniff`.
- A missing document or inaccessible document returns the same not-found response to avoid disclosing existence.
- Storage failures return a generic server error and do not expose the local storage path.

## Upload Behavior

- Blazor `InputFile` submits each selected file through `IDocumentService.CreateAsync`; each file has an independent result.
- The UI reports progress and final result per file. No document row or content access is created for rejected or failed files.
- Metadata and files are not accepted from a public/static-file URL.

## Error Semantics

- `400`: invalid title/category/tags, unsupported type, or file exceeds 25 MB.
- `401`: caller is not authenticated.
- `404`: document is absent or caller lacks access.
- `415`: requested inline preview is unsupported.
- `422`: malware scan reports Unsafe or Unavailable; user receives a safe actionable message.
- `500`: unexpected storage/database failure; no local path or scanner output is returned to the caller.
