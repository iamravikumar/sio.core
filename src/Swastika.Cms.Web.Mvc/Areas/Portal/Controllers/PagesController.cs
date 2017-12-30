﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Swastika.Cms.Mvc.Controllers;
using Microsoft.Data.OData.Query;
using Swastika.Cms.Lib.ViewModels;
using Swastika.Cms.Lib.Models;
using Swastika.Cms.Lib;
using Swastika.Cms.Lib.ViewModels.Info;

namespace Swastika.Cms.Mvc.Areas.Portal.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Area("Portal")]
    [Route("{culture}/Portal/Pages")]
    public class PagesController : BaseController<PagesController>
    {
        public PagesController(IHostingEnvironment env
            //, IStringLocalizer<PortalController> pageLocalizer, IStringLocalizer<SharedResource> localizer
            )
            : base(env)
        {
        }

        //[Route("/portal/pages")]
        [Route("{pageSize:int?}/{pageIndex:int?}")]
        [Route("Index/{pageSize:int?}/{pageIndex:int?}")]
        public async Task<IActionResult> Index(string keyword, int pageSize = 10, int pageIndex = 0)
        {
            var pagingPages = await CategoryListItemViewModel.Repository.GetModelListByAsync(
                cate => cate.Specificulture == _lang && 
                    (string.IsNullOrEmpty(keyword) || cate.Title.Contains(keyword))
                , "Priority", OrderByDirection.Ascending
                , pageSize, pageIndex);
            
            return View(pagingPages.Data);
        }

        [Route("Create")]
        public IActionResult Create()
        {
            //ViewData["Specificulture"] = new SelectList(_context.TtsCulture, "Specificulture", "Specificulture");
            var ttsMenu = new CategoryBEViewModel(new SiocCategory()
            {
                Specificulture = _lang,
                CreatedBy = User.Identity.Name,
                //CreatedDate = DateTime.UtcNow
            });
            return View(ttsMenu);
        }

        // POST: TtsMenu/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryBEViewModel ttsMenu)
        {
            if (ModelState.IsValid)
            {
                var result = await ttsMenu.SaveModelAsync();
                if (result.IsSucceed)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(ttsMenu);
                }
            }
            return View(ttsMenu);
        }

        // GET: TtsMenu/Edit/5
        [Route("Edit/{id}")]
        [Route("Edit/{id}/{pageName}")]
        public async Task<IActionResult> Edit(int? id, string pageName)
        {
            if (id == null)
            {
                return NotFound();
            }

            var getCategory = await CategoryBEViewModel.Repository.GetSingleModelAsync(
                m => m.Id == id && m.Specificulture == _lang
                );
            if (!getCategory.IsSucceed)
            {
                return NotFound();
            }
            return View(getCategory.Data);
        }

        // POST: TtsMenu/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Edit/{id}")]
        [Route("Edit/{id}/{pageName}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryBEViewModel ttsMenu)
        {
            if (id != ttsMenu.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await ttsMenu.SaveModelAsync();
                    if (result.IsSucceed)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, result.Exception.Message);
                        return View(ttsMenu);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryBEViewModel.Repository.CheckIsExists(
                        m => m.Id == ttsMenu.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                //return RedirectToAction("Index");
            }
            ViewData["Action"] = "Edit";
            ViewData["Controller"] = "Pages";
            return View(ttsMenu);
        }

        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            var ttsMenu = await CategoryBEViewModel.Repository.RemoveModelAsync(
                m => m.Id == id && m.Specificulture == _lang);          
            return RedirectToAction("Index");
        }

        [Route("Contents/{id}")]
        [Route("Contents/{id}/{pageSize}/{pageIndex}/{orderBy}/{pageName}")]
        [Route("Contents/{id}/{pageName}")]
        public async Task<IActionResult> Contents(int id, string pageName
            , int? pageSize, int? pageIndex, string orderBy = SWCmsConstants.Default.OrderBy)
        {
            pageSize = pageSize ?? SWCmsConstants.Default.PageSizeArticle;
            pageIndex = pageIndex ?? 0;
            var articles = await InfoArticleViewModel.GetModelListByCategoryAsync(
                id, _lang, orderBy, OrderByDirection.Ascending,
                pageSize, pageIndex);
                
            if (!articles.IsSucceed)
            {
                return NotFound();
            }
            ViewBag.categoryId = id;
            return View(articles.Data);
        }
    }
}