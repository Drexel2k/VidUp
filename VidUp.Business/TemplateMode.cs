using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drexel.VidUp.Business
{
    public enum TemplateMode
    {
        [Description("Folder Based")]
        FolderBased,
        [Description("File Name Based")]
        FileNameBased
    }
}
