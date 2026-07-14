export const SearchOperators = {
  None: 0,
  Equal: 1,
  GreaterThan: 2,
  GreaterThanOrEqual: 3,
  LessThan: 4,
  LessThanOrEqual: 5,
  Contains: 6,
} as const;

export type SearchOperators = typeof SearchOperators[keyof typeof SearchOperators];

export interface SearchObject {
  sort?: number;
  label?: string;
  name?: string;
  type?: string;
  operator?: SearchOperators;
  text?: string;
  value?: any;
}

export interface SearchQueryParams {
  searchObjects: SearchObject[];
  page?: number;
  limit?: number;
}
