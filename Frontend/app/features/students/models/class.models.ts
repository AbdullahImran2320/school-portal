export interface ClassDto {
  classId: number;
  className: string;
  section: string;
  academicYear: string;
  promotionOrder: number;
  studentCount: number;
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
}

export interface AddSectionDto {
  className: string;
  academicYear: string;
  section: string;
}

export interface SectionOptionDto {
  sectionOptionId: number;
  name: string;
}

export interface CreateSectionOptionDto {
  name: string;
}
