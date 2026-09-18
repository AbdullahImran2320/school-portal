import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AdminNavComponent } from '../../../shared/components/admin-nav/admin-nav.component';
import { ClassesService } from '../../students/services/classes.service';
import { ClassGroupDto, SectionOptionDto } from '../../students/models/class.models';

@Component({
  selector: 'app-classes',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminNavComponent],
  templateUrl: './classes.component.html',
  styleUrl: './classes.component.scss'
})
export class ClassesComponent implements OnInit {
  private classesService = inject(ClassesService);

  academicYear = signal<string>(String(new Date().getFullYear()));

  groups = signal<ClassGroupDto[]>([]);
  sectionOptions = signal<SectionOptionDto[]>([]);

  loading = signal(false);
  error = signal<string | null>(null);
  actionError = signal<string | null>(null);

  // Add-class form
  newClassName = signal('');
  newClassCode = signal('');
  addingClass = signal(false);

  // Add-section form (per group, keyed by className so multiple rows can't clash)
  sectionFormOpenFor = signal<string | null>(null);
  newSectionName = signal<string>('');
  newSectionCode = signal<string>('');
  addingSection = signal(false);

  // Inline class-code edit (per row, keyed by classId)
  codeFormOpenFor = signal<number | null>(null);
  editCodeInput = signal<string>('');
  savingCode = signal<number | null>(null);

  // Section-label list management
  newLabelName = signal('');
  addingLabel = signal(false);
  labelsPanelOpen = signal(false);

  deletingId = signal<number | null>(null);
  deletingGroup = signal<string | null>(null);

  // Move-students form (per section, keyed by the "from" classId — only one
  // row's move form open at a time, same pattern as the add-section form)
  moveFormOpenFor = signal<number | null>(null);
  moveTargetId = signal<number | null>(null);
  movingStudents = signal<number | null>(null);

  // Labels not yet used by a given group — what the "add section" dropdown offers
  availableLabelsFor = computed(() => {
    return (group: ClassGroupDto) => {
      const used = new Set(group.sections.map(s => s.section).filter(Boolean));
      return this.sectionOptions().filter(o => !used.has(o.name));
    };
  });

  // Other sections in the same grade/year — the only valid move targets,
  // since the backend restricts bulk moves to siblings of the same class
  // and year (see /api/classes/{id}/move-students).
  moveTargetsFor = computed(() => {
    return (group: ClassGroupDto, fromClassId: number) =>
      group.sections.filter(s => s.classId !== fromClassId);
  });

  ngOnInit() {
    this.loadAll();
  }

  loadAll() {
    this.loading.set(true);
    this.error.set(null);
    this.classesService.getGrouped().subscribe({
      next: (data) => {
        this.groups.set(data.filter(g => g.academicYear === this.academicYear()));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load classes.');
        this.loading.set(false);
      }
    });
    this.classesService.getSectionOptions().subscribe({
      next: (data) => this.sectionOptions.set(data),
      error: () => {} // non-fatal — the add-section dropdown just stays empty
    });
  }

  createClass() {
    const name = this.newClassName().trim();
    const code = this.newClassCode().trim();
    if (!name || !code) return;

    this.actionError.set(null);
    this.addingClass.set(true);
    this.classesService.createClass({ className: name, academicYear: this.academicYear(), classCode: code }).subscribe({
      next: () => {
        this.newClassName.set('');
        this.newClassCode.set('');
        this.addingClass.set(false);
        this.loadAll();
      },
      error: (err) => {
        this.actionError.set(err?.error ?? `Could not create '${name}'.`);
        this.addingClass.set(false);
      }
    });
  }

  openSectionForm(group: ClassGroupDto) {
    this.sectionFormOpenFor.set(group.className);
    this.newSectionName.set('');
    this.newSectionCode.set('');
  }

  cancelSectionForm() {
    this.sectionFormOpenFor.set(null);
  }

