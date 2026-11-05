import { TestBed } from '@angular/core/testing';
import { RequestStatus, RequestSummary } from '../../../core/api/models';
import { SummaryCards } from './summary-cards.component';

const summary: RequestSummary = {
  total: 12432,
  byStatus: { New: 1254, InProgress: 4920, Waiting: 1870, Completed: 4388 },
  byPriority: { Low: 0, Medium: 0, High: 0 },
};

function render(inputs: {
  summary?: RequestSummary | null;
  selected?: RequestStatus[];
  failed?: boolean;
}) {
  const fixture = TestBed.createComponent(SummaryCards);
  fixture.componentRef.setInput('summary', inputs.summary ?? null);
  fixture.componentRef.setInput('selected', inputs.selected);
  fixture.componentRef.setInput('failed', inputs.failed ?? false);
  fixture.detectChanges();
  const cards = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')];
  return { fixture, cards };
}

describe('SummaryCards', () => {
  it('shows a dash, not a loading placeholder, when the counts could not be loaded', () => {
    const { fixture, cards } = render({ summary: null, failed: true });
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('.card__placeholder')).toBeNull();
    expect(cards.map((c) => c.querySelector('.card__count')?.textContent?.trim())).toEqual([
      '—',
      '—',
      '—',
      '—',
    ]);
    expect(el.querySelector('.cards')?.getAttribute('aria-busy')).toBe('false');
  });

  it('shows every status with its count', () => {
    const { cards } = render({ summary });

    expect(cards.map((c) => c.textContent?.replace(/\s+/g, ' ').trim())).toEqual([
      'add_circle חדשות 1,254',
      'autorenew בטיפול 4,920',
      'pause_circle ממתינות 1,870',
      'check_circle הושלמו 4,388',
    ]);
  });

  it('marks the selected statuses as pressed', () => {
    const { cards } = render({ summary, selected: ['Waiting'] });

    expect(cards.map((c) => c.getAttribute('aria-pressed'))).toEqual([
      'false',
      'false',
      'true',
      'false',
    ]);
  });

  it('tells the page which status was clicked', () => {
    const { fixture, cards } = render({ summary });
    const toggled: RequestStatus[] = [];
    fixture.componentInstance.statusToggled.subscribe((s) => toggled.push(s));

    cards[1].click();

    expect(toggled).toEqual(['InProgress']);
  });

  it('keeps the card size while the counts are loading', () => {
    const { fixture, cards } = render({ summary: null });
    const element = fixture.nativeElement as HTMLElement;

    expect(cards).toHaveLength(4);
    expect(element.querySelectorAll('.card__placeholder')).toHaveLength(4);
    expect(element.querySelector('.cards')?.getAttribute('aria-busy')).toBe('true');
  });
});
