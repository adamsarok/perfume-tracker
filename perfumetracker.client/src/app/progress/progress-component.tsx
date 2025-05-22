import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { UserMissionDto } from '@/dto/MissionDto';

export interface ProgressComponentProps {
  readonly Missions: UserMissionDto[];
}

export default function ProgressComponent({Missions}: ProgressComponentProps) {
  return (
    <div className="container mx-auto py-8">
      <div className="space-y-4">
        {Missions.map((mission) => (
          <Card key={mission.id} className="w-full">
            <CardHeader>
              <CardTitle className="flex justify-between items-center">
                <span>{mission.name}</span>
                <span className="text-sm text-muted-foreground">{mission.xp} XP</span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground mb-4">{mission.description}</p>
              {mission.requiredCount && (
                <div className="space-y-2">
                  <div className="flex justify-between text-sm">
                    <span>Progress</span>
                    <span>{mission.progress} / {mission.requiredCount}</span>
                  </div>
                  <Progress value={(mission.progress / mission.requiredCount) * 100} />
                </div>
              )}
              <div className="mt-4 text-sm text-muted-foreground">
                <p>Ends: {new Date(mission.endDate).toLocaleDateString()}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
