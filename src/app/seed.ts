'use server'
import { db } from '@/db';
import { PrismaClient } from '@prisma/client'

export async function SeedCSV() {
  const fs = require('fs');
  const readline = require('readline');

  const stream = fs.createReadStream('src/db/perfumes.csv');
  const reader = readline.createInterface({
    input: stream,
    crlfDelay: Infinity
  });

  reader.on('line', (line: string) => {
    const columns = line.split(';');
    console.log(columns);
    AddPerfume(columns[0], columns[1], parseFloat(columns[2]));
  });

  reader.on('close', () => {
    // Handle the end of the file
  });
}

async function AddPerfume(house: string, perfume: string, rating: number) {
  await db.perfume.create({
    data: {
      house: house,
      perfume: perfume,
      rating: rating
    },
  });
}

// export async function Seed() {
//     const lisboa = await db.perfume.upsert({
//         where: { id: 1 },
//         update: {},
//         create: {
//           house: 'Zara',
//           perfume: 'Lisboa',
//           rating: 8.5,
//           worn: {
//             create: [ 
//             {
//               wornOn: new Date("2024-03-16 00:00:00")
//             },
//             {
//               wornOn: new Date("2024-03-19 00:00:00")
//             } ]
//           },
//         },
//       });
//       const fahrenheit = await prisma.perfume.upsert({
//         where: { id: 2 },
//         update: {},
//         create: {
//           house: 'Dior',
//           perfume: 'Fahrenheit EDT',
//           rating: 8,
//           worn: {
//             create: {
//               wornOn: new Date("2024-03-20 00:00:00")
//             }
//           },
//         },
//       });
//       console.log({ lisboa, fahrenheit })
// } 