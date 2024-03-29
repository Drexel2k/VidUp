﻿using Drexel.VidUp.Business;

namespace Drexel.VidUp.Json.Content
{
    public class DeserializationRepositoryContent
    {
        public static UploadList UploadList { get; set; }

        public static TemplateList TemplateList { get; set; }

        public static PlaylistList PlaylistList { get; set; }

        public static YoutubeAccountList YoutubeAccountList { get; set; }

        public static void ClearRepositories()
        {
            UploadList = null;
            TemplateList = null;
            PlaylistList = null;
            YoutubeAccountList = null;
        }
    }
}
