namespace QBOLibrary.Http
{
    public interface IQboLogStore
    {
        void Write(QboLogModel entry);
    }
}
