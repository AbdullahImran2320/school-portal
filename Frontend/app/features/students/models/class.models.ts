export interface ClassDto {
  classId: number;
  className: string;
  section: string;
  academicYear: string;
  promotionOrder: number;
  studentCount: number;
  classCode: string;
}

export interface ClassGroupDto {
  className: string;
  academicYear: string;
  promotionOrder: number;
  sections: ClassDto[];
}

export interface CreateClassDto {
  className: string;
  academicYear: string;
  classCode: string;
}

export interface AddSectionDto {
  className: string;
  academicYear: string;
  section: string;
  classCode: string;
}

export interface UpdateClassCodeDto {
  classCode: string;
}

export interface SectionOptionDto {
  sectionOptionId: number;
  name: string;
}

export interface CreateSectionOptionDto {
  name: string;
}
