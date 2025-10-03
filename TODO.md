# TODO
- [ ] SameSiteMode.None, HttpOnly = false -> 
- [ ] Critical: Email-only authentication creates security vulnerability.

The current implementation authenticates users solely by matching the GitHub email to a local account email. This creates a serious security issue:

Account takeover risk: If a user has both a local account and a GitHub account with the same email, they can authenticate to the local account via GitHub without proving ownership of that local account.
No GitHub account linking: There's no record that this local account is linked to a specific GitHub account (GitHub's NameIdentifier claim).
Email verification bypass: If local accounts require email verification but GitHub emails don't, this provides an unauthenticated path.
Recommended solution: Implement proper external login account linking:

 			var claims = result.Principal.Identities.First().Claims;

+			var githubUserId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
+			if (string.IsNullOrWhiteSpace(githubUserId))
+				return Results.Unauthorized();
+
 			var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
 			if (string.IsNullOrWhiteSpace(email)) return Results.Unauthorized();

+			// Check if this GitHub account is already linked to a user
+			var loginInfo = new UserLoginInfo("GitHub", githubUserId, "GitHub");
+			var user = await userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
+			
+			if (user == null) {
+				// Check if a user with this email exists
-				var user = await userManager.FindByEmailAsync(email);
-				if (user == null) return Results.Unauthorized();
+				user = await userManager.FindByEmailAsync(email);
+				if (user == null) return Results.Unauthorized();
+				
+				// Link the GitHub account to the existing user
+				var result = await userManager.AddLoginAsync(user, loginInfo);
+				if (!result.Succeeded) return Results.Unauthorized();
+			}

 			await jwtTokenGenerator.WriteToken(user, ctx);
This approach:

Uses GitHub's NameIdentifier (unique user ID) to identify the GitHub account
Checks if the GitHub account is already linked via FindByLoginAsync
Only links to an existing email-matched account if no link exists
Stores the link via AddLoginAsync for future authentication
- [ ] Login failed: Network Error is shown on ratelimit
- [ ] maintenance - clean old outboxmessages messages, move retryCount > 5 to dead letter queue

## Achievements

- [ ] ABC - collect all houses, perfumenames, tags etc. for all ABC letters
- [ ] Fix flaky test:
   ```csharp
 //[Fact] TODO: fix flaky test
	//public async Task SideEffectQueue_CanEnqueue() {
	//	var outboxSeed = await PrepareData();
	//	using var scope = GetTestScope();
	//	var channel = scope.ServiceScope.ServiceProvider.GetRequiredService<ISideEffectQueue>();
	//	channel.Enqueue(outboxSeed[0]);
	//	using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
	//	Assert.True(await channel.Reader.WaitToReadAsync(cts.Token));
	//}
    ```
- [ ] Map unique-violation to a domain/user-friendly error

With the new unique indexes, attempts to add duplicates will surface as DbUpdateException (Postgres 23505). Consider translating this to a validation error (e.g., 409/422) rather than a 500. Sketch:

try {
    // existing logic...
    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
} catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex && pex.SqlState == PostgresErrorCodes.UniqueViolation) {
    // Map to your error contract, e.g. ValidationException with a message on Dto.PerfumeName or a specific DuplicatePerfumeException
    throw new DuplicatePerfumeException(request.Dto.House, request.Dto.PerfumeName, pex);
} 

- [ ] Don’t treat empty string as an error; fix brittle substring/unescape.

Empty AI responses ('') are currently treated as failures due to !result.data.
substring(1, result.data.length - 2) uses the original length after replacements, which can truncate incorrectly.
Prefer a safe JSON-parse when the payload is a JSON-encoded string; otherwise, best-effort unescape and trim code fences.
Use this resilient parsing:

-        if (result.error || !result.data) {
+        if (!result.ok || result.data === undefined) {
           showError("Failed to get AI recommendations", result.error ?? "unknown error");
           return;
         }
-        const cleaned = result.data.replace(/\\n/g, '\n').substring(1, result.data.length - 2);
-        setRecommendations(cleaned);
+        let text = result.data ?? '';
+        try {
+          // If backend returns a JSON-encoded string, unescape it.
+          if ((text.startsWith('"') && text.endsWith('"')) || text.includes('\\n')) {
+            text = JSON.parse(text);
+          }
+        } catch {
+          // Fallback: best-effort newline unescape
+          text = text.replace(/\\n/g, '\n');
+        }
+        // Trim common surrounding code fences/backticks
+        text = text.trim().replace(/^```(?:\w+)?\n?/, '').replace(/```$/, '');
+        setRecommendations(text);


- [ ] 20-26: Prefer result.ok and add a simple submitting guard

Use the AxiosResult.ok flag for consistency across the app and prevent double submits with a local submitting state.

Apply:

   const handleLogin = async (e: React.FormEvent) => {
     e.preventDefault();
     setError("");
-    const result = await loginUser(email.trim(), password);
-    if (result.error || !result.data) {
+    const result = await loginUser(email.trim(), password);
+    if (!result.ok || !result.data) {
       setError("Login failed: " + (result.error ?? "unknown error"));
     } else {
       router.push("/"); // Redirect to home page after successful login
     }
   };
Optionally:

+  const [submitting, setSubmitting] = useState(false);
   const handleLogin = async (e: React.FormEvent) => {
     e.preventDefault();
-    setError("");
+    if (submitting) return;
+    setSubmitting(true);
+    setError("");
     const result = await loginUser(email.trim(), password);
-    if (result.error || !result.data) {
+    if (!result.ok || !result.data) {
       setError("Login failed: " + (result.error ?? "unknown error"));
     } else {
       router.push("/");
     }
+    setSubmitting(false);
   };