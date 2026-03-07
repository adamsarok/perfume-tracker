export async function getPerfumeTrackerApiAddress(): Promise<string | undefined> {
  return import.meta.env.VITE_PERFUMETRACKER_API_ADDRESS;
}
