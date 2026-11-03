import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';

const numbers = new Intl.NumberFormat('he-IL');

/** Hebrew texts for the table paginator, including what screen readers announce. */
@Injectable()
export class HebrewPaginatorIntl extends MatPaginatorIntl {
  override itemsPerPageLabel = 'פניות בעמוד';
  override nextPageLabel = 'העמוד הבא';
  override previousPageLabel = 'העמוד הקודם';
  override firstPageLabel = 'העמוד הראשון';
  override lastPageLabel = 'העמוד האחרון';

  override getRangeLabel = (page: number, pageSize: number, length: number): string => {
    if (length === 0) {
      return 'אין פניות';
    }

    const start = page * pageSize + 1;
    const end = Math.min((page + 1) * pageSize, length);
    return `${numbers.format(start)}–${numbers.format(end)} מתוך ${numbers.format(length)}`;
  };
}
