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
    return 'bg-white text-gray-900 border-gray-200 hover:border-indigo-400 hover:shadow-md cursor-pointer';
  return 'bg-gray-50 text-gray-400 border-gray-100 opacity-60 cursor-not-allowed';
};

export const getStatusBadge = (
  status: string | undefined,
  canTake: boolean | undefined
) => {
  if (status === 'APPROVED') return '✓✓ Approved';
  if (status === 'ATTENDED') return '✓ Attended';
  if (!canTake && !status) return '🔒 Locked';
  return 'Click to update';
};

export const sortSubjects = (subjects: Subject[]) => {
  return [...subjects].sort((a, b) => {
    if (a.isIntegrator && !b.isIntegrator) return -1;
    if (!a.isIntegrator && b.isIntegrator) return 1;
    if (a.semester !== b.semester) return a.semester - b.semester;
    return a.code.localeCompare(b.code);
  });
};
