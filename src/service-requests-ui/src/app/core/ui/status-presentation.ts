import { RequestPriority, RequestStatus } from '../api/request-enums';

/** How statuses and priorities look everywhere: one icon each, and the token name of their colors. */
export const statusIcon: Record<RequestStatus, string> = {
  New: 'add_circle',
  InProgress: 'autorenew',
  Waiting: 'pause_circle',
  Completed: 'check_circle',
};

/** Matches the --app-status-<token>-bg / -fg variables in styles.scss. */
export const statusToken: Record<RequestStatus, string> = {
  New: 'new',
  InProgress: 'in-progress',
  Waiting: 'waiting',
  Completed: 'completed',
};

export const priorityIcon: Record<RequestPriority, string> = {
  Low: 'arrow_downward',
  Medium: 'remove',
  High: 'arrow_upward',
};
