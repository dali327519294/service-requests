/** The values the API sends; they match the server enums by name. */
export const REQUEST_STATUSES = ['New', 'InProgress', 'Waiting', 'Completed'] as const;
export type RequestStatus = (typeof REQUEST_STATUSES)[number];

export const REQUEST_PRIORITIES = ['Low', 'Medium', 'High'] as const;
export type RequestPriority = (typeof REQUEST_PRIORITIES)[number];
