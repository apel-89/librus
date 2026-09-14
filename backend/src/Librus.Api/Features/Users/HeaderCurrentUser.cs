using Librus.Api.Domain;

namespace Librus.Api.Features.Users;


public sealed class HeaderCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string HeaderName = "X-User-Id";
    private const int FallbackUserId = 1;

    public int Id
    {
        get
        {
            var header = accessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();
            return int.TryParse(header, out var id) && id > 0 ? id : FallbackUserId;
        }
    }
}