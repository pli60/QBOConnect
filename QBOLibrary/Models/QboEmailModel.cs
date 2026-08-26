namespace QBOLibrary.Models
{
    public class QboEmailModel
    {
        public string Address { get; set; }

        public QboEmailModel()
        {
        }

        public QboEmailModel(string address)
        {
            Address = address;
        }
    }
}
