import *  as actions from "@/app/actions";
import { Card, CardBody, CardFooter, CardHeader, Divider } from "@nextui-org/react";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const ml = await actions.GetTotalMls();
    return <div>
        <Card className="max-w-[400px]">
        <CardHeader className="flex gap-3">
            <div className="flex flex-col">
            <p className="text-md">Total Mls:</p>
            <p className="text-small text-default-500">{ml}</p>
            </div>
        </CardHeader>
        <Divider/>
        <CardBody>
            <p>TODO: split on category?</p>
        </CardBody>
        <Divider/>
        <CardFooter>
           
        </CardFooter>
    </Card>
    </div>
}