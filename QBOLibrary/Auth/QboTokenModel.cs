using System;

namespace QBOLibrary.Auth
{
    public class QboTokenModel
    {
        public string Environment { get; set; }
        public string RealmId { get; set; }
        public string AccessToken { get; set; }
        public DateTime? AccessExpires { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? RefreshExpires { get; set; }
        public bool RefreshLocked { get; set; }
        public string LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }

        // 5 minute cushion so a call never starts with a token about to expire
        public bool IsAccessValid =>
            !string.IsNullOrEmpty(AccessToken)
            && AccessExpires.HasValue
            && AccessExpires.Value > DateTime.Now.AddMinutes(5);

        public bool IsRefreshValid =>
            !string.IsNullOrEmpty(RefreshToken)
            && (!RefreshExpires.HasValue || RefreshExpires.Value > DateTime.Now);
    }
}
