using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptumHsaSaveItExport
{
    public enum LoginType
    {
        manuallogin,
        autologin
    }
    public class Settings
    {
        public static LoginType Login { get; internal set; }

        static Settings()
        {
            Login = LoginType.autologin;
        }
    }

}
