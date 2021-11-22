using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TemplateListSimple : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<Template>
    {
        [JsonProperty]
        private List<Template> templates;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int TemplateCount { get => this.templates.Count; }

        public TemplateListSimple(List<Template> templates)
        {
            this.templates = new List<Template>();

            if (templates != null)
            {
                this.templates = templates;
            }
        }

        public Template GetTemplateForUpload(Upload upload)
        {
            foreach (Template template in this.templates)
            {
                if (template.TemplateMode == TemplateMode.FolderBased)
                {
                    if (!string.IsNullOrWhiteSpace(template.RootFolderPath))
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
                else
                {
                    if (!string.IsNullOrWhiteSpace(template.PartOfFileName))
                    {
                        if (Path.GetFileName(upload.FilePath).ToLower().Contains(template.PartOfFileName.ToLower()))
                        {
                            return template;
                        }
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

        public void AddTemplate(Template template)
        {
            this.templates.Add(template);

            this.raiseNotifyPropertyChanged("TemplateCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, template));
        }

        public void AddTemplates(List<Template> templates)
        {
            this.templates.AddRange(templates);

            this.raiseNotifyPropertyChanged("TemplateCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, templates));
        }

        public int FindIndex(Predicate<Template> predicate)
        {
            return this.templates.FindIndex(predicate);
        }

        public List<Template> FindAll(Predicate<Template> match)
        {
            return this.templates.FindAll(match);
        }

        public void Remove(Template template)
        {
            this.templates.Remove(template);
            this.raiseNotifyPropertyChanged("TemplateCount");
            //template is removed from uploads in event listener in MainWindowViewModel.templateListCollectionChanged
            //todo: Move to event aggregator

            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, template));
        }

        public Template GetTemplate(int index)
        {
            return this.templates[index];
        }

        public Template GetTemplate(Guid guid)
        {
            return this.templates.Find(template => template.Guid == guid);
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
