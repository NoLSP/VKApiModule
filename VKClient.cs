﻿using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Model;
using VkNet;
using VkNet.Enums.Filters;
using System.Net;
using NLog;

namespace VKApiModule
{
    public class VKClient
    {
        private readonly VkApi p_vkApi;
        private ILogger p_Logger;


        public VKClient(string accessToken)
        {
            p_Logger = LogManager.GetCurrentClassLogger();

            if (String.IsNullOrWhiteSpace(accessToken))
                throw new Exception($"VK Api - {nameof(accessToken)} was null.");

            p_vkApi = new VkApi();

            p_vkApi.Authorize(new ApiAuthParams
            {
                AccessToken = accessToken
            });
        }

        public long GetUserId(string userName)
        {
            var user = p_vkApi.Users.Get(new List<string> { userName }, ProfileFields.All).FirstOrDefault();
            if (user == null)
                throw new Exception($"VK Api - user ({userName}) not found.");

            return user.Id;
        }

        public Dictionary<string, long> GetGroups(long userId)
        {
            return p_vkApi.Groups.Get(new GroupsGetParams() { UserId = userId, Extended = true, Filter = GroupsFilters.Moderator, Fields = GroupsFields.All })
                .ToDictionary(x => x.ScreenName, x => x.Id);
        }

        public Dictionary<string, long> GetAlbums(long groupId)
        {
            var albums = p_vkApi.Photo.GetAlbums(new PhotoGetAlbumsParams
            {
                OwnerId = (-1) * groupId
            });

            return albums.ToDictionary(x => x.Title, x => x.Id);
        }

        public bool PostPublication(long groupId, string message, DateTime publicationDateTimeUtc, string copyright, 
            List<string> imagesPaths, out string reason)
        {
            try
            {
                var photos = UploadPhotosToWall(groupId, imagesPaths);
                if (photos == null)
                {
                    reason = "Upload 0 images.";
                    return false;
                }

                var postId = p_vkApi.Wall.Post(new WallPostParams()
                {
                    OwnerId = (-1) * (long)groupId,
                    FromGroup = true,
                    Message = message,
                    PublishDate = publicationDateTimeUtc,
                    Copyright = copyright,
                    Attachments = photos
                });

                reason = "";
                return true;
            }
            catch (Exception ex)
            {
                p_Logger.Error(ex);
                reason = ex.Message;
                return false;
            }
        }

        public void UploadPhotosToAlbum(long groupId, long albumId, List<string> filePaths)
        {
            var uploadServer = p_vkApi.Photo.GetUploadServer(albumId, groupId);
            var uploadUrl = uploadServer.UploadUrl;

            foreach (var filePath in filePaths)
            {
                var responseString = UploadFile(uploadUrl, filePath);

                var photos = p_vkApi.Photo.Save(new PhotoSaveParams
                {
                    SaveFileResponse = responseString,
                    AlbumId = albumId,
                    GroupId = (long)groupId
                });
            }
        }


        private List<Photo>? UploadPhotosToWall(long groupId, List<string> imagesPaths)
        {
            var result = new List<Photo>();
            var uploadServer = p_vkApi.Photo.GetWallUploadServer(groupId);
            var uploadUrl = uploadServer.UploadUrl;

            foreach (var filePath in imagesPaths)
            {
                var responseString = UploadFile(uploadUrl, filePath);
                var photo = p_vkApi.Photo.SaveWallPhoto(responseString, null, (ulong)groupId).FirstOrDefault();

                if (photo == null)
                {
                    p_Logger.Error($"Can't upload photo - {filePath}");
                    return null;
                }

                result.Add(photo);
            }

            return result;
        }

        private string UploadFile(string uploadUrl, string filePath)
        {
            WebClient webClient = new WebClient();
            byte[] responseArray = webClient.UploadFile(uploadUrl, filePath);
            return System.Text.Encoding.ASCII.GetString(responseArray);
        }

    }
}