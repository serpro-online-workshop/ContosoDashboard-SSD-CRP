using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IProjectService
{
    Task<List<Project>> GetUserProjectsAsync(int userId);
    Task<Project?> GetProjectByIdAsync(int projectId, int requestingUserId);
    Task<Project?> GetProjectForEditingAsync(int projectId, int requestingUserId);
    Task<Project> SaveProjectAsync(Project project, int teamLeadUserId, IReadOnlyCollection<int> developerUserIds, int requestingUserId);
    Task<Project> CreateProjectAsync(Project project);
    Task<bool> UpdateProjectAsync(Project project, int requestingUserId);
    Task<bool> AddProjectMemberAsync(int projectId, int userId, string role, int requestingUserId);
    Task<List<ProjectMember>> GetProjectMembersAsync(int projectId, int requestingUserId);
}

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Project>> GetUserProjectsAsync(int userId)
    {
        var isAdministrator = await _context.Users
            .AnyAsync(user => user.UserId == userId && user.Role == UserRole.Administrator);
        var projectsQuery = _context.Projects.AsQueryable();
        if (!isAdministrator)
        {
            projectsQuery = projectsQuery.Where(project =>
                project.ProjectManagerId == userId || project.ProjectMembers.Any(member => member.UserId == userId));
        }

        return await projectsQuery
            .Include(p => p.ProjectManager)
            .Include(p => p.Tasks)
            .Include(p => p.ProjectMembers)
            .ThenInclude(pm => pm.User)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectForEditingAsync(int projectId, int requestingUserId)
    {
        if (!await IsAdministratorAsync(requestingUserId))
        {
            throw new UnauthorizedAccessException("Administrator access is required to edit projects.");
        }

        return await _context.Projects
            .Include(project => project.ProjectMembers)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(project => project.ProjectId == projectId);
    }

    public async Task<Project> SaveProjectAsync(Project project, int teamLeadUserId, IReadOnlyCollection<int> developerUserIds, int requestingUserId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(developerUserIds);
        if (!await IsAdministratorAsync(requestingUserId))
        {
            throw new UnauthorizedAccessException("Administrator access is required to create or edit projects.");
        }

        ValidateProject(project);
        if (teamLeadUserId <= 0 || project.ProjectManagerId <= 0 || !project.TargetCompletionDate.HasValue ||
            project.TargetCompletionDate.Value.Date < project.StartDate.Date)
        {
            throw new ArgumentException("Project manager, team lead, start date, and a valid target date are required.");
        }

        var developers = developerUserIds.ToArray();
        if (developers.Any(id => id <= 0) || developers.Distinct().Count() != developers.Length ||
            developers.Contains(project.ProjectManagerId) || developers.Contains(teamLeadUserId) ||
            project.ProjectManagerId == teamLeadUserId)
        {
            throw new ArgumentException("A user can have only one project profile.");
        }

        var selectedIds = developers.Append(project.ProjectManagerId).Append(teamLeadUserId).Distinct().ToArray();
        var selectedUsers = await _context.Users
            .Where(user => selectedIds.Contains(user.UserId))
            .ToDictionaryAsync(user => user.UserId);
        if (selectedUsers.Count != selectedIds.Length ||
            selectedUsers[project.ProjectManagerId].Role != UserRole.ProjectManager ||
            selectedUsers[teamLeadUserId].Role != UserRole.TeamLead ||
            developers.Any(id => selectedUsers[id].Role != UserRole.Employee))
        {
            throw new ArgumentException("Select a project manager, a team lead, and employee developers with matching roles.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        Project savedProject;
        if (project.ProjectId <= 0)
        {
            project.CreatedDate = DateTime.UtcNow;
            project.UpdatedDate = DateTime.UtcNow;
            project.StartDate = DateTime.SpecifyKind(project.StartDate.Date, DateTimeKind.Utc);
            project.TargetCompletionDate = DateTime.SpecifyKind(project.TargetCompletionDate.Value.Date, DateTimeKind.Utc);
            project.ProjectMembers.Clear();
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            savedProject = project;
        }
        else
        {
            savedProject = await _context.Projects
                .Include(existingProject => existingProject.ProjectMembers)
                .FirstOrDefaultAsync(existingProject => existingProject.ProjectId == project.ProjectId)
                ?? throw new KeyNotFoundException("The project was not found.");

            savedProject.Name = project.Name.Trim();
            savedProject.Description = string.IsNullOrWhiteSpace(project.Description) ? null : project.Description.Trim();
            savedProject.ProjectManagerId = project.ProjectManagerId;
            savedProject.StartDate = DateTime.SpecifyKind(project.StartDate.Date, DateTimeKind.Utc);
            savedProject.TargetCompletionDate = DateTime.SpecifyKind(project.TargetCompletionDate.Value.Date, DateTimeKind.Utc);
            savedProject.UpdatedDate = DateTime.UtcNow;
            savedProject.ProjectMembers.Clear();
        }

        savedProject.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = savedProject.ProjectId,
            UserId = teamLeadUserId,
            Role = "TeamLead",
            AssignedDate = DateTime.UtcNow
        });
        foreach (var userId in developers)
        {
            savedProject.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = savedProject.ProjectId,
                UserId = userId,
                Role = "Developer",
                AssignedDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _context.Entry(savedProject).Reference(item => item.ProjectManager).LoadAsync();
        await _context.Entry(savedProject).Collection(item => item.ProjectMembers).Query().Include(member => member.User).LoadAsync();
        return savedProject;
    }

    public async Task<Project?> GetProjectByIdAsync(int projectId, int requestingUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectManager)
            .Include(p => p.Tasks)
            .ThenInclude(t => t.AssignedUser)
            .Include(p => p.ProjectMembers)
            .ThenInclude(pm => pm.User)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null) return null;

        // Authorization: User must be project manager or a project member
        var isProjectManager = project.ProjectManagerId == requestingUserId;
        var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!isProjectManager && !isProjectMember)
        {
            return null; // User not authorized to view this project
        }

        return project;
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        project.CreatedDate = DateTime.UtcNow;
        project.UpdatedDate = DateTime.UtcNow;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<bool> UpdateProjectAsync(Project project, int requestingUserId)
    {
        var existingProject = await _context.Projects.FindAsync(project.ProjectId);
        if (existingProject == null) return false;

        // Authorization: Only project manager can update project
        if (existingProject.ProjectManagerId != requestingUserId)
        {
            return false; // User not authorized to update this project
        }

        existingProject.Name = project.Name;
        existingProject.Description = project.Description;
        existingProject.Status = project.Status;
        existingProject.TargetCompletionDate = project.TargetCompletionDate;
        existingProject.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddProjectMemberAsync(int projectId, int userId, string role, int requestingUserId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        // Authorization: Only project manager can add members
        if (project.ProjectManagerId != requestingUserId)
        {
            return false; // User not authorized to add members to this project
        }

        var existingMember = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        if (existingMember != null) return false;

        var projectMember = new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            AssignedDate = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(projectMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<ProjectMember>> GetProjectMembersAsync(int projectId, int requestingUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null) return new List<ProjectMember>();

        // Authorization: User must be project manager or member
        var isProjectManager = project.ProjectManagerId == requestingUserId;
        var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!isProjectManager && !isProjectMember)
        {
            return new List<ProjectMember>(); // User not authorized
        }

        return await _context.ProjectMembers
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync();
    }

    private Task<bool> IsAdministratorAsync(int userId) => _context.Users
        .AnyAsync(user => user.UserId == userId && user.Role == UserRole.Administrator);

    private static void ValidateProject(Project project)
    {
        project.Name = project.Name?.Trim() ?? string.Empty;
        project.Description = string.IsNullOrWhiteSpace(project.Description) ? null : project.Description.Trim();
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            project, new System.ComponentModel.DataAnnotations.ValidationContext(project), validationResults, validateAllProperties: true))
        {
            throw new ArgumentException(validationResults[0].ErrorMessage ?? "The project information is invalid.");
        }
    }
}
