// Reference-only legacy snippet.
// Extracted from BitBetMatic v1 for documentation and migration planning.
// Not part of the active runtime for BitBetMatic 2.0.

private async Task<string> PlaceOrder(string market, string side, decimal amount)
{
    var formattedAmount = FormatAmount(amount, 2);
    if (amount % 1 == 0) { formattedAmount = amount.ToString("N0"); }

    var url = "order";
    var body = new
    {
        market,
        side,
        orderType = "market",
        amountQuote = formattedAmount
    };

    var request = new RestRequest(url, Method.Post);
    request.AddJsonBody(body);
    SetApiRequestHeaders(request, url, JsonConvert.SerializeObject(body));

    var response = await Client.ExecuteAsync(request);
    if (!response.IsSuccessful)
    {
        throw new Exception($"Error placing order: {response.Content}");
    }

    var orderResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);
    return $"Order {side} placed successfully: {orderResponse.orderId}";
}
