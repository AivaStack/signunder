# Signunder

The .NET client for [SignUnder](https://signunder.com), which signs AI-generated images with C2PA Content Credentials under your company's own name, adds an invisible watermark and a fingerprint, and records each output so anyone can check it on your public verify page.

```
dotnet add package Signunder
```

Get an API key at [signunder.com/dashboard](https://signunder.com/dashboard), then:

```csharp
using Signunder.Sdk;

var client = SignunderClient.FromEnvironment();   // SIGNUNDER_API_KEY

var signed = await client.SignAsync("picture.jpg");
await File.WriteAllBytesAsync("picture.signed.jpg", signed.Content);   // serve these bytes unchanged

var check = await client.VerifyAsync("picture.signed.jpg");
Console.WriteLine($"{check.ValidationState}, signed by {check.SignedBy}");

var record = await client.GetRecordAsync(signed.RecordId);
```

In ASP.NET Core, register it once and inject `SignunderClient`:

```csharp
builder.Services.AddSignunderClient(o => o.ApiKey = builder.Configuration["Signunder:ApiKey"]);
```

JPEG, PNG and WebP are supported. Every refusal throws `SignunderException` with the HTTP status and the API's explanation; for a failure on SignUnder's side, `Reference` is the id to quote to support. The client targets .NET 8 and .NET 10 and works in trimmed and native AOT apps.

The API reference is at [signunder.com/docs](https://signunder.com/docs); the source, under Apache-2.0, is at [github.com/AivaStack/signunder](https://github.com/AivaStack/signunder).
