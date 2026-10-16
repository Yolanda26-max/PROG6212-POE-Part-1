using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class ApiTests : IDisposable
{
    private const string Password = "Password123!";

    private readonly RaceDayFactory _factory = new();
    private readonly HttpClient _anon; // a client with no login

    public ApiTests()
    {
        _anon = _factory.CreateClient();
    }

    public void Dispose()
    {
        _anon.Dispose();
        _factory.Dispose();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private string OrganiserCode =>
        _factory.Services.GetRequiredService<IConfiguration>()["OrganiserRegistration:Code"]!;

    /// <summary>Registers a new user, logs in, and returns a client carrying their JWT.</summary>
    private async Task<HttpClient> CreateClientForAsync(string role)
    {
        var email = $"{role.ToLower()}.{Guid.NewGuid():N}@example.com";

        var register = await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = Password,
            Role = role,
            OrganiserCode = role == "Organiser" ? OrganiserCode : null
        });
        register.EnsureSuccessStatusCode();

        var login = await _anon.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private static EventRequest NewEvent(string name = "Pretoria 10km Fun Run") => new()
    {
        EventName = name,
        EventDate = DateTime.UtcNow.Date.AddDays(30),
        Location = "Pretoria, Gauteng",
        Description = "A friendly road run.",
        EventType = "Run",
        DistanceKm = 10
    };

    private static async Task<EventResponse> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEvent());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CategoryRequest { CategoryName = "10km Fun Run", DistanceKm = 10, Price = 150 });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    private static async Task<EnrolmentResponse> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }

    // ------------------------------------------------------------------
    // 1. User registration and login
    // ------------------------------------------------------------------

    [Fact]
    public async Task Register_Participant_ReturnsCreated()
    {
        var response = await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Sipho Test",
            Email = "sipho.test@example.com",
            Password = Password,
            Role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.True(body!.UserId > 0);
    }

    [Fact]
    public async Task Register_Organiser_WithCorrectCode_ReturnsCreated()
    {
        var response = await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Thabo Test",
            Email = "thabo.test@example.com",
            Password = Password,
            Role = "Organiser",
            OrganiserCode = OrganiserCode
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_ReturnsForbidden()
    {
        var response = await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Sneaky User",
            Email = "sneaky@example.com",
            Password = Password,
            Role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var response = await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Wrong Role",
            Email = "wrong.role@example.com",
            Password = Password,
            Role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var request = new RegisterRequest
        {
            FullName = "Dup User",
            Email = "dup.user@example.com",
            Password = Password,
            Role = "Participant"
        };

        var first = await _anon.PostAsJsonAsync("/api/auth/register", request);
        var second = await _anon.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainPassword()
    {
        await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Hash Test",
            Email = "hash.test@example.com",
            Password = Password,
            Role = "Participant"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "hash.test@example.com");

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash); // BCrypt hashes start with $2
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndRole()
    {
        await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Login Test",
            Email = "login.test@example.com",
            Password = Password,
            Role = "Participant"
        });

        var response = await _anon.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "login.test@example.com", Password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal("Participant", auth.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await _anon.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Wrong Pass",
            Email = "wrong.pass@example.com",
            Password = Password,
            Role = "Participant"
        });

        var response = await _anon.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "wrong.pass@example.com", Password = "NotTheRightOne1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // 2. Authenticated vs unauthenticated requests
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _anon.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithToken_ReturnsOwnProfile()
    {
        var participant = await CreateClientForAsync("Participant");

        var response = await participant.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal("Participant", profile!.Role);
    }

    [Fact]
    public async Task GetEvents_WithoutToken_ReturnsOk()
    {
        // Browsing events is public
        var response = await _anon.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // 3. Event management by Organisers
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var organiser = await CreateClientForAsync("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.True(created!.EventId > 0);
        Assert.Equal("Run", created.EventType);
        Assert.Equal(10, created.DistanceKm);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var request = NewEvent();
        request.EventType = "Swim";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsOwner_ReturnsOk()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var ev = await CreateEventAsync(organiser);
        var update = NewEvent("Renamed Event");

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Event", updated!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_AsDifferentOrganiser_ReturnsForbidden()
    {
        var owner = await CreateClientForAsync("Organiser");
        var otherOrganiser = await CreateClientForAsync("Organiser");
        var ev = await CreateEventAsync(owner);

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{ev.EventId}", NewEvent("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwner_ReturnsNoContent_ThenEventIsGone()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var ev = await CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        var afterwards = await _anon.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    // ------------------------------------------------------------------
    // 4. Role-based access: the wrong role is rejected
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var participant = await CreateClientForAsync("Participant");

        var response = await participant.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _anon.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewEventEnrolments_AsParticipant_ReturnsForbidden()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var participant = await CreateClientForAsync("Participant");
        var ev = await CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // 5. Event enrolments are recorded correctly
    // ------------------------------------------------------------------

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreated_AndLinksParticipantEventAndCategory()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var participant = await CreateClientForAsync("Participant");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(category.CategoryId, enrolment!.CategoryId);
        Assert.Equal(ev.EventId, enrolment.EventId);
        Assert.Equal("Confirmed", enrolment.Status);

        // The enrolment is really saved against the logged-in participant
        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/users/me/enrolments");
        var saved = Assert.Single(mine!);
        Assert.Equal(enrolment.EnrolmentId, saved.EnrolmentId);
        Assert.Equal(enrolment.ParticipantId, saved.ParticipantId);
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_ReturnsConflict()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var participant = await CreateClientForAsync("Participant");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        await EnrolAsync(participant, category.CategoryId);
        var second = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_NonExistentCategory_ReturnsNotFound()
    {
        var participant = await CreateClientForAsync("Participant");

        var response = await participant.PostAsync("/api/categories/99999/enrol", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var participant = await CreateClientForAsync("Participant");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Contains(list!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    // ------------------------------------------------------------------
    // 6. Results
    // ------------------------------------------------------------------

    [Fact]
    public async Task CaptureResult_AsOrganiser_ParticipantCanViewOwnResult()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var participant = await CreateClientForAsync("Participant");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            new ResultRequest { FinishTime = new TimeSpan(0, 52, 10), Position = 5, Status = "Finished" });

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<ResultResponse>>("/api/users/me/results");
        var result = Assert.Single(mine!);
        Assert.Equal(5, result.Position);
        Assert.Equal("Finished", result.Status);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_ReturnsForbidden()
    {
        var organiser = await CreateClientForAsync("Organiser");
        var participant = await CreateClientForAsync("Participant");
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            new ResultRequest { FinishTime = new TimeSpan(0, 40, 0), Position = 1, Status = "Finished" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}