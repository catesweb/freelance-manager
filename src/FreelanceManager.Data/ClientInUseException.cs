namespace FreelanceManager.Data;

public class ClientInUseException : Exception
{
    public ClientInUseException(string message) : base(message) { }
}
