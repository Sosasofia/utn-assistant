import { useState, useEffect, useCallback } from 'react';
import { getUserId } from '../utils/subject-utils';
import { api } from '../services/api';
import type { Subject, ProgressStatus } from '../types';

function createProgressMap(subjects: Subject[]) {
  const progress = new Map<string, ProgressStatus>();

  subjects.forEach((subject) => {
    if (subject.status && subject.status !== 'NOT_ENROLLED') {
      progress.set(subject.id, subject.status as ProgressStatus);
    }
  });

  return progress;
}

export function useDashboard() {
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [progress, setProgress] = useState<Map<string, ProgressStatus>>(
    new Map()
  );
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDashboard = useCallback(async () => {
    const userId = getUserId();
    try {
      const data = await api.getDashboard(userId);
      setSubjects(data);
      setProgress(createProgressMap(data));
      setError(null);
    } catch (err) {
      setError('Could not connect to Academic Engine');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void Promise.resolve().then(fetchDashboard);
  }, [fetchDashboard]);

  const toggleStatus = useCallback(
    async (subjectId: string, overrideStatus?: ProgressStatus) => {
      const userId = getUserId();

      let next: ProgressStatus;
      if (overrideStatus) {
        next = overrideStatus;
      } else {
        const current = progress.get(subjectId) || 'NOT_ENROLLED';
        next = 'ATTENDED';
        if (current === 'ATTENDED') next = 'APPROVED';
        if (current === 'APPROVED') next = 'NOT_ENROLLED';
      }

      const previousMap = progress;
      const newMap = new Map(progress);
      if (next === 'NOT_ENROLLED') {
        newMap.delete(subjectId);
      } else {
        newMap.set(subjectId, next);
      }
      setProgress(newMap);

      try {
        await api.updateProgress(userId, subjectId, next);

        const data = await api.getDashboard(userId);
        setSubjects(data);
        setProgress(createProgressMap(data));
        setError(null);
      } catch (e) {
        console.error(e);
        setProgress(previousMap);
        setError('Could not update academic progress');
      }
    },
    [progress]
  );

  return { subjects, progress, isLoading, error, toggleStatus };
}
