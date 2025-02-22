import { beforeAll, vi } from "vitest";

beforeAll(() => {
    vi.mock('next/navigation', () => ({
        useRouter: () => ({
          push: vi.fn(),
          replace: vi.fn(),
          pathname: '/',
          query: {},
          asPath: '/',
          route: '/',
        }),
      }));
});

class IntersectionObserver {
    constructor(callback: any) {
      this.root = null;
      this.rootMargin = "";
      this.thresholds = [];
    }
    disconnect() {
      return null;
    }
    observe(target: Element): void {
      // test
    }
    takeRecords(): IntersectionObserverEntry[] {
      return [];
    }
    unobserve(target: Element): void {
      // test
    }
    readonly root: Element | Document | null;
    readonly rootMargin: string;
    readonly thresholds: readonly number[];
  }
  
  global.IntersectionObserver = IntersectionObserver;