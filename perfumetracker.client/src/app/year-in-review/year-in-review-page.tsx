import { useCallback, useEffect, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { ArrowLeft, ArrowRight, CalendarDays, Droplets, RotateCcw, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { showError } from "@/services/toasty-service";
import { getYearInReview, ReviewRankedItem, YearInReviewCategory, YearInReviewStats } from "./year-in-review-stats";

function Ranking({ items }: { items: ReviewRankedItem[] }) {
  return <div className="mt-7 w-full space-y-3 text-left">
    {items.map((item, index) => <div key={`${item.name}-${index}`} className="flex items-center gap-3 rounded-2xl border bg-muted/50 p-3">
      <span className="w-7 text-2xl font-black text-muted-foreground">{index + 1}</span>
      {item.imageUrl && <img src={item.imageUrl} alt="" className="h-12 w-12 rounded-xl object-cover" />}
      {item.color && <span className="h-9 w-9 rounded-full border-2 border-background" style={{ backgroundColor: item.color }} />}
      <div className="min-w-0 flex-1">
        <p className="truncate font-bold">{item.name}</p>
        {item.subtitle && <p className="truncate text-sm text-muted-foreground">{item.subtitle}</p>}
      </div>
      <span className="text-sm font-semibold text-muted-foreground">{item.count} wears</span>
    </div>)}
  </div>;
}

function Category({ category }: { category: YearInReviewCategory }) {
  return <div>
    <Sparkles className="mx-auto mb-5 h-10 w-10" />
    <p className="text-sm font-bold uppercase tracking-[0.3em] text-muted-foreground">{category.title}</p>
    {category.note && <p className="mt-5 text-5xl font-black">{category.note}</p>}
    {category.imageUrl && <img src={category.imageUrl} alt="" className="mx-auto mt-6 h-32 w-32 rounded-3xl object-cover shadow-lg" />}
    {category.perfumeName && <h2 className="mt-5 text-4xl font-black">{category.perfumeName}</h2>}
    {category.house && <p className="mt-1 text-lg text-muted-foreground">{category.house}</p>}
    {category.ratingFrom != null && category.ratingTo != null &&
      <p className="mt-5 text-3xl font-black">{category.ratingFrom.toFixed(1)} → {category.ratingTo.toFixed(1)}</p>}
    <p className="mx-auto mt-5 max-w-sm text-lg text-muted-foreground">{category.detail}</p>
  </div>;
}

export default function YearInReviewPage() {
	const [stats, setStats] = useState<YearInReviewStats | null>(null);
	const year = stats?.year ?? new Date().getFullYear() - 1;
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(0);
  const touchStart = useRef<number | null>(null);

  useEffect(() => {
		getYearInReview()
      .then(setStats)
      .catch((error) => showError("Could not create your year in review", error))
      .finally(() => setLoading(false));
	}, []);

  const pages = stats ? [
    <div><Sparkles className="mx-auto mb-6 h-12 w-12" /><p className="text-sm font-bold uppercase tracking-[0.35em] text-muted-foreground">Your scent story</p><h1 className="mt-4 text-6xl font-black leading-none">Your {year}<br />in review</h1><p className="mt-6 text-lg text-muted-foreground">A year told one spray at a time.</p></div>,
    <div><Droplets className="mx-auto mb-5 h-10 w-10" /><p className="text-lg font-semibold text-muted-foreground">You logged</p><p className="my-2 text-8xl font-black">{stats.totalWears}</p><p className="text-2xl font-bold">perfume wears</p><p className="mt-5 text-muted-foreground">across {stats.activeDays} scent-filled days</p></div>,
    <div><p className="text-sm font-bold uppercase tracking-[0.3em] text-muted-foreground">Your top scents</p><h2 className="mt-3 text-4xl font-black">The ones you kept coming back to</h2><Ranking items={stats.topPerfumes} /></div>,
    <div><p className="text-sm font-bold uppercase tracking-[0.3em] text-muted-foreground">House favorites</p><h2 className="mt-3 text-4xl font-black">Your most-worn houses</h2><Ranking items={stats.topHouses} /></div>,
    <div><p className="text-sm font-bold uppercase tracking-[0.3em] text-muted-foreground">Your scent profile</p><h2 className="mt-3 text-4xl font-black">Notes that defined your year</h2>{stats.topTags.length ? <Ranking items={stats.topTags} /> : <p className="mt-8 text-xl text-muted-foreground">Add tags to your perfumes to reveal your scent profile.</p>}</div>,
    ...stats.categories.map((category) => <Category key={category.key} category={category} />),
    <div><CalendarDays className="mx-auto mb-5 h-10 w-10" /><p className="text-sm font-bold uppercase tracking-[0.3em] text-muted-foreground">Your year in a bottle</p><p className="mt-6 text-6xl font-black">{stats.uniquePerfumes}</p><p className="text-xl">different perfumes</p><p className="mt-6 text-6xl font-black">{stats.uniqueHouses}</p><p className="text-xl">houses explored</p><p className="mt-7 text-muted-foreground">Peak month: <strong className="text-foreground">{stats.busiestMonth?.name ?? "—"}</strong> · Favorite day: <strong className="text-foreground">{stats.busiestDay?.name ?? "—"}</strong></p></div>,
  ] : [];

  const move = useCallback((direction: number) => {
    setPage((current) => Math.max(0, Math.min(pages.length - 1, current + direction)));
  }, [pages.length]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "ArrowRight" || event.key === " ") move(1);
      if (event.key === "ArrowLeft") move(-1);
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [move]);

  if (loading) return <div className="flex min-h-[65vh] items-center justify-center"><div className="h-12 w-12 animate-spin rounded-full border-4 border-muted border-t-primary" /></div>;
  if (!stats) return <div className="p-8 text-center">Could not create your year in review.</div>;
  if (stats.totalWears === 0) return <div className="rounded-3xl border bg-background p-10 text-center text-foreground shadow-sm"><Sparkles className="mx-auto mb-4" /><h1 className="text-4xl font-black">Your {year} story is waiting</h1><p className="mt-4 text-muted-foreground">Log a perfume wear and come back to watch your year in review take shape.</p></div>;

  return <section
    aria-label={`${year} perfume year in review`}
    className="relative min-h-[680px] overflow-hidden rounded-[2rem] border bg-background text-foreground shadow-sm"
    onTouchStart={(event) => { touchStart.current = event.touches[0].clientX; }}
    onTouchEnd={(event) => { if (touchStart.current !== null) { const distance = touchStart.current - event.changedTouches[0].clientX; if (Math.abs(distance) > 45) move(distance > 0 ? 1 : -1); touchStart.current = null; } }}
  >
    <div className="absolute -right-24 -top-20 h-72 w-72 rounded-full bg-muted/70 blur-3xl" />
    <div className="absolute -bottom-28 -left-20 h-80 w-80 rounded-full bg-muted/50 blur-3xl" />
    <div className="relative flex min-h-[680px] flex-col p-6 sm:p-9">
      <div className="flex gap-1.5" aria-label={`Page ${page + 1} of ${pages.length}`}>{pages.map((_, index) => <button key={index} onClick={() => setPage(index)} aria-label={`Go to page ${index + 1}`} className={`h-1.5 flex-1 rounded-full ${index <= page ? "bg-primary" : "bg-muted"}`} />)}</div>
      <AnimatePresence mode="wait"><motion.div key={page} initial={{ opacity: 0, x: 45 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -45 }} transition={{ duration: 0.28 }} className="flex flex-1 items-center justify-center py-10 text-center"><div className="w-full max-w-md">{pages[page]}</div></motion.div></AnimatePresence>
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="icon" className="rounded-full" onClick={() => move(-1)} disabled={page === 0} aria-label="Previous page"><ArrowLeft /></Button>
        <p className="text-xs font-bold uppercase tracking-[0.25em] text-muted-foreground">Perfume Tracker</p>
        {page === pages.length - 1 ? <Button variant="ghost" size="icon" className="rounded-full" onClick={() => setPage(0)} aria-label="Replay"><RotateCcw /></Button> : <Button variant="ghost" size="icon" className="rounded-full" onClick={() => move(1)} aria-label="Next page"><ArrowRight /></Button>}
      </div>
    </div>
  </section>;
}
