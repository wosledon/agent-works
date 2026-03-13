using System.Text.Json;

namespace OneReport.Services.Implementations;

/// <summary>
/// HTTP API 数据源查询服务
/// </summary>
public interface IApiDataSourceService
{
    /// <summary>
    /// 执行 HTTP API 查询并返回数据流
    /// </summary>
    IAsyncEnumerable<Dictionary<string, object?>> QueryAsync(
        string apiUrl,
        string? method = "GET",
        Dictionary<string, string?>? headers = null,
        object? body = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行分页 API 查询
    /// </summary>
    IAsyncEnumerable<Dictionary<string, object?>> QueryPagedAsync(
        string apiUrl,
        string? pageParamName = "page",
        string? pageSizeParamName = "pageSize",
        int pageSize = 100,
        string? method = "GET",
        Dictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP API 数据源查询服务实现
/// </summary>
public class ApiDataSourceService : IApiDataSourceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiDataSourceService> _logger;

    public ApiDataSourceService(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiDataSourceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<Dictionary<string, object?>> QueryAsync(
        string apiUrl,
        string? method = "GET",
        Dictionary<string, string?>? headers = null,
        object? body = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(headers);
        
        HttpResponseMessage response;
        
        switch (method?.ToUpperInvariant())
        {
            case "POST":
                var jsonBody = body != null ? JsonSerializer.Serialize(body) : "{}";
                using (var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json"))
                {
                    response = await client.PostAsync(apiUrl, content, cancellationToken);
                }
                break;
            case "PUT":
                var putBody = body != null ? JsonSerializer.Serialize(body) : "{}";
                using (var putContent = new StringContent(putBody, System.Text.Encoding.UTF8, "application/json"))
                {
                    response = await client.PutAsync(apiUrl, putContent, cancellationToken);
                }
                break;
            case "DELETE":
                response = await client.DeleteAsync(apiUrl, cancellationToken);
                break;
            case "GET":
            default:
                response = await client.GetAsync(apiUrl, cancellationToken);
                break;
        }

        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = ParseApiResponse(responseContent);
        
        foreach (var item in data)
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<Dictionary<string, object?>> QueryPagedAsync(
        string apiUrl,
        string? pageParamName = "page",
        string? pageSizeParamName = "pageSize",
        int pageSize = 100,
        string? method = "GET",
        Dictionary<string, string?>? headers = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(headers);
        
        int currentPage = 1;
        bool hasMoreData = true;
        
        while (hasMoreData && !cancellationToken.IsCancellationRequested)
        {
            // 构建带分页参数的 URL
            var separator = apiUrl.Contains('?') ? "&" : "?";
            var pagedUrl = $"{apiUrl}{separator}{pageParamName}={currentPage}&{pageSizeParamName}={pageSize}";
            
            HttpResponseMessage response;
            
            if (method?.ToUpperInvariant() == "POST")
            {
                var body = new { page = currentPage, pageSize };
                var jsonBody = JsonSerializer.Serialize(body);
                using var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
                response = await client.PostAsync(apiUrl, content, cancellationToken);
            }
            else
            {
                response = await client.GetAsync(pagedUrl, cancellationToken);
            }
            
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = ParseApiResponse(responseContent);
            
            var items = data.ToList();
            
            if (items.Count == 0)
            {
                hasMoreData = false;
            }
            else
            {
                foreach (var item in items)
                {
                    yield return item;
                }
                
                currentPage++;
                
                // 如果返回的数据少于请求的数量，说明没有更多数据了
                if (items.Count < pageSize)
                {
                    hasMoreData = false;
                }
            }
            
            _logger.LogDebug("API分页查询: 第{Page}页, 返回{Count}条记录", currentPage - 1, items.Count);
        }
    }

    private HttpClient CreateClient(Dictionary<string, string?>? headers)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);
        
        if (headers != null)
        {
            foreach (var header in headers.Where(h => !string.IsNullOrEmpty(h.Value)))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        
        return client;
    }

    private List<Dictionary<string, object?>> ParseApiResponse(string jsonContent)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            
            // 尝试解析常见的 API 响应格式
            // 1. 直接数组: [{...}, {...}]
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return ParseJsonArray(document.RootElement);
            }
            
            // 2. 包装对象: { "data": [{...}], "items": [...], "result": [...] }
            var root = document.RootElement;
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array &&
                    (property.NameEquals("data") || 
                     property.NameEquals("items") || 
                     property.NameEquals("result") ||
                     property.NameEquals("records") ||
                     property.NameEquals("list")))
                {
                    return ParseJsonArray(property.Value);
                }
            }
            
            // 3. 如果根对象是单个对象，包装成数组
            if (root.ValueKind == JsonValueKind.Object)
            {
                return new List<Dictionary<string, object?>> { ParseJsonObject(root) };
            }
            
            return new List<Dictionary<string, object?>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析API响应失败");
            throw new InvalidOperationException("无法解析API响应", ex);
        }
    }

    private List<Dictionary<string, object?>> ParseJsonArray(JsonElement arrayElement)
    {
        var result = new List<Dictionary<string, object?>>();
        
        foreach (var element in arrayElement.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                result.Add(ParseJsonObject(element));
            }
        }
        
        return result;
    }

    private Dictionary<string, object?> ParseJsonObject(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElement(property.Value);
        }
        
        return dict;
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) 
                ? longVal 
                : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ParseJsonObject(element),
            JsonValueKind.Array => ParseJsonArray(element),
            _ => element.ToString()
        };
    }
}
