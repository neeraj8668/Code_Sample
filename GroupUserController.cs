
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    // Route for the controller, making it accessible via "/GroupUser"
    [Route("[controller]")]
    [ApiController]
    // Custom token authentication filter applied to the controller
    [TokenAuth]
    public class GroupUserController : BaseController
    {
        // Dependency injection for the IGroupUserManager interface
        private readonly IGroupUserManager _groupUserManager;

        // Constructor to initialize dependencies
        public GroupUserController(IGroupUserManager groupUserManager, IHttpContextAccessor _httpContextAccessor, IAuditTrailManager _auditManager) 
            : base(_httpContextAccessor, _auditManager)
        {
            _groupUserManager = groupUserManager;
        }

        // Endpoint to get users by organization, only accessible to users with GroupUser.Read permission
        [HttpPost, Route("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.GroupUser.Read)]
        public async Task<IActionResult> GetUsersByOrganizationAsync(GroupUsersByOrganizationFilter usersByOrganizationFilter)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.GetModelErrors();
                if (errors.Any())
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()
                    });
                }
            }
            // Fetch users by organization using the filter provided
            return Ok(await _groupUserManager.GetUsersByOrganizationAsync(usersByOrganizationFilter));
        }

        // Endpoint to get users by group, only accessible to users with GroupUser.Read permission
        [HttpPost, Route("users-by-group")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.GroupUser.Read)]
        public async Task<IActionResult> GetUsersByGroupAsync(UsersByGroupFilter usersByGroupFilter)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.GetModelErrors();
                if (errors.Any())
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()
                    });
                }
            }
            // Fetch users by group using the filter provided
            return Ok(await _groupUserManager.GetUsersByGroupAsync(usersByGroupFilter));
        }

        // Endpoint to get groups by user
        [HttpPost, Route("groups-by-user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGroupsByUserAsync(GroupsByUserIdFilter groupsByUserIdFilter)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.GetModelErrors();
                if (errors.Any())
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()
                    });
                }
            }
            // Fetch groups by user using the filter provided
            return Ok(await _groupUserManager.GetGroupsByUserAsync(groupsByUserIdFilter));
        }

        // Endpoint to create a group-user mapping, only accessible to users with GroupUser.Create permission
        [HttpPost, Route("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.GroupUser.Create)]
        public async Task<IActionResult> SaveGroupUserMappingAsync(GroupUserMappingSaveDeleteModel groupUserMappingSaveDeleteModel)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.GetModelErrors();
                if (errors.Any())
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()
                    });
                }
            }
            // Save the group-user mapping
            var result = await _groupUserManager.SaveGroupUserMappingAsync(groupUserMappingSaveDeleteModel, LoggedInUserId);
            // Create an audit trail for the action
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,
                OrganizationId = OrganizationId,
                KeyType = nameof(Domain.EntityModel.GroupUserMapping),
                Url = GetActionRoute(),
                RequestParametersJson = groupUserMappingSaveDeleteModel.ToJsonIgnoreNulls(),
                Action = nameof(AuditTrailActionType.Add),
                Section = nameof(AuditTrailSectionType.GroupUserMapping),
                SystemRemarks = AuditTrailConstants.SystemRemarks.AddGroupUser,
                NewValuesJson = result.Success ? result.MetaData?.Values.FirstOrDefault().ToJson() : null,
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress
            });
            // Clear metadata for the response
            result.MetaData = null;
            return Ok(result);
        }

        // Endpoint to delete a group-user mapping, only accessible to users with GroupUser.Delete permission
        [HttpDelete, Route("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.GroupUser.Delete)]
        public async Task<IActionResult> DeleteGroupUserMappingAsync(GroupUserMappingSaveDeleteModel groupUserMappingSaveDeleteModel)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.GetModelErrors();
                if (errors.Any())
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()
                    });
                }
            }
            // Delete the group-user mapping
            var result = await _groupUserManager.DeleteGroupUserMappingAsync(groupUserMappingSaveDeleteModel, LoggedInUserId);
            // Create an audit trail for the action
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,
                OrganizationId = OrganizationId,
                KeyType = nameof(Domain.EntityModel.GroupUserMapping),
                Url = GetActionRoute(),
                RequestParametersJson = groupUserMappingSaveDeleteModel.ToJsonIgnoreNulls(),
                Action = nameof(AuditTrailActionType.Delete),
                Section = nameof(AuditTrailSectionType.GroupUserMapping),
                SystemRemarks = AuditTrailConstants.SystemRemarks.RemoveGroupUser,
                OldValuesJson = result.Success ? result.MetaData?.Keys.FirstOrDefault().ToJson() : null,
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress
            });
            // Clear metadata for the response
            result.MetaData = null;
            return Ok(result);
        }
    }
}
