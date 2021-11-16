using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservableYouTubeAccountViewModels : INotifyCollectionChanged, IEnumerable<YouTubeAccountComboboxViewModel>
    {
        private YouTubeAccountList youTubeAccountList;
        private List<YouTubeAccountComboboxViewModel> youTubeAccountComboboxViewModels;

        public int YouTubeAccountCount { get => this.youTubeAccountComboboxViewModels.Count;  }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableYouTubeAccountViewModels(YouTubeAccountList youTubeAccountList)
        {
            this.youTubeAccountList = youTubeAccountList;

            this.youTubeAccountComboboxViewModels = new List<YouTubeAccountComboboxViewModel>();
            foreach (YouTubeAccount youTubeAccount in youTubeAccountList)
            {
                YouTubeAccountComboboxViewModel youTubeAccountComboboxViewModel = new YouTubeAccountComboboxViewModel(youTubeAccount);
                this.youTubeAccountComboboxViewModels.Add(youTubeAccountComboboxViewModel);
            }

            this.youTubeAccountList.CollectionChanged += youTubeAccountListCollectionChanged;
        }

        private void youTubeAccountListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                List<YouTubeAccountComboboxViewModel> newViewModels = new List<YouTubeAccountComboboxViewModel>();
                foreach (YouTubeAccount youTubeAccount in e.NewItems)
                {
                    newViewModels.Add(new YouTubeAccountComboboxViewModel(youTubeAccount));
                }

                this.youTubeAccountComboboxViewModels.AddRange(newViewModels);

                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //if multiple view models removed, remove every view model with a single call, as WPF/MVVM only supports
                //multiple deletes in one call when they are all in direct sequence in the collection
                foreach (YouTubeAccount youTubeAccount in e.OldItems)
                {
                    YouTubeAccountComboboxViewModel oldViewModel = this.youTubeAccountComboboxViewModels.Find(viewModel => viewModel.YouTubeAccountName == youTubeAccount.Name);
                    int index = this.youTubeAccountComboboxViewModels.IndexOf(oldViewModel);
                    this.youTubeAccountComboboxViewModels.Remove(oldViewModel);
                    this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldViewModel, index));
                }

                return;
            }

            throw new InvalidOperationException("ObservableYouTubeAccountViewModels supports only adding and removing.");
        }

        public YouTubeAccountComboboxViewModel GetViewModel(YouTubeAccount youTubeAccount)
        {
            if (youTubeAccount != null)
            {
                foreach (YouTubeAccountComboboxViewModel youTubeAccountComboboxViewModel in this.youTubeAccountComboboxViewModels)
                {
                    if (youTubeAccountComboboxViewModel.YouTubeAccountName == youTubeAccount.Name)
                    {
                        return youTubeAccountComboboxViewModel;
                    }
                }
            }

            return null;
        }

        public YouTubeAccountComboboxViewModel this[int index]
        {
            get => this.youTubeAccountComboboxViewModels[index];
        }


        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public IEnumerator<YouTubeAccountComboboxViewModel> GetEnumerator()
        {
            return this.youTubeAccountComboboxViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
