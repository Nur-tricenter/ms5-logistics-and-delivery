namespace Nurtricenter.Infrastructure.ServiceDiscovery;

/// <summary>
/// Answers with the port the host actually bound to, when configuration did not pin one.
/// Implemented by the web host (<c>ServerAddressesPortResolver</c>); the console worker has no
/// port to advertise and never registers a resolver.
/// </summary>
public interface IServerPortResolver
{
    int? ResolveBoundPort();
}
