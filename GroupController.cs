using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using Storyboard.API.Filter;
using Storyboard.BL.Managers;
using Storyboard.Domain.ApiRequestModel;
using Storyboard.Domain.ApiResponseModel;
using Storyboard.Domain.Constant;
using Storyboard.Domain.Enum;
using Storyboard.Domain.Interface.Business;
using Storyboard.Helper.Helpers;
using Storyboard.Helper.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storyboard.API.Controllers
{
    // Define the route for this controller and mark it as an API controller
    [Route("[controller]")]
    [ApiController]
    [TokenAuth] // Custom attribute for token-based authentication
    public class GroupController : BaseController
    {
        // Declare dependencies to be injected via constructor
        private readonly IGroupManager groupManager;
        private readonly IGroupUserManager groupUserManager;
        private readonly IGroupPermissionManager groupPermissionManager;
        private readonly IPermissionManager permissionManager;

        // Constructor for dependency injection
        public GroupController(IGroupManager _groupManager, IGroupUserManager _groupUserManager, IPermissionManager _permissionManager, IGroupPermissionManager _groupPermissionManager, IHttpContextAccessor _httpContextAccessor, IAuditTrailManager _auditManager) : base(_httpContextAccessor, _auditManager)
        {
            groupManager = _groupManager;
            groupUserManager = _groupUserManager;
            groupPermissionManager = _groupPermissionManager;
            permissionManager = _permissionManager;
        }

        // Endpoint to get a list of groups as key-value pairs, filtered by search term
        [HttpGet, Route("key-value")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.Dashboard.AdminPanel)] // Custom attribute for authorization
        public async Task<IActionResult> GetGroupListAsync(string search)
        {
            var result = await groupManager.GetGroupListAsync(search, OrganizationId);
            return Ok(result);
        }

        // Endpoint to get a filtered list of groups
        [HttpPost, Route("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.Group.Read)]
        public async Task<IActionResult> GetGroupListAsync(GroupFilter userFilter)
        {
            // If no OrganizationId is specified, use the one from the current context
            if (string.IsNullOrEmpty(userFilter.OrganizationId))
                userFilter.OrganizationId = OrganizationId;

            // Default IsActive to true if not specified
            if (userFilter.IsActive == null)
            {
                userFilter.IsActive = true;
            }

            var result = await groupManager.GetGroupListAsync(userFilter);

            // Log the action in the audit trail
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,
                OrganizationId = OrganizationId,
                KeyType = nameof(Domain.EntityModel.Group),
                Url = GetActionRoute(),
                RequestParametersJson = userFilter.ToJsonIgnoreNulls(),
                Action = nameof(AuditTrailActionType.Get),
                Section = nameof(AuditTrailSectionType.Group),
                SystemRemarks = AuditTrailConstants.SystemRemarks.ViewGroupList,
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress
            });

            return Ok(result);
        }

        // Endpoint to create a new group
        [HttpPost, Route("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AuthorizeClaim(Permissions.Group.Create)]
        public async Task<IActionResult> CreateGroupAsync(GroupCreateModel groupCreateModel)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.GetModelErrors();
                if (errors.Count() > 0)
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()
                    });
                }
            }

            var result = await groupManager.CreateGroupAsync(groupCreateModel, LoggedInUserId);

            // Log the action in the audit trail
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,
                OrganizationId = OrganizationId,
                KeyType = nameof(Domain.EntityModel.Group),
                Url = GetActionRoute(),
                RequestParametersJson = groupCreateModel.ToJsonIgnoreNulls(),
                Action = nameof(AuditTrailActionType.Add),
                Section = nameof(AuditTrailSectionType.Group),
                NewValuesJson = result.Success ? result.MetaData?.Values.FirstOrDefault().ToJson() : null,
                SystemRemarks = AuditTrailConstants.SystemRemarks.AddGroup,
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress
            });

            result.MetaData = null;
            return Ok(result);
        }

        // Endpoint to get details of a group by ID
        [HttpGet, Route("{groupId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AuthorizeClaim(Permissions.Group.Read)]
        public async Task<IActionResult> GetGroupByIdAsync(string groupId)
        {
            var result = await groupManager.GetGroupAsync(groupId, OrganizationId);

            // Log the action in the audit trail
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,
                OrganizationId = OrganizationId,
                KeyType = nameof(Domain.EntityModel.Group),
                Url = GetActionRoute(),
                RequestParametersJson = groupId,
                Action = nameof(AuditTrailActionType.Get),
                Section = nameof(AuditTrailSectionType.Group),
                SystemRemarks = AuditTrailConstants.SystemRemarks.ViewGroupDetail,
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress
            });

            return Ok(result);
        }

        // Endpoint to delete a group by ID
        [HttpDelete, Route("delete/{groupId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AuthorizeClaim(Permissions.Group.Delete)]
        public async Task<IActionResult> DeleteGroupAsync(string groupId)
        {
            var result = await groupManager.DeleteGroupAsync(groupId);

            // Log the action in the audit trail
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,
                OrganizationId = OrganizationId,
                KeyType = nameof(Domain.EntityModel.Group),
                KeyId = groupId,
                Url = GetActionRoute(),
                RequestParametersJson = groupId,
                Action = nameof(AuditTrailActionType.Delete),
                Section = nameof(AuditTrailSectionType.Group),
                OldValuesJson = result.Success ? result.MetaData?.Keys.FirstOrDefault().ToJsonIgnoreNulls() : null,
                SystemRemarks = AuditTrailConstants.SystemRemarks.DeleteGroup,
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress
            });

            result.MetaData = null;
            return Ok(result);
        }

        // Endpoint to update a group
        [HttpPut, Route("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AuthorizeClaim(Permissions.Group.Update)]
        public async Task<IActionResult> UpdateGroupAsync(GroupUpdateModel groupModel)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.GetModelErrors();
                if (errors.Count() > 0)
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()
                    });
                }
            }

            groupModel.OrganizationId = OrganizationId;
            var result = await groupManager.UpdateGroupAsync(groupModel, LoggedInUserId);

            // Log the action in the audit trail
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,
                OrganizationId = OrganizationId,
                KeyType = nameof(Domain.EntityModel.Group),
                KeyId = groupModel.GroupId,
                Url = GetActionRoute(),
                RequestParametersJson = groupModel.ToJsonIgnoreNulls(),
                Action = nameof(AuditTrailActionType.Update),
                Section = nameof(AuditTrailSectionType.Group),
                OldValuesJson = result.Success ? result.MetaData?.Keys.FirstOrDefault().ToJsonIgnoreNulls() : null,
                NewValuesJson = result.Success ? result.MetaData?.Values.FirstOrDefault().ToJson() : null,
                SystemRemarks = AuditTrailConstants.SystemRemarks.UpdateGroup,
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress
            });

            result.MetaData = null;
            return Ok(result);
        }

        // Endpoint to get all available users for a group
        [HttpGet, Route("group-user/available-users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllAvailableGroupUser()
        {
            var result = await groupUserManager.GetAllAvailableGroupUserAsync(OrganizationId, true);
            return Ok(result);
        }

        // Endpoint to get all permissions
        [HttpGet, Route("permission/list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllPermission()
        {
            var result = await permissionManager.GetAllPermissionAsync(OrganizationId);
            return Ok(result);
        }
    }
}
