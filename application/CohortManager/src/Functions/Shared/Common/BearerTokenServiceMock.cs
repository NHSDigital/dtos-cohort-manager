namespace Common;

class BearerTokenServiceMock : IBearerTokenService
{

    public async Task<string> GetBearerToken()
    {
        await Task.CompletedTask;
        return "some-fake-bearer-token";
    }
}
