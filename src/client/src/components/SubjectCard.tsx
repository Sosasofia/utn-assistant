import type { Subject, ProgressStatus } from '../types';
import { getStatusColor, getStatusBadge } from '../utils/subject-utils';

interface Props {
  subject: Subject;
  status: ProgressStatus | undefined;
  showCorrelatives: boolean;
  progressMap: Map<string, ProgressStatus>;
  onClick: (s: Subject) => void;
}

export function SubjectCard({
  subject,
  status,
  showCorrelatives,
  progressMap,
  onClick,
}: Props) {
  const hasPrereqs =
    subject.correlativesAsTarget && subject.correlativesAsTarget.length > 0;

  const isClickable =
    status === 'ATTENDED' || status === 'APPROVED' || subject.canTake;

  return (
    <button
      type="button"
      disabled={!isClickable}
      onClick={() => onClick(subject)}
      className={`relative w-full p-3 rounded-lg border-2 transition-all duration-200 select-none text-left
        ${getStatusColor(status, subject.canTake)}
        ${subject.isIntegrator ? 'ring-1 ring-purple-300 ring-offset-1' : ''}
      `}
    >
      <div className="flex justify-between items-start mb-2">
        <span className="font-mono text-[10px] text-slate-500 bg-slate-100 px-1 rounded">
          {subject.code}
        </span>
        {hasPrereqs && <span className="text-xs text-slate-400">🔗</span>}
      </div>

      <h3 className="font-semibold text-sm leading-snug mb-1">
        {subject.name}
      </h3>

      <div className="flex flex-wrap gap-1 mb-3">
        {subject.isIntegrator && (
          <span className="text-[10px] bg-purple-100 text-purple-700 px-1.5 rounded">
            Integrator
          </span>
        )}
        {subject.isElective && (
          <span className="text-[10px] bg-blue-50 text-blue-600 px-1.5 rounded">
            Elective
          </span>
        )}
      </div>

      <div className="text-[10px] font-bold uppercase tracking-wider text-center py-1 bg-white/50 rounded">
        {getStatusBadge(status, subject.canTake)}
      </div>

      {showCorrelatives && hasPrereqs && (
        <div className="mt-3 pt-2 border-t border-current border-opacity-10 text-[10px]">
          <p className="font-semibold opacity-70 mb-1">Prerequisites:</p>
          <ul className="space-y-1">
            {subject.correlativesAsTarget?.map((rule) => {
              const myStatus = progressMap.get(rule.requiredSubject.id);

              const isMet =
                rule.type === 'ATTENDED'
                  ? myStatus === 'ATTENDED' || myStatus === 'APPROVED'
                  : myStatus === 'APPROVED';

              return (
                <li
                  key={rule.id}
                  className={`flex items-center gap-1.5 ${isMet ? 'opacity-100' : 'opacity-60 font-medium text-red-700'}`}
                >
                  <span className={isMet ? 'text-green-600' : 'text-red-500'}>
                    {isMet ? '✓' : '✗'}
                  </span>

                  <span className="truncate flex-1">
                    {rule.requiredSubject.name}
                  </span>

                  {rule.type === 'APPROVED' ? (
                    <span
                      className="text-[9px] bg-red-100 text-red-800 border border-red-200 px-1 rounded"
                      title="Requires Final Exam"
                    >
                      FINAL
                    </span>
                  ) : (
                    <span
                      className="text-[9px] bg-yellow-100 text-yellow-800 border border-yellow-200 px-1 rounded"
                      title="Requires Regularizada"
                    >
                      CURSADA
                    </span>
                  )}
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </button>
  );
}
