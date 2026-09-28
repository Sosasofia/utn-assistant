import { useState } from 'react';
import { getStatusColor, getStatusBadge } from '../utils/subject-utils';
import type { Subject, ProgressStatus } from '../types';

interface Props {
  subject: Subject;
  status: ProgressStatus | undefined;
  showCorrelatives: boolean;
  collapseVersion: number;
  progressMap: Map<string, ProgressStatus>;
  onClick: (s: Subject) => void;
}

export function SubjectCard({
  subject,
  status,
  showCorrelatives,
  collapseVersion,
  progressMap,
  onClick,
}: Props) {
  const [isExpanded, setIsExpanded] = useState(false);

  const [prevCollapseVersion, setPrevCollapseVersion] = useState(0);

  if (collapseVersion !== prevCollapseVersion) {
    setPrevCollapseVersion(collapseVersion);
    setIsExpanded(false);
  }

  const hasPrerequisites =
    subject.correlativesAsTarget && subject.correlativesAsTarget.length > 0;

  let explicitStatus = '';
  if (status === 'APPROVED') {
    explicitStatus = ' - Aprobada';
  } else if (status === 'ATTENDED') {
    explicitStatus = ' - Puede rendir final';
  } else if (subject.canTake) {
    explicitStatus = ' - Puede cursar';
  }

  const correlatives = subject.correlativesAsTarget || [];

  const isClickable =
    status === 'ATTENDED' || status === 'APPROVED' || subject.canTake;

  const shouldShowPrereqs =
    (showCorrelatives || isExpanded) && hasPrerequisites;

  return (
    <div className="relative w-full hover:z-50">
      <button
        type="button"
        onClick={() => {
          if (isClickable) onClick(subject);
        }}
        className={`w-full p-3 rounded-lg border-2 transition-all duration-200 select-none text-left
        ${getStatusColor(status, subject.canTake)}
        ${subject.isIntegrator ? 'ring-1 ring-purple-300 ring-offset-1' : ''}
      `}
      >
        <div className="flex justify-between items-start mb-2">
          <span className="font-mono text-[10px] text-slate-500 bg-slate-100 px-1 rounded">
            [{subject.code}]{explicitStatus}
          </span>

          <h3 className="font-semibold text-sm leading-snug mb-2 flex items-start gap-1 justify-between">
            {hasPrerequisites && (
              <span
                onClick={(e) => {
                  e.stopPropagation();
                  setIsExpanded(!isExpanded);
                }}
                className="flex items-center justify-center w-4 h-4 text-[10px] font-bold text-slate-600 bg-slate-200 hover:bg-slate-300 rounded-full cursor-pointer transition-colors"
                title="Ver correlativas"
              >
                i
              </span>
            )}
          </h3>
        </div>

        <h3 className="font-semibold text-sm leading-snug mb-1">
          {subject.name}
        </h3>

        <div className="flex flex-wrap gap-1 mb-3">
          {subject.isIntegrator && (
            <span className="text-[10px] bg-purple-100 text-purple-700 px-1.5 rounded">
              Integradora
            </span>
          )}
          {subject.isElective && (
            <span className="text-[10px] bg-blue-50 text-blue-600 px-1.5 rounded">
              Electiva
            </span>
          )}
        </div>

        <div className="text-[10px] font-bold uppercase tracking-wider text-center py-1 bg-white/50 rounded">
          {getStatusBadge(status, subject.canTake)}
        </div>

        {shouldShowPrereqs && (
          <div className="mt-3 pt-2 border-t border-current border-opacity-10 text-[10px]">
            <p className="font-semibold opacity-70 mb-1">Prerequisitos:</p>
            <ul className="space-y-1">
              {correlatives.map((rule) => {
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
    </div>
  );
}
