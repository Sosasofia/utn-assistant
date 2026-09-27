import type { Subject } from '../types';

export const api = {
  sendMessageToChatAPI: async (userText: string) => {
    const res = await fetch('/api/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ message: userText }),
    });

    if (!res.ok) {
      throw new Error(`Chat API error! status: ${res.status}`);
    }
    return res.json();
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
