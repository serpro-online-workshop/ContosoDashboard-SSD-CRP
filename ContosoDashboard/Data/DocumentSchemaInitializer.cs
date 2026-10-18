using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

public sealed class DocumentSchemaInitializer
{
    private static readonly string[] Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS "Documents" (
            "DocumentId" INTEGER NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY AUTOINCREMENT,
            "Title" TEXT NOT NULL,
            "Description" TEXT NULL,
            "Category" TEXT NOT NULL,
            "OriginalFileName" TEXT NOT NULL,
            "StorageKey" TEXT NOT NULL,
            "FileSize" INTEGER NOT NULL,
            "ContentType" TEXT NOT NULL,
            "UploadedAtUtc" TEXT NOT NULL,
            "UploaderId" INTEGER NOT NULL,
            "ProjectId" INTEGER NULL,
            "TaskId" INTEGER NULL,
            "SafetyStatus" INTEGER NOT NULL,
            CONSTRAINT "FK_Documents_Users_UploaderId" FOREIGN KEY ("UploaderId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT,
            CONSTRAINT "FK_Documents_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE RESTRICT,
            CONSTRAINT "FK_Documents_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("TaskId") ON DELETE RESTRICT
        );
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Documents_StorageKey\" ON \"Documents\" (\"StorageKey\");",
        "CREATE INDEX IF NOT EXISTS \"IX_Documents_UploaderId_UploadedAtUtc\" ON \"Documents\" (\"UploaderId\", \"UploadedAtUtc\");",
        "CREATE INDEX IF NOT EXISTS \"IX_Documents_ProjectId_SafetyStatus_UploadedAtUtc\" ON \"Documents\" (\"ProjectId\", \"SafetyStatus\", \"UploadedAtUtc\");",
        "CREATE INDEX IF NOT EXISTS \"IX_Documents_SafetyStatus_Title\" ON \"Documents\" (\"SafetyStatus\", \"Title\");",
        """
        CREATE TABLE IF NOT EXISTS "DocumentTags" (
            "DocumentTagId" INTEGER NOT NULL CONSTRAINT "PK_DocumentTags" PRIMARY KEY AUTOINCREMENT,
            "DocumentId" INTEGER NOT NULL,
            "Value" TEXT NOT NULL,
            "NormalizedValue" TEXT NOT NULL,
            CONSTRAINT "FK_DocumentTags_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("DocumentId") ON DELETE CASCADE
        );
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_DocumentTags_DocumentId_NormalizedValue\" ON \"DocumentTags\" (\"DocumentId\", \"NormalizedValue\");",
        """
        CREATE TABLE IF NOT EXISTS "DocumentShares" (
            "DocumentShareId" INTEGER NOT NULL CONSTRAINT "PK_DocumentShares" PRIMARY KEY AUTOINCREMENT,
            "DocumentId" INTEGER NOT NULL,
            "SharedByUserId" INTEGER NOT NULL,
            "RecipientUserId" INTEGER NULL,
            "RecipientDepartment" TEXT NULL,
            "CreatedAtUtc" TEXT NOT NULL,
            CONSTRAINT "CK_DocumentShares_ExactlyOneRecipient" CHECK (("RecipientUserId" IS NOT NULL AND "RecipientDepartment" IS NULL) OR ("RecipientUserId" IS NULL AND "RecipientDepartment" IS NOT NULL)),
            CONSTRAINT "FK_DocumentShares_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("DocumentId") ON DELETE CASCADE,
            CONSTRAINT "FK_DocumentShares_Users_SharedByUserId" FOREIGN KEY ("SharedByUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT,
            CONSTRAINT "FK_DocumentShares_Users_RecipientUserId" FOREIGN KEY ("RecipientUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
        );
        """,
        "CREATE INDEX IF NOT EXISTS \"IX_DocumentShares_DocumentId_RecipientUserId\" ON \"DocumentShares\" (\"DocumentId\", \"RecipientUserId\");",
        "CREATE INDEX IF NOT EXISTS \"IX_DocumentShares_DocumentId_RecipientDepartment\" ON \"DocumentShares\" (\"DocumentId\", \"RecipientDepartment\");",
        """
        CREATE TABLE IF NOT EXISTS "DocumentActivities" (
            "DocumentActivityId" INTEGER NOT NULL CONSTRAINT "PK_DocumentActivities" PRIMARY KEY AUTOINCREMENT,
            "DocumentId" INTEGER NULL,
            "ActorUserId" INTEGER NOT NULL,
            "ActivityType" INTEGER NOT NULL,
            "OccurredAtUtc" TEXT NOT NULL,
            "Details" TEXT NULL,
            CONSTRAINT "FK_DocumentActivities_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("DocumentId") ON DELETE SET NULL,
            CONSTRAINT "FK_DocumentActivities_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
        );
        """,
        "CREATE INDEX IF NOT EXISTS \"IX_DocumentActivities_ActivityType_OccurredAtUtc\" ON \"DocumentActivities\" (\"ActivityType\", \"OccurredAtUtc\");"
    ];

    private readonly ApplicationDbContext _context;

    public DocumentSchemaInitializer(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var statement in Statements)
            {
                await _context.Database.ExecuteSqlRawAsync(statement, cancellationToken);
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }
}