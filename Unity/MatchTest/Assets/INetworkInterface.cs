namespace Assets
{
    public interface INetworkInterface
    {
        bool PollConnected();
        string GetConnectionInfo();
        string Connect(string connectionInfo);
    }
}