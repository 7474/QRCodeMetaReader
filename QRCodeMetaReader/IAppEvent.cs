using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace QRCodeMetaReader
{
    interface IAppEvent
    {
        Task OnSuspending(object sender, SuspendingEventArgs e);
        Task OnResuming(object sender, object e);
    }
}
