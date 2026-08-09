import { post, get } from "./axios-service";

export interface ChatAgentRequest {
  conversationId?: string | null;
  message: string;
}

export interface ChatAgentResponse {
  conversationId: string;
  assistantMessage: string;
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  role: string;
  content: string;
  messageIndex: number;
  createdAt: string;
}

export interface ChatConversation {
  id: string;
  title?: string | null;
  messages: ChatMessage[];
  createdAt: string;
  updatedAt: string;
}

export interface ChatConversationSummary {
  id: string;
  title?: string | null;
  createdAt: string;
  updatedAt: string;
}

export const chatWithAgent = async (
  request: ChatAgentRequest
): Promise<ChatAgentResponse> => {
  const response = await post<ChatAgentResponse>("/chat", {
    conversationId: request.conversationId,
    message: request.message,
  });

  if (!response.ok || !response.data) {
    throw new Error(response.error || "Failed to chat with agent");
  }

  return response.data;
};

export const getConversations = async (): Promise<ChatConversationSummary[]> => {
  const response = await get<ChatConversationSummary[]>("/chat/conversations");

  if (!response.ok || !response.data) {
    throw new Error(response.error || "Failed to get conversations");
  }

  return response.data;
};

export const getConversation = async (
  conversationId: string
): Promise<ChatConversation | null> => {
  const response = await get<ChatConversation>(
    `/chat/conversations/${conversationId}`
  );

  if (!response.ok) {
    return null;
  }

  return response.data || null;
};
