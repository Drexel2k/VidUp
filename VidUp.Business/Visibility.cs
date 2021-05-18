using System.ComponentModel;

namespace Drexel.VidUp.Business
{
    public enum Visibility
    {
        [Description("Public")]
        Public,
        [Description("Not Listed")]
        Unlisted,
        [Description("Private")]
        Private,
    }
}
