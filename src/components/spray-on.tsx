import { MagicWand } from "@/icons/magic-wand";
import { Button, Input } from "@nextui-org/react";
import * as perfumeWornRepo from "../db/perfume-worn-repo";
import { toast } from "react-toastify";
import { useState } from "react";

export interface SprayOnProps {
    perfumeId: number | undefined
    onSuccess: () => void
    className: string
}

export default function SprayOnComponent({perfumeId, onSuccess, className} : SprayOnProps) {
    const yymmdd = new Date().toISOString().slice(0, 10);
    const [dateStr, setDateStr] = useState<string>(yymmdd);

    const onSprayOn = async () => {
        if (perfumeId) {
            let date: Date;
            if (dateStr < yymmdd) {
                date = new Date(dateStr);
                date.setHours(12, 0, 0);
            } else {
                date = new Date();
            }
            const result = await perfumeWornRepo.wearPerfume(perfumeId, date);
            if (result.ok) {
                toast.success("Smell on!");
                onSuccess();
            } else toast.error(result.error);
        }
    }

    return (
        <div className={"flex items-center space-x-4 " + className}>
        <Button color="secondary"
            startContent={<MagicWand />}
            onPress={() => onSprayOn()}
            className="flex-2"
                >Spray On</Button>
        <Input type="date" className="flex-1" 
            defaultValue={yymmdd}
            onValueChange={setDateStr}
        >
            Worn On
            </Input>
        </div>
    )
}