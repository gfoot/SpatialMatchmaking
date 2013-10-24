namespace Assets
{
    public interface INetworkInterface
    {
        bool Ready { get; }
        bool Connecting { get; }
        bool Connected { get; }
        string NetworkError { get; }
        string GetConnectionInfo();
        string GetBadConnectionInfo();
        string Listen(string expectedClientUuid);
        void StopListening();
        bool Connect(string connectionInfo, string localUuid);
    }
}