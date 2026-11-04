import { TestBed } from '@angular/core/testing';
import { RequestQuery } from '../../../core/api/models';
import { ActiveFilterChips } from './active-filter-chips.component';

describe('ActiveFilterChips', () => {
  function render(query: RequestQuery) {
    const fixture = TestBed.createComponent(ActiveFilterChips);
    fixture.componentRef.setInput('query', query);
    fixture.detectChanges();
    const removed: Partial<RequestQuery>[] = [];
    let cleared = 0;
    fixture.componentInstance.removed.subscribe((p) => removed.push(p));
    fixture.componentInstance.cleared.subscribe(() => cleared++);
    const el = fixture.nativeElement as HTMLElement;
    const chips = () =>
      [...el.querySelectorAll('mat-chip')].map((c) => c.textContent!.replace('close', '').trim());
    const remove = (i: number) =>
      el.querySelectorAll<HTMLButtonElement>('[matChipRemove]')[i].click();
    return { el, chips, remove, removed, cleared: () => cleared };
  }

  it('shows one chip per active filter, with the whole last day of the range', () => {
    const { chips } = render({
      search: 'אסבסט',
      status: ['New', 'Waiting'],
      priority: ['High'],
      createdFrom: new Date(2026, 9, 1),
      createdTo: new Date(2026, 10, 1),
    });

    expect(chips()).toEqual([
      'חיפוש: אסבסט',
      'סטטוס: חדשה',
      'סטטוס: ממתינה',
      'עדיפות: גבוהה',
      // The range sits inside left-to-right isolate marks, so it reads 01/10 → 31/10 in Hebrew text
      'נוצרה: ⁦01/10/26–31/10/26⁩',
    ]);
  });

  it('removes only the chosen status', () => {
    const { remove, removed } = render({ status: ['New', 'Waiting'] });

    remove(0);

    expect(removed).toEqual([{ status: ['Waiting'] }]);
  });

  it('removes both ends of the date range together', () => {
    const { remove, removed } = render({
      createdFrom: new Date(2026, 9, 1),
      createdTo: new Date(2026, 10, 1),
    });

    remove(0);

    expect(removed).toEqual([{ createdFrom: undefined, createdTo: undefined }]);
  });

  it('clears everything at once', () => {
    const { el, cleared } = render({ search: 'x', priority: ['Low'] });

    [...el.querySelectorAll('button')].find((b) => b.textContent?.includes('ניקוי הכל'))!.click();

    expect(cleared()).toBe(1);
  });

  it('shows nothing when no filter is set', () => {
    const { el } = render({ sortBy: 'Priority', page: 3 });

    expect(el.querySelector('.active')).toBeNull();
  });
});
