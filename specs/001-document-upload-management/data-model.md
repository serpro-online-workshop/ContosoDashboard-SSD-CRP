# Data Model: Document Upload and Management

## Document

Represents an uploaded work file and its searchable metadata.

| Field | Type | Required | Rules |
|---|---|---:|---|
| DocumentId | integer | yes | Primary key; follows existing User/Project key convention. |
| Title | string | yes | Non-empty; maximum 255 characters. |
| Description | string | no | Maximum 2,000 characters. |
| Category | string | yes | One of Project Documents, Team Resources, Personal Files, Reports, Presentations, Other. |
| OriginalFileName | string | yes | Display metadata only; never used as a storage path. |
| StorageKey | string | yes | Unique generated relative key; never an absolute or web-root path. |
| FileSize | 64-bit integer | yes | Greater than zero and at most 25 MB per file. |
| ContentType | string | yes | Validated MIME value; maximum 255 characters. |
| UploadedAtUtc | datetime | yes | Captured by the service in UTC. |
| UploaderId | integer | yes | Foreign key to User. |
| ProjectId | integer | no | Foreign key to Project; uploader must have project access. |
| TaskId | integer | no | Foreign key to TaskItem; when present, validate its project association. |
| SafetyStatus | enum/string | yes | Pending, Clean, Rejected; only Clean documents are accessible. |

**Relationships**: many-to-one User (uploader); optional many-to-one Project and TaskItem; one-to-many DocumentTag, DocumentShare, and DocumentActivity.

## DocumentTag

Represents one user-provided search tag associated with a document.

| Field | Type | Required | Rules |
|---|---|---:|---|
| DocumentTagId | integer | yes | Primary key. |
| DocumentId | integer | yes | Foreign key to Document; deleted with the document. |
| Value | string | yes | Trimmed, non-empty, bounded length; normalized value is unique per document. |

## DocumentShare

Represents read-only sharing with one user or all members of one department.

| Field | Type | Required | Rules |
|---|---|---:|---|
| DocumentShareId | integer | yes | Primary key. |
| DocumentId | integer | yes | Foreign key to Document. |
| SharedByUserId | integer | yes | Must be the owner or project manager authorized to manage the document. |
| RecipientUserId | integer | conditional | Foreign key to User; exactly one recipient kind is set. |
| RecipientDepartment | string | conditional | Existing User.Department group; exactly one recipient kind is set. |
| CreatedAtUtc | datetime | yes | Captured in UTC. |

**Access rule**: a direct share grants preview and download only. It does not grant metadata edits, replacement, deletion, or resharing. Project role permissions are evaluated independently and remain unchanged.

## DocumentActivity

Provides the audit data used by administrator reports.

| Field | Type | Required | Rules |
|---|---|---:|---|
| DocumentActivityId | integer | yes | Primary key. |
| DocumentId | integer | no | Nullable for rejected uploads that never become a Document row. |
| ActorUserId | integer | yes | Foreign key to User. |
| ActivityType | string/enum | yes | Upload, Download, Delete, Share, or RejectedUpload. |
| OccurredAtUtc | datetime | yes | Captured in UTC. |
| Details | string | no | Non-sensitive event context; never store file contents or credentials. |

## Existing Entities and State

- **User**: uploader, recipient, department membership, role, and authorization identity.
- **Project**: optional document context and source of manager/member authorization.
- **TaskItem**: optional task association; task documents inherit project context when a project exists.
- **Notification**: in-app notification for successful shares and project uploads.

## State Transitions

`Pending scan -> Clean -> Available`

`Pending scan -> Rejected -> Removed from staging`

No document in a non-clean safety state may be returned by list, search, preview, download, share, or task/project attachment operations.

## Validation and Consistency

- Validate title, category, extension, content type, actual size, project/task access, and tag bounds before persistence.
- Generate a random storage key; preserve the original name only as display metadata.
- Scan a staged file before moving it into final storage and before exposing metadata.
- If metadata persistence fails after file movement, remove the file as compensation; if cleanup fails, record an actionable operational error without exposing the file.
- A share recipient loses access when the direct share is revoked or the document is deleted. Project-based access is recalculated from current membership.
- Database changes are additive and idempotent to preserve the existing SQLite database.
