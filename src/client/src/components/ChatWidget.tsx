import React, { useState, useRef, useEffect } from 'react';
import { api } from '../services/api';

type Message = {
  role: 'user' | 'assistant';
  content: string;
};

const renderFormattedText = (text: string) => {
  const lines = text.split('\n');

  return lines.map((line, lineIndex) => {
    let isBullet = false;
    let content = line;

    // Detect bullet points starting with "- " or "* "
    if (line.trim().startsWith('- ') || line.trim().startsWith('* ')) {
      isBullet = true;
      content = line.trim().substring(2);
    }

    // Split text by **...** to isolate bold portions
    const parts = content.split(/(\*\*.*?\*\*)/g);
    const parsedParts = parts.map((part, i) => {
      if (part.startsWith('**') && part.endsWith('**')) {
        return (
          <strong key={i} className="font-bold text-blue-900">
            {part.slice(2, -2)}
          </strong>
        );
      }
      return part;
    });

    if (isBullet) {
      return (
        <div key={lineIndex} className="flex items-start ml-2 my-0.5">
          <span className="mr-1.5">•</span>
          <span>{parsedParts}</span>
        </div>
      );
    }

    return (
      <div key={lineIndex} className="min-h-4 mb-1 last:mb-0">
        {parsedParts}
      </div>
    );
  });
};

export const ChatWidget: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;

    const userText = query.trim();
    setQuery('');
    setMessages((prev) => [...prev, { role: 'user', content: userText }]);
    setIsLoading(true);

    try {
      const res = await api.sendMessageToChatAPI(userText);

      setMessages((prev) => [
        ...prev,
        { role: 'assistant', content: res.answer },
      ]);
    } catch (error) {
      console.error('Chat error:', error);
      setMessages((prev) => [
        ...prev,
        {
          role: 'assistant',
          content: 'Error conectando con el asistente académico.',
        },
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed bottom-6 right-6 z-50 font-sans">
      {isOpen ? (
        <div className="w-80 h-96 bg-white border border-gray-300 rounded-lg shadow-2xl flex flex-col overflow-hidden">
          <div className="bg-blue-600 text-white p-3 flex justify-between items-center shadow-sm">
            <h3 className="font-semibold text-sm">Asistente UTN</h3>
            <button
              onClick={() => setIsOpen(false)}
              className="text-white hover:text-gray-200 text-xl leading-none px-1"
              aria-label="Cerrar chat"
            >
              ×
            </button>
          </div>

          <div className="flex-1 p-3 overflow-y-auto bg-gray-50 flex flex-col gap-3">
            {messages.length === 0 && (
              <div className="text-gray-500 text-xs text-center mt-4 flex flex-col gap-2">
                <p>
                  ¡Hola! Soy tu asistente académico basado en el plan de
                  estudios 2023.
                </p>
                <p>Pregúntame sobre materias, correlativas o contenidos.</p>
              </div>
            )}

            {messages.map((msg, idx) => (
              <div
                key={idx}
                className={`p-2.5 rounded-lg text-sm max-w-[85%] shadow-sm ${
                  msg.role === 'user'
                    ? 'bg-blue-600 text-white self-end rounded-br-none'
                    : 'bg-white border border-gray-200 text-gray-800 self-start rounded-bl-none'
                }`}
              >
                {msg.role === 'user'
                  ? msg.content
                  : renderFormattedText(msg.content)}
              </div>
            ))}

            {isLoading && (
              <div className="text-gray-400 text-xs self-start italic bg-gray-100 px-3 py-2 rounded-lg rounded-bl-none">
                Buscando en el plan de estudios...
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          <form
            onSubmit={handleSubmit}
            className="p-3 border-t border-gray-200 bg-white flex gap-2"
          >
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ej: ¿Qué necesito para Sistemas?"
              className="flex-1 border border-gray-300 rounded-md px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              disabled={isLoading}
            />
            <button
              type="submit"
              disabled={isLoading || !query.trim()}
              className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-1.5 rounded-md text-sm disabled:bg-blue-300 transition-colors font-medium"
            >
              Enviar
            </button>
          </form>
        </div>
      ) : (
        <button
          onClick={() => setIsOpen(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white rounded-full h-14 w-14 shadow-xl flex items-center justify-center transition-transform hover:scale-105"
          aria-label="Abrir asistente"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-6 w-6"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
            />
          </svg>
        </button>
      )}
    </div>
  );
};
