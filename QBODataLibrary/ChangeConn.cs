using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBODataLibrary
{
    public static class ChangeConn
    {
        public static void ChangeConnStr(string conn)
        {
            Properties.Settings.Default.SQLConnectionString = conn;
        }
    }
}