namespace Assets
{
    public interface INetworkInterface
    {
        bool Ready { get; }
        bool Connecting { get; }
        bool Connected { get; }
        string NetworkError { get; }
        string GetConnectionInfo();
        string Listen();
        bool Connect(string connectionInfo);
    }
}