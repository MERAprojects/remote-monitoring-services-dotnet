// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Models
{
    public class ValueServiceModel
    {
        public string CollectionId { get; set; }
        public string Key { get; set; }
        public string Data { get; set; }
        public string ETag { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public ValueServiceModel()
        {
        }

        internal ValueServiceModel(KeyValueEntity entity)
        {
            if (entity == null) return;

            this.CollectionId = entity.CollectionId;
            this.Key = entity.Key;
            this.Data = entity.Data;
            this.ETag = entity.ETag;
            this.Timestamp = entity.Timestamp;
        }
    }
}
