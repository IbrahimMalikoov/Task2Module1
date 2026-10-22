using System.Text.Json;
using UniversityClient.Models;

namespace UniversityClient.Services;

public class CourseApiClient : ICourseApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CourseApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CourseApiClient(IHttpClientFactory httpClientFactory, ILogger<CourseApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ClientResponse<IEnumerable<CourseViewModel>>> GetCoursesAsync(string? search = null)
    {
        var client = _httpClientFactory.CreateClient("UniversityApi");
        var url = string.IsNullOrWhiteSpace(search)
            ? "/api/v2/courses"
            : $"/api/v2/courses?search={Uri.EscapeDataString(search)}";

        try
        {
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResult = TryParseApiResult<IEnumerable<CourseViewModel>>(content);
                var message = errorResult?.ErrorMessage ?? $"API вернул статус {(int)response.StatusCode} ({response.StatusCode})";
                return new ClientResponse<IEnumerable<CourseViewModel>>
                {
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = message
                };
            }

            var apiResult = JsonSerializer.Deserialize<ApiResult<IEnumerable<CourseViewModel>>>(content, _jsonOptions);
            return new ClientResponse<IEnumerable<CourseViewModel>>
            {
                Success = true,
                StatusCode = (int)response.StatusCode,
                Data = apiResult?.Result ?? Enumerable.Empty<CourseViewModel>()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API connection failure");
            return new ClientResponse<IEnumerable<CourseViewModel>>
            {
                Success = false,
                ErrorMessage = $"Не удалось подключиться к University API ({client.BaseAddress}). Проверьте, запущен ли API-сервер."
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "API request timeout");
            return new ClientResponse<IEnumerable<CourseViewModel>>
            {
                Success = false,
                ErrorMessage = "Время ожидания ответа от University API истекло."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling API");
            return new ClientResponse<IEnumerable<CourseViewModel>>
            {
                Success = false,
                ErrorMessage = $"Произошла непредвиденная ошибка: {ex.Message}"
            };
        }
    }

    public async Task<ClientResponse<CourseViewModel>> GetCourseByIdAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("UniversityApi");
        var url = $"/api/v2/courses/{id}";

        try
        {
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResult = TryParseApiResult<CourseViewModel>(content);
                var message = errorResult?.ErrorMessage ?? $"API вернул статус {(int)response.StatusCode} ({response.StatusCode})";
                return new ClientResponse<CourseViewModel>
                {
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = message
                };
            }

            var apiResult = JsonSerializer.Deserialize<ApiResult<CourseViewModel>>(content, _jsonOptions);
            return new ClientResponse<CourseViewModel>
            {
                Success = true,
                StatusCode = (int)response.StatusCode,
                Data = apiResult?.Result
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API connection failure");
            return new ClientResponse<CourseViewModel>
            {
                Success = false,
                ErrorMessage = $"Не удалось подключиться к University API ({client.BaseAddress}). Проверьте, запущен ли API-сервер."
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "API request timeout");
            return new ClientResponse<CourseViewModel>
            {
                Success = false,
                ErrorMessage = "Время ожидания ответа от University API истекло."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling API");
            return new ClientResponse<CourseViewModel>
            {
                Success = false,
                ErrorMessage = $"Произошла непредвиденная ошибка: {ex.Message}"
            };
        }
    }

    private ApiResult<T>? TryParseApiResult<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ApiResult<T>>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
