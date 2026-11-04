import { TestBed } from '@angular/core/testing';
import { MatPaginator, MatPaginatorIntl } from '@angular/material/paginator';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { PagedResult, RequestListItem, RequestQuery } from '../../../core/api/models';
import { HebrewPaginatorIntl } from '../../../core/i18n/paginator-intl.he';
import { RequestsTable } from './requests-table.component';

const item = (id: number, patch: Partial<RequestListItem> = {}): RequestListItem => ({
  id,
  title: `פנייה ${id}`,
  organizationName: 'עיריית חיפה',
  status: 'Waiting',
  priority: 'High',
  assignedTo: 'דנה',
  createdAt: '2026-10-01T08:30:00Z',
  updatedAt: '2026-10-02T09:00:00Z',
  rowVersion: 'AAAAAAAAB9E=',
  ...patch,
});

describe('RequestsTable', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: MatPaginatorIntl, useClass: HebrewPaginatorIntl }],
    });
  });

  function render(items: RequestListItem[], query: RequestQuery = {}) {
    const fixture = TestBed.createComponent(RequestsTable);
    const result: PagedResult<RequestListItem> = {
      items,
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 20,
      totalCount: 120,
      totalPages: 6,
    };
    fixture.componentRef.setInput('result', result);
    fixture.componentRef.setInput('query', query);
    fixture.detectChanges();
    const sorted: unknown[] = [];
    const paged: unknown[] = [];
    fixture.componentInstance.sortChanged.subscribe((s) => sorted.push(s));
    fixture.componentInstance.pageChanged.subscribe((p) => paged.push(p));
    const el = fixture.nativeElement as HTMLElement;
    const header = (text: string) =>
      [...el.querySelectorAll<HTMLElement>('th')].find((th) => th.textContent?.trim() === text)!;
    const paginator = () =>
      fixture.debugElement.query(By.directive(MatPaginator)).componentInstance as MatPaginator;
    return { fixture, el, header, paginator, sorted, paged };
  }

  it('shows a row per request, with its title linking to the request', () => {
    const { el } = render([item(42), item(43)]);

    const links = [...el.querySelectorAll<HTMLAnchorElement>('td a')];
    expect(el.querySelectorAll('tr[mat-row]').length).toBe(2);
    expect(links[0].textContent?.trim()).toBe('פנייה 42');
    expect(links[0].getAttribute('href')).toBe('/requests/42');
  });

  it('marks the sorted column for screen readers and reports a new sort', () => {
    const { header, sorted } = render([item(1)]);

    expect(header('נוצרה').getAttribute('aria-sort')).toBe('descending');

    header('עדיפות').click();

    expect(sorted).toEqual([{ sortBy: 'Priority', sortDirection: 'Asc' }]);
  });

  it('reports the next page', () => {
    const { el, paged } = render([item(1)]);

    el.querySelector<HTMLButtonElement>('button[aria-label="העמוד הבא"]')!.click();

    expect(paged).toEqual([{ page: 2, pageSize: 20 }]);
  });

  it('starts over at the first page when the page size changes', () => {
    // From page 6 (requests 101–120) Material alone would land on page 3; the table must start over
    const { paginator, paged } = render([item(1)], { page: 6 });

    paginator()._changePageSize(50);

    expect(paged).toEqual([{ page: 1, pageSize: 50 }]);
  });

  it('shows every status and priority word in full, and marks a request nobody took', () => {
    const { el } = render([item(1, { assignedTo: null })]);

    const chips = [...el.querySelectorAll('.chip')].map((c) => c.textContent?.trim());
    expect(chips).toEqual(['pause_circleממתינה', 'arrow_upwardגבוהה']);
    expect(el.textContent).toContain('לא שויך');
  });

  it('keeps each date in reading order inside the Hebrew row', () => {
    const { el } = render([item(1)]);

    // Without it, RTL puts the time before the date ("08:30 01/10/26")
    expect(el.querySelectorAll('td span[dir=ltr]').length).toBe(2);
  });
});
