import { getPerfumeSimilar } from '@/services/recommendations';
import type { NextApiRequest, NextApiResponse } from 'next';

type ResponseData = {
    recommendations: string,
    error: string
}

export default async function handler(req: NextApiRequest, res: NextApiResponse<ResponseData>) {
    let microserviceUrl = '';
    try {
        //const reqJson = await req.body.json();
        const { pastPerfumes } = req.body;
        const recommendations = await getPerfumeSimilar(pastPerfumes);
        if (recommendations) res.status(200).json({ recommendations, error: '' });
        else res.status(500).json({ error: `ChatGPT returned no data`, recommendations: '' });
    } catch (err: unknown) {
        if (err instanceof Error) {
            console.error(err.message);
            res.status(500).json({ error: `Error: ${err.message}`,  recommendations: '' });
        } else {
            console.error('Unknown error occured');
            res.status(500).json({ error: `Unknown error occured`, recommendations: '' });
        }
    }
}