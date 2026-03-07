class IntersectionObserver {
  constructor(_callback: unknown) {
    this.root = null;
    this.rootMargin = "";
    this.thresholds = [];
  }
  disconnect() {
    return null;
  }
  observe(_target: Element): void {
    // test
  }
  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }
  unobserve(_target: Element): void {
    // test
  }
  readonly root: Element | Document | null;
  readonly rootMargin: string;
  readonly thresholds: readonly number[];
}

global.IntersectionObserver = IntersectionObserver;
