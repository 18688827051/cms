﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using SiteServer.API.Common;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Create;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.DataCache.Content;
using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Attributes;
using SiteServer.Utils;

namespace SiteServer.API.Controllers.Pages.Cms
{
    [RoutePrefix("pages/cms/contentsLayerCheck")]
    public class PagesContentsLayerCheckController : ControllerBase
    {
        private const string Route = "";

        [HttpGet, Route(Route)]
        public IHttpActionResult GetConfig()
        {
            try
            {
                var request = GetRequest();

                var siteId = request.GetQueryInt("siteId");
                var channelId = request.GetQueryInt("channelId");
                var contentIdList = TranslateUtils.StringCollectionToIntList(request.GetQueryString("contentIds"));

                if (!request.IsAdminLoggin ||
                    !request.AdminPermissionsImpl.HasChannelPermissions(siteId, channelId,
                        ConfigManager.ChannelPermissions.ContentCheck))
                {
                    return Unauthorized();
                }

                var siteInfo = SiteManager.GetSiteInfo(siteId);
                if (siteInfo == null) return BadRequest("无法确定内容对应的站点");

                var channelInfo = ChannelManager.GetChannelInfo(siteId, channelId);
                if (channelInfo == null) return BadRequest("无法确定内容对应的栏目");

                var retval = new List<IDictionary<string, object>>();
                foreach (var contentId in contentIdList)
                {
                    var contentInfo = ContentManager.GetContentInfo(siteInfo, channelInfo, contentId);
                    if (contentInfo == null) continue;

                    var dict = contentInfo.ToDictionary();
                    dict["title"] = WebUtils.GetContentTitle(siteInfo, contentInfo, string.Empty);
                    dict["checkState"] =
                        CheckManager.GetCheckState(siteInfo, contentInfo);
                    retval.Add(dict);
                }

                var isChecked = CheckManager.GetUserCheckLevel(request.AdminPermissionsImpl, siteInfo, siteId, out var checkedLevel);
                var checkedLevels = CheckManager.GetCheckedLevels(siteInfo, isChecked, checkedLevel, true);

                var allChannels =
                    ChannelManager.GetChannels(siteId, request.AdminPermissionsImpl, ConfigManager.ChannelPermissions.ContentAdd);

                return Ok(new
                {
                    Value = retval,
                    CheckedLevels = checkedLevels,
                    CheckedLevel = checkedLevel,
                    AllChannels = allChannels
                });
            }
            catch (Exception ex)
            {
                LogUtils.AddErrorLog(ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route(Route)]
        public IHttpActionResult Submit()
        {
            try
            {
                var request = GetRequest();

                var siteId = request.GetPostInt("siteId");
                var channelId = request.GetPostInt("channelId");
                var contentIdList = TranslateUtils.StringCollectionToIntList(request.GetPostString("contentIds"));
                var checkedLevel = request.GetPostInt("checkedLevel");
                var isTranslate = request.GetPostBool("isTranslate");
                var translateChannelId = request.GetPostInt("translateChannelId");
                var reasons = request.GetPostString("reasons");

                if (!request.IsAdminLoggin ||
                    !request.AdminPermissionsImpl.HasChannelPermissions(siteId, channelId,
                        ConfigManager.ChannelPermissions.ContentCheck))
                {
                    return Unauthorized();
                }

                var siteInfo = SiteManager.GetSiteInfo(siteId);
                if (siteInfo == null) return BadRequest("无法确定内容对应的站点");

                var channelInfo = ChannelManager.GetChannelInfo(siteId, channelId);
                if (channelInfo == null) return BadRequest("无法确定内容对应的栏目");

                var isChecked = checkedLevel >= siteInfo.CheckContentLevel;
                if (isChecked)
                {
                    checkedLevel = 0;
                }
                var tableName = ChannelManager.GetTableName(siteInfo, channelInfo);

                var contentInfoList = new List<ContentInfo>();
                foreach (var contentId in contentIdList)
                {
                    var contentInfo = ContentManager.GetContentInfo(siteInfo, channelInfo, contentId);
                    if (contentInfo == null) continue;

                    contentInfo.Set(ContentAttribute.CheckUserName, request.AdminName);
                    contentInfo.Set(ContentAttribute.CheckDate, DateTime.Now);
                    contentInfo.Set(ContentAttribute.CheckReasons, reasons);

                    contentInfo.Checked = isChecked;
                    contentInfo.CheckedLevel = checkedLevel;

                    if (isTranslate && translateChannelId > 0)
                    {
                        var translateChannelInfo = ChannelManager.GetChannelInfo(siteId, translateChannelId);
                        contentInfo.ChannelId = translateChannelInfo.Id;
                        translateChannelInfo.ContentDao.Update(siteInfo, translateChannelInfo, contentInfo);
                    }
                    else
                    {
                        channelInfo.ContentDao.Update(siteInfo, channelInfo, contentInfo);
                    }

                    contentInfoList.Add(contentInfo);

                    var checkInfo = new ContentCheckInfo
                    {
                        TableName = tableName,
                        SiteId = siteId,
                        ChannelId = contentInfo.ChannelId,
                        ContentId = contentInfo.Id,
                        UserName = request.AdminName,
                        Checked = isChecked,
                        CheckedLevel = checkedLevel,
                        CheckDate = DateTime.Now,
                        Reasons = reasons
                    };
                    DataProvider.ContentCheckDao.Insert(checkInfo);
                }

                if (isTranslate && translateChannelId > 0)
                {
                    ContentManager.RemoveCache(tableName, channelId);
                    var translateTableName = ChannelManager.GetTableName(siteInfo, translateChannelId);
                    ContentManager.RemoveCache(translateTableName, translateChannelId);
                }

                request.AddSiteLog(siteId, "批量审核内容");

                foreach (var contentInfo in contentInfoList)
                {
                    CreateManager.CreateContent(siteId, contentInfo.ChannelId, contentInfo.Id);
                }
                CreateManager.TriggerContentChangedEvent(siteId, channelId);
                if (isTranslate && translateChannelId > 0)
                {
                    CreateManager.TriggerContentChangedEvent(siteId, translateChannelId);
                }

                return Ok(new
                {
                    Value = contentIdList
                });
            }
            catch (Exception ex)
            {
                LogUtils.AddErrorLog(ex);
                return InternalServerError(ex);
            }
        }
    }
}
