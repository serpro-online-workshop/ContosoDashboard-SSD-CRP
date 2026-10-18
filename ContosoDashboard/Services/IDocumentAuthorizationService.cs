namespace ContosoDashboard.Services;

public interface IDocumentAuthorizationService
{
    Task<bool> CanViewAsync(int documentId, int requestingUserId);
    Task<bool> CanManageAsync(int documentId, int requestingUserId);
    Task<bool> CanUploadToProjectAsync(int projectId, int requestingUserId);
    Task<bool> CanAccessTaskAsync(int taskId, int requestingUserId);
}