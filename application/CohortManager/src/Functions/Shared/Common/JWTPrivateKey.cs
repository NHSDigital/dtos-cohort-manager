namespace Common;

public class JWTPrivateKey
{
    public JWTPrivateKey(string privateKey)
    {
        PrivateKey = privateKey;
    }
    public string PrivateKey { get; }
}