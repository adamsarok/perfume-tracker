import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { getWornBeforeID } from "@/services/perfume-worn-service";

export interface ReviewRankedItem {
  name: string;
  subtitle?: string;
  count: number;
  color?: string;
  imageUrl?: string;
}

export interface YearInReviewStats {
  year: number;
  totalWears: number;
  uniquePerfumes: number;
  uniqueHouses: number;
  activeDays: number;
  topPerfumes: ReviewRankedItem[];
  topHouses: ReviewRankedItem[];
  topTags: ReviewRankedItem[];
  busiestMonth: ReviewRankedItem | null;
  busiestDay: ReviewRankedItem | null;
}

const countBy = <T>(items: T[], key: (item: T) => string) => {
  const counts = new Map<string, number>();
  items.forEach((item) => counts.set(key(item), (counts.get(key(item)) ?? 0) + 1));
  return counts;
};

const ranked = (counts: Map<string, number>, limit = 5): ReviewRankedItem[] =>
  [...counts.entries()]
    .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
    .slice(0, limit)
    .map(([name, count]) => ({ name, count }));

export function aggregateYearInReview(events: PerfumeWornDTO[], year: number): YearInReviewStats {
  const yearlyEvents = events.filter((event) => new Date(event.eventDate).getFullYear() === year);
  const perfumeDetails = new Map<string, PerfumeWornDTO>();
  yearlyEvents.forEach((event) => perfumeDetails.set(event.perfumeId, event));

  const topPerfumes = ranked(countBy(yearlyEvents, (event) => event.perfumeId)).map((item) => {
    const perfume = perfumeDetails.get(item.name)!;
    return {
      ...item,
      name: perfume.perfumeName,
      subtitle: perfume.perfumeHouse,
      imageUrl: perfume.perfumeImageUrl,
    };
  });

  const tagDetails = new Map<string, { name: string; color: string }>();
  const tagOccurrences: string[] = [];
  yearlyEvents.forEach((event) => event.perfumeTags.forEach((tag) => {
    tagDetails.set(tag.id, { name: tag.tagName, color: tag.color });
    tagOccurrences.push(tag.id);
  }));
  const topTags = ranked(countBy(tagOccurrences, (tagId) => tagId)).map((item) => ({
    ...item,
    name: tagDetails.get(item.name)?.name ?? item.name,
    color: tagDetails.get(item.name)?.color,
  }));

  const months = ranked(countBy(yearlyEvents, (event) =>
    new Date(event.eventDate).toLocaleDateString(undefined, { month: "long" })
  ), 1)[0] ?? null;
  const days = ranked(countBy(yearlyEvents, (event) =>
    new Date(event.eventDate).toLocaleDateString(undefined, { weekday: "long" })
  ), 1)[0] ?? null;

  return {
    year,
    totalWears: yearlyEvents.length,
    uniquePerfumes: perfumeDetails.size,
    uniqueHouses: new Set(yearlyEvents.map((event) => event.perfumeHouse)).size,
    activeDays: new Set(yearlyEvents.map((event) => new Date(event.eventDate).toDateString())).size,
    topPerfumes,
    topHouses: ranked(countBy(yearlyEvents, (event) => event.perfumeHouse)),
    topTags,
    busiestMonth: months,
    busiestDay: days,
  };
}

export async function getYearInReview(year: number): Promise<YearInReviewStats> {
  const events: PerfumeWornDTO[] = [];
  let cursor: number | null = null;
  const pageSize = 100;
  const yearStart = new Date(year, 0, 1);

  while (true) {
    const result = await getWornBeforeID(cursor, pageSize);
    if (result.error) throw new Error(result.error);
    const page = result.data ?? [];
    events.push(...page.filter((event) => new Date(event.eventDate) >= yearStart));

    if (page.length < pageSize || page.some((event) => new Date(event.eventDate) < yearStart)) break;
    const nextCursor = page.at(-1)?.sequenceNumber ?? null;
    if (nextCursor === null || nextCursor === cursor) break;
    cursor = nextCursor;
  }

  return aggregateYearInReview(events, year);
}
