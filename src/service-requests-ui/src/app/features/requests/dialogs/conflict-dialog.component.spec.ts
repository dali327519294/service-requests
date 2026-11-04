import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { RequestDetails } from '../../../core/api/models';
import { ConflictChoice, ConflictData, ConflictDialog } from './conflict-dialog.component';

const current = (allowedTransitions: RequestDetails['allowedTransitions']): RequestDetails => ({
  id: 42,
  title: 'רעש מגנרטור בלילה',
  organizationName: 'עיריית חיפה',
  status: 'InProgress',
  priority: 'High',
  assignedTo: null,
  createdAt: '2026-10-01T08:30:00Z',
  updatedAt: '2026-10-04T09:00:00Z',
  rowVersion: 'v2',
  allowedTransitions,
});

describe('ConflictDialog', () => {
  // A closing dialog waits for its exit animation, which never ends in jsdom
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    }),
  );

  async function open(data: ConflictData) {
    const ref = TestBed.inject(MatDialog).open<ConflictDialog, ConflictData, ConflictChoice>(
      ConflictDialog,
      { data },
    );
    let choice: ConflictChoice | undefined;
    ref.afterClosed().subscribe((c) => (choice = c));
    TestBed.tick();
    await new Promise((r) => setTimeout(r)); // the dialog renders into the overlay asynchronously
    const pane = document.querySelector<HTMLElement>('.cdk-overlay-pane')!;
    const button = (text: string) =>
      [...pane.querySelectorAll<HTMLButtonElement>('button')].find((b) =>
        b.textContent?.includes(text),
      );
    return { pane, button, choice: () => choice, ref };
  }

  afterEach(() => TestBed.inject(MatDialog).closeAll());

  it('offers to try again while the move is still allowed, and says so on the way out', async () => {
    const { button, choice } = await open({
      attempted: 'Waiting',
      current: current(['Waiting', 'Completed']),
    });

    button('לשנות בכל זאת ל: ממתינה')!.click();
    await new Promise((r) => setTimeout(r));

    expect(choice()).toBe('retry');
  });

  it('explains instead of offering a move the new status no longer allows', async () => {
    const { pane, button } = await open({ attempted: 'New', current: current(['Waiting']) });

    expect(button('לשנות בכל זאת')).toBeUndefined();
    expect(pane.textContent).toContain('המעבר ל"חדשה" כבר לא אפשרי מהסטטוס הנוכחי.');
  });
});
