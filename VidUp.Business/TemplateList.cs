#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TemplateList : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<Template>
    {
        [JsonProperty]
        private List<Template> templates;
        private string templatesImagesStorageFolder;
        private string thumbnailFallbackStorageFolder;
        private string thumbnailFallbackImageFolder;
        private CheckFileUsage checkFileUsage;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int TemplateCount { get => this.templates.Count; }

        public CheckFileUsage CheckFileUsage
        {
            set
            {
                this.checkFileUsage = value;
            }
        }

        public TemplateList(List<Template> templates, string templatesImagesStorageFolder, string thumbnailFallbackStorageFolder) : this(templatesImagesStorageFolder, thumbnailFallbackStorageFolder)
        {
            if(templates != null)
            {
                this.templates = templates;

                foreach(Template template in templates)
                {
                    template.PropertyChanged += templatePropertyChanged;
                    template.TemplateList = this;
                }
            }
        }

        public TemplateList(string templatesImagesStorageFolder, string thumbnailFallbackFilePath)
        {
            this.templatesImagesStorageFolder = templatesImagesStorageFolder;
            this.thumbnailFallbackStorageFolder = thumbnailFallbackFilePath;
            this.templates = new List<Template>();
        }

        private void templatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            if (e.PropertyName == "IsDefault")
            {
                Template template = (Template)sender;
                if (sender != null)
                {
                    if (template.IsDefault == true)
                    {
                        foreach (Template templateInternal in this.templates)
                        {
                            if (templateInternal != template && templateInternal.IsDefault)
                            {
                                templateInternal.IsDefault = false;
                            }
                        }
                    }
                }
            }

            if (e.PropertyName == "ThumbnailFallbackFilePath")
            {
                PropertyChangedEventArgsEx args = (PropertyChangedEventArgsEx)e;

                string oldValue = (string)args.OldValue;
                this.deleteThumbnailFallbackIfPossible(oldValue);
            }

            if (e.PropertyName == "ImageFilePathForEditing")
            {
                PropertyChangedEventArgsEx args = (PropertyChangedEventArgsEx)e;

                string oldValue = (string)args.OldValue;
                this.deleteImageIfPossible(oldValue);
            }
        }

        private void deleteThumbnailFallbackIfPossible(string thumbnailFallbackFilePath)
        {
            string thumbnailFileFolder = Path.GetDirectoryName(thumbnailFallbackFilePath);
            if (!string.IsNullOrWhiteSpace(thumbnailFileFolder))
            {
                if (String.Compare(Path.GetFullPath(this.thumbnailFallbackImageFolder).TrimEnd('\\'), thumbnailFileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    return;
                }

                bool found = false;
                foreach (Template template in this.templates)
                {
                    if (template.ThumbnailFallbackFilePath != null)
                    {
                        if (String.Compare(Path.GetFullPath(thumbnailFallbackFilePath), Path.GetFullPath(template.ThumbnailFallbackFilePath), StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    if (this.checkFileUsage != null)
                    {
                        found = this.checkFileUsage(thumbnailFallbackFilePath);
                    }
                }

                if (!found)
                {
                    if (File.Exists(thumbnailFallbackFilePath))
                    {
                        File.Delete(thumbnailFallbackFilePath);
                    }
                }
            }
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
                string newFilePath = this.CopyTemplateImageToStorageFolder(template.ImageFilePathForEditing);
                template.ImageFilePathForEditing = newFilePath;
                template.PropertyChanged += this.templatePropertyChanged;
            }

            this.templates.AddRange(templates);

            this.raiseNotifyPropertyChanged("TemplateCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, templates));
        }

        public string CopyThumbnailFallbackToStorageFolder(string thumbnailFallbackFilePath)
        {
            return this.CopyImageToStorageFolder(thumbnailFallbackFilePath, this.thumbnailFallbackStorageFolder);
        }

        public string CopyTemplateImageToStorageFolder(string templateImageFilePath)
        {
            return this.CopyImageToStorageFolder(templateImageFilePath, this.templatesImagesStorageFolder);
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

        public bool TemplateContainsFallbackThumbnail(string filePath)
        {
            if (filePath != null)
            {
                foreach (Template template in this.templates)
                {
                    if (template.ThumbnailFallbackFilePath != null)
                    {
                        if (String.Compare(Path.GetFullPath(filePath), Path.GetFullPath(template.ThumbnailFallbackFilePath), StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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
            template.PropertyChanged -= this.templatePropertyChanged;

            this.deleteThumbnailFallbackIfPossible(template.ThumbnailFallbackFilePath);
            this.deleteImageIfPossible(template.ImageFilePathForEditing);

            this.raiseNotifyPropertyChanged("TemplateCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, template));
        }

        /// <summary>
        /// Checks:
        /// 1. is image not in template image storage folder -> do nothing
        /// 2. is image referenced in any other template -> do nothing
        /// </summary>
        /// <param name="imageFilePath"></param>
        private void deleteImageIfPossible(string imageFilePath)
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

        public IEnumerator<Template> GetEnumerator()
        {
            return this.templates.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void raiseNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
