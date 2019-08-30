using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos.Table;

[assembly: InternalsVisibleTo("Services.Test")]

namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services
{
    internal sealed class KeyValueEntity : TableEntity
    {
        public string CollectionId => this.PartitionKey;
        public string Key => this.RowKey;
        public string Data { get; set;}

        public KeyValueEntity()
        {
        }

        public KeyValueEntity(string collectionId, string key, string data)
            : base(collectionId, key)
        {
            this.Data = data;
        }
    }
}
