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
                string newFilePath = this.CopyPictureToStorageFolder(template.PictureFilePathForEditing, this.templatesImagesStorageFolder);
                template.PictureFilePathForEditing = newFilePath;
            }

            this.templates.AddRange(templates);
        }

        public string CopyPictureToStorageFolder(string pictureFilePath, string storageFolder)
        {
            string fileFolder = Path.GetDirectoryName(pictureFilePath);
            if (String.Compare(Path.GetFullPath(storageFolder).TrimEnd('\\'), fileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return pictureFilePath;
            }

            string fileName = Path.GetFileName(pictureFilePath);
            string targetFilePath = Path.Combine(storageFolder, fileName);
            if(File.Exists(targetFilePath))
            {
                targetFilePath = this.addFileNameAppendix(fileName, 2);
            }

            File.Copy(pictureFilePath, targetFilePath);

            return targetFilePath;
        }

        private string addFileNameAppendix(string fileName, int appendix)
        {
            string newfileName = string.Format("{0}{1}{2}", Path.GetFileNameWithoutExtension(fileName), appendix, Path.GetExtension(fileName));
            string targetFilePath = Path.Combine(this.templatesImagesStorageFolder, newfileName);
            if (File.Exists(targetFilePath))
            {
                targetFilePath = this.addFileNameAppendix(fileName, ++appendix);
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
            this.deletePictureIfPossible(template.PictureFilePathForEditing);
        }

        private void deletePictureIfPossible(string pictureFilePath)
        {
            if (pictureFilePath != null)
            {
                string pictureFileFolder = Path.GetDirectoryName(pictureFilePath);
                if (String.Compare(Path.GetFullPath(this.templatesImagesStorageFolder).TrimEnd('\\'), pictureFileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    return;
                }

                bool found = false;
                foreach (Template template in this.templates)
                {
                    if (template.PictureFilePathForEditing != null)
                    {
                        if (String.Compare(Path.GetFullPath(pictureFilePath), Path.GetFullPath(template.PictureFilePathForEditing), StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    if (File.Exists(pictureFilePath))
                    {
                        File.Delete(pictureFilePath);
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
