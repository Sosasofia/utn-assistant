import type { Subject } from '../types';

export type ChatResponse = {
  answer: string;
};

const getUserId = (): string => {
  const userId = localStorage.getItem('userId');
  if (!userId) {
    throw new Error('User ID not found in local storage');
  }
  return userId;
};

const getChatHistory = (): Array<{ role: string; content: string }> => {
  const userId = getUserId();
  return JSON.parse(localStorage.getItem(`chatHistory_${userId}`) || '[]');
};

const appendToChatHistory = (content: string, role: 'user' | 'assistant') => {
  const userId = getUserId();
  const history = getChatHistory();
  history.push({ content, role });
  localStorage.setItem(`chatHistory_${userId}`, JSON.stringify(history));
};

export const api = {
  sendMessageToChatAPI: async (userText: string): Promise<ChatResponse> => {
    const userId = getUserId();

    const currentHistory = getChatHistory();
    appendToChatHistory(userText, 'user');

    const res = await fetch('/api/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        message: userText,
        userId,
        history: currentHistory,
      }),
    });

    if (!res.ok) {
      throw new Error(`Chat API error! status: ${res.status}`);
    }

    const data = (await res.json()) as ChatResponse;

    if (data.answer) {
      appendToChatHistory(data.answer, 'assistant');
    }

    return data;
  },

  fetchElectiveOptions: async (): Promise<Subject[]> => {
    const res = await fetch('/api/academic-history/electives/options');
    if (!res.ok) {
      throw new Error(`Failed to load elective options: ${res.status}`);
    }
    return res.json();
  },

  getDashboard: async (userId: string): Promise<Subject[]> => {
    const res = await fetch(`/api/academic-history/dashboard?userId=${userId}`);
    if (!res.ok) {
      throw new Error(`Failed to load dashboard: ${res.status}`);
    }
    return res.json();
  },

  updateProgress: async (
    userId: string,
    targetId: string,
    status: string
  ): Promise<void> => {
    const method = status === 'NOT_ENROLLED' ? 'DELETE' : 'PUT';
    const res = await fetch(
      `/api/academic-history/${userId}/progress/${targetId}`,
      {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status }),
      }
    );

    if (!res.ok) {
      throw new Error(`Failed to update progress: ${res.status}`);
    }
  },
};
