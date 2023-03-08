using System;
using System.Collections.Generic;
using System.Text;

internal static class JwtUtil
{
    /**
     * Returns JWT token's payload data.
     */
    public static Dictionary<string, object> GetTokenPayload(string token, IJsonSerializer serializer)
    {
        try
        {
            var content = token.Split('.')[1];
            var jsonPayload = Encoding.UTF8.GetString(Base64Url.Decode(content));
            return serializer.DecodeJwt(token, jsonPayload);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            return new();
        }
    }

    /**
     * Checks whether a JWT token is expired or not.
     * Tokens without `exp` payload key are considered valid.
     * Tokens with empty payload (eg. invalid token strings) are considered expired.
     *
     * @param token The token to check.
     * @param [expirationThreshold] Time in seconds that will be subtracted from the token `exp` property.
     */
    public static bool IsTokenExpired(string token, IJsonSerializer serializer, long expirationThresholdSecs = 0)
    {
        var payload = GetTokenPayload(token, serializer);

        if (payload.Count == 0) return false;
        var exp = GetTokenExpiryUnixSecs(payload);
        var isExpired = exp - expirationThresholdSecs > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
        return !isExpired;
    }

    public static long? GetTokenExpiryUnixSecs(Dictionary<string, object> payload)
    {
        if (!payload.ContainsKey("exp")) return null;
        var exp = Convert.ToInt64(payload["exp"]);
        return exp;
    }
}


/*

 Implementation of Base64Url lifted from https://github.com/dvsekhvalnov/jose-jwt
 The MIT License (MIT)

Copyright (c) 2014-2021 dvsekhvalnov

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

internal static class Base64Url
{
    public static string Encode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        output = output.Split('=')[0]; // Remove any trailing '='s
        output = output.Replace('+', '-'); // 62nd char of encoding
        output = output.Replace('/', '_'); // 63rd char of encoding
        return output;
    }

    public static byte[] Decode(string input)
    {
        var output = input;
        output = output.Replace('-', '+'); // 62nd char of encoding
        output = output.Replace('_', '/'); // 63rd char of encoding
        switch (output.Length % 4) // Pad with trailing '='s
        {
            case 0: break; // No pad chars in this case
            case 2: output += "=="; break; // Two pad chars
            case 3: output += "="; break; // One pad char
            default: throw new System.ArgumentOutOfRangeException(nameof(input), "Illegal base64url string!");
        }
        var converted = Convert.FromBase64String(output); // Standard base64 decoder
        return converted;
    }
}