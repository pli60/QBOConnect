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

        public static int RequestTimeoutSeconds { get; set; } = 60;

        // Retries apply to 429 and 5xx only, never to 400 or 403
        public static int MaxRetries { get; set; } = 3;
        public static int RetryBaseDelayMs { get; set; } = 500;

        // A refresh lock older than this is treated as abandoned
        public static int StaleLockSeconds { get; set; } = 120;

        // How long to wait for another machine to finish its refresh
        public static int RefreshWaitAttempts { get; set; } = 10;
        public static int RefreshWaitMs { get; set; } = 1000;

        public static bool LogRequestBodies { get; set; } = true;

        public static bool IsConfigured =>
            !string.IsNullOrEmpty(ClientId)
            && !string.IsNullOrEmpty(ClientSecret)
            && !string.IsNullOrEmpty(RedirectUri);
    }
}
