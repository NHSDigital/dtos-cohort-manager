namespace Common;

class BearerTokenServiceMock : IBearerTokenService
{

    public async Task<string> GetBearerToken()
    {
        return "some-fake-bearer-token";
    }
}
