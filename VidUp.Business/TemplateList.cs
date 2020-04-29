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
        private string templatesImagesStorageFolder;

        public int TemplateCount { get => this.templates.Count; }

        public TemplateList(List<Template> templates, string templatesImagesStorageFolder) : this(templatesImagesStorageFolder)
        {
            if(templates != null)
            {
                this.templates = templates;
            }
        }

        public TemplateList(string templatesImagesStorageFolder)
        {
            this.templatesImagesStorageFolder = templatesImagesStorageFolder;
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
            foreach (Template template in templates)
            {
                string newFilePath = this.CopyImageToStorageFolder(template.ImageFilePathForEditing, this.templatesImagesStorageFolder);
                template.ImageFilePathForEditing = newFilePath;
            }

            this.templates.AddRange(templates);
        }

        public string CopyImageToStorageFolder(string imageFilePath, string storageFolder)
        {
            if (!string.IsNullOrWhiteSpace(imageFilePath))
            {
                string fileFolder = Path.GetDirectoryName(imageFilePath);
                if (String.Compare(Path.GetFullPath(storageFolder).TrimEnd('\\'), fileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return imageFilePath;
                }

                string fileName = Path.GetFileName(imageFilePath);
                string targetFilePath = Path.Combine(storageFolder, fileName);
                if (File.Exists(targetFilePath))
                {
                    targetFilePath = this.addFileNameAppendix(fileName, storageFolder, 2);
                }

                File.Copy(imageFilePath, targetFilePath);

                return targetFilePath;
            }

            return null;
        }

        private string addFileNameAppendix(string fileName, string storageFolder, int appendix)
        {
            string newfileName = string.Format("{0}{1}{2}", Path.GetFileNameWithoutExtension(fileName), appendix, Path.GetExtension(fileName));
            string targetFilePath = Path.Combine(storageFolder, newfileName);
            if (File.Exists(targetFilePath))
            {
                targetFilePath = this.addFileNameAppendix(fileName, storageFolder, ++appendix);
            }

            return targetFilePath;
        }

        public int FindIndex(Predicate<Template> predicate)
        {
            return this.templates.FindIndex(predicate);
        }

        public void Remove(Template template)
        {
            this.templates.Remove(template);
            this.DeleteImageIfPossible(template.ImageFilePathForEditing);
        }

        /// <summary>
        /// Checks:
        /// 1. is image not in template image storage folder -> do nothing
        /// 2. is image referenced in any other template -> do nothing
        /// </summary>
        /// <param name="imageFilePath"></param>
        public void DeleteImageIfPossible(string imageFilePath)
        {
            if (imageFilePath != null)
            {
                string imageFileFolder = Path.GetDirectoryName(imageFilePath);
                if (String.Compare(Path.GetFullPath(this.templatesImagesStorageFolder).TrimEnd('\\'), imageFileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    return;
                }

                bool found = false;
                foreach (Template template in this.templates)
                {
                    if (template.ImageFilePathForEditing != null)
                    {
                        if (String.Compare(Path.GetFullPath(imageFilePath), Path.GetFullPath(template.ImageFilePathForEditing), StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    if (File.Exists(imageFilePath))
                    {
                        File.Delete(imageFilePath);
                    }
                }
            }
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
            Template template = this.templates.Find(templatek => templatek.Guid == upload.Template.Guid);
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
