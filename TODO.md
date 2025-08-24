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