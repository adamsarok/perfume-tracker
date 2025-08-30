# TODO

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


- [ ] 44-49: Dispose HttpResponseMessage and propagate cancellation (and set a content type)

Prevent socket/resource leaks, make the request cancelable, and send a sane content type.

-    var httpClient = httpClientFactory.CreateClient();
-    var response = await httpClient.PutAsync(presignedUrl, new StreamContent(stream));
-    if (!response.IsSuccessStatusCode) {
+    var httpClient = httpClientFactory.CreateClient();
+    using var content = new StreamContent(stream);
+    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
+    using var response = await httpClient.PutAsync(presignedUrl, content, ct);
+    if (!response.IsSuccessStatusCode) {
       throw new InvalidOperationException($"Failed to upload image to R2: {response.StatusCode}");
     }
And in the endpoint (lines 10–25) add and wire a CancellationToken:

- app.MapPut("/api/images/upload/{perfumeId}", async (Guid perfumeId,
+ app.MapPut("/api/images/upload/{perfumeId}", async (Guid perfumeId,
   IFormFile file,
   R2Configuration configuration,
   PerfumeTrackerContext perfumeTrackerContext,
-  UploadImageHandler uploadImageHandler) => {
+  UploadImageHandler uploadImageHandler,
+  CancellationToken ct) => {
     ...
-    await using var stream = file.OpenReadStream();
-    perfume.ImageObjectKeyNew = await uploadImageHandler.UploadImage(stream);
-    await perfumeTrackerContext.SaveChangesAsync();
+    await using var stream = file.OpenReadStream();
+    perfume.ImageObjectKeyNew = await uploadImageHandler.UploadImage(stream, ct);
+    await perfumeTrackerContext.SaveChangesAsync(ct);

- [ ] 35-41: Guard stream reset and add CancellationToken to UploadImage
In PerfumeTracker.Server/Features/R2/UploadImage.cs, wrap the stream.Position = 0; call in a if (stream.CanSeek) check—falling back to copying into a seekable MemoryStream when CanSeek is false—and update the signature to

public async Task<Guid> UploadImage(Stream stream, CancellationToken ct = default)
then pass ct from your endpoint into UploadImage. Verified no other UploadImage call sites exist; the default ct parameter preserves existing callers.

- [ ] 7-15: Add CancellationToken support across the upload pipeline

Change SeedDemoImagesAsync signature to
Task<List<Guid>> SeedDemoImagesAsync(UploadImageHandler handler, CancellationToken ct = default) and pass ct into handler.UploadImage.
Update UploadImageHandler.UploadImage to
Task<Guid> UploadImage(Stream stream, CancellationToken ct)
and propagate ct through its internal HTTP/file‐upload calls.
Update all call sites (e.g. in Program.cs and Features/R2/UploadImage.cs) from
handler.UploadImage(stream) → handler.UploadImage(stream, ct)

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