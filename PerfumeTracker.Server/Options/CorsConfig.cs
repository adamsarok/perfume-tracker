using System.ComponentModel.DataAnnotations;

namespace PerfumeTracker.Server.Options;

public class CorsConfiguration {
	[Required]
	[MinLength(1, ErrorMessage = "At least one allowed origin must be configured")]
	public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}