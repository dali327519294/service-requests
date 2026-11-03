import { HebrewPaginatorIntl } from './paginator-intl.he';

describe('HebrewPaginatorIntl', () => {
  const intl = new HebrewPaginatorIntl();

  it('shows the range of the first page with thousands separators', () => {
    expect(intl.getRangeLabel(0, 20, 10012)).toBe('1–20 מתוך 10,012');
  });

  it('ends the last page at the total', () => {
    expect(intl.getRangeLabel(500, 20, 10012)).toBe('10,001–10,012 מתוך 10,012');
  });

  it('says there are no requests when the list is empty', () => {
    expect(intl.getRangeLabel(0, 20, 0)).toBe('אין פניות');
  });
});
