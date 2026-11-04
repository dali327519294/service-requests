import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { provideNativeDateAdapter } from '@angular/material/core';
import { RequestQuery } from '../../../core/api/models';
import { RequestFilters } from './request-filters.component';

describe('RequestFilters', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: [provideNativeDateAdapter()] });
  });

  afterEach(() => vi.useRealTimers());

  function render(query: RequestQuery = {}) {
    const fixture = TestBed.createComponent(RequestFilters);
    fixture.componentRef.setInput('query', query);
    fixture.detectChanges();
    const emitted: Partial<RequestQuery>[] = [];
    fixture.componentInstance.changed.subscribe((c) => emitted.push(c));
    return { fixture, emitted, el: fixture.nativeElement as HTMLElement };
  }

  const type = (input: HTMLInputElement, text: string) => {
    input.value = text;
    input.dispatchEvent(new Event('input'));
  };

  it('sends the search once, 300ms after typing stops', () => {
    const { el, emitted } = render();
    const search = el.querySelector<HTMLInputElement>('input[formControlName=search]')!;

    type(search, 'א');
    vi.advanceTimersByTime(100);
    type(search, 'אס');
    vi.advanceTimersByTime(100);
    type(search, 'אסב');
    vi.advanceTimersByTime(299);
    expect(emitted).toEqual([]);

    vi.advanceTimersByTime(1);
    expect(emitted).toEqual([{ search: 'אסב' }]);
  });

  it('shows a query from the URL without sending it back', () => {
    const { fixture, el, emitted } = render();

    fixture.componentRef.setInput('query', { search: 'רעש', priority: ['High'] });
    fixture.detectChanges();
    vi.advanceTimersByTime(1000);

    expect(el.querySelector<HTMLInputElement>('input[formControlName=search]')!.value).toBe('רעש');
    expect(emitted).toEqual([]);
  });

  it('counts the filters set inside the panel on its button', () => {
    const { el } = render({ priority: ['High'], assignedTo: 'dana.levi', search: 'x' });

    expect(el.querySelector('.filters__count')?.textContent).toBe('(2)');
  });

  it('sends a priority at once when it is chosen', () => {
    const { fixture, el, emitted } = render();
    el.querySelector<HTMLButtonElement>('[aria-controls=more-filters]')!.click();
    fixture.detectChanges();

    el.querySelectorAll<HTMLButtonElement>('mat-button-toggle button')[2].click();

    expect(emitted).toEqual([{ priority: ['High'] }]);
  });

  it('includes the whole last day the user picked', () => {
    const { fixture, emitted } = render();
    const filters = fixture.componentInstance as unknown as {
      form: FormGroup;
      datesChanged(): void;
    };

    filters.form.patchValue({ from: new Date(2026, 9, 1), to: new Date(2026, 9, 31) });
    filters.datesChanged();

    expect(emitted.at(-1)).toEqual({
      createdFrom: new Date(2026, 9, 1),
      createdTo: new Date(2026, 10, 1),
    });
  });
});
