import { Button } from "./ui/button";
import {
  Drawer,
  DrawerTrigger,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
  DrawerDescription,
  DrawerFooter,
  DrawerClose,
} from "./ui/drawer";
import { Input } from "./ui/input";

export interface PlaylistDrawerProps {
  readonly className: string;
}

export default function PlaylistDrawer({ className }: PlaylistDrawerProps) {
  return (
    <div className={className}>
      <Drawer>
        <div className="flex items-center space-x-2">
          <DrawerTrigger asChild>
            <Button>Add to Playlist</Button>
          </DrawerTrigger>
          <Input placeholder="Playlist"></Input>
        </div>
        <DrawerContent>
          <DrawerHeader>
            <DrawerTitle>Are you absolutely sure?</DrawerTitle>
            <DrawerDescription>This action cannot be undone.</DrawerDescription>
          </DrawerHeader>
          <DrawerFooter>
            <DrawerClose>
              <Button>Cancel</Button>
            </DrawerClose>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>
    </div>
  );
}
