
namespace HelloWorldUserRestrictedAPI;

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.WebUtilities;
using System.Web;
using System.Net.Http;
using System.Threading.Tasks;


class OAuth
{
    Random random = new Random();
    private string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private string AuthorizationUri = "https://sandbox.api.service.nhs.uk/oauth2/authorize";  // Authorization code endpoint
    private string RedirectUri = "http://localhost:5000/callback";  // Callback endpoint
    private string TokenUri = "https://sandbox.api.service.nhs.uk/oauth2/token";  // Get tokens endpoint      
    private string client_id = "Your_Client_Id";  // change to your Client_ID
    private string client_secret = "Your_Client_Secret";  // change to your client_secret
    private string state;
    public void GetCode()
    {
        var _state = new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());
        this.state = _state;
        var dictionary = new Dictionary<string, string>()
           {
               {"client_id", client_id},
               {"redirect_uri", RedirectUri },
               {"response_type", "code"},
               {"state", _state}
           };
        string url = QueryHelpers.AddQueryString(AuthorizationUri, dictionary);
        Console.WriteLine("Open link in your browser " + url);
    }

    public async Task GetAccessToken()
    {
        Console.WriteLine("Enter callback url:");
        string CallbackUrl = Console.ReadLine();
        Uri siteUri = new Uri(CallbackUrl);
        string _code = HttpUtility.ParseQueryString(siteUri.Query).Get("code");
        string _state = HttpUtility.ParseQueryString(siteUri.Query).Get("state");

        if (this.state == _state)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("cache-control", "no-cache");
            var dict = new Dictionary<string, string>()
               {
                   {"grant_type", "authorization_code"},
                   {"client_id", client_id},
                   {"client_secret", client_secret},
                   {"redirect_uri", RedirectUri},
                   {"code", _code}
               };
            FormUrlEncodedContent postData = new FormUrlEncodedContent(dict);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, TokenUri);
            request.Content = postData;
            var response = await client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject(responseString);
            Console.WriteLine(json);
        }
        else
        {
            Console.WriteLine("callback is malformed");
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        OAuth OAuth = new OAuth();
        OAuth.GetCode();
        await OAuth.GetAccessToken();
    }
}
