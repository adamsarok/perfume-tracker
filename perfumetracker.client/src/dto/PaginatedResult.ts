export interface PaginatedResult<T> {
    items: T[];
    totalCount: number;
    pageSize: number;
    page: number;
}