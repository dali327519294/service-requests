import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [App], providers: [provideRouter(routes)] });
  });

  it('shows the app title in Hebrew', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.brand__title')?.textContent).toBe('ניהול פניות');
  });

  it('offers a skip link to the main content', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('a.skip-link')?.getAttribute('href')).toBe('#main');
    expect(element.querySelector('main#main')).not.toBeNull();
  });

  it('moves focus to the main content without leaving the page', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/requests?status=New');
    await fixture.whenStable();
    const link = fixture.nativeElement.querySelector('a.skip-link') as HTMLAnchorElement;
    const click = new MouseEvent('click', { cancelable: true });

    link.dispatchEvent(click);

    expect(click.defaultPrevented).toBe(true); // the browser does not follow "/#main"
    expect(document.activeElement).toBe(fixture.nativeElement.querySelector('main'));
    expect(TestBed.inject(Location).path()).toBe('/requests?status=New'); // filters survive
  });

  it('opens the requests page with a Hebrew tab title', async () => {
    const harness = await RouterTestingHarness.create('/');

    expect(TestBed.inject(Location).path()).toBe('/requests');
    expect(document.title).toBe('פניות | ניהול פניות');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('פניות');
  });
});
