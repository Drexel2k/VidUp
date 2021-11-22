using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    public abstract class TemplateListBase : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<Template>
    {
        [JsonProperty]
        protected List<Template> templates;

        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event PropertyChangedEventHandler PropertyChanged;

        public int TemplateCount
        {
            get => this.templates.Count;
        }

        public Template this[int index]
        {
            get { return this.templates[index]; }
        }

        public abstract void AddTemplate(Template template);

        public abstract void AddTemplates(List<Template> templates);


        public int FindIndex(Predicate<Template> predicate)
        {
            return this.templates.FindIndex(predicate);
        }

        public List<Template> FindAll(Predicate<Template> match)
        {
            return this.templates.FindAll(match);
        }

        public abstract void Delete(Template template);

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

        protected void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected void raiseNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
