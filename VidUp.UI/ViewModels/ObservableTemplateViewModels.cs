using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.EventAggregation;

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservableTemplateViewModels : INotifyCollectionChanged, IEnumerable<TemplateComboboxViewModel>
    {
        private TemplateList templateList;
        private List<TemplateComboboxViewModel> templateComboboxViewModels;

        private Dictionary<YoutubeAccount, TemplateList> templateListsByAccount;
        private Dictionary<YoutubeAccount, ObservableTemplateViewModels> observableTemplateViewModelsByAccount;

        public int TemplateCount { get => this.templateComboboxViewModels.Count;  }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableTemplateViewModels(TemplateList templateList, bool createByAccount, bool addAll, bool addNone)
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

            if (createByAccount)
            {
                this.templateListsByAccount = new Dictionary<YoutubeAccount, TemplateList>();
                this.observableTemplateViewModelsByAccount = new Dictionary<YoutubeAccount, ObservableTemplateViewModels>();

                Dictionary<YoutubeAccount, List<Template>> templatesByAccount = new Dictionary<YoutubeAccount, List<Template>>();
                foreach (Template template in this.templateList)
                {
                    List<Template> templates;
                    if (!templatesByAccount.TryGetValue(template.YoutubeAccount, out templates))
                    {
                        templates = new List<Template>();
                        templatesByAccount.Add(template.YoutubeAccount, templates);
                    }

                    templates.Add(template);
                }

                foreach (KeyValuePair<YoutubeAccount, List<Template>> accountTemplates in templatesByAccount)
                {
                    TemplateList accountTemplateList = new TemplateList(accountTemplates.Value);
                    this.templateListsByAccount.Add(accountTemplates.Key, accountTemplateList);
                    this.observableTemplateViewModelsByAccount.Add(accountTemplates.Key, new ObservableTemplateViewModels(accountTemplateList, false, false, false));
                }
            }

            this.templateList.CollectionChanged += this.templateListCollectionChanged;
            EventAggregator.Instance.Subscribe<TemplateYoutubeAccountChangedMessage>(this.templateYoutubeAccountChanged);
        }

        private void templateYoutubeAccountChanged(TemplateYoutubeAccountChangedMessage templateYoutubeAccountChangedMessage)
        {
            if (this.templateListsByAccount != null)
            {
                this.templateListsByAccount[templateYoutubeAccountChangedMessage.OldAccount].Remove(templateYoutubeAccountChangedMessage.Template);

                TemplateList accountTemplateList;
                if (!this.templateListsByAccount.TryGetValue(templateYoutubeAccountChangedMessage.NewAccount, out accountTemplateList))
                {
                    List<Template> templates = new List<Template>();
                    templates.Add(templateYoutubeAccountChangedMessage.Template);
                    accountTemplateList = new TemplateList(templates);
                    this.templateListsByAccount.Add(templateYoutubeAccountChangedMessage.NewAccount, accountTemplateList);
                    this.observableTemplateViewModelsByAccount.Add(templateYoutubeAccountChangedMessage.NewAccount, new ObservableTemplateViewModels(accountTemplateList, false, false, false));
                }
                else
                {
                    accountTemplateList.AddTemplate(templateYoutubeAccountChangedMessage.Template);
                }
            }
        }

        public ObservableTemplateViewModels(TemplateList templateList, YoutubeAccount youtubeAccount)
        {
            this.templateList = templateList;
            this.templateComboboxViewModels = new List<TemplateComboboxViewModel>();

            foreach (Template template in templateList)
            {
                TemplateComboboxViewModel templateViewModel = new TemplateComboboxViewModel(template);
                this.templateComboboxViewModels.Add(templateViewModel);
            }

            this.templateList.CollectionChanged += this.templateListCollectionChanged;
        }

        private void templateListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                List<TemplateComboboxViewModel> newViewModels = new List<TemplateComboboxViewModel>();
                foreach (Template template in e.NewItems)
                {
                    newViewModels.Add(new TemplateComboboxViewModel(template));

                    if (this.templateListsByAccount != null)
                    {
                        this.templateListsByAccount[template.YoutubeAccount].AddTemplate(template);
                    }
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
                    oldViewModel.Dispose();

                    if (this.templateListsByAccount != null)
                    {
                        this.templateListsByAccount[template.YoutubeAccount].Remove(template);
                    }

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

        public TemplateComboboxViewModel GetFirstViewModel(Predicate<TemplateComboboxViewModel> match)
        {
            if (match != null)
            {
                foreach (TemplateComboboxViewModel templateComboboxViewModel in this.templateComboboxViewModels)
                {
                    if (match(templateComboboxViewModel))
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

        public ObservableTemplateViewModels this[YoutubeAccount youtubeAccount]
        {
            get
            {
                if (!this.observableTemplateViewModelsByAccount.ContainsKey(youtubeAccount))
                {
                    return null;
                }

                return this.observableTemplateViewModelsByAccount[youtubeAccount];
            }
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
