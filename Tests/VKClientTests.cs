using System.Text.Json;
using VKApiModule;

namespace Tests
{
    public class VKClientTests
    {
        private dynamic p_Settings;

        public VKClientTests() 
        {
            var defenition = new
            {
                AccessToken = "",
                UserName = "Test",
                Groups = new Dictionary<string, List<string>>(),
                UserId = (long)1,
                GroupsIds = new long[] { 1 }
            };

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var fileContent = File.ReadAllText(filePath);

            p_Settings = JsonSerializer.Deserialize(fileContent, defenition.GetType())!;
        }

        [Fact]
        public void InitializeTest()
        {
            var groups = (p_Settings.Groups as Dictionary<string, List<string>>)!;
            Assert.True(VKManager.Initialize(p_Settings.UserName, p_Settings.AccessToken, groups.Keys.ToList(), out string reason));
            Assert.True(String.IsNullOrWhiteSpace(reason));
            Assert.Equal(p_Settings.UserId, VKManager.UserId);
            Assert.Equal(p_Settings.GroupsIds[0], VKManager.GroupsIdsByName[groups.Keys.First()]);
        }

        [Fact]
        public void ObtainAlbumsTest()
        {
            //var resultStatus = VKManager.ObtainAlbums("", new List<string> { "blabla" }, out var resultReason);
            //Assert.False(resultStatus);
            //Assert.Equal($"Not initialized.", resultReason);

            var groups = (p_Settings.Groups as Dictionary<string, List<string>>)!;
            Assert.True(VKManager.Initialize(p_Settings.UserName, p_Settings.AccessToken, groups.Keys.ToList(), out string reason));
            Assert.True(String.IsNullOrWhiteSpace(reason));

            var resultStatus = VKManager.ObtainAlbums(groups.Keys.First(), groups.Values.First(), out var resultReason);
            Assert.True(resultStatus);

            resultStatus = VKManager.ObtainAlbums(groups.Keys.First(), new List<string> { "blabla" }, out resultReason);
            Assert.False(resultStatus);
            Assert.Equal($"Not found albums: blabla", resultReason);

            resultStatus = VKManager.ObtainAlbums("blabla", new List<string> { "blabla" }, out resultReason);
            Assert.False(resultStatus);
            Assert.Equal($"Group 'blabla' not found.", resultReason);
        }

        [Fact]
        public void PostPublicationTest()
        {
            //var resultStatus = VKManager.PostPublication("","", DateTime.UtcNow, "", new List<string> { "" }, out var resultReason);
            //Assert.False(resultStatus);
            //Assert.Equal($"Not initialized.", resultReason);

            var groups = (p_Settings.Groups as Dictionary<string, List<string>>)!;
            Assert.True(VKManager.Initialize(p_Settings.UserName, p_Settings.AccessToken, groups.Keys.ToList(), out string reason));
            Assert.True(String.IsNullOrWhiteSpace(reason));

            var resultStatus = VKManager.PostPublication("blabla","", DateTime.UtcNow, "", new List<string> { "" }, out var resultReason);
            Assert.False(resultStatus);
            Assert.Equal($"Group 'blabla' not found locally. Try refresh groups.", resultReason);
        }

        [Fact]
        public void UploadPhotosToAlbumTest()
        {
            //var resultStatus = VKManager.UploadPhotosToAlbum("blabla", "blabla", new List<string> { "" }, out var resultReason);
            //Assert.False(resultStatus);
            //Assert.Equal($"Not initialized.", resultReason);

            var groups = (p_Settings.Groups as Dictionary<string, List<string>>)!;
            Assert.True(VKManager.Initialize(p_Settings.UserName, p_Settings.AccessToken, groups.Keys.ToList(), out string reason));
            Assert.True(String.IsNullOrWhiteSpace(reason));

            var resultStatus = VKManager.UploadPhotosToAlbum("blabla", "blabla", new List<string> { "" }, out var resultReason);
            Assert.False(resultStatus);
            Assert.Equal($"Group 'blabla' not found.", resultReason);

            resultStatus = VKManager.UploadPhotosToAlbum(groups.Keys.First(), "blabla", new List<string> { "" }, out resultReason);
            Assert.False(resultStatus);
            Assert.Equal($"Album 'blabla' not found in group '{groups.Keys.First()}'.", resultReason);
        }

        [Fact]
        public void UpdateScheduledPostsTest()
        {
            //var resultStatus = VKManager.UploadPhotosToAlbum("blabla", "blabla", new List<string> { "" }, out var resultReason);
            //Assert.False(resultStatus);
            //Assert.Equal($"Not initialized.", resultReason);

            var groups = (p_Settings.Groups as Dictionary<string, List<string>>)!;
            Assert.True(VKManager.Initialize(p_Settings.UserName, p_Settings.AccessToken, groups.Keys.ToList(), out string reason));
            Assert.True(String.IsNullOrWhiteSpace(reason));

            Assert.Equal(0, VKManager.ScheduledPostsByGroupId.Sum(x => x.Value.Count()));
        }
    }
}