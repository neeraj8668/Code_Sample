
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
    public class GroupUserManager : BaseManager, IGroupUserManager
    {
        private readonly IUnitOfWork unitOfWork;

        // Constructor initializing dependencies
        public GroupUserManager(IUnitOfWork _unitOfWork, IHttpContextAccessor _httpContextAccessor, IStringLocalizer<SharedResource> _sharedResource)
            : base(_httpContextAccessor, _sharedResource)
        {
            unitOfWork = _unitOfWork;
        }

        // Method to get users by organization with filtering options
        public async Task<ResponseViewModel<List<GroupUserListItem>>> GetUsersByOrganizationAsync(GroupUsersByOrganizationFilter usersByOrganizationFilter)
        {
            var isFilterApplied = false;
            var data = unitOfWork.Of<Domain.EntityModel.User>().QueryAsync(x => x.IsDeleted != true).AsNoTracking()
                .Include(x => x.Organization)
                .Include(x => x.GroupUserMappings)
                .Where(x => x.OrganizationId == usersByOrganizationFilter.OrganizationId && x.IsActive == true
                       && (x.GroupUserMappings.Count() == 0
                         || (x.GroupUserMappings.Any() && x.GroupUserMappings.FirstOrDefault().GroupId == usersByOrganizationFilter.GroupId))
                         );

            // Applying filters based on the input filter object
            if (!string.IsNullOrEmpty(usersByOrganizationFilter.FirstName))
            {
                isFilterApplied = true;
                data = data.Where(x => x.FirstName.Contains(usersByOrganizationFilter.FirstName));
            }

            if (!string.IsNullOrEmpty(usersByOrganizationFilter.LastName))
            {
                isFilterApplied = true;
                data = data.Where(x => x.LastName.Contains(usersByOrganizationFilter.LastName));
            }

            if (!string.IsNullOrEmpty(usersByOrganizationFilter.EmailAddress))
            {
                isFilterApplied = true;
                data = data.Where(x => x.EmailAddress.Contains(usersByOrganizationFilter.EmailAddress));
            }

            if (!string.IsNullOrEmpty(usersByOrganizationFilter.Search))
            {
                isFilterApplied = true;
                data = data.Where(x => x.FirstName.Contains(usersByOrganizationFilter.Search)
                            || x.LastName.Contains(usersByOrganizationFilter.Search)
                            || x.EmailAddress.Contains(usersByOrganizationFilter.Search));
            }

            if (usersByOrganizationFilter.IsGroupUser.HasValue)
            {
                isFilterApplied = true;
                data = data.Where(x => x.GroupUserMappings.Any(x => x.GroupId == usersByOrganizationFilter.GroupId) == usersByOrganizationFilter.IsGroupUser);
            }

            // Adding default sorting if no sorting is provided
            if (string.IsNullOrEmpty(usersByOrganizationFilter.SortOrder))
            {
                usersByOrganizationFilter.SortOrder = HelperConstants.Query.Acsending;
            }
            if (string.IsNullOrEmpty(usersByOrganizationFilter.SortBy))
            {
                usersByOrganizationFilter.SortBy = nameof(User.FirstName).ToLower();
            }

            // Applying sorting based on the input filter object
            if (!string.IsNullOrEmpty(usersByOrganizationFilter.SortBy) && !string.IsNullOrEmpty(usersByOrganizationFilter.SortOrder))
            {
                if (usersByOrganizationFilter.SortBy?.ToLower() == nameof(User.FirstName).ToLower())
                {
                    isFilterApplied = true;
                    data = usersByOrganizationFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                        ? data.OrderByDescending(x => x.FirstName)
                        : data.OrderBy(x => x.FirstName);
                }
                else if (usersByOrganizationFilter.SortBy?.ToLower() == nameof(User.LastName).ToLower())
                {
                    isFilterApplied = true;
                    data = usersByOrganizationFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                        ? data.OrderByDescending(x => x.LastName)
                        : data.OrderBy(x => x.LastName);
                }
                else if (usersByOrganizationFilter.SortBy?.ToLower() == nameof(User.EmailAddress).ToLower())
                {
                    isFilterApplied = true;
                    data = usersByOrganizationFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                        ? data.OrderByDescending(x => x.EmailAddress)
                        : data.OrderBy(x => x.EmailAddress);
                }
            }

            if (!isFilterApplied)
            {
                data = data.OrderByDescending(x => x.GroupUserMappings.Any(x => x.GroupId == usersByOrganizationFilter.GroupId));
            }

            var totalRecords = await data.CountAsync();

            // Applying pagination if specified
            if (usersByOrganizationFilter.PageNo.HasValue && usersByOrganizationFilter.PageSize.HasValue)
            {
                data = data.Skip((usersByOrganizationFilter.PageNo.Value - 1) * usersByOrganizationFilter.PageSize.Value).Take(usersByOrganizationFilter.PageSize.Value);
            }

            // Fetching the data and mapping to the response model
            if (data.Any())
            {
                var allUsersByOrganization = data.AsNoTracking().Select(i => new GroupUserListItem
                {
                    UserId = i.UserId,
                    OrganizationId = i.OrganizationId,
                    FirstName = i.FirstName,
                    LastName = i.LastName,
                    EmailAddress = i.EmailAddress,
                    IsGroupUser = i.GroupUserMappings.Any(x => x.GroupId == usersByOrganizationFilter.GroupId),
                    IsActive = i.IsActive
                });

                return new ResponseViewModel<List<GroupUserListItem>>
                {
                    Success = true,
                    Data = allUsersByOrganization.ToList(),
                    MetaData = new Dictionary<object, object>()
                    {
                        {ResponseMetaDataType.TotalRecords , totalRecords}
                    }
                };
            }

            return new ResponseViewModel<List<GroupUserListItem>>()
            {
                Success = false,
                Message = GetResponseMessage(MessageConstant._2801)
            };
        }

        // Method to get users by group with filtering options
        public async Task<ResponseViewModel<GroupUserModel>> GetUsersByGroupAsync(UsersByGroupFilter usersByGroupFilter)
        {
            var data = unitOfWork.Of<Domain.EntityModel.GroupUserMapping>().QueryAsync().AsNoTracking()
                .Include(g => g.Group)
                .Include(g => g.User)
                .Where(x => x.GroupId == usersByGroupFilter.GroupId && x.IsActive == true);

            // Checking if data exists and applying filters
            if (data.Any())
            {
                if (!string.IsNullOrEmpty(usersByGroupFilter.FirstName))
                {
                    data = data.Where(x => x.User.FirstName.Contains(usersByGroupFilter.FirstName));
                }

                if (!string.IsNullOrEmpty(usersByGroupFilter.LastName))
                {
                    data = data.Where(x => x.User.LastName.Contains(usersByGroupFilter.LastName));
                }

                if (!string.IsNullOrEmpty(usersByGroupFilter.EmailAddress))
                {
                    data = data.Where(x => x.User.EmailAddress.Contains(usersByGroupFilter.EmailAddress));
                }

                // Applying sorting based on the input filter object
                if (!string.IsNullOrEmpty(usersByGroupFilter.SortBy) && !string.IsNullOrEmpty(usersByGroupFilter.SortOrder))
                {
                    if (usersByGroupFilter.SortBy?.ToLower() == nameof(User.FirstName).ToLower())
                    {
                        data = usersByGroupFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                            ? data.OrderByDescending(x => x.User.FirstName)
                            : data.OrderBy(x => x.User.FirstName);
                    }

                    if (usersByGroupFilter.SortBy?.ToLower() == nameof(User.LastName).ToLower())
                    {
                        data = usersByGroupFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                            ? data.OrderByDescending(x => x.User.LastName)
                            : data.OrderBy(x => x.User.LastName);
                    }

                    if (usersByGroupFilter.SortBy?.ToLower() == nameof(User.EmailAddress).ToLower())
                    {
                        data = usersByGroupFilter.SortOrder.ToUpper() == HelperConstants.Query.Descending
                            ? data.OrderByDescending(x => x.User.EmailAddress)
                            : data.OrderBy(x => x.User.EmailAddress);
                    }
                }

                var totalRecords = await data.CountAsync();

                // Applying pagination if specified
                if (usersByGroupFilter.PageNo.HasValue && usersByGroupFilter.PageSize.HasValue)
                {
                    data = data.Skip((usersByGroupFilter.PageNo.Value - 1) * usersByGroupFilter.PageSize.Value).Take(usersByGroupFilter.PageSize.Value);
                }

                if (data.Any())
                {
                    GroupUserMapping groupUserMapping = data.FirstOrDefault();

                    GroupUserModel groupUserModel = new()
                    {
                        GroupId = groupUserMapping.GroupId,
                        GroupStatus = groupUserMapping.IsActive.Value,
                        GroupName = groupUserMapping.Group.GroupName,
                        GroupUsers = data.AsNoTracking().Select(i => new GroupUserListItem
                        {
                            UserId = i.User.UserId,
                            OrganizationId = i.User.OrganizationId,
                            FirstName = i.User.FirstName,
                            LastName = i.User.LastName,
                            EmailAddress = i.User.EmailAddress,
                            IsActive = i.User.IsActive
                        }).ToList()
                    };

                    return new ResponseViewModel<GroupUserModel>
                    {
                        Success = true,
                    }
                }
            }
        }
    }
}