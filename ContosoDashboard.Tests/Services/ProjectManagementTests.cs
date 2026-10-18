using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class ProjectManagementTests
{
    [Fact]
    public async Task AdministratorCreatesProjectWithUniqueManagerLeadAndDevelopers()
    {
        await using var harness = await ProjectHarness.CreateAsync();

        var project = await harness.Service.SaveProjectAsync(CreateProject(), 3, [4], requestingUserId: 1);

        Assert.Equal(2, project.ProjectManagerId);
        Assert.Contains(project.ProjectMembers, member => member.UserId == 3 && member.Role == "TeamLead");
        Assert.Contains(project.ProjectMembers, member => member.UserId == 4 && member.Role == "Developer");
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(3, 3)]
    public async Task SaveRejectsUsersAssignedToMultipleProjectProfiles(int teamLeadId, int developerId)
    {
        await using var harness = await ProjectHarness.CreateAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => harness.Service.SaveProjectAsync(
            CreateProject(), teamLeadId, [developerId], requestingUserId: 1));
    }

    [Fact]
    public async Task NonAdministratorCannotCreateOrEditProjectTeam()
    {
        await using var harness = await ProjectHarness.CreateAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => harness.Service.SaveProjectAsync(
            CreateProject(), 3, [4], requestingUserId: 2));
    }

    [Fact]
    public async Task OnlyAdministratorCanLoadProjectForEditing()
    {
        await using var harness = await ProjectHarness.CreateAsync();

        Assert.NotNull(await harness.Service.GetProjectForEditingAsync(1, requestingUserId: 1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => harness.Service.GetProjectForEditingAsync(1, requestingUserId: 2));
    }

    [Fact]
    public async Task AdministratorCanListProjectsOutsideTheirOwnMembership()
    {
        await using var harness = await ProjectHarness.CreateAsync();
        harness.Context.Projects.Add(CreateProject());
        await harness.Context.SaveChangesAsync();

        var projects = await harness.Service.GetUserProjectsAsync(1);

        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, project => project.Name == "New project");
    }

    [Fact]
    public async Task EditingProjectReplacesTheTeamWithoutDuplicateProfiles()
    {
        await using var harness = await ProjectHarness.CreateAsync();
        var project = await harness.Service.SaveProjectAsync(CreateProject(), 3, [4], requestingUserId: 1);
        harness.Context.Users.AddRange(
            new User { UserId = 50, Email = "new-lead@example.test", DisplayName = "New Lead", Role = UserRole.TeamLead },
            new User { UserId = 51, Email = "new-developer@example.test", DisplayName = "New Developer", Role = UserRole.Employee });
        await harness.Context.SaveChangesAsync();
        var update = CreateProject();
        update.ProjectId = project.ProjectId;
        update.Name = "Updated project";

        var updated = await harness.Service.SaveProjectAsync(update, 50, [51], requestingUserId: 1);

        Assert.Equal("Updated project", updated.Name);
        Assert.Contains(updated.ProjectMembers, member => member.UserId == 50 && member.Role == "TeamLead");
        Assert.Contains(updated.ProjectMembers, member => member.UserId == 51 && member.Role == "Developer");
        Assert.Equal(2, updated.ProjectMembers.Count);
    }

    private static Project CreateProject() => new()
    {
        Name = "New project",
        Description = "Project description",
        ProjectManagerId = 2,
        StartDate = DateTime.UtcNow.Date,
        TargetCompletionDate = DateTime.UtcNow.Date.AddDays(30),
        Status = ProjectStatus.Planning
    };

    private sealed class ProjectHarness : IAsyncDisposable
    {
        private ProjectHarness(SqliteConnection connection, ApplicationDbContext context, ProjectService service)
        {
            Connection = connection;
            Context = context;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public ProjectService Service { get; }

        public static async Task<ProjectHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new ProjectHarness(connection, context, new ProjectService(context));
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}