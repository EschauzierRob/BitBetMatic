// Reference-only legacy snippet.
// Extracted from BitBetMatic v1 for documentation and migration planning.
// Not part of the active runtime for BitBetMatic 2.0.

private void SetApiRequestHeaders(RestRequest request, string url, string body = "")
{
    var timestamp = GetTime();

    request.AddHeader("Bitvavo-Access-Key", Environment.GetEnvironmentVariable("BITVAVO_API_KEY"));
    request.AddHeader("Bitvavo-Access-Signature", GenerateSignature(timestamp, request.Method.ToString().ToUpper(), url, body));
    request.AddHeader("Bitvavo-Access-Timestamp", timestamp);
    request.AddHeader("Bitvavo-Access-Window", 60000);
    request.AddHeader("Content-Type", "application/json");
}

private string GenerateSignature(string timestamp, string method, string url, string body)
{
    string prehashString = $"{timestamp}{method}/v2/{url}{body}";

    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("BITVAVO_API_SECRET"))))
    {
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(prehashString));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
