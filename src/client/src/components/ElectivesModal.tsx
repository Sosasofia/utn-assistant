import { useState, useEffect } from 'react';
import { api } from '../services/api';
import type { Subject, ProgressStatus } from '../types';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSave: (realSubjectId: string, status: ProgressStatus) => void;
  year: number;
}

export function ElectiveModal({ isOpen, onClose, onSave, year }: Props) {
  const [options, setOptions] = useState<Subject[]>([]);
  const [selectedId, setSelectedId] = useState<string>('');
  const [status, setStatus] = useState<ProgressStatus>('ATTENDED');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!isOpen) return;

    let cancelled = false;
    const loadOptions = async () => {
      setLoading(true);
      try {
        const data = await api.fetchElectiveOptions();

        if (cancelled) return;

        if (Array.isArray(data)) {
          const filtered = data.filter((s) => {
            if (year === 3 || year === 4) {
              return s.year === 3 || s.year === 4;
            }
            return s.year === year;
          });

          setOptions(filtered);
        } else {
          setOptions([]);
        }
      } catch (err) {
        if (!cancelled) {
          console.error('Failed to fetch electives:', err);
          setOptions([]);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void loadOptions();

    return () => {
      cancelled = true;
    };
  }, [isOpen, year]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-slate-900/50 flex items-center justify-center z-[100] backdrop-blur-sm">
      <div className="bg-white p-6 rounded-xl shadow-2xl w-96 max-w-[90vw] animate-in fade-in zoom-in duration-200">
        <h2 className="text-xl font-bold text-slate-800 mb-1">
          Select Elective
        </h2>
        <p className="text-sm text-slate-500 mb-4">For Year {year}</p>

        {loading ? (
          <div className="text-center py-8 text-slate-400">
            Loading options...
          </div>
        ) : (
          <>
            <div className="mb-5">
              <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">
                Available Subjects
              </label>
              <select
                className="w-full border border-slate-300 rounded-lg p-2.5 text-sm focus:ring-2 focus:ring-indigo-500 outline-none bg-slate-50"
                value={selectedId}
                onChange={(e) => setSelectedId(e.target.value)}
              >
                <option value="">-- Choose a subject --</option>
                {options.map((opt) => (
                  <option key={opt.id} value={opt.id}>
                    {opt.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="mb-6">
              <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">
                Mark As
              </label>
              <div className="grid grid-cols-2 gap-3">
                <button
                  onClick={() => setStatus('ATTENDED')}
                  className={`py-2 rounded-lg border text-sm font-semibold transition-colors ${
                    status === 'ATTENDED'
                      ? 'bg-yellow-100 border-yellow-400 text-yellow-900'
                      : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  Firma (Attended)
                </button>
                <button
                  onClick={() => setStatus('APPROVED')}
                  className={`py-2 rounded-lg border text-sm font-semibold transition-colors ${
                    status === 'APPROVED'
                      ? 'bg-green-100 border-green-400 text-green-900'
                      : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  Final (Approved)
                </button>
              </div>
            </div>
          </>
        )}

        <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition"
          >
            Cancel
          </button>
          <button
            onClick={() => {
              if (selectedId) onSave(selectedId, status);
            }}
            disabled={!selectedId}
            className="px-4 py-2 text-sm font-bold text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition shadow-sm"
          >
            Save
          </button>
        </div>
      </div>
    </div>
  );
}
