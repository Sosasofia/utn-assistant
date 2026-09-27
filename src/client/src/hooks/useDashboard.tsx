import { useState, useEffect, useCallback } from 'react';
import { getUserId } from '../utils/subject-utils';
import { api } from '../services/api';
import type { Subject, ProgressStatus } from '../types';

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

      const newMap = new Map<string, ProgressStatus>();
      data.forEach((s) => {
        if (s.status && s.status !== 'NOT_ENROLLED') {
          newMap.set(s.id, s.status as ProgressStatus);
        }
      });
      setProgress(newMap);
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

  const toggleStatus = async (
    subject: Subject,
    overrideStatus?: ProgressStatus,
    overrideId?: string
  ) => {
    const userId = getUserId();
    const targetId = overrideId || subject.id;

    let next: string;
    if (overrideStatus) {
      next = overrideStatus;
    } else {
      const current = progress.get(targetId) || 'NOT_ENROLLED';
      next = 'ATTENDED';
      if (current === 'ATTENDED') next = 'APPROVED';
      if (current === 'APPROVED') next = 'NOT_ENROLLED';
    }

    const newMap = new Map(progress);
    if (next === 'NOT_ENROLLED') {
      newMap.delete(targetId);
    } else {
      newMap.set(targetId, next as ProgressStatus);
    }
    setProgress(newMap);

    try {
      await api.updateProgress(userId, targetId, next);

      const data = await api.getDashboard(userId);
      setSubjects(data);
    } catch (e) {
      console.error(e);
      fetchDashboard();
    }
  };

  return { subjects, progress, isLoading, error, toggleStatus };
}
