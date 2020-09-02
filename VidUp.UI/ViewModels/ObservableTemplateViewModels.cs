#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservableTemplateViewModels : INotifyCollectionChanged, IEnumerable<TemplateComboboxViewModel>
    {
        private TemplateList templateList;
        private List<TemplateComboboxViewModel> templateComboboxViewModels;

        public int TemplateCount { get => this.templateComboboxViewModels.Count;  }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableTemplateViewModels(TemplateList templateList, bool addAll, bool addNone)
        {
            this.templateList = templateList;

            this.templateComboboxViewModels = new List<TemplateComboboxViewModel>();

            if (addAll)
            {
                Template allTemplate = new Template("All");
                TemplateComboboxViewModel allViewModel = new TemplateComboboxViewModel(allTemplate);

                this.templateComboboxViewModels.Add(allViewModel);
            }

            if(addNone)
            {
                Template noneTemplate = new Template("None");
                TemplateComboboxViewModel noViewModel = new TemplateComboboxViewModel(noneTemplate);

                this.templateComboboxViewModels.Add(noViewModel);
            }

            foreach (Template template in templateList)
            {
                TemplateComboboxViewModel templateViewModel = new TemplateComboboxViewModel(template);
                this.templateComboboxViewModels.Add(templateViewModel);
            }

            this.templateList.CollectionChanged += templateListCollectionChanged;
        }

        private void templateListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                List<TemplateComboboxViewModel> newViewModels = new List<TemplateComboboxViewModel>();
                foreach (Template template in e.NewItems)
                {
                    newViewModels.Add(new TemplateComboboxViewModel(template));
                }

                this.templateComboboxViewModels.AddRange(newViewModels);

                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //if multiple view models removed, remove every view model with a single call, as WPF/MVVM only supports
                //multiple deletes in one call when they are all in direct sequence in the collection
                foreach (Template template in e.OldItems)
                {
                    TemplateComboboxViewModel oldViewModel = this.templateComboboxViewModels.Find(viewModel => viewModel.Template == template);
                    int index = this.templateComboboxViewModels.IndexOf(oldViewModel);
                    this.templateComboboxViewModels.Remove(oldViewModel);
                    this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldViewModel, index));
                }

                return;
            }

            throw new InvalidOperationException("ObservableTemplateViewModels supports only adding and removing.");
        }

        internal TemplateComboboxViewModel GetViewModel(Template template)
        {
            if (template != null)
            {
                foreach (TemplateComboboxViewModel templateComboboxViewModel in this.templateComboboxViewModels)
                {
                    if (templateComboboxViewModel.Template == template)
                    {
                        return templateComboboxViewModel;
                    }
                }
            }

            return null;
        }

        public TemplateComboboxViewModel this[int index]
        {
            get => this.templateComboboxViewModels[index];
        }


        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
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
    }
}
