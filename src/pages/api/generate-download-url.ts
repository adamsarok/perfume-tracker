import type { NextApiRequest, NextApiResponse } from 'next';

type ResponseData = {
    url: string,
    error: string
}

export default async function handler(req: NextApiRequest, res: NextApiResponse<ResponseData>) {
    try {
        if (!req.query.key) {
            res.status(400).json({ error: 'Filename is required', url: '' });
            return;
        }
        const key = Array.isArray(req.query.key) ? req.query.key[0] : req.query.key;

        const microserviceUrl = `http://localhost:8080/generate-download-url?key=${encodeURIComponent(key)}`;
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
            res.status(500).json({ error: `Error: ${err.message}`, url: '' });
        } else {
            res.status(500).json({ error: `Unknown error occured`, url: '' });
        }
    }
}