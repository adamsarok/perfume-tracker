using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PerfumeTracker.Server.Options;

public class JwtConfiguration {
	[Required]
	public string Key { get; set; } = string.Empty;

	[Required]
	public string Issuer { get; set; } = string.Empty;

	[Required]
	public string Audience { get; set; } = string.Empty;

	[Range(1, int.MaxValue)]
	public int ExpirationHours { get; set; }

	public SymmetricSecurityKey GetSecurityKey() => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
}