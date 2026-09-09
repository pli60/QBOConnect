namespace QBOLibrary.Models
{
    // Requires QuickBooks Online Plus or Advanced
    public class QboClassModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string Name { get; set; }
        public string FullyQualifiedName { get; set; }
        public bool? Active { get; set; }
        public bool? SubClass { get; set; }
        public QboRefModel ParentRef { get; set; }
        public QboMetaDataModel MetaData { get; set; }
    }
}
