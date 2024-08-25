import { getOpenAIResponse } from '@/services/openai-service';
import type { NextApiRequest, NextApiResponse } from 'next';
import * as recommendationRepo from '@/db/recommendation-repo';

type ResponseData = {
    recommendations: string,
    error: string
}

export default async function handler(req: NextApiRequest, res: NextApiResponse<ResponseData>) {
    try {
        const { pastPerfumes } = req.body;
        const query = `Based on these past choices: ${pastPerfumes}, suggest 3 perfumes.`;
        const recommendations = await getOpenAIResponse(query);
        recommendationRepo.insertRecommendation(query, recommendations);
        if (recommendations) {
            res.status(200).json({ recommendations, error: '' });
        }
        else res.status(500).json({ error: `ChatGPT returned no data`, recommendations: '' });
    } catch (err: unknown) {
        if (err instanceof Error) {
            console.error(err.message);
            res.status(500).json({ error: `Error: ${err.message}`, recommendations: '' });
        } else {
            console.error('Unknown error occured');
            res.status(500).json({ error: `Unknown error occured`, recommendations: '' });
        }
    }
}