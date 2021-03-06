﻿using System.Linq;
using SiteServer.API.Tests;
using SiteServer.CMS.Core;
using SiteServer.CMS.Model;
using SiteServer.Utils;
using Xunit;

namespace SiteServer.API.Tests.Database.Repositories
{
    [TestCaseOrderer("SiteServer.CMS.Tests.PriorityOrderer", "SiteServer.CMS.Tests")]
    public class AccessTokenRepositoryTest: IClassFixture<EnvironmentFixture>
    {
        public EnvironmentFixture Fixture { get; }

        public AccessTokenRepositoryTest(EnvironmentFixture fixture)
        {
            Fixture = fixture;
        }

        [SkippableFact, TestPriority(0)]
        public void BasicTest()
        {
            Skip.IfNot(TestEnv.IntegrationTestMachine);

            var accessTokenInfo = new AccessTokenInfo();

            DataProvider.AccessTokenDao.Insert(accessTokenInfo);
            Assert.True(accessTokenInfo.Id > 0);
            var token = accessTokenInfo.Token;
            Assert.False(string.IsNullOrWhiteSpace(token));

            accessTokenInfo = DataProvider.AccessTokenDao.Get(accessTokenInfo.Id);
            Assert.NotNull(accessTokenInfo);

            accessTokenInfo.Title = "title";
            var updated = DataProvider.AccessTokenDao.Update(accessTokenInfo);
            Assert.True(updated);

            DataProvider.AccessTokenDao.Regenerate(accessTokenInfo);
            Assert.NotEqual(token, accessTokenInfo.Token);

            var deleted = DataProvider.AccessTokenDao.Delete(accessTokenInfo.Id);
            Assert.True(deleted);
        }

        [SkippableFact, TestPriority(0)]
        public void IsTitleExists()
        {
            Skip.IfNot(TestEnv.IntegrationTestMachine);

            const string testTitle = "IsTitleExists";

            var exists = DataProvider.AccessTokenDao.IsTitleExists(testTitle);

            Assert.False(exists);

            var accessTokenInfo = new AccessTokenInfo
            {
                Title = testTitle
            };
            DataProvider.AccessTokenDao.Insert(accessTokenInfo);

            exists = DataProvider.AccessTokenDao.IsTitleExists(testTitle);

            Assert.True(exists);

            var deleted = DataProvider.AccessTokenDao.Delete(accessTokenInfo.Id);
            Assert.True(deleted);
        }

        [SkippableFact, TestPriority(0)]
        public void GetAccessTokenInfoList()
        {
            Skip.IfNot(TestEnv.IntegrationTestMachine);

            var accessTokenInfo = new AccessTokenInfo
            {
                Title = "title"
            };
            DataProvider.AccessTokenDao.Insert(accessTokenInfo);

            var list = DataProvider.AccessTokenDao.GetAll();

            Assert.True(list.Any());

            var deleted = DataProvider.AccessTokenDao.Delete(accessTokenInfo.Id);
            Assert.True(deleted);
        }
    }
}
