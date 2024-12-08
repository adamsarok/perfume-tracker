import { getOpenAIResponse } from '@/services/openai-service';
import type { NextApiRequest, NextApiResponse } from 'next';
import * as recommendationRepo from '@/db/recommendation-repo';

interface ResponseData {
    recommendations: string,
    error: string
}

function RemoveDiacritics(input: string){
    return input.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
}

export default async function handler(req: NextApiRequest, res: NextApiResponse<ResponseData>) {
    try {
        let { query } = req.body;
        query = RemoveDiacritics(query);
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