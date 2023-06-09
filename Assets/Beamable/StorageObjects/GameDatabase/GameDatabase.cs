using Beamable.Common;
using MongoDB.Driver;

namespace Beamable.Server
{
	[StorageObject("GameDatabase")]
	public class GameDatabase : MongoStorageObject
	{
	}

	public static class GameDatabaseExtension
	{
		/// <summary>
		/// Get an authenticated MongoDB instance for GameDatabase
		/// </summary>
		/// <returns></returns>
		public static Promise<IMongoDatabase> GameDatabaseDatabase(this IStorageObjectConnectionProvider provider)
			=> provider.GetDatabase<GameDatabase>();

		/// <summary>
		/// Gets a MongoDB collection from GameDatabase by the requested name, and uses the given mapping class.
		/// If you don't want to pass in a name, consider using <see cref="GameDatabaseCollection{TCollection}()"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> GameDatabaseCollection<TCollection>(
			this IStorageObjectConnectionProvider provider, string name)
			where TCollection : StorageDocument
			=> provider.GetCollection<GameDatabase, TCollection>(name);

		/// <summary>
		/// Gets a MongoDB collection from GameDatabase by the requested name, and uses the given mapping class.
		/// If you want to control the collection name separate from the class name, consider using <see cref="GameDatabaseCollection{TCollection}(string)"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> GameDatabaseCollection<TCollection>(
			this IStorageObjectConnectionProvider provider)
			where TCollection : StorageDocument
			=> provider.GetCollection<GameDatabase, TCollection>();
	}
}
