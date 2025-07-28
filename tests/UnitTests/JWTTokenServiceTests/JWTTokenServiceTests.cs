
namespace NHS.CohortManager.Tests.UnitTests.JwtTokenServiceTests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

[TestClass]
public class JwtTokenServiceTests
{
    private readonly string _audience;
    private readonly string _clientId;

    private readonly Mock<IOptions<JwtTokenServiceConfig>> _jwtTokenServiceConfig = new();
    private readonly Mock<ISigningCredentialsProvider> _mockSigningCredentialsProvider = new();

    public JwtTokenServiceTests()
    {

        var testConfig = new JwtTokenServiceConfig
        {
            Audience = "my-audience",
            ClientId = "my-client-id",
            KId = "",
            AuthTokenURL = "",
            LocalPrivateKeyFileName = "",
            PrivateKey = ""
        };

        _jwtTokenServiceConfig.Setup(x => x.Value).Returns(testConfig);
        _mockSigningCredentialsProvider = new Mock<ISigningCredentialsProvider>();

    }

    [TestMethod]
    public void GenerateJwt_ReturnsValidJwtToken()
    {
        var fakePrivateKey = generatePrivateKey();
        var dummySecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fakePrivateKey))
        {
            KeyId = "dummy-kid"
        };

        _mockSigningCredentialsProvider.Setup(p => p.CreateSigningCredentials())
            .Returns(new SigningCredentials(dummySecurityKey, SecurityAlgorithms.HmacSha256));

        var generator = new JwtTokenService(_jwtTokenServiceConfig.Object, _mockSigningCredentialsProvider.Object);

        // Act
        var token = generator.GenerateJwt();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.AreEqual("my-client-id", jwt.Issuer);
        Assert.AreEqual("my-audience", jwt.Audiences.First());
        Assert.AreEqual("my-client-id", jwt.Claims.First(c => c.Type == "sub").Value);
    }

    private string generatePrivateKey()
    {
        // Arrange
        using var rsa = RSA.Create(4096);

        // Export as PKCS#8 private key
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();

        string base64 = Convert.ToBase64String(privateKeyBytes);

        return base64;
    }

}
