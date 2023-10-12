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
        private static long p_UserId;
        private static Dictionary<string, long> p_GroupsIdsByName = new Dictionary<string, long>();
        private static Dictionary<long, Dictionary<string, long>> p_AlbumsIdsByNameByGroupId = new Dictionary<long, Dictionary<string, long>>();
        private static VKClient? p_VKClient;

        private static ILogger p_Logger = LogManager.GetCurrentClassLogger();


        public static void Initialize(string userName, string accessToken, List<string> groupsNames)
        {
            p_VKClient = new VKClient(accessToken);
            
            p_UserId = p_VKClient.GetUserId(userName);

            p_GroupsIdsByName = p_VKClient.GetGroups(p_UserId)
                .Where(x => groupsNames.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public static bool ObtainAlbums(string groupName, List<string> albumsNames, out string reason)
        {
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }

            if(!p_GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found.";
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

                if(groupAlbumsIdsByName.Count() != albumsNames.Count())
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

            if(!p_GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found locally. Try refresh groups.";
                return false;
            }

            return p_VKClient.PostPublication(groupId, message, publicationDateTimeUtc, copyright, imagesPaths, out reason);
        }

        public static bool UploadPhotosToAlbum(string groupName, string albumName, List<string> filePaths, out string reason)
        {//todo можно добавить подписи к картинкам в альбоме
            if(p_VKClient == null)
            {
                reason = $"Not initialized.";
                return false;
            }
            
            if(!p_GroupsIdsByName.TryGetValue(groupName, out var groupId))
            {
                reason = $"Group '{groupName}' not found.";
                return false;
            }

            if(!p_AlbumsIdsByNameByGroupId.TryGetValue(groupId, out var groupAlbumsIdsByName) || !groupAlbumsIdsByName.TryGetValue(albumName, out var albumId))
            {
                reason = $"Album '{albumName}' not found in group '{groupName}'.";
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