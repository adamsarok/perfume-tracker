import { get } from "@/services/axios-service";

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

export async function getYearInReview(year: number): Promise<YearInReviewStats> {
  const result = await get<YearInReviewStats>(`/year-in-review/${encodeURIComponent(year)}`);
  if (!result.ok || !result.data) {
    throw new Error(result.error || "Could not create year in review");
  }
  return result.data;
}
