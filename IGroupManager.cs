using Storyboard.Domain.ApiResponseModel;
using Storyboard.Helper.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storyboard.Domain.Interface.Business
{
    // Interface defining methods for managing groups
    public interface IGroupManager
    {
        // Method to retrieve a list of groups based on a filter
        Task<ResponseViewModel<List<GroupViewModel>>> GetGroupListAsync(GroupFilter filter);

        // Method to retrieve a list of groups based on a search term and organization ID
        Task<ResponseViewModel<List<KeyValueModel<string, string>>>> GetGroupListAsync(string search, string organizationId);

        // Method to retrieve details of a specific group based on group ID and organization ID
        Task<ResponseViewModel<GroupViewModel2>> GetGroupAsync(string groupId, string organizationId);

        // Method to create a new group with the provided details and logged in user ID
        Task<ResponseViewModel<object>> CreateGroupAsync(GroupCreateModel groupCreateObj, string loggedInUserId);

        // Method to update an existing group with the provided details and logged in user ID
        Task<ResponseViewModel<object>> UpdateGroupAsync(GroupUpdateModel groupModel, string loggedInUserId);

        // Method to delete a group based on the group ID
        Task<ResponseViewModel<object>> DeleteGroupAsync(string id);
    }
}