  addSection(group: ClassGroupDto) {
    const section = this.newSectionName();
    const code = this.newSectionCode().trim();
    if (!section || !code) return;

    this.actionError.set(null);
    this.addingSection.set(true);
    this.classesService.addSection({
      className: group.className,
      academicYear: group.academicYear,
      section,
      classCode: code
    }).subscribe({
      next: () => {
        this.sectionFormOpenFor.set(null);
        this.addingSection.set(false);
        this.loadAll();
      },
      error: (err) => {
        this.actionError.set(err?.error ?? `Could not add section to '${group.className}'.`);
        this.addingSection.set(false);
      }
    });
  }

  openCodeForm(classId: number, currentCode: string) {
    this.actionError.set(null);
    this.codeFormOpenFor.set(classId);
    this.editCodeInput.set(currentCode);
  }

  cancelCodeForm() {
    this.codeFormOpenFor.set(null);
  }

  saveCode(classId: number) {
    const code = this.editCodeInput().trim();
    if (!code) return;

    this.actionError.set(null);
    this.savingCode.set(classId);
    this.classesService.updateClassCode(classId, { classCode: code }).subscribe({
      next: () => {
        this.savingCode.set(null);
        this.codeFormOpenFor.set(null);
        this.loadAll();
      },
      error: (err) => {
        this.savingCode.set(null);
        this.actionError.set(err?.error ?? 'Could not save the roll number code.');
      }
    });
  }

  deleteSection(classId: number, label: string) {
    if (!confirm(`Delete ${label}? This can't be undone.`)) return;

    this.actionError.set(null);
    this.deletingId.set(classId);
    this.classesService.deleteSection(classId).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.loadAll();
      },
      error: (err) => {
        this.actionError.set(err?.error ?? `Could not delete ${label}.`);
        this.deletingId.set(null);
      }
    });
  }

  deleteClassGroup(group: ClassGroupDto) {
    if (!confirm(`Delete '${group.className}' and all its sections? This can't be undone.`)) return;

    this.actionError.set(null);
    this.deletingGroup.set(group.className);
    this.classesService.deleteClassGroup(group.className, group.academicYear).subscribe({
      next: () => {
        this.deletingGroup.set(null);
        this.loadAll();
      },
      error: (err) => {
        this.actionError.set(err?.error ?? `Could not delete '${group.className}'.`);
        this.deletingGroup.set(null);
      }
    });
  }

  openMoveForm(classId: number) {
    this.actionError.set(null);
    this.moveFormOpenFor.set(classId);
    this.moveTargetId.set(null);
  }

  cancelMoveForm() {
    this.moveFormOpenFor.set(null);
    this.moveTargetId.set(null);
  }

  moveStudents(fromClassId: number) {
    const toClassId = this.moveTargetId();
    if (!toClassId) return;

    this.actionError.set(null);
    this.movingStudents.set(fromClassId);
    this.classesService.moveStudents(fromClassId, toClassId).subscribe({
      next: () => {
        this.movingStudents.set(null);
        this.moveFormOpenFor.set(null);
        this.moveTargetId.set(null);
        this.loadAll();
      },
      error: (err) => {
        this.actionError.set(err?.error ?? 'Could not move students.');
        this.movingStudents.set(null);
      }
    });
  }

  addLabel() {
    const name = this.newLabelName().trim();
    if (!name) return;

    this.actionError.set(null);
    this.addingLabel.set(true);
    this.classesService.addSectionOption({ name }).subscribe({
      next: (option) => {
        this.sectionOptions.update(list => [...list, option].sort((a, b) => a.name.localeCompare(b.name)));
        this.newLabelName.set('');
        this.addingLabel.set(false);
      },
      error: (err) => {
        this.actionError.set(err?.error ?? `Could not add '${name}' to the section list.`);
        this.addingLabel.set(false);
      }
    });
  }

  deleteLabel(option: SectionOptionDto) {
    if (!confirm(`Remove '${option.name}' from the section list? Existing sections named '${option.name}' are unaffected.`)) return;

    this.classesService.deleteSectionOption(option.sectionOptionId).subscribe({
      next: () => this.sectionOptions.update(list => list.filter(o => o.sectionOptionId !== option.sectionOptionId)),
      error: (err: HttpErrorResponse) =>
        this.actionError.set(err?.error ?? `Could not remove '${option.name}'.`)
    });
  }
}