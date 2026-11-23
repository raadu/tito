using System.ComponentModel.DataAnnotations;

namespace Tito.Api.Configuration;

/// <summary>Bound from the "Company" section of configuration.</summary>
public sealed class CompanyOptions
{
    public const string Section = "Company";

    /// <summary>IANA time zone id, e.g. "Europe/Stockholm". Decides what "today" means.</summary>
    [Required]
    public string TimeZone { get; set; } = "";
}

/// <summary>Bound from "Cors": which browser origins may call the API.</summary>
public sealed class CorsOptions
{
    public const string Section = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>Bound from "RateLimit": protects the write endpoints from spam.</summary>
public sealed class RateLimitOptions
{
    public const string Section = "RateLimit";

    [Range(1, 10_000)]
    public int WritesPerMinute { get; set; } = 20;
}
