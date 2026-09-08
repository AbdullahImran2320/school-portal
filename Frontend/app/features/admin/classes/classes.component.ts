import { Component, OnInit, inject, signal, computed } from '@angular/core';
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
  addingClass = signal(false);

  // Add-section form (per group, keyed by className so multiple rows can't clash)
  sectionFormOpenFor = signal<string | null>(null);
  newSectionName = signal<string>('');
  addingSection = signal(false);

  // Section-label list management
  newLabelName = signal('');
  addingLabel = signal(false);
  labelsPanelOpen = signal(false);

  deletingId = signal<number | null>(null);
  deletingGroup = signal<string | null>(null);

  // Labels not yet used by a given group — what the "add section" dropdown offers
  availableLabelsFor = computed(() => {
    return (group: ClassGroupDto) => {
      const used = new Set(group.sections.map(s => s.section).filter(Boolean));
      return this.sectionOptions().filter(o => !used.has(o.name));
    };
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
    if (!name) return;

    this.actionError.set(null);
    this.addingClass.set(true);
    this.classesService.createClass({ className: name, academicYear: this.academicYear() }).subscribe({
      next: () => {
        this.newClassName.set('');
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
  }

  cancelSectionForm() {
    this.sectionFormOpenFor.set(null);
  }

  addSection(group: ClassGroupDto) {
    const section = this.newSectionName();
    if (!section) return;

    this.actionError.set(null);
    this.addingSection.set(true);
    this.classesService.addSection({
      className: group.className,
      academicYear: group.academicYear,
      section
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
      error: (err) => this.actionError.set(err?.error ?? `Could not remove '${option.name}'.`)
    });
  }
}
