using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Model;
using VkNet;
using VkNet.Enums.Filters;
using System.Net;
using NLog;

namespace VKApiModule
{
    public static class VKManager
    {
        public static long UserId { get; private set; }
        public static Dictionary<string, long> GroupsIdsByName { get; private set; } = new Dictionary<string, long>();
        private static Dictionary<long, Dictionary<string, long>> p_AlbumsIdsByNameByGroupId = new Dictionary<long, Dictionary<string, long>>();
        public static Dictionary<long, List<(long Id, DateTime DateTime)>> ScheduledPostsByGroupId { get; private set; } = new Dictionary<long, List<(long, DateTime)>>();
        private static VKClient? p_VKClient;

        private static ILogger p_Logger = LogManager.GetCurrentClassLogger();


        public static bool Initialize(string userName, string accessToken, List<string> groupsNames, out string reason)
        {
            try
            {
                p_VKClient = new VKClient(accessToken);
                
                UserId = p_VKClient.GetUserId(userName);

                GroupsIdsByName = p_VKClient.GetGroups(UserId)
                    .Where(x => groupsNames.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);

                foreach(var groupId in GroupsIdsByName.Values)
                {
                    ScheduledPostsByGroupId.Add(groupId, p_VKClient.GetScheduledPosts(groupId));
                }

                reason = "";
                return true;
            }
            catch(Exception e)
            {
                p_VKClient = null;
                p_Logger.Error(e);
                reason = e.Message;
                return false;
            }
        }

        public static bool ObtainAlbums(string groupName, List<string> albumsNames, out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            if(!GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found locally. Try refresh groups.";
                return false;
            }

            try
            {
                var albums = p_VKClient.GetAlbums(groupId);

                if(!p_AlbumsIdsByNameByGroupId.ContainsKey(groupId))
                    p_AlbumsIdsByNameByGroupId.Add(groupId, new Dictionary<string, long>());

                var groupAlbumsIdsByName = p_AlbumsIdsByNameByGroupId[groupId];

                foreach (var albumTitle in albums.Keys)
                {
                    if(albumsNames.Contains(albumTitle))
                    {
                        if(!groupAlbumsIdsByName.ContainsKey(albumTitle))
                            groupAlbumsIdsByName.Add(albumTitle, albums[albumTitle]);
                    }
                }

                if(groupAlbumsIdsByName.Count() != albumsNames.Count() || albumsNames.Any(x => !groupAlbumsIdsByName.Keys.Contains(x)))
                {
                    var notFoundAlbumsNames = albumsNames.Where(x => !groupAlbumsIdsByName.Keys.Contains(x));
                    reason = $"Not found albums: {String.Join(", ", notFoundAlbumsNames)}";
                    return false;
                }

                reason = "";
                return true;
            }
            catch(Exception e)
            {
                p_Logger.Error(e);
                reason = e.Message;
                return false;
            }
        }

        public static bool UpdateScheduledPosts(out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            foreach(var groupId in GroupsIdsByName.Values)
            {
                if(!UpdateScheduledPosts(groupId, out reason))
                    return false;
            }

            reason = "";
            return true;
        }

        public static bool UpdateScheduledPosts(IEnumerable<string> groupsNames, out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            foreach(var groupName in groupsNames)
            {
                if(!UpdateScheduledPosts(groupName, out reason))
                    return false;
            }

            reason = "";
            return true;
        }


        public static bool UpdateScheduledPosts(string groupName, out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            if(!GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found locally. Try refresh groups.";
                return false;
            }

            if(!UpdateScheduledPosts(groupId, out reason))
                return false;

            reason = "";
            return true;
        }

        private static bool UpdateScheduledPosts(long groupId, out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            if(!ScheduledPostsByGroupId.TryGetValue(groupId, out var scheduledPosts))
            {
                scheduledPosts = new List<(long, DateTime)>();
                ScheduledPostsByGroupId.Add(groupId, scheduledPosts);
            }

            try
            {
                scheduledPosts = p_VKClient.GetScheduledPosts(groupId);

                reason = "";
                return true;
            }
            catch(Exception e)
            {
                p_Logger.Error(e);
                reason = e.Message;
                return false;
            }
        }

        public static bool PostPublication(string groupName, string message, DateTime publicationDateTimeUtc, string copyright, 
            List<string> imagesPaths, out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            if(!GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found locally. Try refresh groups.";
                return false;
            }

            try
            {
                p_VKClient.PostPublication(groupId, message, publicationDateTimeUtc, copyright, imagesPaths);

                reason = "";
                return true;
            }
            catch(Exception e)
            {
                p_Logger.Error(e);
                reason = e.Message;
                return false;
            }
        }

        public static bool DeleteLastScheduledPublication(string groupName, out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            if(!GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found locally. Try refresh groups.";
                return false;
            }

            if(!ScheduledPostsByGroupId.TryGetValue(groupId, out var scheduledPosts) || scheduledPosts.Count() == 0)
            {
                reason = "";
                return true;
            }

            var lastPostId = scheduledPosts.OrderByDescending(x => x.DateTime).First().Id;

            try
            {
                if(!p_VKClient.DeletePublication(groupId, lastPostId))
                {
                    reason = $"VK Api returned false.";
                    return false;
                }

                reason = "";
                return true;
            }
            catch(Exception e)
            {
                p_Logger.Error(e);
                reason = e.Message;
                return false;
            }
        }

        public static bool UploadPhotosToAlbum(string groupName, string albumName, List<string> filePaths, out string reason)
        {//todo можно добавить подписи к картинкам в альбоме
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }
            
            if(!GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found locally. Try refresh groups.";
                return false;
            }

            if(!p_AlbumsIdsByNameByGroupId.TryGetValue(groupId, out var groupAlbumsIdsByName) || !groupAlbumsIdsByName.TryGetValue(albumName, out var albumId))
            {
                reason = $"Album '{albumName}' not found locally in group '{groupName}'. Try refresh albums.";
                return false;
            }

            try
            {
                p_VKClient.UploadPhotosToAlbum(groupId, albumId, filePaths);

                reason = "";
                return true;
            }
            catch (Exception e)
            {
                reason = e.Message;
                return false;
            }
        }
    }
}