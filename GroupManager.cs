using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Storyboard.DL.Repository;
using Storyboard.Domain.ApiResponseModel;
using Storyboard.Domain.Constant;
using Storyboard.Domain.EntityModel;
using Storyboard.Domain.Interface.Business;
using Storyboard.Domain.Interface.Repository;
using Storyboard.Domain.Resources;
using Storyboard.Helper.Constant;
using Storyboard.Helper.Helpers;
using Storyboard.Helper.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storyboard.BL.Managers
{
    public class GroupManager : BaseManager, IGroupManager
    {
        private readonly IUnitOfWork unitOfWork;

        // Constructor to initialize dependencies
        public GroupManager(IUnitOfWork _unitOfWork, IHttpContextAccessor _httpContextAccessor, IStringLocalizer<SharedResource> _sharedResource)
            : base(_httpContextAccessor, _sharedResource)
        {
            unitOfWork = _unitOfWork;
        }

        // Method to get a list of groups based on provided filters
        public async Task<ResponseViewModel<List<GroupViewModel>>> GetGroupListAsync(GroupFilter filter)
        {
            // Prepare the initial query to fetch groups excluding the default "AllPermission" group
            var query = unitOfWork.Of<Domain.EntityModel.Group>().QueryAsync(x => x.GroupName != Constants.DefaultGroupType.AllPermission).AsNoTracking().
                Join(unitOfWork.Of<Domain.EntityModel.Organization>().QueryAsync().AsNoTracking(), g => g.OrganizationId, o => o.OrganizationId, (g, o) => new { g, o });

            // Apply filters if provided
            if (!string.IsNullOrEmpty(filter.GroupName))
            {
                query = query.Where(x => x.g.GroupName.Contains(filter.GroupName));
            }
            if (!string.IsNullOrEmpty(filter.OrganizationId))
            {
                query = query.Where(x => x.g.OrganizationId == filter.OrganizationId);
            }
            if (filter.IsActive != null)
            {
                query = query.Where(x => x.g.IsActive == filter.IsActive.Value);
            }

            // Set default sorting if not provided
            if (string.IsNullOrEmpty(filter.SortOrder))
            {
                filter.SortOrder = HelperConstants.Query.Acsending;
            }
            if (string.IsNullOrEmpty(filter.SortBy))
            {
                filter.SortBy = "groupname";
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy) && !string.IsNullOrEmpty(filter.SortOrder))
            {
                if (filter.SortBy?.ToLower() == "groupname")
                {
                    if (filter.SortOrder.ToUpper() == HelperConstants.Query.Descending)
                        query = query.OrderByDescending(x => x.g.GroupName);
                    else
                        query = query.OrderBy(x => x.g.GroupName);
                }
            }

            // Get the total record count before applying pagination
            var totalRecords = await query.CountAsync();

            // Apply pagination if provided
            if (filter.PageNo.HasValue && filter.PageSize.HasValue)
            {
                query = query.Skip((filter.PageNo.Value - 1) * filter.PageSize.Value).Take(filter.PageSize.Value);
            }

            // Fetch the data and project it into the GroupViewModel
            var data = await query
               .AsNoTracking().Select(i => new GroupViewModel
               {
                   GroupId = i.g.GroupId,
                   GroupName = i.g.GroupName,
                   IsActive = Common.GetActiveStatus(i.g.IsActive),
                   OrganizationName = i.o.OrganizationName,
                   OrganizationId = i.g.OrganizationId
               }).ToListAsync();

            // Return the response with the fetched data and metadata
            return new ResponseViewModel<List<GroupViewModel>>
            {
                Success = true,
                Data = data,
                MetaData = new Dictionary<object, object>()
                {
                    {ResponseMetaDataType.TotalRecords , totalRecords}
                }
            };
        }

        // Method to get a list of groups based on a search term and organization ID
        public async Task<ResponseViewModel<List<KeyValueModel<string, string>>>> GetGroupListAsync(string search, string organizationId)
        {
            // Prepare the query to fetch active groups for the specified organization
            var query = unitOfWork.Of<Domain.EntityModel.Group>()
                .QueryAsync(x =>
                    x.OrganizationId == organizationId &&
                    x.IsActive == true)
                .AsNoTracking();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.GroupName.Contains(search));
            }

            // Fetch the data and project it into the KeyValueModel
            var data = await query
               .AsNoTracking().Select(i => new KeyValueModel<string, string>
               {
                   Key = i.GroupId,
                   Value = i.GroupName
               }).ToListAsync();

            // Return the response with the fetched data
            return new ResponseViewModel<List<KeyValueModel<string, string>>>
            {
                Success = true,
                Data = data
            };
        }

        // Method to get details of a group by its ID and organization ID
        public async Task<ResponseViewModel<GroupViewModel2>> GetGroupAsync(string groupId, string organizationId)
        {
            // Fetch the group details
            var group = await unitOfWork.Of<Domain.EntityModel.Group>()
                .QueryAsync(x => x.GroupId.Equals(groupId))
                .FirstOrDefaultAsync();

            // Fetch the permissions associated with the group
            var allPermissions = await unitOfWork.Of<Domain.EntityModel.Permission>().QueryAsync()
                .Include(x => x.GroupPermissions)
                .Where(x => x.GroupPermissions.Any(x => x.GroupId == groupId))
                .ToListAsync();

            // Fetch all distinct actions associated with permissions
            var allActions = await unitOfWork.Of<Domain.EntityModel.Permission>().QueryAsync().Select(x => x.Action).Distinct().ToListAsync();

            // Fetch the users associated with the group
            var groupUsersQuery = unitOfWork.Of<Domain.EntityModel.GroupUserMapping>()
                .QueryAsync(x => x.GroupId == groupId && x.OrganizationId == organizationId && x.User != null
                     && x.User.IsFirstLogin == true && x.User.IsEmailVerified == true && x.User.IsActive == true);

            var groupUsers = await groupUsersQuery.Select(x => new GetGroupUserResListVM
            {
                UserId = x.UserId,
                UserName = $"{x.User.FirstName ?? ""} {x.User.LastName ?? ""}".Trim()
            }).ToListAsync();

            // Prepare the response with the fetched details
            var response = new GroupViewModel2
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                IsActive = group.IsActive,
                GroupUsers = groupUsers,
                OrganizationId = organizationId,
                Permissions = allPermissions?.Select(x => x.PermissionId).ToList() ?? new List<string>()
            };
            return new ResponseViewModel<GroupViewModel2>
            {
                Success = true,
                Data = response,
                MetaData = new Dictionary<object, object>
                {
                    {ResponseMetaDataType.PermissionActions, allActions}
                }
            };
        }

        // Method to create a new group
        public async Task<ResponseViewModel<object>> CreateGroupAsync(GroupCreateModel groupCreateObj, string loggedInUserId)
        {
            ResponseViewModel<object> response = default;
            List<string> errors = new List<string>();

            // Validate input
            if (groupCreateObj == null)
            {
                response = new ResponseViewModel<object>()
                {
                    Message = GetResponseMessage(MessageConstant._1002),
                    Success = false
                };
                return response;
            }

            if (string.IsNullOrEmpty(groupCreateObj.GroupName))
            {
                errors.Add(GetResponseMessage(MessageConstant._2401));
            }

            if (groupCreateObj.GroupName == Constants.DefaultGroupType.AllPermission)
            {
                errors.Add(GetResponseMessage(MessageConstant._2407));
            }
            if (!await ValidateDuplicateGroupNameAsync(groupCreateObj.GroupName, groupCreateObj.OrganizationId))
            {
                return new ResponseViewModel<object>()
                {
                    Message = GetResponseMessage(MessageConstant._2411),
                    Success = false
                };
            }
            if (!errors.Any())
            {
                // Generate the next sequence ID for the new group
                var groupId = await unitOfWork.Of<Domain.EntityModel.Group>().GetNextSequence();
                if (groupId != null)
                {
                    // Insert the new group into the database
                    await unitOfWork.Of<Domain.EntityModel.Group>()
                          .InsertAsync(new Domain.EntityModel.Group()
                          {
                              GroupId = groupId,
                              GroupName = groupCreateObj.GroupName,
                              OrganizationId = groupCreateObj.OrganizationId,
                              IsActive = true,
                              CreatedBy = loggedInUserId,
                              CreatedOn = DateTimeHelper.GetCurrentTime()
                          });

                    var (oldValues, newValues) = unitOfWork.GetAuditDetail();

                    unitOfWork.GetAuditDetail(oldValues, newValues);

                    await unitOfWork.SaveChangesAsync();

                    var metaData = new Dictionary<object, object>
                    {
                        { oldValues, newValues }
                    };

                    // Prepare and return the response with success message
                    response = new ResponseViewModel<object>()
                    {
                        Message = GetResponseMessage(MessageConstant._2402, groupCreateObj.GroupName),
                        Success = true,
                        Data = groupCreateObj,
                        MetaData = metaData
                    };
                }
            }
            else
            {
                // Return response with errors if validation failed
                response = new ResponseViewModel<object>()
            }
        }
    }
}