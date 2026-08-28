import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { licenseGuard } from './core/guards/license.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: '',
    loadComponent: () =>
      import('./shared/layout/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent), canActivate: [licenseGuard] },
      {
  path: 'students',
  loadComponent: () => import('./features/students/student-list/student-list.component').then(m => m.StudentListComponent),
  canActivate: [roleGuard(['Admin', 'Accountant', 'Teacher']), licenseGuard]
},
{
  path: 'students/new',
  loadComponent: () => import('./features/students/student-form/student-form.component').then(m => m.StudentFormComponent),
  canActivate: [roleGuard(['Admin']), licenseGuard]
},
{
  path: 'students/:id/edit',
  loadComponent: () => import('./features/students/student-form/student-form.component').then(m => m.StudentFormComponent),
  canActivate: [roleGuard(['Admin']), licenseGuard]
},
     {
  path: 'fees',
  loadComponent: () => import('./features/fees/fee-grid/fee-grid.component').then(m => m.FeeGridComponent),
  canActivate: [roleGuard(['Admin', 'Accountant']), licenseGuard]
},
{
  path: 'fees/defaulters',
  loadComponent: () => import('./features/fees/defaulters/defaulters.component').then(m => m.DefaultersComponent),
  canActivate: [roleGuard(['Admin', 'Accountant']), licenseGuard]
},
{
  path: 'fees/components',
  loadComponent: () => import('./features/fees/fee-components/fee-components.component').then(m => m.FeeComponentsComponent),
  canActivate: [roleGuard(['Admin', 'Accountant']), licenseGuard]
},
{
  path: 'fees/vouchers',
  loadComponent: () => import('./features/fees/vouchers/vouchers.component').then(m => m.VouchersComponent),
  canActivate: [roleGuard(['Admin', 'Accountant']), licenseGuard]
},
{
  path: 'fees/charges/:studentId',
  loadComponent: () => import('./features/fees/student-charges/student-charges.component').then(m => m.StudentChargesComponent),
  canActivate: [roleGuard(['Admin', 'Accountant']), licenseGuard]
},
{
  path: 'attendance',
  loadComponent: () => import('./features/attendance/mark-attendance/mark-attendance.component').then(m => m.MarkAttendanceComponent),
  canActivate: [roleGuard(['Admin', 'Teacher']), licenseGuard]
},
{
  path: 'attendance/report',
  loadComponent: () => import('./features/attendance/student-report/student-report.component').then(m => m.StudentReportComponent),
  canActivate: [roleGuard(['Admin', 'Teacher']), licenseGuard]
},
{
  path: 'academics/subjects',
  loadComponent: () => import('./features/academics/subjects/subjects.component').then(m => m.SubjectsComponent),
  canActivate: [roleGuard(['Admin', 'Teacher']), licenseGuard]
},
{
  path: 'academics/exams',
  loadComponent: () => import('./features/academics/exams/exams.component').then(m => m.ExamsComponent),
  canActivate: [roleGuard(['Admin', 'Teacher']), licenseGuard]
},
{
  path: 'academics/results',
  loadComponent: () => import('./features/academics/record-results/record-results.component').then(m => m.RecordResultsComponent),
  canActivate: [roleGuard(['Admin', 'Teacher']), licenseGuard]
},
{
  path: 'academics/report-card',
  loadComponent: () => import('./features/academics/report-card/report-card.component').then(m => m.ReportCardComponent),
  canActivate: [roleGuard(['Admin', 'Teacher']), licenseGuard]
},
{
  path: 'academics',
  redirectTo: 'academics/subjects',
  pathMatch: 'full'
},
{
  path: 'admin/users',
  loadComponent: () => import('./features/admin/users/users.component').then(m => m.UsersComponent),
  canActivate: [roleGuard(['Admin']), licenseGuard]
},
{
  path: 'admin/audit-log',
  loadComponent: () => import('./features/admin/audit-log/audit-log.component').then(m => m.AuditLogComponent),
  canActivate: [roleGuard(['Admin']), licenseGuard]
},
{
  path: 'admin',
  redirectTo: 'admin/users',
  pathMatch: 'full'
},
      {
        path: 'license',
        loadComponent: () => import('./features/license/pages/license-page/license-page.component').then(m => m.LicensePageComponent)
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  {
    path: 'license/expired',
    loadComponent: () => import('./features/license/pages/license-expired/license-expired.component').then(m => m.LicenseExpiredComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '/login' }
];