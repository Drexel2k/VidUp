using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drexel.VidUp.Business
{
    public class ReSerialize
    {
        private bool allUploads;
        private bool uploadList;
        private bool templateList;
        private bool playlistList;
        private bool youtubeAccountList;
        public bool AllUploads 
        {
            get => this.allUploads;
            set
            {
                if (value)
                {
                    this.allUploads = value;
                }
            }
        }

        public bool UploadList
        {
            get => this.uploadList;
            set
            {
                if (value)
                {
                    this.uploadList = value;
                }
            }
        }

        public bool TemplateList
        {
            get => this.templateList;
            set
            {
                if (value)
                {
                    this.templateList = value;
                }
            }
        }

        public bool PlaylistList
        {
            get => this.playlistList;
            set
            {
                if (value)
                {
                    this.playlistList = value;
                }
            }
        }

        public bool YoutubeAccountList
        {
            get => this.youtubeAccountList;
            set
            {
                if (value)
                {
                    this.youtubeAccountList = value;
                }
            }
        }

    }
}
