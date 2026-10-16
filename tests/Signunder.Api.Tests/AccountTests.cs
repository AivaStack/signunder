using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.Tokens;

namespace Signunder.Api.Tests;

public class AccountTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static string NewUser(out Guid id, string email = "founder@acme.example")
    {
        id = Guid.NewGuid();
        return ApiFactory.UserToken(id, email);
    }

    private static async Task<JsonObject> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>())!;

    private async Task<JsonObject> SignUpAsync(string token, string name)
    {
        var response = await api.ClientWithUser(token).PostAsJsonAsync("/v1/account/tenant", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await Json(response);
    }

    [Fact]
    public async Task A_signed_in_person_creates_a_company_and_its_first_key_works()
    {
        var token = NewUser(out _);
        var client = api.ClientWithUser(token);

        var before = await Json(await client.GetAsync("/v1/account"));
        Assert.Equal("founder@acme.example", (string?)before["email"]);
        Assert.Null(before["tenant"]);

        var created = await SignUpAsync(token, "Acme Images Ltd.");
        var slug = (string)created["tenant"]!["slug"]!;
        Assert.StartsWith("acme-images-ltd", slug);
        Assert.Equal("Acme Images Ltd.", (string?)created["tenant"]!["name"]);
        var key = (string)created["api_key"]!["key"]!;
        Assert.StartsWith("su_test_", key);

        // The key signs, and the output is found on the company's public verify page.
        var image = new ByteArrayContent([20, 1]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var signed = await api.ClientWithKey(key).PostAsync("/v1/sign", new MultipartFormDataContent { { image, "file", "a.jpg" } });
        Assert.Equal(HttpStatusCode.OK, signed.StatusCode);
        var id = signed.Headers.GetValues("SignUnder-Record-Id").Single();
        Assert.Equal(HttpStatusCode.OK, (await api.ClientWithKey(null).GetAsync($"/public/v1/{slug}/records/{id}")).StatusCode);

        // The key is shown once; afterwards only its prefix.
        var after = await Json(await client.GetAsync("/v1/account"));
        var listed = after["api_keys"]!.AsArray().Single()!;
        Assert.Equal(key[..12], (string?)listed["prefix"]);
        Assert.False(listed.AsObject().ContainsKey("key"));
    }

    [Fact]
    public async Task One_company_per_person()
    {
        var token = NewUser(out _);
        await SignUpAsync(token, "First Co");

        var again = await api.ClientWithUser(token).PostAsJsonAsync("/v1/account/tenant", new { name = "Second Co" });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Taken_and_reserved_names_get_a_suffixed_slug()
    {
        var first = await SignUpAsync(NewUser(out _), "Same Name");
        var second = await SignUpAsync(NewUser(out _), "Same Name");
        var demo = await SignUpAsync(NewUser(out _), "Demo");

        Assert.Equal("same-name", (string?)first["tenant"]!["slug"]);
        Assert.Matches("^same-name-[0-9a-f]{6}$", (string?)second["tenant"]!["slug"]);
        Assert.Matches("^demo-[0-9a-f]{6}$", (string?)demo["tenant"]!["slug"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    public async Task A_name_is_required(string name)
    {
        var response = await api.ClientWithUser(NewUser(out _)).PostAsJsonAsync("/v1/account/tenant", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Keys_are_added_up_to_the_limit_and_a_revoked_key_stops_working()
    {
        var token = NewUser(out _);
        await SignUpAsync(token, "Keyholder");
        var client = api.ClientWithUser(token);

        var keys = new List<JsonObject>();
        for (var i = 1; i < Account.AccountEndpoints.MaxActiveKeys; i++)
        {
            var response = await client.PostAsync("/v1/account/keys", null);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            keys.Add(await Json(response));
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/v1/account/keys", null)).StatusCode);

        var key = (string)keys[0]["key"]!;
        var record = "/v1/records/01ARZ3NDEKTSV4RRFFQ69G5FAV";
        Assert.Equal(HttpStatusCode.NotFound, (await api.ClientWithKey(key).GetAsync(record)).StatusCode);

        var revoked = await client.PostAsync($"/v1/account/keys/{keys[0]["id"]}/revoke", null);
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        Assert.NotNull((await Json(revoked))["revoked_at"]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.ClientWithKey(key).GetAsync(record)).StatusCode);

        // A slot is free again.
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/v1/account/keys", null)).StatusCode);
    }

    [Fact]
    public async Task Nobody_else_can_revoke_your_keys()
    {
        var owner = NewUser(out _);
        var created = await SignUpAsync(owner, "Owner Co");
        var other = NewUser(out _, "someone@else.example");
        await SignUpAsync(other, "Other Co");

        var response = await api.ClientWithUser(other).PostAsync($"/v1/account/keys/{created["api_key"]!["id"]}/revoke", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public static TheoryData<string> BadTokens()
    {
        var id = Guid.NewGuid();
        var otherKey = new ECDsaSecurityKey(ECDsa.Create(ECCurve.NamedCurves.nistP256)) { KeyId = "test" };
        return
        [
            ApiFactory.UserToken(id, "a@b.example", key: otherKey),
            ApiFactory.UserToken(id, "a@b.example", issuer: "https://elsewhere.supabase.co/auth/v1"),
            ApiFactory.UserToken(id, "a@b.example", audience: "anon"),
            ApiFactory.UserToken(id, "a@b.example", expires: DateTime.UtcNow.AddMinutes(-10)),
            ApiFactory.DemoKey,
        ];
    }

    [Theory]
    [MemberData(nameof(BadTokens))]
    public async Task Forged_foreign_expired_or_api_key_credentials_are_refused(string token)
    {
        var response = await api.ClientWithUser(token).GetAsync("/v1/account");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_anonymous_session_is_not_a_person()
    {
        var token = ApiFactory.UserToken(Guid.NewGuid(), "a@b.example", role: "anon");

        var response = await api.ClientWithUser(token).GetAsync("/v1/account");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_website_may_call_with_a_session_token()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/v1/account");
        request.Headers.Add("Origin", ApiFactory.AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        var response = await api.ClientWithKey(null).SendAsync(request);

        Assert.Equal(ApiFactory.AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
