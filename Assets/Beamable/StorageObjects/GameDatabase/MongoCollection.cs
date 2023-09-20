using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Server;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Beamable.StorageObjects.GameDatabase
{
    public abstract class MongoCollection<TDatabase, TDocument> where TDatabase: MongoStorageObject
    {
        private readonly IStorageObjectConnectionProvider _connectionProvider;
        
        protected virtual string CollectionName => string.Empty;
        protected virtual IEnumerable<CreateIndexModel<TDocument>> Indexes => Array.Empty<CreateIndexModel<TDocument>>();
        protected virtual IEnumerable<CreateVectorIndexModel<TDocument>> VectorIndexes => Array.Empty<CreateVectorIndexModel<TDocument>>();

        protected MongoCollection(IStorageObjectConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        protected Promise<IMongoCollection<TDocument>> GetCollection()
        {
            if (string.IsNullOrEmpty(CollectionName))
                return Promise<IMongoCollection<TDocument>>.Failed(new ArgumentException("Collection name cannot be null or empty."));
            
            return _connectionProvider.GetDatabase<TDatabase>().Map(db => 
                db.GetCollection<TDocument>(CollectionName)
            );
        }
        
        public async Promise EnsureIndexes()
        {
            var collection = await GetCollection();
            if(Indexes.Any()) { 
                await collection.Indexes.CreateManyAsync(Indexes);
            }

            if (VectorIndexes.Any())
            {
                try
                {
                    var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
                    {
                        { "createSearchIndexes", CollectionName}, 
                        { "indexes", new BsonArray(VectorIndexes.Select(i => i.ToBson())) }
                    });
                    await collection.Database.RunCommandAsync<BsonDocument>(command);
                }
                // IndexAlreadyExists
                catch (MongoCommandException ex) when(ex.Code == 68)
                {
                    BeamableLogger.Log("Vector Index already exists, continuing.");
                }
            }
        }
    }
}