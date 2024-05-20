
Overview


This code manage groups, users, permissions, and their relationships. It allows for the creation, updating, and deletion of groups, as well as the assignment of users and permissions to these groups. The following sections will provide a detailed overview of the code structure and the database schema.

Code Structure

Interfaces
IGroupManager
This interface defines methods for managing groups, including retrieving, creating, updating, and deleting groups.

public interface IGroupManager
{
    Task<ResponseViewModel<List<GroupViewModel>>> GetGroupListAsync(GroupFilter filter);
    Task<ResponseViewModel<List<KeyValueModel<string, string>>>> GetGroupListAsync(string search, string organizationId);
    Task<ResponseViewModel<GroupViewModel2>> GetGroupAsync(string groupId, string organizationId);
    Task<ResponseViewModel<object>> CreateGroupAsync(GroupCreateModel groupCreateObj, string loggedInUserId);
    Task<ResponseViewModel<object>> UpdateGroupAsync(GroupUpdateModel groupModel, string loggedInUserId);
    Task<ResponseViewModel<object>> DeleteGroupAsync(string id);
}

IGroupPermissionManager
This interface defines methods for managing permissions within groups, including retrieving, saving, and deleting group permissions.

public interface IGroupPermissionManager
{
    Task<ResponseViewModel<List<PermissionModel>>> GetGroupPermissionsByGroupAsync(GroupPermissionsByGroupFilter groupPermissionsByGroupFilter);
    Task<ResponseViewModel<GroupPermissionViewModel>> SaveGroupPermissionAsync(GroupPermissionSaveDeleteModel groupPermissionSaveDeleteModel, string loggedInUserId);
    Task<ResponseViewModel<string>> DeleteGroupPermissionAsync(GroupPermissionSaveDeleteModel groupPermissionSaveDeleteModel, string loggedInUserId);
}

IGroupUserManager
This interface defines methods for managing users within groups, including retrieving users by organization and group, saving and deleting group-user mappings, and retrieving all available users for a group.

public interface IGroupUserManager
{
    Task<ResponseViewModel<List<GroupUserListItem>>> GetUsersByOrganizationAsync(GroupUsersByOrganizationFilter usersByOrganizationFilter);
    Task<ResponseViewModel<GroupUserModel>> GetUsersByGroupAsync(UsersByGroupFilter usersByGroupFilter);
    Task<ResponseViewModel<UserGroupModel>> GetGroupsByUserAsync(GroupsByUserIdFilter groupsByUserIdFilter);
    Task<ResponseViewModel<GroupUserMappingViewModel>> SaveGroupUserMappingAsync(GroupUserMappingSaveDeleteModel groupUserMappingSaveDeleteModel, string loggedInUserId);
    Task<ResponseViewModel<string>> DeleteGroupUserMappingAsync(GroupUserMappingSaveDeleteModel groupUserMappingSaveDeleteModel, string loggedInUserId);
    Task<ResponseViewModel<List<OrganizationAllUserViewModel>>> GetAllAvailableGroupUserAsync(string organizationId, bool? isActive = true);
}
Models

The models used in the interfaces represent the data structures for groups, users, permissions, and their mappings. These include GroupViewModel, PermissionModel, GroupUserListItem, GroupPermissionViewModel, and others.

API Response Model
The ResponseViewModel<T> is used to standardize API responses, encapsulating the data along with metadata such as status and error messages.

Database Structure
Tables
Group

Stores information about groups.
Example: Admin
User

Stores information about users.
Examples: User1, User2
Permission

Stores information about permissions.
Examples: ReadTemplate, CreateTemplate
GroupPermission

Maps groups to permissions.
Examples:
Admin -> ReadTemplate
Admin -> CreateTemplate
GroupUser

Maps groups to users.
Examples:
Admin -> User1
Admin -> User2
Relationships
GroupPermission links the Group and Permission tables, indicating which permissions are assigned to which groups.
GroupUser links the Group and User tables, indicating which users belong to which groups.

Usage
Managing Groups
Use IGroupManager to create, retrieve, update, and delete groups.

Managing Permissions
Use IGroupPermissionManager to assign permissions to groups and manage these assignments.

Managing Users
Use IGroupUserManager to assign users to groups, retrieve users by group or organization, and manage these assignments.



Example
Creating a Group

var groupCreateObj = new GroupCreateModel { Name = "New Group" };
var response = await groupManager.CreateGroupAsync(groupCreateObj, loggedInUserId);
Assigning a Permission to a Group

var groupPermissionSaveDeleteModel = new GroupPermissionSaveDeleteModel { GroupId = "Admin", PermissionId = "ReadTemplate" };
var response = await groupPermissionManager.SaveGroupPermissionAsync(groupPermissionSaveDeleteModel, loggedInUserId);
Assigning a User to a Group

var groupUserMappingSaveDeleteModel = new GroupUserMappingSaveDeleteModel { GroupId = "Admin", UserId = "User1" };
var response = await groupUserManager.SaveGroupUserMappingAsync(groupUserMappingSaveDeleteModel, loggedInUserId);
