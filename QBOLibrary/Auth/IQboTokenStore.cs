namespace QBOLibrary.Auth
{
    public interface IQboTokenStore
    {
        QboTokenModel Get();

        // Returns true only if this caller acquired the refresh lock
        bool TryLockForRefresh(string owner, int staleLockSeconds);

        void SaveRefreshed(QboTokenModel token, string owner);
        void SaveAuthorized(QboTokenModel token, string owner);
        void ReleaseLock(string owner);
    }
}
