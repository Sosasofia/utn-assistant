import { useState, useRef, useEffect } from 'react';
import { api } from '../services/api';

type Message = {
  id: string;
  role: 'user' | 'assistant';
  content: string;
};
const renderFormattedText = (text: string) => {
  const lines = text.split('\n');

  return lines.map((line, lineIndex) => {
    let isBullet = false;
    let isHeading = false;
    let content = line.trim();

    if (content.startsWith('#')) {
      isHeading = true;
      content = content.replace(/^#+\s*/, '');
    }
    else if (content.startsWith('- ') || content.startsWith('* ')) {
      isBullet = true;
      content = content.substring(2);
    }

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

    if (isHeading) {
      return (
        <h3 key={lineIndex} className="font-bold text-slate-900 text-sm mt-3 mb-1 border-b border-slate-200 pb-1">
          {parsedParts}
        </h3>
      );
    }

    if (isBullet) {
      return (
        <div key={lineIndex} className="flex items-start ml-2 my-0.5">
          <span className="mr-1.5 text-slate-400">•</span>
          <span className="text-slate-700">{parsedParts}</span>
        </div>
      );
    }

    return (
      <div key={lineIndex} className="min-h-4 mb-1 last:mb-0 text-slate-700">
        {parsedParts}
      </div>
    );
  });
};

const createMessage = (role: Message['role'], content: string): Message => ({
  id: crypto.randomUUID(),
  role,
  content,
});

export function ChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isOpen) {
      messagesEndRef.current?.scrollIntoView({ behavior: 'auto' });
    }
  }, [messages, isLoading, isOpen]);

  const handleSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!query.trim()) return;

    const userText = query.trim();
    setQuery('');
    setMessages((prev) => [...prev, createMessage('user', userText)]);
    setIsLoading(true);

    try {
      const res = await api.sendMessageToChatAPI(userText);
      setMessages((prev) => [...prev, createMessage('assistant', res.answer)]);
    } catch (error) {
      console.error('Chat error:', error);

      setMessages((prev) => [
        ...prev,
        createMessage(
          'assistant',
          'Error conectando con el asistente académico.'
        ),
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed bottom-6 right-6 z-50 font-sans">
      {isOpen ? (
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="chat-title"
          className="w-100 h-162.5 bg-white border border-slate-200 rounded-xl shadow-2xl flex flex-col overflow-hidden"
        >
          <div className="bg-blue-600 text-white p-3.5 flex justify-between items-center shadow-sm">
            <h2 id="chat-title" className="font-semibold text-sm tracking-wide">
              Asistente UTN
            </h2>
            <button
              type="button"
              onClick={() => setIsOpen(false)}
              className="text-white hover:text-blue-100 text-xl font-bold leading-none px-1 pb-1 transition-colors cursor-pointer"
              aria-label="Cerrar chat"
            >
              ×
            </button>
          </div>

          <div
            role="log"
            aria-live="polite"
            aria-busy={isLoading}
            className="flex-1 p-4 overflow-y-auto bg-slate-50 flex flex-col gap-3"
          >
            {messages.length === 0 && (
              <div className="text-slate-500 text-sm text-center mt-6 flex flex-col gap-3 px-2">
                <p>
                  ¡Hola! Soy tu asistente académico basado en el plan de
                  estudios 2023.
                </p>
                <p>Pregúntame sobre materias, correlativas o contenidos.</p>
              </div>
            )}

            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`p-3 rounded-xl text-sm max-w-[85%] shadow-sm ${msg.role === 'user'
                  ? 'bg-blue-600 text-white self-end rounded-br-none'
                  : 'bg-white border border-slate-200 text-slate-700 self-start rounded-bl-none'
                  }`}
              >
                {msg.role === 'user'
                  ? msg.content
                  : renderFormattedText(msg.content)}
              </div>
            ))}

            {isLoading && (
              <div className="text-slate-400 text-xs self-start italic bg-slate-100 px-3 py-2 rounded-xl rounded-bl-none border border-slate-200">
                Buscando en el plan de estudios...
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          <form
            onSubmit={handleSubmit}
            className="p-3 border-t border-slate-200 bg-white flex gap-2 items-center"
          >
            <input
              autoFocus
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ej: ¿Qué necesito para Sistemas?"
              className="flex-1 border-2 border-blue-500 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-0 transition-all text-slate-700 placeholder-slate-400"
            />
            <button
              type="submit"
              disabled={isLoading || !query.trim()}
              className="bg-blue-400 hover:bg-blue-500 text-white px-5 py-2 rounded-lg text-sm font-medium disabled:bg-blue-200 transition-colors"
            >
              Enviar
            </button>
          </form>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setIsOpen(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white rounded-full h-14 w-14 shadow-xl flex items-center justify-center transition-transform hover:scale-105"
          aria-label="Abrir asistente"
          aria-expanded={isOpen}
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
}