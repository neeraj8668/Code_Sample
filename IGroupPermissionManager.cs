using Storyboard.Domain.ApiResponseModel;
using Storyboard.Helper.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storyboard.Domain.Interface.Business
{
    // Interface defining methods for managing group permissions
    public interface IGroupPermissionManager
    {
        // Method to retrieve a list of permissions assigned to a specific group based on a filter
        Task<ResponseViewModel<List<PermissionModel>>> GetGroupPermissionsByGroupAsync(GroupPermissionsByGroupFilter groupPermissionsByGroupFilter);

        // Method to save (add or update) permissions for a specific group
        // Takes a model with the permission details and the ID of the logged-in user
        Task<ResponseViewModel<GroupPermissionViewModel>> SaveGroupPermissionAsync(GroupPermissionSaveDeleteModel groupPermissionSaveDeleteModel, string loggedInUserId);

        // Method to delete permissions for a specific group
        // Takes a model with the permission details and the ID of the logged-in user
        Task<ResponseViewModel<string>> DeleteGroupPermissionAsync(GroupPermissionSaveDeleteModel groupPermissionSaveDeleteModel, string loggedInUserId);
    }
}
