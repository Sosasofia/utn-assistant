import { useCallback, useMemo, useState } from 'react';
import { useDashboard } from './hooks/useDashboard';
import { SubjectCard } from './components/SubjectCard';
import { ElectiveModal } from './components/ElectivesModal';
import { sortSubjects } from './utils/subject-utils';
import { ChatWidget } from './components/ChatWidget';
import type { Subject, ProgressStatus } from './types';

export default function App() {
  const { subjects, progress, isLoading, error, toggleStatus } = useDashboard();
  const [showCorrelatives, setShowCorrelatives] = useState(false);
  const [collapseVersion, setCollapseVersion] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalYear, setModalYear] = useState(0);

  const handleCardClick = useCallback(
    (subject: Subject) => {
      if (subject.isElective && subject.name.startsWith('Electiva')) {
        setModalYear(subject.year);
        setIsModalOpen(true);
      } else {
        toggleStatus(subject.id);
      }
    },
    [toggleStatus]
  );

  const handleModalSave = useCallback(
    (realSubjectId: string, status: ProgressStatus) => {
      setIsModalOpen(false);
      toggleStatus(realSubjectId, status);
    },
    [toggleStatus]
  );

  const { subjectsByYear, years } = useMemo(() => {
    if (!subjects.length) {
      return { subjectsByYear: {}, years: [] };
    }

    const processed = subjects.map((s) => ({ ...s }));
    const takenPool3Subjects = processed.filter(
      (s) =>
        s.isElective &&
        !s.name.startsWith('Electiva') &&
        s.year === 3 &&
        progress.has(s.id)
    );

    const YEAR_3_CAPACITY = 1;
    takenPool3Subjects.forEach((sub, index) => {
      if (index >= YEAR_3_CAPACITY) sub.year = 4;
    });

    const takenCounts: Record<number, number> = {};
    processed.forEach((s) => {
      const isReal = s.isElective && !s.name.startsWith('Electiva');
      if (isReal && progress.has(s.id)) {
        takenCounts[s.year] = (takenCounts[s.year] || 0) + 1;
      }
    });

    const displaySubjects = processed.filter((s) => {
      const isPlaceholder = s.isElective && s.name.startsWith('Electiva');
      const isReal = s.isElective && !isPlaceholder;
      const isTaken = progress.has(s.id);

      if (isReal && !isTaken) return false;
      if (isPlaceholder && takenCounts[s.year] && takenCounts[s.year] > 0) {
        takenCounts[s.year]--;
        return false;
      }
      return true;
    });

    const subjectsByYear = displaySubjects.reduce(
      (acc, subject) => {
        if (!acc[subject.year]) acc[subject.year] = [];
        acc[subject.year].push(subject);
        return acc;
      },
      {} as Record<number, Subject[]>
    );

    return {
      subjectsByYear,
      years: Object.keys(subjectsByYear)
        .map(Number)
        .sort((a, b) => a - b),
    };
  }, [subjects, progress]);

  return (
    <>
      <div className="min-h-screen bg-slate-50 font-sans text-slate-900">
        <header className="bg-white shadow-sm sticky top-0 z-50 border-b border-slate-200">
          <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
            <h1 className="text-2xl font-bold text-slate-800">
              🎓 UTN Asistente
            </h1>
            <div className="flex items-center gap-2">
              <label className="flex items-center gap-2 text-sm text-slate-600 cursor-pointer bg-slate-100 px-3 py-1.5 rounded-md hover:bg-slate-200">
                <input
                  type="checkbox"
                  checked={showCorrelatives}
                  onChange={(e) => setShowCorrelatives(e.target.checked)}
                  className="w-4 h-4 rounded text-indigo-600"
                />
                Mostrar todas las correlativas
              </label>
              <button
                type="button"
                onClick={() => {
                  setShowCorrelatives(false);
                  setCollapseVersion((version) => version + 1);
                }}
                className="px-3 py-1.5 text-sm text-slate-600 bg-slate-100 rounded-md hover:bg-slate-200"
              >
                Colapsar todo
              </button>
            </div>
          </div>
        </header>

        <main className="max-w-7xl mx-auto px-6 py-8">
          {isLoading && <div className="text-center py-20">Loading...</div>}
          {error && (
            <div className="text-red-600 bg-red-50 p-4 rounded text-center">
              {error}
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-6">
            {years.map((year) => (
              <div key={year} className="flex flex-col gap-4">
                <div className="flex items-center gap-3 top-20 bg-slate-50/95 backdrop-blur py-2 z-10">
                  <div className="w-8 h-8 bg-slate-800 rounded-full flex items-center justify-center text-white font-bold text-sm">
                    {year}
                  </div>
                  <h2 className="font-bold text-slate-700"> Nivel {year}</h2>
                </div>

                <div className="flex flex-col gap-3">
                  {sortSubjects(subjectsByYear[year]).map((subject) => (
                    <SubjectCard
                      key={subject.id}
                      subject={subject}
                      status={progress.get(subject.id)}
                      progressMap={progress}
                      showCorrelatives={showCorrelatives}
                      collapseVersion={collapseVersion}
                      onClick={handleCardClick}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        </main>

        <ElectiveModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onSave={handleModalSave}
          year={modalYear}
        />
      </div>
      <ChatWidget />
    </>
  );
}
