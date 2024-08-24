import { OpenAI } from 'openai';
import { OPENAI_API_KEY, R2_API_ADDRESS } from './conf';
import { env } from "process";

//const openai = new OpenAI({ apiKey: OPENAI_API_KEY });

export async function getPerfumeSimilar(pastChoices: string[]) {
  console.log(`OPENAI_API_KEY: ${OPENAI_API_KEY}`); //TODO complete
  
  /*const response = await openai.chat.completions.create({
    model: "gpt-4o-mini",
    messages: [
      { role: "system", content: "You are a perfume recommendation expert." },
      { role: "user", content: `Based on these past choices: ${pastChoices.join(', ')}, suggest 3 perfumes.` }
    ],
  });

  return response.choices[0].message.content;*/
}