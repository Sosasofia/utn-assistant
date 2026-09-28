import type { Subject } from '../types';

export const getUserId = () => {
  let storedId = localStorage.getItem('userId');
  if (!storedId) {
    storedId = sessionStorage.getItem('userId');
    if (storedId) localStorage.setItem('userId', storedId);
  }
  if (!storedId) {
    storedId = 'guest-' + Math.random().toString(36).slice(2, 11);
    localStorage.setItem('userId', storedId);
  }
  return storedId;
};

export const getStatusColor = (
  status: string | undefined,
  canTake: boolean | undefined
) => {
  if (status === 'APPROVED')
    return 'bg-green-100 text-green-800 border-green-200 shadow-sm';
  if (status === 'ATTENDED')
    return 'bg-yellow-100 text-yellow-800 border-yellow-200 shadow-sm';
  if (canTake)
    return 'bg-white text-slate-900 border-slate-300 shadow-sm hover:border-blue-500 hover:shadow-md cursor-pointer transition-all';
  return 'bg-slate-100 text-slate-500 border-slate-200 shadow-sm cursor-not-allowed';
};

export const getStatusBadge = (
  status: string | undefined,
  canTake: boolean | undefined
) => {
  if (status === 'APPROVED') return '✓✓ Aprobada';
  if (status === 'ATTENDED') return '✓ Cursada';
  if (!canTake && !status) return '🔒 Bloqueada';
  return 'Click para actualizar';
};

export const sortSubjects = (subjects: Subject[]) => {
  return [...subjects].sort((a, b) => {
    if (a.isIntegrator && !b.isIntegrator) return -1;
    if (!a.isIntegrator && b.isIntegrator) return 1;
    if (a.semester !== b.semester) return a.semester - b.semester;
    return a.code.localeCompare(b.code);
  });
};
