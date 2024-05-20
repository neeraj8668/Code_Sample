using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Storyboard.DL.Repository;
using Storyboard.Domain.ApiResponseModel;
using Storyboard.Domain.Constant;
using Storyboard.Domain.EntityModel;
using Storyboard.Domain.Interface.Business;
using Storyboard.Domain.Resources;
using Storyboard.Helper.Constant;
using Storyboard.Helper.Helpers;
using Storyboard.Helper.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storyboard.BL.Managers
{
    // Manager class responsible for handling group permissions related operations
    public class GroupPermissionManager : BaseManager, IGroupPermissionManager
    {
        // Unit of work instance for database operations
        private readonly IUnitOfWork unitOfWork;

        // Constructor to initialize dependencies
        public GroupPermissionManager(IUnitOfWork _unitOfWork, IHttpContextAccessor _httpContextAccessor, IStringLocalizer<SharedResource> _sharedResource)
            : base(_httpContextAccessor, _sharedResource)
        {
            unitOfWork = _unitOfWork;
        }

        // Method to get group permissions by group filter
        public async Task<ResponseViewModel<List<PermissionModel>>> GetGroupPermissionsByGroupAsync(GroupPermissionsByGroupFilter groupPermissionsByGroupFilter)
        {
            var isFilterApplied = false;
            // Initial query to get active permissions with related group permissions
            var data = unitOfWork.Of<Domain.EntityModel.Permission>().QueryAsync().AsNoTracking()
                .Include(x => x.GroupPermissions)
                .Where(x => x.IsActive == true);

            // Apply filters based on the group permissions filter provided
            if (!string.IsNullOrEmpty(groupPermissionsByGroupFilter.PermissionName))
            {
                isFilterApplied = true;
                data = data.Where(x => (x.Name + x.Action) == groupPermissionsByGroupFilter.PermissionName || x.Name == groupPermissionsByGroupFilter.PermissionName);
            }

            if (!string.IsNullOrEmpty(groupPermissionsByGroupFilter.Search))
            {
                isFilterApplied = true;
                data = data.Where(x =>
                x.Name.Contains(groupPermissionsByGroupFilter.Search) 
                || x.Action.Contains(groupPermissionsByGroupFilter.Search) 
                || (x.Name + x.Action).Contains(groupPermissionsByGroupFilter.Search) 
                || x.Description.Contains(groupPermissionsByGroupFilter.Search));
            }

            if (groupPermissionsByGroupFilter.IsGroupPermission.HasValue)
            {
                isFilterApplied = true;
                data = data.Where(x => x.GroupPermissions.Any(x => x.GroupId == groupPermissionsByGroupFilter.GroupId) == groupPermissionsByGroupFilter.IsGroupPermission);
            }

            // Apply default sorting if no sorting is provided
            if (string.IsNullOrEmpty(groupPermissionsByGroupFilter.SortOrder))
            {
                groupPermissionsByGroupFilter.SortOrder = HelperConstants.Query.Acsending;
            }
            if (string.IsNullOrEmpty(groupPermissionsByGroupFilter.SortBy))
            {
                groupPermissionsByGroupFilter.SortBy = nameof(Permission.PermissionName).ToLower();
            }

            // Apply sorting based on the provided sort by and sort order values
            if (!string.IsNullOrEmpty(groupPermissionsByGroupFilter.SortBy) && !string.IsNullOrEmpty(groupPermissionsByGroupFilter.SortOrder))
            {
                if (groupPermissionsByGroupFilter.SortBy?.ToLower() == nameof(Permission.PermissionName).ToLower())
                {
                    isFilterApplied = true;
                }
                else if (groupPermissionsByGroupFilter.SortBy?.ToLower() == nameof(Permission.Description).ToLower())
                {
                    isFilterApplied = true;
                    data = groupPermissionsByGroupFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                        ? data.OrderByDescending(x => x.Description)
                        : data.OrderBy(x => x.Description);
                }
            }

            if (!isFilterApplied)
            {
                data = data.OrderByDescending(x => x.GroupPermissions.Any(x => x.GroupId == groupPermissionsByGroupFilter.GroupId));
            }

            // Get the total record count before pagination
            var totalRecords = await data.CountAsync();

            // Apply pagination if page number and page size are provided
            if (groupPermissionsByGroupFilter.PageNo.HasValue && groupPermissionsByGroupFilter.PageSize.HasValue)
            {
                data = data
                    .Skip((groupPermissionsByGroupFilter.PageNo.Value - 1) * groupPermissionsByGroupFilter.PageSize.Value)
                    .Take(groupPermissionsByGroupFilter.PageSize.Value);
            }

            // Return the filtered and sorted permissions
            if (data.Any())
            {
                var allPermissionsByGroup = data.AsNoTracking().Select(i => new PermissionModel
                {
                    PermissionId = i.PermissionId,
                    PermissionName = i.PermissionName,
                    Description = i.Description,
                    IsGroupPermission = i.GroupPermissions.Any(x => x.GroupId == groupPermissionsByGroupFilter.GroupId),
                    IsActive = i.IsActive
                }).ToList();

                if (groupPermissionsByGroupFilter.SortBy?.ToLower() == nameof(Permission.PermissionName).ToLower())
                {
                    isFilterApplied = true;
                    allPermissionsByGroup = groupPermissionsByGroupFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                        ? allPermissionsByGroup.OrderByDescending(x => x.PermissionName).ToList()
                        : allPermissionsByGroup.OrderBy(x => x.PermissionName).ToList();
                }
                return new ResponseViewModel<List<PermissionModel>>
                {
                    Success = true,
                    Data = allPermissionsByGroup,
                    MetaData = new Dictionary<object, object>()
                    {
                        {ResponseMetaDataType.TotalRecords , totalRecords}
                    }
                };
            }

            // Return a response indicating no permissions found
            return new ResponseViewModel<List<PermissionModel>>()
            {
                Success = false,
                Message = GetResponseMessage(MessageConstant._1001)
            };
        }

        // Method to save a group permission
        public async Task<ResponseViewModel<GroupPermissionViewModel>> SaveGroupPermissionAsync(GroupPermissionSaveDeleteModel groupPermissionSaveDeleteModel, string loggedInUserId)
        {
            ResponseViewModel<GroupPermissionViewModel> response = default;
            List<string> errors = new();

            // Validate the input model
            if (string.IsNullOrEmpty(groupPermissionSaveDeleteModel.OrganizationId))
            {
                errors.Add(GetResponseMessage(MessageConstant._1004));
            }

            if (string.IsNullOrEmpty(groupPermissionSaveDeleteModel.GroupId))
            {
                errors.Add(GetResponseMessage(MessageConstant._1006));
            }

            if (string.IsNullOrEmpty(groupPermissionSaveDeleteModel.PermissionId))
            {
                errors.Add(GetResponseMessage(MessageConstant._2604));
            }

            var (oldValues, newValues) = unitOfWork.GetAuditDetail();
            var metaData = new Dictionary<object, object>();

            if (!errors.Any())
            {
                // Check if the group permission already exists
                var groupPermission = await unitOfWork.Of<Domain.EntityModel.GroupPermission>()
                  .QueryAsync(x => x.GroupId.Equals(groupPermissionSaveDeleteModel.GroupId) &&
                  x.PermissionId.Equals(groupPermissionSaveDeleteModel.PermissionId)).FirstOrDefaultAsync();

                if (groupPermission != null)
                {
                    // Update existing group permission
                    groupPermission.IsActive = true;
                    groupPermission.ModifiedOn = DateTimeHelper.GetCurrentTime();

                    await unitOfWork.Of<Domain.EntityModel.GroupPermission>().UpdateAsync(groupPermission);
                    unitOfWork.GetAuditDetail(oldValues, newValues);

                    await unitOfWork.SaveChangesAsync();

                    metaData.Add(oldValues, newValues);
                }
                else
                {
                    // Insert new group permission
                    groupPermission = new Domain.EntityModel.GroupPermission()
                    {
                        GroupPermissionId = await unitOfWork.Of<Domain.EntityModel.GroupPermission>().GetNextSequence(),
                        OrganizationId = groupPermissionSaveDeleteModel.OrganizationId,
                        GroupId = groupPermissionSaveDeleteModel.GroupId,
                        PermissionId = groupPermissionSaveDeleteModel.PermissionId,
                        IsActive = true,
                        CreatedBy = loggedInUserId,
                        CreatedOn = DateTimeHelper.GetCurrentTime()
                    };

                    await unitOfWork.Of<Domain.EntityModel.GroupPermission>().InsertAsync(groupPermission);
                    unitOfWork.GetAuditDetail(oldValues, newValues);

                    await unitOfWork.SaveChangesAsync();

                    metaData.Add(oldValues, newValues);
                }

                // Return the response with the group permission details
                response = new ResponseViewModel<GroupPermissionViewModel>()
                {
                    Success = true,
                    Data = new GroupPermissionViewModel
                    {
                        GroupPermissionId = groupPermission.GroupPermissionId,
                        OrganizationId = groupPermission.OrganizationId,
                        GroupId = groupPermission.GroupId,
                        IsActive = groupPermission.IsActive,
                        CreatedBy = groupPermission.CreatedBy,
                        CreatedOn = groupPermission.CreatedOn,
                        ModifiedBy = groupPermission.ModifiedBy,
                        ModifiedOn = groupPermission.ModifiedOn
                    },
                    Message = GetResponseMessage(MessageConstant._2601),
                    MetaData = metaData
                };
            }
            else
            {
                // Return the response with validation errors
                response = new ResponseViewModel<GroupPermissionViewModel>()
                {
                    ErrorMessage = errors.ToArray(),
                    Success = false
                };
            }

            return response;
        }
    }
}
