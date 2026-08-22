using System;

namespace QBOTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // Matches the LIMS runtime pattern - connection set at startup, not in config
            QBODataLibrary.ChangeConn.ChangeConnStr(
                "Data Source=10.1.10.249;Initial Catalog=LW_DEV;User ID=mylims1;Password=groundPork3$");

            QBOLibrary.QboConfig.IsDev = true;
            QBOLibrary.QboConfig.ClientId = "";
            QBOLibrary.QboConfig.ClientSecret = "";

            Console.WriteLine($"Environment: {QBOLibrary.QboConfig.Environment}");
            Console.WriteLine($"Base URL:    {QBOLibrary.QboConfig.BaseUrl}");
            Console.ReadLine();
        }
    }
}