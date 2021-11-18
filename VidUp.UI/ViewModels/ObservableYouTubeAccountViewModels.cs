using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.EventAggregation;

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservableYoutubeAccountViewModels : INotifyCollectionChanged, IEnumerable<YoutubeAccountComboboxViewModel>
    {
        private YoutubeAccountList youtubeAccountList;
        private List<YoutubeAccountComboboxViewModel> youtubeAccountComboboxViewModels;

        public int YoutubeAccountCount { get => this.youtubeAccountComboboxViewModels.Count;  }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableYoutubeAccountViewModels(YoutubeAccountList youtubeAccountList, bool addAll)
        {
            this.youtubeAccountList = youtubeAccountList;

            this.youtubeAccountComboboxViewModels = new List<YoutubeAccountComboboxViewModel>();

            if (addAll)
            {
                YoutubeAccount allAccount = new YoutubeAccount(null, "All");
                YoutubeAccountComboboxViewModel allViewModel = new YoutubeAccountComboboxViewModel(allAccount);
                this.youtubeAccountComboboxViewModels.Add(allViewModel);
            }

            foreach (YoutubeAccount youtubeAccount in youtubeAccountList)
            {
                YoutubeAccountComboboxViewModel youtubeAccountComboboxViewModel = new YoutubeAccountComboboxViewModel(youtubeAccount);
                this.youtubeAccountComboboxViewModels.Add(youtubeAccountComboboxViewModel);
            }

            this.youtubeAccountList.CollectionChanged += youtubeAccountListCollectionChanged;
        }

        private void youtubeAccountListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                List<YoutubeAccountComboboxViewModel> newViewModels = new List<YoutubeAccountComboboxViewModel>();
                foreach (YoutubeAccount youtubeAccount in e.NewItems)
                {
                    newViewModels.Add(new YoutubeAccountComboboxViewModel(youtubeAccount));
                }

                this.youtubeAccountComboboxViewModels.AddRange(newViewModels);

                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //if multiple view models removed, remove every view model with a single call, as WPF/MVVM only supports
                //multiple deletes in one call when they are all in direct sequence in the collection
                foreach (YoutubeAccount youtubeAccount in e.OldItems)
                {
                    YoutubeAccountComboboxViewModel oldViewModel = this.youtubeAccountComboboxViewModels.Find(viewModel => viewModel.YoutubeAccount.Name == youtubeAccount.Name);
                    int index = this.youtubeAccountComboboxViewModels.IndexOf(oldViewModel);
                    this.youtubeAccountComboboxViewModels.Remove(oldViewModel);
                    this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldViewModel, index));
                }

                return;
            }

            throw new InvalidOperationException("ObservableYoutubeAccountViewModels supports only adding and removing.");
        }

        public YoutubeAccountComboboxViewModel GetViewModel(YoutubeAccount youtubeAccount)
        {
            if (youtubeAccount != null)
            {
                foreach (YoutubeAccountComboboxViewModel youtubeAccountComboboxViewModel in this.youtubeAccountComboboxViewModels)
                {
                    if (youtubeAccountComboboxViewModel.YoutubeAccount.Name == youtubeAccount.Name)
                    {
                        return youtubeAccountComboboxViewModel;
                    }
                }
            }

            return null;
        }

        public YoutubeAccountComboboxViewModel this[int index]
        {
            get => this.youtubeAccountComboboxViewModels[index];
        }


        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public IEnumerator<YoutubeAccountComboboxViewModel> GetEnumerator()
        {
            return this.youtubeAccountComboboxViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
