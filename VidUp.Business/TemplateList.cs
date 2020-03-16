using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TemplateList : IEnumerable
    {
        [JsonProperty]
        private List<Template> templates;

        public int TemplateCount { get => this.templates.Count; }

        public TemplateList(List<Template> templates)
        {
            if(templates == null)
            {
                templates = new List<Template>();
            }

            this.templates = templates;
        }

        public TemplateList()
        {
            this.templates = new List<Template>();
        }

        #region IEnumerable
        public List<Template>.Enumerator GetEnumerator()
        {
            return this.templates.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }

        public Template GetTemplateForUpload(Upload upload)
        {
            foreach (Template template in this.templates)
            {
                if (template.RootFolderPath != null)
                {
                    DirectoryInfo templateRootDirectory = new DirectoryInfo(template.RootFolderPath);
                    DirectoryInfo uploadDirectory = new DirectoryInfo(upload.FilePath);

                    while (uploadDirectory.Parent != null)
                    {
                        if (uploadDirectory.Parent.FullName == templateRootDirectory.FullName)
                        {
                            return template;
                        }
                        else uploadDirectory = uploadDirectory.Parent;
                    }
                }
            }

            return null;
        }

        public Template GetDefaultTemplate()
        {
            return this.templates.Find(template => template.IsDefault);
        }
        #endregion IEnumerable

        public Template this[int index]
        {
            get
            {
                return this.templates[index];
            }
        }

        public void AddTemplates(List<Template> templates)
        {
            this.templates.AddRange(templates);
        }

        public int FindIndex(Predicate<Template> predicate)
        {
            return this.templates.FindIndex(predicate);
        }

        public void Remove(Template template)
        {
            this.templates.Remove(template);
        }

        public Template GetTemplate(int index)
        {
            return this.templates[index];
        }

        public Template GetTemplate(Guid guid)
        {
            return this.templates.Find(template => template.Guid == guid);
        }

        public void AddUpload(Upload upload)
        {
            Template template = this.templates.Find(template => template.Guid == upload.Template.Guid);
            template.AddUpload(upload);
        }

        public ReadOnlyCollection<Template> GetReadonlyTemplateList()
        {
            return this.templates.AsReadOnly();
        }

        public Template Find(Predicate<Template> match)
        {
            return this.templates.Find(match);
        }
    }
}
