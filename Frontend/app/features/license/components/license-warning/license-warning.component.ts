import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-license-warning',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './license-warning.component.html',
  styleUrl: './license-warning.component.scss'
})
export class LicenseWarningComponent {
  @Input() daysRemaining = 0;
  @Input() message = '';
}
