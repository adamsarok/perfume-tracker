import type { NextApiRequest, NextApiResponse } from 'next'
import path from 'path';
import fs, { createWriteStream } from 'fs';
import { readFile } from 'fs/promises';
import { R2_Api_Address } from '../../services/conf';

type ResponseData = {
    guid: string,
    error: string
}

export const config = {
    api: {
        bodyParser: false, // Disabling Next.js body parsing to handle the file upload manually
    },
};

async function sendFile(filePath: string, fileName: string, res: NextApiResponse<ResponseData>) {
    try {
        // Read the file from disk
        const fileBuffer = await readFile(filePath);
        console.log('wtf2');
        // Forward the file to another microservice
        const microserviceUrl = `${R2_Api_Address}/upload-image?fileName=${encodeURIComponent(fileName)}`;
        const response = await fetch(microserviceUrl, {
            method: 'PUT',
            body: fileBuffer,
            headers: {
                'Content-Type': 'image/jpeg',
            },
        });
        const json = await response.json();
        console.log(json);
        if (!response.ok) {
            throw new Error(`Failed to upload file to microservice: ${response.statusText}`);
        }
        res.status(200).json({
            error: '',
            guid: json.objectKey
        });
    } catch (err: unknown) {
        if (err instanceof Error) {
            res.status(500).json({ error: `Error: ${err.message}`, guid: '' });
        } else {
            res.status(500).json({ error: `Unknown error occured`, guid: '' });
        }
    }
}

export default async function handler(req: NextApiRequest, res: NextApiResponse<ResponseData>) {
    if (!R2_Api_Address) {
        res.status(400).json({ error: 'R2 API address is not configured', guid: '' });
        return;
    }
    if (!req.query.filename) {
        res.status(400).json({ error: 'Filename is required', guid: '' });
        return;
    }
    const filename = Array.isArray(req.query.filename) ? req.query.filename[0] : req.query.filename;

    const filePath = path.join(process.cwd(), 'uploads', filename);

    try {
        const fileStream = createWriteStream(filePath);

        await new Promise<void>((resolve, reject) => {
            req.pipe(fileStream);

            fileStream.on('finish', resolve);
            fileStream.on('error', reject);
        });

        await sendFile(filePath, filename, res);
    } catch (err: unknown) {
        if (err instanceof Error) {
            res.status(500).json({ error: `Error: ${err.message}`, guid: '' });
        } else {
            res.status(500).json({ error: `Unknown error occured`, guid: '' });
        }
    }
    res.status(500).json({ error: `Unknown error occured`, guid: '' });
}