namespace QBOLibrary.Models
{
    public class QboPhoneModel
    {
        public string FreeFormNumber { get; set; }

        public QboPhoneModel()
        {
        }

        public QboPhoneModel(string freeFormNumber)
        {
            FreeFormNumber = freeFormNumber;
        }
    }
}
