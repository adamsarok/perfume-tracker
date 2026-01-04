"use client";

import { useState, useRef, useEffect } from "react";
import {
  chatWithAgent,
  ChatAgentResponse,
} from "@/services/chat-agent-service";
import { initializeApiUrl } from "@/services/axios-service";
import * as signalR from "@microsoft/signalr";

interface Message {
  role: "user" | "assistant";
  content: string;
}

export default function ChatAgentPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [toolCallProgress, setToolCallProgress] = useState<{
    isActive: boolean;
    message: string;
    startTime: number | null;
  }>({ isActive: false, message: "", startTime: null });
  const [elapsedTime, setElapsedTime] = useState(0);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // Timer for elapsed time during tool calls
  useEffect(() => {
    let interval: NodeJS.Timeout;
    if (toolCallProgress.isActive && toolCallProgress.startTime) {
      interval = setInterval(() => {
        setElapsedTime((Date.now() - toolCallProgress.startTime!) / 1000);
      }, 100);
    } else {
      setElapsedTime(0);
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [toolCallProgress.isActive, toolCallProgress.startTime]);

  const startProgressTimer = (message: string) => {
    setToolCallProgress({ isActive: true, message, startTime: Date.now() });
  };
  const setProgressMessage = (message: string) => {
    setToolCallProgress((prev) => ({ ...prev, message }));
  };
  const stopProgressTimer = () => {
    setToolCallProgress((prev) => ({
      ...prev,
      message: "Agent is idle.",
      isActive: false,
      startTime: null,
    }));
  };

  const handleSend = async () => {
    if (!input.trim() || isLoading) return;

    const userMessage: Message = {
      role: "user",
      content: input,
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput("");
    setIsLoading(true);

    try {
      startProgressTimer("Agent is thinking...");

      const response: ChatAgentResponse = await chatWithAgent({
        conversationId: conversationId,
        message: input,
      });

      stopProgressTimer();

      // Update conversation ID if this is the first message
      if (!conversationId) {
        setConversationId(response.conversationId);
      }

      const assistantMessage: Message = {
        role: "assistant",
        content: response.assistantMessage,
      };

      setMessages((prev) => [...prev, assistantMessage]);
    } catch (error) {
      console.error("Error chatting with agent:", error);
      const errorMessage: Message = {
        role: "assistant",
        content: "Sorry, I encountered an error. Please try again.",
      };
      setMessages((prev) => [...prev, errorMessage]);
      stopProgressTimer();
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    let connection: signalR.HubConnection | null = null;

    let isMounted = true;
    (async () => {
      if (!isMounted) return;

      const apiUrl = await initializeApiUrl();

      if (!apiUrl) {
        console.error("API URL not configured");
        return;
      }

      if (!isMounted) return;

      connection = new signalR.HubConnectionBuilder()
        .withUrl(`${apiUrl}/hubs/chat-progress`)
        .withAutomaticReconnect()
        .build();

      interface ProgressMsg {
        message: string;
      }
      connection.on("ProgressMsg", (notification: ProgressMsg) => {
        setProgressMessage(notification.message);
      });

      if (!isMounted) {
        await connection.stop();
        return;
      }

      try {
        await connection.start();
        console.log("SignalR Connected");
      } catch (err) {
        console.error("SignalR Connection Error: ", err);
      }
    })();

    return () => {
      isMounted = false;
      if (connection) {
        connection.stop().catch((error) => {
          console.error("SignalR Disconnected: ", error);
        });
      }
    };
  }, []);

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleNewConversation = () => {
    setMessages([]);
    setConversationId(null);
    setInput("");
    stopProgressTimer();
  };

  return (
    <div className="flex flex-col h-screen max-w-4xl mx-auto p-4">
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-2xl font-bold">Perfume Agent Chat</h1>
        <button
          onClick={handleNewConversation}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 transition"
        >
          New Conversation
        </button>
      </div>

      {toolCallProgress.isActive && (
        <div className="mb-2 px-4 py-2 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-700 flex items-center justify-between">
          <span>{toolCallProgress.message}</span>
          <span className="font-mono font-semibold">
            {elapsedTime.toFixed(1)}s
          </span>
        </div>
      )}

      <div className="flex-1 max-h-[60vh] overflow-y-auto mb-4 border rounded-lg p-4 bg-gray-50">
        {messages.length === 0 ? (
          <div className="text-center text-gray-500 mt-10">
            <p className="text-lg">Welcome to the Perfume Agent! 👋</p>
            <p className="mt-2">
              Ask me anything about perfumes, and I&lsquo;ll help you find the
              perfect scent.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {messages.map((message, index) => (
              <div
                key={index}
                className={`flex ${
                  message.role === "user" ? "justify-end" : "justify-start"
                }`}
              >
                <div
                  className={`max-w-[80%] rounded-lg p-3 ${
                    message.role === "user"
                      ? "bg-blue-500 text-white"
                      : "bg-white border border-gray-200"
                  }`}
                >
                  <div className="font-semibold mb-1">
                    {message.role === "user" ? "You" : "Agent"}
                  </div>
                  <div className="whitespace-pre-wrap">{message.content}</div>
                </div>
              </div>
            ))}
            {isLoading && (
              <div className="flex justify-start">
                <div className="bg-white border border-gray-200 rounded-lg p-3">
                  <div className="font-semibold mb-1">Agent</div>
                  <div className="flex space-x-2">
                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></div>
                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-100"></div>
                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-200"></div>
                  </div>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>
        )}
      </div>

      <div className="flex gap-2">
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Type your message... (Press Enter to send, Shift+Enter for new line)"
          className="flex-1 p-3 border rounded-lg resize-none focus:outline-none focus:ring-2 focus:ring-blue-500"
          rows={3}
          disabled={isLoading}
        />
        <button
          onClick={handleSend}
          disabled={!input.trim() || isLoading}
          className="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition disabled:bg-gray-300 disabled:cursor-not-allowed"
        >
          {isLoading ? "Sending..." : "Send"}
        </button>
      </div>
    </div>
  );
}
