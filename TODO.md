# TODO

- [ ] maintenance - clean old outboxmessages messages, move retryCount > 5 to dead letter queue

## Achievements

- [ ] ABC - collect all houses, perfumenames, tags etc. for all ABC letters
- [ ] //[Fact] TODO: fix flaky test
	//public async Task SideEffectQueue_CanEnqueue() {
	//	var outboxSeed = await PrepareData();
	//	using var scope = GetTestScope();
	//	var channel = scope.ServiceScope.ServiceProvider.GetRequiredService<ISideEffectQueue>();
	//	channel.Enqueue(outboxSeed[0]);
	//	using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
	//	Assert.True(await channel.Reader.WaitToReadAsync(cts.Token));
	//}
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
- [ ] 33-37: Don’t set multipart/form-data header manually — it drops the boundary and can break uploads

When sending FormData, the browser/Axios will set the Content-Type with the correct boundary. Manually forcing 'multipart/form-data' typically omits the boundary and can cause server-side parsers to fail. Remove the headers override here.

Apply this diff:

      const response = await put<UploadResponse>(qry, formData, {
       headers: {
          'Content-Type': 'multipart/form-data',
      },
     });
     const response = await put<UploadResponse>(qry, formData);

- [ ] 33-37: Remove Manual Multipart/Form-Data Header and Audit Error Toasts

We’ve confirmed via rg that this is the only hard-coded Content-Type: multipart/form-data in the repo—remove it here to let your HTTP client set the correct boundary automatically and prevent sporadic upload failures.

• In perfumetracker.client/src/components/upload-component.tsx at line 35, delete the explicit header:

 headers: {
   'Content-Type': 'multipart/form-data',
 },

 - [ ] 88-96: Fix direct state mutation and use controlled checkboxes

onCheckedChange={(checked) => (localSettings.showFemalePerfumes = ... )} mutates state directly and uses defaultChecked. This won’t trigger re-renders and can desync UI. Use controlled checked and setLocalSettings updates.

-            <Checkbox
-              id="female"
-              defaultChecked={localSettings.showFemalePerfumes}
-              onCheckedChange={(checked) =>
-                (localSettings.showFemalePerfumes = checked as boolean)
-              }
-            >
-              Female
-            </Checkbox>
+            <Checkbox
+              id="female"
+              checked={localSettings.showFemalePerfumes}
+              onCheckedChange={(checked) =>
+                setLocalSettings((s) => ({ ...s!, showFemalePerfumes: checked === true }))
+              }
+            />
@@
-            <Checkbox
-              id="unisex"
-              defaultChecked={localSettings.showUnisexPerfumes}
-              onCheckedChange={(checked) =>
-                (localSettings.showUnisexPerfumes = checked as boolean)
-              }
-            >
-              Unisex
-            </Checkbox>
+            <Checkbox
+              id="unisex"
+              checked={localSettings.showUnisexPerfumes}
+              onCheckedChange={(checked) =>
+                setLocalSettings((s) => ({ ...s!, showUnisexPerfumes: checked === true }))
+              }
+            />
@@
-            <Checkbox
-              id="male"
-              defaultChecked={localSettings.showMalePerfumes}
-              onCheckedChange={(checked) =>
-                (localSettings.showMalePerfumes = checked as boolean)
-              }
-            >
-              Male
-            </Checkbox>
+            <Checkbox
+              id="male"
+              checked={localSettings.showMalePerfumes}
+              onCheckedChange={(checked) =>
+                setLocalSettings((s) => ({ ...s!, showMalePerfumes: checked === true }))
+              }
+            />
Note: if your Checkbox supports an indeterminate state, checked === true keeps types safe.

Also applies to: 105-113, 122-130

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

- [ ] Guard against divide-by-zero/NaN in XP progress calculation.

If xpNextLevel === xpLastLevel, this yields Infinity/NaN. Clamp to [0,100] and guard denominator.

-              <Progress
-                value={
-                  ((xp.xp - xp.xpLastLevel) /
-                    (xp.xpNextLevel - xp.xpLastLevel)) *
-                  100
-                }
-              />
+              <Progress
+                value={
+                  (xp.xpNextLevel - xp.xpLastLevel) > 0
+                    ? Math.min(
+                        100,
+                        Math.max(
+                          0,
+                          ((xp.xp - xp.xpLastLevel) /
+                            (xp.xpNextLevel - xp.xpLastLevel)) * 100
+                        )
+                      )
+                    : 0
+                }
+              />

- [ ] Do not mutate React state directly; update via setPerfume.

Directly assigning perfume.perfume.imageUrl mutates state outside setState, risking stale renders and hard-to-debug issues.

-      const result = await get<string>(qryPresigned);
-      if (result.error) {
+      const result = await get<string>(qryPresigned);
+      if (result.error) {
         showError("Could not get presigned url", result.error);
         return;
       }
-      perfume.perfume.imageUrl = result.data ?? "";
-      setImageUrl(perfume.perfume.imageUrl);
+      setPerfume(prev =>
+        prev
+          ? {
+              ...prev,
+              perfume: { ...prev.perfume, imageUrl: result.data ?? "" },
+            }
+          : prev
+      );
+      setImageUrl(result.data ?? "");

- [ ] perfumetracker.client/src/components/message-box.tsx (1)
72-82: Fix nested button: add asChild to DialogClose

Without asChild, DialogClose renders a button element, causing a <button><button/></button> nesting around your secondary Button. This is invalid HTML and can break keyboard focus and click handling.

Apply this diff:

-                <DialogClose>
+                <DialogClose asChild>
                   <Button
                     color="danger"
                     type="button"
                     onClick={() => {
                       if (onButton2) onButton2();
                     }}
                   >
                     {button2text}
                   </Button>
                   </DialogClose>