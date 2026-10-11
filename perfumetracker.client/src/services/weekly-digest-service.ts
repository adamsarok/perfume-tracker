import { get } from './axios-service';

export interface DigestSummary { id: string; weekStart: string; completedAt: string }
export interface DigestHistory { items: DigestSummary[]; total: number; page: number }
export interface DigestDetail extends DigestSummary {
  digest: {
    introduction: string;
    moodOrOccasion: string;
    categories: {
      strategy: string;
      summary: string;
      shortageReason: string;
      picks: { perfumeId: string; house: string; perfumeName: string; reason: string; isAvailable: boolean }[];
    }[];
  };
}

export async function getDigestHistory(page: number): Promise<DigestHistory> {
  const result = await get<DigestHistory>(`/weekly-digests?page=${page}`);
  if (!result.ok || !result.data) throw new Error('Could not load weekly digests. Please try again.');
  return result.data;
}

export async function getDigest(id: string): Promise<DigestDetail> {
  const result = await get<DigestDetail>(`/weekly-digests/${encodeURIComponent(id)}`);
  if (!result.ok || !result.data) throw new Error('Could not load this digest. Please try again.');
  return result.data;
}
