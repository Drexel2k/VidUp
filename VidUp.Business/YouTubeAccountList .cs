using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace Drexel.VidUp.Business
{
    public class YouTubeAccountList : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<YouTubeAccount>
    {
        private List<YouTubeAccount> youTubeAccounts;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int AccountCount { get => this.youTubeAccounts.Count; }

        public YouTubeAccountList(List<YouTubeAccount> youTubeAccounts)
        {
            this.youTubeAccounts = new List<YouTubeAccount>();

            if (youTubeAccounts != null)
            {
                this.youTubeAccounts = youTubeAccounts;
            }
        }

        public YouTubeAccount this[int index]
        {
            get
            {
                return this.youTubeAccounts[index];
            }
        }

        public void AddYouTubeAccounts(List<YouTubeAccount> youTubeAccounts)
        {
            this.youTubeAccounts.AddRange(youTubeAccounts);

            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, youTubeAccounts));
        }

        public int FindIndex(Predicate<YouTubeAccount> predicate)
        {
            return this.youTubeAccounts.FindIndex(predicate);
        }

        public void Remove(string name)
        {
            YouTubeAccount youTubeAccount = this.youTubeAccounts.Find(acc => acc.Name == name);
            if (youTubeAccount != null)
            {
                if (File.Exists(youTubeAccount.FilePath))
                {
                    File.Delete(youTubeAccount.FilePath);
                }

                this.youTubeAccounts.Remove(youTubeAccount);
                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, youTubeAccount));
            }
        }

        public YouTubeAccount GetYouTubeAccount(int index)
        {
            return this.youTubeAccounts[index];
        }

        public YouTubeAccount GetYouTubeAccount(string name)
        {
            return this.youTubeAccounts.Find(youTubeAccount => youTubeAccount.Name == name);
        }

        public ReadOnlyCollection<YouTubeAccount> GetReadOnlyYouTubeAccountList()
        {
            return this.youTubeAccounts.AsReadOnly();
        }

        public YouTubeAccount Find(Predicate<YouTubeAccount> match)
        {
            return this.youTubeAccounts.Find(match);
        }

        public IEnumerator<YouTubeAccount> GetEnumerator()
        {
            return this.youTubeAccounts.GetEnumerator();
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
    }
}
