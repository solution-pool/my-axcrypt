using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axantum.AxCrypt.Core.UI
{
    public interface ISignIn
    {
        bool IsSigningIn { get; set; }

        Task SignIn();
    }
}