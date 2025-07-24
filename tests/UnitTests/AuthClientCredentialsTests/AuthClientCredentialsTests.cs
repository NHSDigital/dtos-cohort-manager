
namespace NHS.CohortManager.Tests.UnitTests.JwtTokenServiceTests;

using System.IdentityModel.Tokens.Jwt;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;

[TestClass]
public class AuthClientCredentialsTests
{
    private readonly Mock<IOptions<JwtTokenServiceConfig>> _jwtTokenServiceConfig = new();
    private readonly Mock<IJwtTokenService> _jwtHandler = new();

    private readonly HttpClient _httpClient;

    private readonly string access_token = "some_fake_token";

    private AuthorizationClientCredentials authClientCredentials;
    public AuthClientCredentialsTests()
    {
        var testConfig = new JwtTokenServiceConfig
        {
            Audience = "my-audience",
            ClientId = "my-client-id",
            KId = "",
            AuthTokenURL = "http://www.some_fake_url.com",
            LocalPrivateKeyFileName = "",
            PrivateKey = "",

        };

        _jwtTokenServiceConfig.Setup(x => x.Value).Returns(testConfig);
        _jwtHandler.Setup(x => x.GenerateJwt(It.IsAny<int>())).Returns("some-fak-jwt-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{ \"access_token\": \"some_fake_token\" }"),
            });

        _httpClient = new HttpClient(handlerMock.Object);


        authClientCredentials = new AuthorizationClientCredentials(_jwtHandler.Object, _httpClient, _jwtTokenServiceConfig.Object);
    }

    [TestMethod]
    public async Task AccessToken_Valid_TokenAsync()
    {
        var res = await authClientCredentials.AccessToken(5);

        Assert.AreEqual(access_token, res);
    }
}
