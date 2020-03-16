using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UploadList : IEnumerable
    {
        [JsonProperty]
        private List<Upload> uploads;

        public UploadList()
        {
            this.uploads = new List<Upload>();
        }

        #region IEnumerable
        public List<Upload>.Enumerator GetEnumerator()
        {
            return this.uploads.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }
        #endregion IEnumerable

        public void Remove(Upload upload)
        {
            this.uploads.Remove(upload);
        }

        public void AddUploads(List<Upload> uploads, TemplateList templateList)
        {
            foreach(Upload upload in uploads)
            {
                Template template = templateList.GetTemplateForUpload(upload);
                if (template != null)
                {
                    upload.Template = template;
                }
                else
                {
                    template = templateList.GetDefaultTemplate();
                    if (template != null)
                    {
                        upload.Template = template;
                    }
                }
            }

            this.uploads.AddRange(uploads);
        }

        public Upload GetUpload(int index)
        {
            return this.uploads[index];
        }

        public Upload GetUpload(Predicate<Upload> match)
        {
            return this.uploads.Find(match);
        }

        public List<Upload> GetUploads(Predicate<Upload> match)
        {
            return this.uploads.FindAll(match);
        }

        public int FindIndex(Predicate<Upload> predicate)
        {
            return this.uploads.FindIndex(predicate);
        }

        public ReadOnlyCollection<Upload> GetReadyOnlyUploadList()
        {
            return this.uploads.AsReadOnly();
        }
    }
}
