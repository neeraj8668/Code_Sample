using Storyboard.Domain.ApiResponseModel;
using Storyboard.Helper.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storyboard.Domain.Interface.Business
{
    // Interface defining methods for managing users within groups
    public interface IGroupUserManager
    {
        // Method to retrieve a list of users by organization based on specified filter criteria
        Task<ResponseViewModel<List<GroupUserListItem>>> GetUsersByOrganizationAsync(GroupUsersByOrganizationFilter usersByOrganizationFilter);

        // Method to retrieve a list of users by group based on specified filter criteria
        Task<ResponseViewModel<GroupUserModel>> GetUsersByGroupAsync(UsersByGroupFilter usersByGroupFilter);

        // Method to retrieve a list of groups by user based on specified filter criteria
        Task<ResponseViewModel<UserGroupModel>> GetGroupsByUserAsync(GroupsByUserIdFilter groupsByUserIdFilter);

        // Method to save (add or update) group-user mapping
        // Takes a model with the group-user mapping details and the ID of the logged-in user
        Task<ResponseViewModel<GroupUserMappingViewModel>> SaveGroupUserMappingAsync(GroupUserMappingSaveDeleteModel groupUserMappingSaveDeleteModel, string loggedInUserId);

        // Method to delete group-user mapping
        // Takes a model with the group-user mapping details and the ID of the logged-in user
        Task<ResponseViewModel<string>> DeleteGroupUserMappingAsync(GroupUserMappingSaveDeleteModel groupUserMappingSaveDeleteModel, string loggedInUserId);

        // Method to retrieve a list of all available users in an organization who are eligible to be added to a group
        // Takes the organization ID and an optional filter for active status
        Task<ResponseViewModel<List<OrganizationAllUserViewModel>>> GetAllAvailableGroupUserAsync(string organizationId, bool? isActive = true);
    }
}
