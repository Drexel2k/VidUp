using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace Drexel.VidUp.Business
{
    public class YoutubeAccountList : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<YoutubeAccount>
    {
        private List<YoutubeAccount> youtubeAccounts;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int AccountCount { get => this.youtubeAccounts.Count; }

        public YoutubeAccountList(List<YoutubeAccount> youtubeAccounts)
        {
            this.youtubeAccounts = new List<YoutubeAccount>();

            if (youtubeAccounts != null)
            {
                this.youtubeAccounts = youtubeAccounts;
            }
        }

        public YoutubeAccount this[int index]
        {
            get
            {
                return this.youtubeAccounts[index];
            }
        }

        public void AddYoutubeAccount(YoutubeAccount youtubeAccount)
        {
            this.youtubeAccounts.Add(youtubeAccount);

            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, youtubeAccount));
        }

        public int FindIndex(Predicate<YoutubeAccount> predicate)
        {
            return this.youtubeAccounts.FindIndex(predicate);
        }

        public void Remove(string name)
        {
            YoutubeAccount youtubeAccount = this.youtubeAccounts.Find(acc => acc.Name == name);
            if (youtubeAccount != null)
            {
                if (File.Exists(youtubeAccount.FilePath))
                {
                    File.Delete(youtubeAccount.FilePath);
                }

                this.youtubeAccounts.Remove(youtubeAccount);
                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, youtubeAccount));
            }
        }

        public YoutubeAccount GetYoutubeAccount(int index)
        {
            return this.youtubeAccounts[index];
        }

        public YoutubeAccount GetYoutubeAccount(string name)
        {
            return this.youtubeAccounts.Find(youtubeAccount => youtubeAccount.Name == name);
        }

        public ReadOnlyCollection<YoutubeAccount> GetReadOnlyYoutubeAccountList()
        {
            return this.youtubeAccounts.AsReadOnly();
        }

        public YoutubeAccount Find(Predicate<YoutubeAccount> match)
        {
            return this.youtubeAccounts.Find(match);
        }

        public IEnumerator<YoutubeAccount> GetEnumerator()
        {
            return this.youtubeAccounts.GetEnumerator();
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
