import { convertToParamMap } from '@angular/router';
import { RequestQuery } from '../../../core/api/models';
import { toParams, toQuery } from './query-params';

const read = (params: Record<string, string | string[]>) => toQuery(convertToParamMap(params));

describe('query params', () => {
  it('survives a round trip through the URL', () => {
    const query: RequestQuery = {
      search: 'אסבסט',
      status: ['New', 'Waiting'],
      priority: ['High'],
      organizationName: 'מפעל',
      assignedTo: 'dana.levi',
      createdFrom: new Date(2026, 9, 1),
      createdTo: new Date(2026, 10, 1),
      sortBy: 'Priority',
      sortDirection: 'Asc',
      page: 3,
      pageSize: 50,
    };

    expect(toQuery(convertToParamMap(toParams(query)))).toEqual(query);
  });

  it('keeps defaults out of the URL', () => {
    expect(toParams(read({}))).toEqual({});
  });

  it('falls back to defaults for invalid values', () => {
    const query = read({
      pageSize: '999',
      page: '-2',
      sortBy: 'Password',
      status: ['New', 'Bogus'],
      createdFrom: '2026-02-31',
    });

    expect(query.pageSize).toBe(20);
    expect(query.page).toBe(1);
    expect(query.sortBy).toBe('CreatedAt');
    expect(query.status).toEqual(['New']);
    expect(query.createdFrom).toBeUndefined();
  });

  it('includes the whole last day of the range', () => {
    expect(read({ createdTo: '2026-10-31' }).createdTo).toEqual(new Date(2026, 10, 1));
  });
});
