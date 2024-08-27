import { beforeAll, vi } from "vitest";

beforeAll(() => {
    // vi.mock('intersection-observer', () => ({
    //     observe: vi.fn(),
    //     unobserve: vi.fn(),
    //     disconnect: vi.fn(),
    //   }));
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
    constructor(callback: any) {}
    observe() {
      return null;
    }
    unobserve() {
      return null;
    }
    disconnect() {
      return null;
    }
  }
  
  global.IntersectionObserver = IntersectionObserver;