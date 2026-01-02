# TODO
- [ ] EmbeddingRetryService should handle updates via outbox pattern
- [ ] Tests structure should reflect project structure
- [x] Prompt or occasion based recommendation - user provides occasion text, get perfume style from LLM, search vector based on perfume style
- [ ] Completion is slow - add progress spinner
- [ ] Enable DB search for update, change identify to LLM search
- [ ] Add Family to embedding
- [ ] Tenant query filter is becoming painful for background services. IgnoreQueryfilters is needed which then ignores deleted filter as well - implement with extension instead:

public static class QueryExtensions {
    public static IQueryable<T> ForCurrentUser<T>(this IQueryable<T> query, ITenantProvider tenant) 
        where T : IUserOwned => 
        query.Where(e => e.UserId == tenant.GetUserId());
}