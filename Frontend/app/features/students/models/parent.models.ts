export type PrimaryGuardian = 'Mother' | 'Father' | 'MotherAndFather' | 'Other';

export interface ParentDto {
  parentId: number;
  fatherName: string;
  fatherMobile: string;
  fatherOccupation?: string;
  motherName?: string;
  motherMobile?: string;
  primaryGuardian: PrimaryGuardian;
  address: string;
  childrenCount: number;
}

export interface UpsertParentDto {
  fatherName: string;
  fatherMobile: string;
  fatherOccupation?: string;
  motherName?: string;
  motherMobile?: string;
  primaryGuardian: PrimaryGuardian;
  address: string;
}