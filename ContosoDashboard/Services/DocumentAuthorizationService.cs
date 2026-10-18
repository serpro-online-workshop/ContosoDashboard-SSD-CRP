using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuthorizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanViewAsync(int documentId, int requestingUserId)
    {
        var (document, user) = await GetDocumentAndUserAsync(documentId, requestingUserId);
        if (document == null || user == null || document.SafetyStatus != DocumentSafetyStatus.Clean)
        {
            return false;
        }

        return IsAdministrator(user) ||
            document.UploaderId == user.UserId ||
            IsProjectMemberOrManager(document, user.UserId) ||
            IsTeamLeadForUploader(document, user) ||
            HasDirectOrDepartmentShare(document, user);
    }

    public async Task<bool> CanManageAsync(int documentId, int requestingUserId)
    {
        var (document, user) = await GetDocumentAndUserAsync(documentId, requestingUserId);
        if (document == null || user == null || document.SafetyStatus != DocumentSafetyStatus.Clean)
        {
            return false;
        }

        return IsAdministrator(user) ||
            document.UploaderId == user.UserId ||
            document.Project?.ProjectManagerId == user.UserId ||
            IsTeamLeadForUploader(document, user);
    }

    public async Task<bool> CanUploadToProjectAsync(int projectId, int requestingUserId)
    {
        var user = await _context.Users.FindAsync(requestingUserId);
        if (user == null)
        {
            return false;
        }

        if (IsAdministrator(user))
        {
            return await _context.Projects.AnyAsync(project => project.ProjectId == projectId);
        }

        return await _context.Projects.AnyAsync(project =>
            project.ProjectId == projectId &&
            (project.ProjectManagerId == requestingUserId || project.ProjectMembers.Any(member => member.UserId == requestingUserId)));
    }

    public async Task<bool> CanAccessTaskAsync(int taskId, int requestingUserId)
    {
        var user = await _context.Users.FindAsync(requestingUserId);
        if (user == null)
        {
            return false;
        }

        if (IsAdministrator(user))
        {
            return await _context.Tasks.AnyAsync(task => task.TaskId == taskId);
        }

        return await _context.Tasks
            .Include(task => task.Project)
                .ThenInclude(project => project!.ProjectMembers)
            .AnyAsync(task => task.TaskId == taskId &&
                (task.AssignedUserId == requestingUserId ||
                 task.CreatedByUserId == requestingUserId ||
                 task.Project!.ProjectManagerId == requestingUserId ||
                 task.Project.ProjectMembers.Any(member => member.UserId == requestingUserId)));
    }

    private async Task<(Document? Document, User? User)> GetDocumentAndUserAsync(int documentId, int requestingUserId)
    {
        var user = await _context.Users.FindAsync(requestingUserId);
        var document = await _context.Documents
            .Include(item => item.Uploader)
            .Include(item => item.Project)
                .ThenInclude(project => project!.ProjectMembers)
            .Include(item => item.Shares)
            .FirstOrDefaultAsync(item => item.DocumentId == documentId);

        return (document, user);
    }

    private static bool IsAdministrator(User user) => user.Role == UserRole.Administrator;

    private static bool IsProjectMemberOrManager(Document document, int userId)
    {
        if (document.Project == null)
        {
            return false;
        }

        return document.Project.ProjectManagerId == userId ||
            document.Project.ProjectMembers.Any(member => member.UserId == userId);
    }

    private static bool IsTeamLeadForUploader(Document document, User user)
    {
        return user.Role == UserRole.TeamLead &&
            !string.IsNullOrWhiteSpace(user.Department) &&
            string.Equals(user.Department, document.Uploader.Department, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasDirectOrDepartmentShare(Document document, User user)
    {
        return document.Shares.Any(share =>
            share.RecipientUserId == user.UserId ||
            (!string.IsNullOrWhiteSpace(user.Department) &&
             string.Equals(share.RecipientDepartment, user.Department, StringComparison.OrdinalIgnoreCase)));
    }
}