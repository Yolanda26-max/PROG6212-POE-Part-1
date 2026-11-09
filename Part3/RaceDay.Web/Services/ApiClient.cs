using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public int StatusCode { get; set; }
    }

    public class ApiClient
    {
        public const string TokenKey = "JWT";

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        public Task<ApiResult<T>> GetAsync<T>(string url) =>
            SendAsync<T>(HttpMethod.Get, url, null);

        public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
            SendAsync<T>(HttpMethod.Post, url, body);

        public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) =>
            SendAsync<T>(HttpMethod.Put, url, body);

        public Task<ApiResult<T>> DeleteAsync<T>(string url) =>
            SendAsync<T>(HttpMethod.Delete, url, null);

        private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body)
        {
            try
            {
                var request = new HttpRequestMessage(method, url);

                // Attach the logged-in user's token (stored in session at login)
                var token = _accessor.HttpContext?.Session.GetString(TokenKey);
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    T? data = default;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        try { data = JsonSerializer.Deserialize<T>(content, JsonOptions); }
                        catch (JsonException) { /* response wasn't JSON of type T; ignore */ }
                    }

                    return new ApiResult<T>
                    {
                        Success = true,
                        Data = data,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiResult<T>
                {
                    Success = false,
                    Error = FriendlyError(response.StatusCode, content),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<T>
                {
                    Success = false,
                    Error = "We couldn't reach the RaceDay server. Please try again shortly.",
                    StatusCode = 0
                };
            }
            catch (TaskCanceledException)
            {
                return new ApiResult<T>
                {
                    Success = false,
                    Error = "The server took too long to respond. Please try again.",
                    StatusCode = 0
                };
            }
        }

        // Turns API error responses into a clean message (never raw errors)
        private static string FriendlyError(System.Net.HttpStatusCode status, string content)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var name in new[] { "message", "Message", "error", "title", "detail" })
                        {
                            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                                return prop.GetString()!;
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.String)
                    {
                        return root.GetString()!;
                    }
                }
                catch (JsonException)
                {
                    // Plain text response
                    if (content.Length < 200) return content.Trim('"');
                }
            }

            return status switch
            {
                System.Net.HttpStatusCode.Unauthorized => "Please log in to continue.",
                System.Net.HttpStatusCode.Forbidden => "You don't have permission to do that.",
                System.Net.HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
                System.Net.HttpStatusCode.Conflict => "That action conflicts with existing data.",
                System.Net.HttpStatusCode.BadRequest => "Some of the information entered isn't valid.",
                _ => "Something went wrong. Please try again."
            };
        }
    }
}