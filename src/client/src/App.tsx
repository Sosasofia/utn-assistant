import { useState, useMemo } from 'react';
import { useDashboard } from './hooks/useDashboard';
import { SubjectCard } from './components/SubjectCard';
import { ElectiveModal } from './components/ElectivesModal';
import { sortSubjects } from './utils/subject-utils';
import { ChatWidget } from './components/ChatWidget';
import type { Subject, ProgressStatus } from './types';

export default function App() {
  const { subjects, progress, isLoading, error, toggleStatus } = useDashboard();
  const [showCorrelatives, setShowCorrelatives] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalYear, setModalYear] = useState(0);

  console.log('App render', { subjects, progress, isLoading, error });

  const handleCardClick = (subject: Subject) => {
    if (subject.isElective && subject.name.startsWith('Electiva')) {
      setModalYear(subject.year);
      setIsModalOpen(true);
    } else {
      toggleStatus(subject);
    }
  };

  const handleModalSave = (realSubjectId: string, status: ProgressStatus) => {
    setIsModalOpen(false);
    toggleStatus({ id: realSubjectId } as Subject, status, realSubjectId);
  };

  const displaySubjects = useMemo(() => {
    if (!subjects.length) return [];

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
      if (index >= YEAR_3_CAPACITY) {
        sub.year = 4;
      }
    });

    const takenCounts: Record<number, number> = {};
    processed.forEach((s) => {
      const isReal = s.isElective && !s.name.startsWith('Electiva');
      const isTaken = progress.has(s.id);

      if (isReal && isTaken) {
        takenCounts[s.year] = (takenCounts[s.year] || 0) + 1;
      }
    });

    return processed.filter((s) => {
      const isPlaceholder = s.isElective && s.name.startsWith('Electiva');
      const isReal = s.isElective && !isPlaceholder;
      const isTaken = progress.has(s.id);

      if (isReal && !isTaken) return false;

      if (isPlaceholder) {
        if (takenCounts[s.year] && takenCounts[s.year] > 0) {
          takenCounts[s.year]--;
          return false;
        }
      }
      return true;
    });
  }, [subjects, progress]);

  const subjectsByYear = displaySubjects.reduce(
    (acc, subject) => {
      if (!acc[subject.year]) acc[subject.year] = [];
      acc[subject.year].push(subject);
      return acc;
    },
    {} as Record<number, Subject[]>
  );

  const years = Object.keys(subjectsByYear)
    .map(Number)
    .sort((a, b) => a - b);

  return (
    <>
      <div className="min-h-screen bg-slate-50 font-sans text-slate-900">
        <header className="bg-white shadow-sm sticky top-0 z-50 border-b border-slate-200">
          <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
            <h1 className="text-2xl font-bold text-slate-800">
              🎓 UTN Planner
            </h1>
            <label className="flex items-center gap-2 text-sm text-slate-600 cursor-pointer bg-slate-100 px-3 py-1.5 rounded-md hover:bg-slate-200">
              <input
                type="checkbox"
                checked={showCorrelatives}
                onChange={(e) => setShowCorrelatives(e.target.checked)}
                className="w-4 h-4 rounded text-indigo-600"
              />
              Show Prerequisites
            </label>
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
                  <h2 className="font-bold text-slate-700">Year {year}</h2>
                </div>

                <div className="flex flex-col gap-3">
                  {sortSubjects(subjectsByYear[year]).map((subject) => (
                    <SubjectCard
                      key={subject.id}
                      subject={subject}
                      status={progress.get(subject.id)}
                      progressMap={progress}
                      showCorrelatives={showCorrelatives}
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
