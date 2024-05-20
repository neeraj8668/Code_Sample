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
    // Attribute to define the base route for the controller
    [Route("[controller]")]
    [ApiController]
    [TokenAuth]
    public class GroupPermissionController : BaseController
    {
        // Private field to hold the instance of the group permission manager
        private readonly IGroupPermissionManager _groupPermissionManager;

        // Constructor for the controller
        // This constructor is called when the controller is instantiated
        // It receives dependencies via dependency injection
        public GroupPermissionController(
            IGroupPermissionManager permissionManager,  // Dependency for managing group permissions
            IHttpContextAccessor httpContextAccessor,   // Dependency to access HTTP context
            IAuditTrailManager auditManager)            // Dependency to manage audit trails
            : base(httpContextAccessor, auditManager)   // Call to the base class constructor
        {
            _groupPermissionManager = permissionManager;  // Assigning the injected group permission manager to the private field
        }

        // Endpoint to list group permissions by group
        // This method handles POST requests to the "list" route
        [HttpPost, Route("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.GroupPermission.Read)]
        public async Task<IActionResult> GetGroupPermissionsByGroupAsync(GroupPermissionsByGroupFilter groupPermissionsByGroupFilter)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                // If the model state is invalid, get the list of errors
                var errors = ModelState.GetModelErrors();
                if (errors.Any())
                {
                    return Ok(new ResponseViewModel<IEnumerable<string>>()
                    {
                        Success = false,
                        ErrorMessage = errors.ToArray()  // Return the errors as an array
                    });
                }
            }

            var result = await _groupPermissionManager.GetGroupPermissionsByGroupAsync(groupPermissionsByGroupFilter);
            return Ok(result);
        }

        // Endpoint to create group permissions
        // This method handles POST requests to the "create" route
        [HttpPost, Route("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.GroupPermission.Create)]
        public async Task<IActionResult> SaveGroupPermissionAsync(GroupPermissionSaveDeleteModel groupPermissionSaveDeleteModel)
        {
            // Check if the model state is valid
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

            var result = await _groupPermissionManager.SaveGroupPermissionAsync(groupPermissionSaveDeleteModel, LoggedInUserId);
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType, 
                OrganizationId = OrganizationId, 
                KeyType = nameof(Domain.EntityModel.GroupPermission),
                Url = GetActionRoute(),  
                RequestParametersJson = groupPermissionSaveDeleteModel.ToJsonIgnoreNulls(),  
                Action = nameof(AuditTrailActionType.Add),  
                Section = nameof(AuditTrailSectionType.GroupPermission),  
                OldValuesJson = result.Success ? result.MetaData?.Keys.FirstOrDefault().ToJsonIgnoreNulls() : null,  
                NewValuesJson = result.Success ? result.MetaData?.Values.FirstOrDefault().ToJson() : null,  
                SystemRemarks = AuditTrailConstants.SystemRemarks.AddGroupPermission,  
                CreatedBy = LoggedInUserId, 
                IpAddress = IpAddress 
            });
            result.MetaData = null;
            return Ok(result);
        }

        // Endpoint to delete group permissions
        // This method handles DELETE requests to the "delete" route
        [HttpDelete, Route("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AuthorizeClaim(Permissions.GroupPermission.Delete)]
        public async Task<IActionResult> DeleteGroupPermissionAsync(GroupPermissionSaveDeleteModel groupPermissionSaveDeleteModel)
        {
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

            var result = await _groupPermissionManager.DeleteGroupPermissionAsync(groupPermissionSaveDeleteModel, LoggedInUserId);
            await auditManager.CreateAuditTrailAsync(new CreateAuditTrailViewModel
            {
                UserType = UserType,  
                OrganizationId = OrganizationId,  
                KeyType = nameof(Domain.EntityModel.GroupPermission),  
                Url = GetActionRoute(),
                RequestParametersJson = groupPermissionSaveDeleteModel.ToJsonIgnoreNulls(), 
                Action = nameof(AuditTrailActionType.Delete),  
                Section = nameof(AuditTrailSectionType.GroupPermission),  
                OldValuesJson = result.Success ? result.MetaData?.Keys.FirstOrDefault().ToJsonIgnoreNulls() : null,  
                SystemRemarks = AuditTrailConstants.SystemRemarks.RemoveGroupPermission,  
                CreatedBy = LoggedInUserId,
                IpAddress = IpAddress  // The IP address of the user
            });

            result.MetaData = null;
            return Ok(result);
        }
    }
}
