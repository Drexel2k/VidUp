namespace Drexel.VidUp.UI.EventAggregation
{
    public class TemplateDisplayPropertyChangedMessage
    {
        public string Property { get; }

        public TemplateDisplayPropertyChangedMessage(string property)
        {
            this.Property = property;
        }
    }
}
