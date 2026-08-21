namespace QBOLibrary
{
    public static class QboConfig
    {
        public const string MinorVersion = "75";

        public const string SandboxBaseUrl = "https://sandbox-quickbooks.api.intuit.com";
        public const string ProductionBaseUrl = "https://quickbooks.api.intuit.com";
        public const string TokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
        public const string AuthorizeUrl = "https://appcenter.intuit.com/connect/oauth2";
        public const string Scope = "com.intuit.quickbooks.accounting";

        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string RedirectUri { get; set; }

        // Mirrors LIMSUser.SignedinDatabase.IsDev
        public static bool IsDev { get; set; }

        public static string Environment => IsDev ? "SANDBOX" : "PRODUCTION";
        public static string BaseUrl => IsDev ? SandboxBaseUrl : ProductionBaseUrl;
    }
}