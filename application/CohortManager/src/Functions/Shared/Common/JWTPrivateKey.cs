namespace Common;

public class JwtPrivateKey
{
    public JwtPrivateKey(string privateKey)
    {
        PrivateKey = privateKey;
    }
    public string PrivateKey { get; }
}