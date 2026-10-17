using System.Security.Cryptography;
using System.Text;

namespace PerfumeTracker.Evals.Recording;

public static class Fingerprints {
	/// <summary>Stable short hash used to namespace recordings by prompt + tool shape.</summary>
	public static string Compute(string value) =>
		Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
}
