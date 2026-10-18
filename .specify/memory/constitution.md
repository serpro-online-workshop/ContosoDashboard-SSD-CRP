<!--
Sync Impact Report
Version change: 1.0.0 -> 1.1.0 (expanded security requirements for file handling)
Modified principles:
- III. Authorization and Data Isolation -> III. Authorization, Data Isolation, and Safe File Handling
Added sections: none
Removed sections: none
Follow-up TODO: confirm the original ratification date.
This temporary report should be removed before this file is committed.
-->
# ContosoDashboard Constitution

## Core Principles

### I. Training-Only Product
ContosoDashboard MUST be treated as a fictional training application. Its mock authentication,
sample data, and simplified architecture MUST NOT be described or relied upon as production-ready.
Documentation and UI copy MUST identify training-only behavior where users could mistake it for
real authentication, authorization, or external integration. This prevents training shortcuts from
being mistaken for security guarantees.

### II. Offline-First Architecture
Features MUST work locally without requiring cloud services or external credentials. The current
application stack is ASP.NET Core 10, Blazor Server, Entity Framework Core, and SQLite. New
infrastructure dependencies MUST be accessed through suitable service abstractions so training
scenarios can remain offline and future migration options remain explicit.

### III. Authorization, Data Isolation, and Safe File Handling
Every protected page MUST require authentication, and service operations that read or change
user-owned data MUST verify the requesting user's access. Project access MUST be limited to its
manager or members, and users MUST NOT be able to change another user's profile or task data.
Features that accept files MUST treat uploaded content and original filenames as untrusted. They
MUST use generated storage identifiers, validate size and file type, store files outside `wwwroot`,
and authorize every preview or download. Files MUST remain unavailable until malware scanning
passes; if scanning cannot complete, processing MUST fail closed. Mock authentication is for
training only; production identity and security requirements MUST be stated explicitly rather than
inferred from this implementation.

### IV. Validated Changes
User input MUST be validated at the UI and service or persistence boundary as appropriate. Data
access MUST remain in application services rather than being embedded in page components. Changes
MUST be verified with the narrowest relevant tests; when automated tests do not cover the behavior,
the implementation MUST be built and the affected user flow MUST be checked manually. This keeps
the training code understandable while making regressions observable.

### V. Localized and Accessible UI
All user-facing text MUST use the localization resources; new or changed interface text MUST be
provided for every supported culture: English (`en-US`), Brazilian Portuguese (`pt-BR`), Spanish
(`es`), and German (`de`). Dates and numbers MUST use the active culture. Controls MUST retain
semantic labels and keyboard operation so the same workflows remain usable across languages and
input methods.

## Security and Technical Constraints

- The application MUST remain usable offline and MUST NOT require production cloud resources.
- SQLite is the local development database. Schema changes MUST account for existing local
	databases and preserve user data.
- Authentication is a user-selection mock with cookie-based sessions. It MUST NOT be represented
	as password-based or production identity verification.
- Secrets, tokens, and real personal data MUST NOT be committed or included in training seed data.
- Authorization checks MUST be enforced in services for data access, even when a page is protected.
- File uploads MUST use generated storage names rather than user-supplied paths, and uploaded files
	MUST NOT be served directly from a web-accessible directory.
- File type and size limits MUST be validated before storage. Malware scanning MUST complete before
	access is granted; unavailable scanning MUST prevent the file from becoming available.

## Development and Quality Workflow

- Features MUST be guided by the repository's specification, plan, and task artifacts when those
	artifacts exist; implementation MUST not silently expand their scope.
- Changes MUST preserve the existing Blazor, service, and Entity Framework conventions and avoid
	unrelated refactoring.
- Before completion, contributors MUST build the application and run relevant automated tests.
	User-visible flows, authentication, persistence, and localization MUST receive targeted checks
	when affected.
- Review MUST confirm that authorization boundaries, validation, and all supported translations
	remain consistent with this constitution.

## Governance

This constitution governs ContosoDashboard development and takes precedence over conflicting local
development guidance. Amendments MUST include their rationale, affected principles, and any required
migration or compatibility steps. Changes require review and approval by the project maintainer.

Versioning follows semantic versioning: MAJOR for incompatible principle removals or redefinitions,
MINOR for new principles or materially expanded requirements, and PATCH for clarifications and
non-semantic wording changes. Each review MUST check relevant work against the principles above;
exceptions MUST be documented and approved rather than implicit.

**Version**: 1.1.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm original adoption date | **Last Amended**: 2026-10-18
