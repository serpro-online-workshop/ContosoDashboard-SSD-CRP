using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class DocumentShareTests
{
    [Fact]
    public async Task DirectShareGrantsReadOnlyAccessAndRevocationRemovesIt()
    {
        await using var harness = await ShareHarness.CreateAsync();
        var document = await harness.AddDocumentAsync(projectId: null);
        var recipient = new User { UserId = 50, Email = "recipient@example.test", DisplayName = "Recipient", Department = "Sales", Role = UserRole.Employee };
        harness.Context.Users.Add(recipient);
        await harness.Context.SaveChangesAsync();

        var share = await harness.Service.ShareAsync(document.DocumentId, new DocumentShareRequest { RecipientUserId = recipient.UserId }, 4);

        Assert.True(await harness.Authorization.CanViewAsync(document.DocumentId, recipient.UserId));
        Assert.False(await harness.Authorization.CanManageAsync(document.DocumentId, recipient.UserId));
        Assert.Equal(document.DocumentId, Assert.Single(await harness.Service.GetSharedWithMeAsync(recipient.UserId)).DocumentId);
        var notification = await harness.Context.Notifications.SingleAsync(item => item.UserId == recipient.UserId);
        Assert.Equal(NotificationType.DocumentShare, notification.Type);
        Assert.Equal(document.Title, notification.Message);
        Assert.Contains(await harness.Context.DocumentActivities.ToListAsync(), activity => activity.ActivityType == DocumentActivityType.Share && activity.ActorUserId == 4);
        Assert.True(await harness.Service.RevokeShareAsync(share.DocumentShareId, 4));
        Assert.False(await harness.Authorization.CanViewAsync(document.DocumentId, recipient.UserId));
        Assert.Empty(await harness.Service.GetSharedWithMeAsync(recipient.UserId));
    }

    [Fact]
    public async Task DepartmentShareUsesCurrentMembershipAndProjectRoleRemainsIndependent()
    {
        await using var harness = await ShareHarness.CreateAsync();
        var document = await harness.AddDocumentAsync(projectId: 1);
        var recipient = new User { UserId = 51, Email = "department@example.test", DisplayName = "Department user", Department = "Marketing", Role = UserRole.Employee };
        harness.Context.Users.Add(recipient);
        await harness.Context.SaveChangesAsync();

        var share = await harness.Service.ShareAsync(document.DocumentId, new DocumentShareRequest { RecipientDepartment = "Marketing" }, 4);

        Assert.True(await harness.Authorization.CanViewAsync(document.DocumentId, recipient.UserId));
        Assert.Single(await harness.Service.GetSharedWithMeAsync(recipient.UserId));
        recipient.Department = "Sales";
        await harness.Context.SaveChangesAsync();
        Assert.False(await harness.Authorization.CanViewAsync(document.DocumentId, recipient.UserId));
        Assert.Empty(await harness.Service.GetSharedWithMeAsync(recipient.UserId));

        Assert.True(await harness.Service.RevokeShareAsync(share.DocumentShareId, 4));
        Assert.True(await harness.Authorization.CanViewAsync(document.DocumentId, 2));
    }

    private sealed class ShareHarness : IAsyncDisposable
    {
        private ShareHarness(SqliteConnection connection, ApplicationDbContext context, DocumentAuthorizationService authorization, DocumentService service)
        {
            Connection = connection;
            Context = context;
            Authorization = authorization;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public DocumentAuthorizationService Authorization { get; }
        public DocumentService Service { get; }

        public static async Task<ShareHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var authorization = new DocumentAuthorizationService(context);
            var service = new DocumentService(context, new ShareStorage(), new ShareScanner(), authorization, notificationService: new NotificationService(context));
            return new ShareHarness(connection, context, authorization, service);
        }

        public async Task<Document> AddDocumentAsync(int? projectId)
        {
            var document = new Document
            {
                Title = "Shareable plan",
                Category = "Project Documents",
                OriginalFileName = "plan.pdf",
                StorageKey = $"4/{projectId?.ToString() ?? "personal"}/plan-{Guid.NewGuid():N}.pdf",
                FileSize = 3,
                ContentType = "application/pdf",
                UploadedAtUtc = DateTime.UtcNow,
                UploaderId = 4,
                ProjectId = projectId,
                SafetyStatus = DocumentSafetyStatus.Clean
            };
            Context.Documents.Add(document);
            await Context.SaveChangesAsync();
            return document;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class ShareStorage : IFileStorageService
    {
        public Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class ShareScanner : IFileSafetyScanner
    {
        public Task<FileSafetyScanResult> ScanAsync(string stagedPath, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}