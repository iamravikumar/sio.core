// Licensed to the Sio I/O Foundation under one or more agreements.
// The Sio I/O Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sio.Domain.Core.ViewModels;
using Sio.Cms.Lib.Models.Cms;
using Sio.Cms.Lib;
using Sio.Cms.Lib.Services;
using System.Linq.Expressions;
using System.Web;
using Sio.Cms.Lib.ViewModels.SioPagePages;
using Microsoft.AspNetCore.SignalR;
using Sio.Cms.Hub;
using Microsoft.Extensions.Caching.Memory;

namespace Sio.Cms.Api.Controllers.v1
{
    [Produces("application/json")]
    [Route("api/v1/{culture}/page-page")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ApiPageArticleController))]
    public class ApiPagePageController :
        BaseGenericApiController<SioCmsContext, SioPagePage>
    {
        public ApiPagePageController(IMemoryCache memoryCache, IHubContext<PortalHub> hubContext) : base(memoryCache, hubContext)
        {

        }
        #region Get

        // GET api/page/id
        [HttpGet, HttpOptions]
        [Route("delete/{parentId}/{id}")]
        public async Task<RepositoryResponse<SioPagePage>> DeleteAsync(int parentId, int id)
        {
            return await base.DeleteAsync<ReadViewModel>(
                model => model.Id == id && model.ParentId == parentId && model.Specificulture == _lang, true);
        }

        // GET api/pages/id
        [HttpGet, HttpOptions]
        [Route("detail/{parentId}/{id}/{viewType}")]
        public async Task<ActionResult<JObject>> Details(string viewType, int? parentId, int? id)
        {
            string msg = string.Empty;
            switch (viewType)
            {

                default:
                    if (parentId.HasValue && id.HasValue)
                    {
                        Expression<Func<SioPagePage, bool>> predicate = model => model.ParentId == parentId && model.Id == id && model.Specificulture == _lang;
                        var portalResult = await base.GetSingleAsync<ReadViewModel>($"{viewType}_{parentId}_{id}", predicate);
                        if (portalResult.IsSucceed)
                        {
                            portalResult.Data.Page.DetailsUrl = SioCmsHelper.GetRouterUrl("Article", new { portalResult.Data.Page.SeoName }, Request, Url);
                        }

                        return Ok(JObject.FromObject(portalResult));
                    }
                    else
                    {
                        var model = new SioPagePage()
                        {
                            Specificulture = _lang,
                            Status = SioService.GetConfig<int>("DefaultStatus"),
                            Priority = ReadViewModel.Repository.Max(a => a.Priority).Data + 1
                        };

                        RepositoryResponse<ReadViewModel> result = await base.GetSingleAsync<ReadViewModel>($"{viewType}_default", null, model);
                        return Ok(JObject.FromObject(result));
                    }
            }
        }


        #endregion Get

        #region Post

        // POST api/page
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin, Admin")]
        [HttpPost, HttpOptions]
        [Route("save")]
        public async Task<RepositoryResponse<ReadViewModel>> Save([FromBody]ReadViewModel model)
        {
            if (model != null)
            {
                var result = await base.SaveAsync<ReadViewModel>(model, true);
                return result;
            }
            return new RepositoryResponse<ReadViewModel>() { Status = 501 };
        }

        // POST api/page
        [HttpPost, HttpOptions]
        [Route("save/{parentId}/{id}")]
        public async Task<RepositoryResponse<SioPagePage>> SaveFields(int parentId, int id, [FromBody]List<EntityField> fields)
        {
            if (fields != null)
            {
                var result = new RepositoryResponse<SioPagePage>() { IsSucceed = true };
                foreach (var property in fields)
                {
                    if (result.IsSucceed)
                    {
                        result = await ReadViewModel.Repository.UpdateFieldsAsync(c => c.ParentId == parentId && c.Id == id && c.Specificulture == _lang, fields).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }

                }
                return result;
            }
            return new RepositoryResponse<SioPagePage>();
        }

        // GET api/page
        [HttpPost, HttpOptions]
        [Route("list")]
        public async Task<ActionResult<JObject>> GetList(
            [FromBody] RequestPaging request)
        {
            var query = HttpUtility.ParseQueryString(request.Query ?? "");
            bool isParent = int.TryParse(query.Get("parent_id"), out int parentId);            
            bool isPage = int.TryParse(query.Get("page_id"), out int id);
            ParseRequestPagingDate(request);
            Expression<Func<SioPagePage, bool>> predicate = model =>
                        model.Specificulture == _lang
                        && (!isParent || model.ParentId == parentId)                        
                        && (!isPage || model.ParentId == id)
                        && (!request.Status.HasValue || model.Status == request.Status.Value)
                        && (string.IsNullOrWhiteSpace(request.Keyword)
                            || (model.Description.Contains(request.Keyword)
                            ))
                        ;
            string key = $"{request.Key}_{request.Query}_{request.PageSize}_{request.PageIndex}";
            switch (request.Key)
            {
                default:
                    var listItemResult = await base.GetListAsync<ReadViewModel>(key, request, predicate);
                    listItemResult.Data.Items.ForEach(n => n.IsActived = true);
                    return JObject.FromObject(listItemResult);
            }
        }

        [HttpPost, HttpOptions]
        [Route("update-infos")]
        public async Task<RepositoryResponse<List<ReadViewModel>>> UpdateInfos([FromBody]List<ReadViewModel> models)
        {
            if (models != null)
            {                
                return await base.SaveListAsync(models, false);
            }
            else
            {
                return new RepositoryResponse<List<ReadViewModel>>();
            }
        }
        #endregion Post
    }
}
