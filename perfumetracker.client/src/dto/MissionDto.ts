export interface UserMissionDto {
  id: string;
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  xp: number;
  missionType: string;
  requiredCount: number | null;
  requiredName: string | null;
  progress: number;
  isCompleted: boolean;
}