using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class DocumentAuthorizationServiceTests
{
    [Fact]
    public async Task OwnerCanViewAndManageCleanDocument()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();

        Assert.True(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, fixture.Owner.UserId));
        Assert.True(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, fixture.Owner.UserId));
    }

    [Fact]
    public async Task ProjectMemberCanViewButCannotManageDocument()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();
        var member = await fixture.AddUserAsync(UserRole.Employee, "Other");
        fixture.Context.ProjectMembers.Add(new ProjectMember { ProjectId = fixture.Project.ProjectId, UserId = member.UserId, Role = "TeamMember" });
        await fixture.Context.SaveChangesAsync();

        Assert.True(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, member.UserId));
        Assert.False(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, member.UserId));
    }

    [Fact]
    public async Task ProjectManagerCanManageProjectDocument()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();

        Assert.True(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, fixture.Manager.UserId));
        Assert.True(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, fixture.Manager.UserId));
    }

    [Fact]
    public async Task TeamLeadCanManageDocumentsUploadedByDepartmentMembers()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();
        var teamLead = await fixture.AddUserAsync(UserRole.TeamLead, fixture.Owner.Department!);

        Assert.True(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, teamLead.UserId));
        Assert.True(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, teamLead.UserId));
    }

    [Fact]
    public async Task DirectShareAllowsViewButNotManage()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();
        var recipient = await fixture.AddUserAsync(UserRole.Employee, "Other");
        fixture.Context.DocumentShares.Add(new DocumentShare
        {
            DocumentId = fixture.Document.DocumentId,
            SharedByUserId = fixture.Owner.UserId,
            RecipientUserId = recipient.UserId
        });
        await fixture.Context.SaveChangesAsync();

        Assert.True(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, recipient.UserId));
        Assert.False(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, recipient.UserId));
    }

    [Fact]
    public async Task DepartmentShareAllowsViewToCurrentDepartmentMembers()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();
        var recipient = await fixture.AddUserAsync(UserRole.Employee, fixture.Owner.Department!);
        fixture.Context.DocumentShares.Add(new DocumentShare
        {
            DocumentId = fixture.Document.DocumentId,
            SharedByUserId = fixture.Owner.UserId,
            RecipientDepartment = fixture.Owner.Department
        });
        await fixture.Context.SaveChangesAsync();

        Assert.True(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, recipient.UserId));
        Assert.False(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, recipient.UserId));
    }

    [Fact]
    public async Task AdministratorCanViewAndManageAnyCleanDocument()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();
        var administrator = await fixture.AddUserAsync(UserRole.Administrator, "IT");

        Assert.True(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, administrator.UserId));
        Assert.True(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, administrator.UserId));
    }

    [Fact]
    public async Task NonMemberCannotViewOrManageDocument()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync();
        var outsider = await fixture.AddUserAsync(UserRole.Employee, "Other");

        Assert.False(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, outsider.UserId));
        Assert.False(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, outsider.UserId));
    }

    [Fact]
    public async Task DocumentWithNonCleanSafetyStatusIsNotAccessible()
    {
        await using var fixture = await AuthorizationFixture.CreateAsync(DocumentSafetyStatus.Pending);

        Assert.False(await fixture.Service.CanViewAsync(fixture.Document.DocumentId, fixture.Owner.UserId));
        Assert.False(await fixture.Service.CanManageAsync(fixture.Document.DocumentId, fixture.Owner.UserId));
    }

    private sealed class AuthorizationFixture : IAsyncDisposable
    {
        private AuthorizationFixture(SqliteConnection connection, ApplicationDbContext context, User owner, User manager, Project project, Document document)
        {
            Connection = connection;
            Context = context;
            Owner = owner;
            Manager = manager;
            Project = project;
            Document = document;
            Service = new DocumentAuthorizationService(context);
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public User Owner { get; }
        public User Manager { get; }
        public Project Project { get; }
        public Document Document { get; }
        public DocumentAuthorizationService Service { get; }

        public static async Task<AuthorizationFixture> CreateAsync(DocumentSafetyStatus safetyStatus = DocumentSafetyStatus.Clean)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var owner = new User { Email = "owner@example.test", DisplayName = "Owner", Department = "Engineering", Role = UserRole.Employee };
            var manager = new User { Email = "manager@example.test", DisplayName = "Manager", Department = "Other", Role = UserRole.ProjectManager };
            context.Users.AddRange(owner, manager);
            await context.SaveChangesAsync();

            var project = new Project { Name = "Project", ProjectManagerId = manager.UserId, Status = ProjectStatus.Active };
            context.Projects.Add(project);
            await context.SaveChangesAsync();

            var document = new Document
            {
                Title = "Document",
                Category = "Reports",
                OriginalFileName = "document.pdf",
                StorageKey = $"{owner.UserId}/personal/{Guid.NewGuid():N}.pdf",
                FileSize = 100,
                ContentType = "application/pdf",
                UploadedAtUtc = DateTime.UtcNow,
                UploaderId = owner.UserId,
                ProjectId = project.ProjectId,
                SafetyStatus = safetyStatus
            };
            context.Documents.Add(document);
            await context.SaveChangesAsync();

            return new AuthorizationFixture(connection, context, owner, manager, project, document);
        }

        public async Task<User> AddUserAsync(UserRole role, string department)
        {
            var suffix = Guid.NewGuid().ToString("N");
            var user = new User { Email = $"{suffix}@example.test", DisplayName = suffix, Department = department, Role = role };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}