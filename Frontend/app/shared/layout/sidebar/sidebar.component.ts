import { Component, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { AuthService } from '../../../core/services/auth.service';

interface NavItem {
  label: string;
  path: string;
  icon: string;
  roles: string[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgIcon],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  private allItems: NavItem[] = [
    { label: 'Dashboard',  path: '/dashboard',  icon: 'tablerLayoutDashboard', roles: ['Admin', 'Accountant', 'Teacher'] },
    { label: 'Students',   path: '/students',   icon: 'tablerUsers',           roles: ['Admin', 'Accountant', 'Teacher'] },
    { label: 'Fees',       path: '/fees',        icon: 'tablerReceipt',        roles: ['Admin', 'Accountant'] },
    { label: 'Academics',  path: '/academics',   icon: 'tablerBook',           roles: ['Admin', 'Teacher'] },
    { label: 'Attendance', path: '/attendance',  icon: 'tablerCalendarCheck',  roles: ['Admin', 'Teacher'] },
    { label: 'Admin',      path: '/admin',       icon: 'tablerSettings',       roles: ['Admin'] },
  ];

  constructor(private auth: AuthService) {}

  visibleItems = computed(() => {
    const role = this.auth.role();
    if (!role) return [];
    return this.allItems.filter(item => item.roles.includes(role));
  });

  schoolName = 'Bright Grammar School';
  campusName = 'Main Campus';
  developedBy = 'Developed by Rana Abdullah';
}