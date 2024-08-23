import type { NextApiRequest, NextApiResponse } from 'next';
import { R2_Api_Address } from '../../services/conf';

type ResponseData = {
    url: string,
    error: string
}

export default async function handler(req: NextApiRequest, res: NextApiResponse<ResponseData>) {
    let microserviceUrl = '';
    try {
        if (!req.query.key) {
            res.status(400).json({ error: 'Filename is required', url: '' });
            return;
        }
        if (!R2_Api_Address) {
            res.status(400).json({ error: 'R2 API address is not configured', url: '' });
            return;
        }
        const key = Array.isArray(req.query.key) ? req.query.key[0] : req.query.key;
        microserviceUrl = `${R2_Api_Address}/generate-download-url?key=${encodeURIComponent(key)}`;
        const response = await fetch(microserviceUrl, {
            method: 'GET'
        });
        const json = await response.json();
        console.log(json);
        if (!response.ok) {
            throw new Error(`Failed to get download url from microservice: ${response.statusText}`);
        }

        res.status(200).json({ url: json.downloadURL, error: '' });
    } catch (err: unknown) {
        if (err instanceof Error) {
            console.error(err.message);
            res.status(500).json({ error: `MicroserviceURL:${microserviceUrl} Error: ${err.message}`, url: '' });
        } else {
            console.error('Unknown error occured');
            res.status(500).json({ error: `MicroserviceURL:${microserviceUrl} Unknown error occured`, url: '' });
        }
    }
}