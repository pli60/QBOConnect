namespace QBOLibrary.Services
{
    // Implemented by QBODataLibrary.QboMapAccess against the QBO_MAP table.
    // Declared here so QBOLibrary never has to reference the data project.
    public interface IQboMapStore
    {
        string GetQboId(string entity, string limsKey);

        void Upsert(string entity, string limsKey, string limsName,
                    string qboId, string qboName, string matchStatus, string updatedBy);
    }
}
