using System.ComponentModel.DataAnnotations;
using System.Text;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class TaskDocumentIntegrationTests
{
    [Fact]
    public async Task TaskUploadInheritsItsProjectAssociation()
    {
        await using var harness = await TaskHarness.CreateAsync();

        var document = await harness.Service.CreateAsync(CreateTaskRequest(), 4);

        Assert.Equal(2, document.TaskId);
        Assert.Equal(1, document.ProjectId);
        Assert.Equal(1, Assert.Single(harness.Storage.ProjectIds));
    }

    [Fact]
    public async Task UserWithoutTaskAccessCannotAttachADocument()
    {
        await using var harness = await TaskHarness.CreateAsync();
        harness.Context.Users.Add(new User
        {
            UserId = 50,
            Email = "outsider@example.test",
            DisplayName = "Outsider",
            Department = "Sales",
            Role = UserRole.Employee
        });
        await harness.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => harness.Service.CreateAsync(CreateTaskRequest(), 50));

        Assert.Equal(0, harness.Storage.StageCount);
        Assert.Empty(harness.Context.Documents);
    }

    [Fact]
    public async Task ProjectUploadNotifiesOtherProjectMembers()
    {
        await using var harness = await TaskHarness.CreateAsync();
        var request = CreateTaskRequest(taskId: null, projectId: 1);

        await harness.Service.CreateAsync(request, 4);

        Assert.Contains(await harness.Context.Notifications.ToListAsync(), notification => notification.UserId == 2 && notification.Type == NotificationType.ProjectDocumentUpload);
        Assert.Contains(await harness.Context.Notifications.ToListAsync(), notification => notification.UserId == 3 && notification.Type == NotificationType.ProjectDocumentUpload);
        Assert.DoesNotContain(await harness.Context.Notifications.ToListAsync(), notification => notification.UserId == 4 && notification.Type == NotificationType.ProjectDocumentUpload);
    }

    private static DocumentUploadRequest CreateTaskRequest(int? taskId = 2, int? projectId = null) => new()
    {
        Title = "Task attachment",
        Category = "Project Documents",
        OriginalFileName = "attachment.pdf",
        ContentType = "application/pdf",
        FileSize = Encoding.UTF8.GetByteCount("task file"),
        Content = new MemoryStream(Encoding.UTF8.GetBytes("task file")),
        TaskId = taskId,
        ProjectId = projectId
    };

    private sealed class TaskHarness : IAsyncDisposable
    {
        private TaskHarness(SqliteConnection connection, ApplicationDbContext context, TaskStorage storage, DocumentService service)
        {
            Connection = connection;
            Context = context;
            Storage = storage;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public TaskStorage Storage { get; }
        public DocumentService Service { get; }

        public static async Task<TaskHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var storage = new TaskStorage();
            var service = new DocumentService(context, storage, new TaskScanner(), new DocumentAuthorizationService(context), notificationService: new NotificationService(context));
            return new TaskHarness(connection, context, storage, service);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class TaskStorage : IFileStorageService
    {
        public int StageCount { get; private set; }
        public List<int?> ProjectIds { get; } = new();

        public async Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default)
        {
            StageCount++;
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            return new StagedFile($"stage/{Guid.NewGuid():N}", buffer.Length);
        }

        public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default)
        {
            ProjectIds.Add(projectId);
            return Task.FromResult($"{uploaderId}/{projectId?.ToString() ?? "personal"}/{Guid.NewGuid():N}{extension}");
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TaskScanner : IFileSafetyScanner
    {
        public Task<FileSafetyScanResult> ScanAsync(string stagedPath, CancellationToken cancellationToken = default) => Task.FromResult(FileSafetyScanResult.Clean);
    }
}