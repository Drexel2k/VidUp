using Drexel.VidUp.Business;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservableTemplateViewModels : INotifyCollectionChanged, IEnumerable<TemplateComboboxViewModel>
    {
        private List<TemplateComboboxViewModel> templateComboboxViewModels;
        private TemplateList templateList;

        public int TemplateCount { get => templateComboboxViewModels.Count;  }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableTemplateViewModels(TemplateList templateList)
        {
            this.templateList = templateList;

            this.templateComboboxViewModels = new List<TemplateComboboxViewModel>();
            foreach (Template template in templateList)
            {
                TemplateComboboxViewModel templateViewModel = new TemplateComboboxViewModel(template);
                this.templateComboboxViewModels.Add(templateViewModel);
            }
        }

        public TemplateComboboxViewModel this[int index]
        {
            get => this.templateComboboxViewModels[index];
        }


        private void RaiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, args);
            }
        }

        public IEnumerator<TemplateComboboxViewModel> GetEnumerator()
        {
            return this.templateComboboxViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void AddTemplates(List<Template> templates)
        {
            List<TemplateComboboxViewModel> newViewModels = new List<TemplateComboboxViewModel>();
            foreach (Template template in templates)
            {
                TemplateComboboxViewModel templateComboboxViewModel = new TemplateComboboxViewModel(template);
                newViewModels.Add(templateComboboxViewModel);
            }

            this.templateComboboxViewModels.AddRange(newViewModels);
            this.RaiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
        }

        public void DeleteTemplate(Template template)
        {
            int position = this.templateComboboxViewModels.FindIndex(tvm => tvm.Guid == template.Guid.ToString());
            TemplateComboboxViewModel templateViewModel = this.templateComboboxViewModels[position];
            this.templateComboboxViewModels.Remove(templateViewModel);
            this.RaiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, templateViewModel, position));
        }

        public TemplateComboboxViewModel GetTemplateByGuid(Guid guid)
        {
            return this.templateComboboxViewModels.Find(templateviewModel => templateviewModel.Guid == guid.ToString());
        }
    }
}
