using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("GameServer")]
	public class GameServer : Microservice
	{
		[ClientCallable]
		public void ServerCall()
		{
			// This code executes on the server.
		}
	}
}
