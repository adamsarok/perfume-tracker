import { OpenAI } from 'openai';

const openai = new OpenAI(); 
export async function getOpenAIResponse(query: string) {
  const response = await openai.chat.completions.create({
    model: "gpt-4o-mini",
    messages: [
      { role: "system", content: "You are a perfume recommendation expert." },
      { role: "user", content: query }
    ],
  });

  return response.choices[0].message.content;
}