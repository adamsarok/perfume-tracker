import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Sparkles } from 'lucide-react';
import { getDigest, getDigestHistory } from '@/services/weekly-digest-service';
import { Button } from '@/components/ui/button';
import { useUserStore } from '@/stores/user-store';

const categoryNames: Record<string, string> = {
  ForgottenFavorite: 'Forgotten favorites', SimilarToLastUsed: 'Inspired by recent wears',
  Seasonal: 'For the season', Random: 'A little surprise', LeastUsed: 'Underused gems',
  LongestTimeAgo: 'Time to revisit', MoodOrOccasion: 'This week’s mood',
};
const weekLabel = (date: string) => `Week of ${new Date(date).toLocaleDateString(undefined, { timeZone: 'UTC', year: 'numeric', month: 'short', day: 'numeric' })}`;

export default function WeeklyDigestPage() {
	const userId = useUserStore(state => state.user?.id);
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<string | null>(null);
  const history = useQuery({ queryKey: ['weekly-digests', userId, page], queryFn: () => getDigestHistory(page), enabled: !!userId });
  const digestId = selected ?? history.data?.items[0]?.id;
  const detail = useQuery({ queryKey: ['weekly-digest', userId, digestId], queryFn: () => getDigest(digestId!), enabled: !!userId && !!digestId });

  return <main className="mx-auto max-w-5xl space-y-6 px-4 py-8">
    <header>
      <h1 className="flex items-center gap-2 text-3xl font-semibold"><Sparkles aria-hidden="true" /> Your weekly fragrance digest</h1>
      <p className="mt-2 text-muted-foreground">Fresh inspiration for enjoying your collection.</p>
    </header>
    {history.isPending && <p role="status">Loading digests…</p>}
    {history.isError && <div role="alert"><p>{history.error.message}</p><Button className="mt-2" onClick={() => void history.refetch()}>Try again</Button></div>}
    {history.data && history.data.total === 0 && <p className="rounded-xl border p-6 text-muted-foreground">Your weekly digests will appear here when available.</p>}
    {history.data && history.data.total > 0 && <div className="flex flex-wrap items-center gap-3">
      <label htmlFor="digest-week">Choose a week</label>
      <select id="digest-week" className="rounded-md border bg-background p-2" value={digestId ?? ''} onChange={event => setSelected(event.target.value)}>
        {history.data.items.map(item => <option key={item.id} value={item.id}>{weekLabel(item.weekStart)}</option>)}
      </select>
      <Button variant="outline" disabled={page === 1} onClick={() => { setSelected(null); setPage(page - 1); }}>Newer weeks</Button>
      <Button variant="outline" disabled={page * 20 >= history.data.total} onClick={() => { setSelected(null); setPage(page + 1); }}>Older weeks</Button>
    </div>}
    {digestId && detail.isPending && <p role="status">Loading this week’s inspiration…</p>}
    {detail.isError && <div role="alert"><p>{detail.error.message}</p><Button className="mt-2" onClick={() => void detail.refetch()}>Try again</Button></div>}
    {detail.data && <article className="space-y-6">
      <div className="rounded-xl border bg-muted/30 p-6">
        <h2 className="text-xl font-semibold">{weekLabel(detail.data.weekStart)}</h2>
        <p className="mt-3 whitespace-pre-line leading-relaxed">{detail.data.digest.introduction}</p>
        <p className="mt-3 text-sm font-medium">This week’s mood: {detail.data.digest.moodOrOccasion}</p>
      </div>
      <div className="grid grid-cols-1 gap-5">
        {detail.data.digest.categories.map(category => <section key={category.strategy} className="rounded-xl border p-5">
          <h3 className="text-lg font-semibold">{categoryNames[category.strategy] ?? category.strategy}</h3>
          <p className="mt-2 text-sm text-muted-foreground">{category.summary}</p>
          <ol className="mt-4 list-decimal space-y-4 pl-5">
            {category.picks.map(pick => <li key={pick.perfumeId}>
              {pick.isAvailable
                ? <Link className="font-medium underline underline-offset-4" to="/perfumes/$id" params={{ id: pick.perfumeId }}>{pick.house} — {pick.perfumeName}</Link>
                : <span className="font-medium">{pick.house} — {pick.perfumeName}</span>}
              <p className="mt-1 text-sm leading-relaxed">{pick.reason}</p>
            </li>)}
          </ol>
          {category.shortageReason && <p className="mt-4 text-sm text-muted-foreground">{category.shortageReason}</p>}
        </section>)}
      </div>
    </article>}
  </main>;
}
