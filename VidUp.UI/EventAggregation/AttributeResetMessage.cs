using Drexel.VidUp.Business;

namespace Drexel.VidUp.Utils.EventAggregation
{
    public class AttributeResetMessage
    {
        public Upload Upload { get; }
        public string Attribute { get; }

        public AttributeResetMessage(Upload upload, string attribute)
        {
            this.Upload = upload;
            this.Attribute = attribute;
        }

    }
}
