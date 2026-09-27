export type Subject = {
  id: string;
  code: string;
  name: string;
  year: number;
  semester: number;
  credits?: number;
  isIntegrator?: boolean;
  isElective?: boolean;
  status?: string;
  canTake?: boolean;
  missingPrerequisites?: Correlative[];
  correlativesAsTarget?: Correlative[];
};

export type Correlative = {
  id: string;
  type: 'APPROVED' | 'ATTENDED';
  isTransient?: boolean;
  requiredSubject: {
    id: string;
    code: string;
    name: string;
  };
};

export type ProgressStatus = 'NOT_ENROLLED' | 'ATTENDED' | 'APPROVED';

export interface User {
  id: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface UserSubjectProgress {
  id: string;
  userId: string;
  subjectId: string;
  status: ProgressStatus;
  createdAt: Date;
  updatedAt: Date;
}
