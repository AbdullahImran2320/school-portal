import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AdminNavComponent } from '../../../shared/components/admin-nav/admin-nav.component';
import { AuthService } from '../../../core/services/auth.service';
import { AdminService } from '../services/admin.service';
import { AppRole, ASSIGNABLE_ROLES, UserSummaryDto } from '../models/admin.models';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminNavComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  private adminService = inject(AdminService);
  private authService = inject(AuthService);

  assignableRoles = ASSIGNABLE_ROLES;

  users = signal<UserSummaryDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  actionError = signal<string | null>(null);

  editingUserId = signal<number | null>(null);
  editRole = signal<AppRole>('Pending');
  savingId = signal<number | null>(null);

  currentUsername = computed(() => this.authService.currentUser()?.username ?? null);

  // Pending signups surface first — they're the ones actually blocked from using the app.
  sortedUsers = computed(() => {
    const list = this.users();
    return [...list].sort((a, b) => {
      if (a.role === 'Pending' && b.role !== 'Pending') return -1;
      if (b.role === 'Pending' && a.role !== 'Pending') return 1;
      return a.fullName.localeCompare(b.fullName);
    });
  });

  pendingCount = computed(() => this.users().filter(u => u.role === 'Pending').length);

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.loading.set(true);
    this.error.set(null);
    this.adminService.getUsers().subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load users.');
        this.loading.set(false);
      }
    });
  }

  isSelf(user: UserSummaryDto): boolean {
    return this.currentUsername() !== null && user.username === this.currentUsername();
  }

  startEdit(user: UserSummaryDto) {
    if (this.isSelf(user)) return; // guarded in the template too; belt and braces
    this.editingUserId.set(user.userId);
    this.editRole.set(user.role);
  }

  cancelEdit() {
    this.editingUserId.set(null);
  }

  saveRole(user: UserSummaryDto) {
    if (this.isSelf(user)) return;

    const newRole = this.editRole();
    this.actionError.set(null);
    this.savingId.set(user.userId);
    this.adminService.updateUserRole(user.userId, newRole).subscribe({
      next: () => {
        this.users.update(list => list.map(u => u.userId === user.userId ? { ...u, role: newRole } : u));
        this.editingUserId.set(null);
        this.savingId.set(null);
      },
      error: () => {
        this.actionError.set(`Could not update the role for ${user.fullName}.`);
        this.savingId.set(null);
      }
    });
  }
}
