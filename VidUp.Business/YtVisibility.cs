using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Drexel.VidUp.Business
{
    public enum YtVisibility
    {
        [Description("Public")]
        Public,
        [Description("Not Listed")]
        Unlisted,
        [Description("Private")]
        Private,
    }
}
