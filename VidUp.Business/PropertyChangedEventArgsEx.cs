#region

using System.ComponentModel;

#endregion

namespace Drexel.VidUp.Business
{
    public class PropertyChangedEventArgsEx : PropertyChangedEventArgs
    {
        public virtual object OldValue { get; private set; }
        public virtual object NewValue { get; private set; }

        public PropertyChangedEventArgsEx(string propertyName, object oldValue,
               object newValue)
               : base(propertyName)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }
    }

}
